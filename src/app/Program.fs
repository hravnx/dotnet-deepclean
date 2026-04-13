open System.Diagnostics
open System.IO

let private dotnetProjectExtensions = set [ ".csproj"; ".fsproj"; ".vbproj" ]
let private solutionFileExtensions = set [ ".sln"; ".slnx" ]
let private buildArtifactDirectories = [ "bin"; "obj" ]

let findProjectDirectories (rootPath: string) : seq<string> = seq {
    for directory in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories) do
        let containsProjectFile =
            Directory.EnumerateFiles(directory)
            |> Seq.exists (fun filePath ->
                let extension = Path.GetExtension(filePath)
                dotnetProjectExtensions.Contains(extension)
            )

        if containsProjectFile then
            yield directory
}

let deepClean (projectPath: string) : int =
    buildArtifactDirectories
    |> List.sumBy (fun directoryName ->
        let directoryPath = Path.Combine(projectPath, directoryName)

        if Directory.Exists(directoryPath) then
            Directory.Delete(directoryPath, true)
            1
        else
            0
    )

let findSolutionFiles (rootPath: string) : seq<string> = seq {
    for filePath in Directory.EnumerateFiles(rootPath) do
        let extension = Path.GetExtension(filePath)

        if solutionFileExtensions.Contains(extension) then
            yield filePath
}

let restoreSolution (solutionPath: string) : int =
    printfn "Running dotnet restore for %s" (Path.GetFileName(solutionPath))

    use restoreProcess =
        new Process(
            StartInfo =
                ProcessStartInfo(
                    FileName = "dotnet",
                    Arguments = $"restore \"{solutionPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(solutionPath),
                    UseShellExecute = false
                )
        )

    restoreProcess.Start() |> ignore
    restoreProcess.WaitForExit()
    restoreProcess.ExitCode

let restoreSolutions (rootPath: string) : int =
    findSolutionFiles rootPath
    |> Seq.map restoreSolution
    |> Seq.tryFind ((<>) 0)
    |> Option.defaultValue 0

[<EntryPoint>]
let main _ =
    let currentDirectory = Directory.GetCurrentDirectory()

    let removedFolderCount =
        findProjectDirectories currentDirectory
        |> Seq.sumBy deepClean

    printfn "Removed %d folders." removedFolderCount

    if removedFolderCount > 0 then
        restoreSolutions currentDirectory
    else
        0
