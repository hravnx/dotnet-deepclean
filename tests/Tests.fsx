#load "../src/app/Cleanup.fs"
#load "../src/app/Restore.fs"
#load "../src/app/DeepClean.fs"

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks

let equal expected actual =
    if expected <> actual then
        failwithf "Expected %A, got %A" expected actual

let withTemp action =
    let root = Path.Combine(Path.GetTempPath(), "deepclean-tests-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(root) |> ignore
    try action root
    finally Directory.Delete(root, true)

let directory path = Directory.CreateDirectory(path) |> ignore
let file path = File.WriteAllText(path, "fixture")

let project root extension =
    directory (Path.Combine(root, "bin", "Debug"))
    directory (Path.Combine(root, "obj"))
    file (Path.Combine(root, "Project" + extension))

let options root skip : DeepClean.RunOptions = {
    CurrentDirectory = root
    SkipRestore = skip
}

let tests : (string * (unit -> unit)) list = [
    "cleans current and nested projects, preserving unrelated folders", fun () -> withTemp (fun root ->
        project root ".csproj"
        project (Path.Combine(root, "src", "FSharp")) ".fsproj"
        project (Path.Combine(root, "src", "VisualBasic")) ".vbproj"
        let unrelated = Path.Combine(root, "unrelated", "bin")
        directory unrelated
        let source = Path.Combine(root, "Program.cs")
        file source
        equal 6 (Cleanup.clean root)
        equal false (Directory.Exists(Path.Combine(root, "bin")))
        equal true (Directory.Exists unrelated)
        equal true (File.Exists source)
        equal 0 (Cleanup.clean root)
    )

    "does not descend into artifacts being removed", fun () -> withTemp (fun root ->
        project root ".csproj"
        project (Path.Combine(root, "obj", "generated")) ".csproj"
        equal 2 (Cleanup.clean root)
        equal false (Directory.Exists(Path.Combine(root, "obj")))
    )

    "skips external links, cycles, linked artifacts and linked roots", fun () -> withTemp (fun root ->
        let external = Path.Combine(root, "external")
        let workspace = Path.Combine(root, "workspace")
        project external ".csproj"
        directory workspace
        file (Path.Combine(workspace, "Project.csproj"))
        Directory.CreateSymbolicLink(Path.Combine(workspace, "external"), external) |> ignore
        Directory.CreateSymbolicLink(Path.Combine(workspace, "cycle"), workspace) |> ignore
        Directory.CreateSymbolicLink(Path.Combine(workspace, "bin"), Path.Combine(external, "bin")) |> ignore
        equal 0 (Cleanup.clean workspace)
        equal 0 (Cleanup.clean (Path.Combine(workspace, "external")))
        equal true (Directory.Exists(Path.Combine(external, "bin", "Debug")))
        equal true (Directory.Exists(Path.Combine(external, "obj")))
    )

    "deleting an artifact preserves targets of links inside it", fun () -> withTemp (fun root ->
        let workspace = Path.Combine(root, "workspace")
        let external = Path.Combine(root, "external")
        project workspace ".csproj"
        directory external
        let sentinel = Path.Combine(external, "keep.txt")
        file sentinel
        Directory.CreateSymbolicLink(Path.Combine(workspace, "obj", "link"), external) |> ignore
        equal 2 (Cleanup.clean workspace)
        equal true (File.Exists sentinel)
    )

    "restores root solutions after cleanup only", fun () -> withTemp (fun root ->
        project root ".csproj"
        let first = Path.Combine(root, "A.sln")
        let second = Path.Combine(root, "B.slnx")
        file first
        file second
        file (Path.Combine(root, "ignore.txt"))
        directory (Path.Combine(root, "nested"))
        file (Path.Combine(root, "nested", "Nested.sln"))
        let calls = ResizeArray<string>()
        let restore (path: string) =
            equal false (Directory.Exists(Path.Combine(root, "obj")))
            calls.Add(path)
            0
        let result = DeepClean.run restore (options root false)
        equal 2 result.RemovedFolderCount
        equal 0 result.ExitCode
        equal [ first; second ] (List.ofSeq calls)
    )

    "skip restore still cleans", fun () -> withTemp (fun root ->
        project root ".csproj"
        file (Path.Combine(root, "A.sln"))
        let result = DeepClean.run (fun _ -> failwith "Unexpected restore") (options root true)
        equal 2 result.RemovedFolderCount
        equal 0 result.ExitCode
    )

    "no cleanup means no restore", fun () -> withTemp (fun root ->
        file (Path.Combine(root, "A.sln"))
        let result = DeepClean.run (fun _ -> failwith "Unexpected restore") (options root false)
        equal 0 result.RemovedFolderCount
        equal 0 result.ExitCode
    )

    "no solution means successful cleanup without restore", fun () -> withTemp (fun root ->
        project root ".csproj"
        let result = DeepClean.run (fun _ -> failwith "Unexpected restore") (options root false)
        equal 2 result.RemovedFolderCount
        equal 0 result.ExitCode
    )

    "first restore failure stops execution and propagates its exit code", fun () -> withTemp (fun root ->
        project root ".csproj"
        for name in [ "A.sln"; "B.slnx"; "C.sln" ] do
            file (Path.Combine(root, name))
        let calls = ResizeArray<string>()
        let restore (path: string) =
            calls.Add(Path.GetFileName(path))
            if calls.Count = 2 then 17 else 0
        let result = DeepClean.run restore (options root false)
        equal 17 result.ExitCode
        equal [ "A.sln"; "B.slnx" ] (List.ofSeq calls)
    )

    "process drains both streams and preserves arguments, directory and exit code", fun () -> withTemp (fun root ->
        let workingDirectory = Path.Combine(root, "with spaces")
        directory workingDirectory
        let argument = "argument with spaces and \"quotes\""
        let startInfo = ProcessStartInfo(FileName = "dotnet", WorkingDirectory = workingDirectory)
        for arg in [ "fsi"; "--exec"; Path.Combine(__SOURCE_DIRECTORY__, "ProcessFixture.fsx"); argument ] do
            startInfo.ArgumentList.Add(arg)
        let task = Task.Run(fun () -> Restore.execute startInfo)
        if not (task.Wait(TimeSpan.FromSeconds(30.0))) then
            eprintfn "Process output test timed out (possible pipe deadlock)."
            Environment.Exit(1)
        let result = task.Result
        equal 23 result.ExitCode
        equal (String('e', 1024 * 1024)) result.StandardError
        // GetFullPath does not resolve macOS's /var -> /private/var alias.
        equal true (result.StandardOutput.StartsWith(String('o', 1024 * 1024) + "|"))
        equal true (result.StandardOutput.EndsWith("with spaces|" + argument))
    )
]

let mutable failures = 0
for name, test in tests do
    try
        test ()
        printfn "PASS %s" name
    with ex ->
        failures <- failures + 1
        eprintfn "FAIL %s: %s" name ex.Message

printfn "%d tests, %d failures" tests.Length failures
if failures > 0 then Environment.Exit(1)
