module DeepClean

open System
open System.Diagnostics
open System.IO
open System.Reflection

let private dotnetProjectExtensions = set [ ".csproj"; ".fsproj"; ".vbproj" ]
let private solutionFileExtensions = set [ ".sln"; ".slnx" ]
let private buildArtifactDirectories = [ "bin"; "obj" ]

type RunOptions = {
    CurrentDirectory: string
    SkipRestore: bool
}

let private findProjectDirectories (rootPath: string) : seq<string> = seq {
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

let private deepClean (projectPath: string) : int =
    buildArtifactDirectories
    |> List.sumBy (fun directoryName ->
        let directoryPath = Path.Combine(projectPath, directoryName)

        if Directory.Exists(directoryPath) then
            Directory.Delete(directoryPath, true)
            1
        else
            0
    )

let private findSolutionFiles (rootPath: string) : seq<string> = seq {
    for filePath in Directory.EnumerateFiles(rootPath) do
        let extension = Path.GetExtension(filePath)

        if solutionFileExtensions.Contains(extension) then
            yield filePath
}

let private restoreSolution (solutionPath: string) : int =
    printfn "Running dotnet restore for %s" (Path.GetFileName(solutionPath))

    use restoreProcess =
        new Process(
            StartInfo =
                ProcessStartInfo(
                    FileName = "dotnet",
                    Arguments = $"restore \"{solutionPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(solutionPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                )
        )

    restoreProcess.Start() |> ignore
    let standardOutput = restoreProcess.StandardOutput.ReadToEnd()
    let standardError = restoreProcess.StandardError.ReadToEnd()
    restoreProcess.WaitForExit()

    if restoreProcess.ExitCode <> 0 then
        if not (String.IsNullOrWhiteSpace(standardOutput)) then
            printf "%s" standardOutput

        if not (String.IsNullOrWhiteSpace(standardError)) then
            eprintf "%s" standardError

    restoreProcess.ExitCode

let private restoreSolutions (rootPath: string) : int =
    findSolutionFiles rootPath
    |> Seq.map restoreSolution
    |> Seq.tryFind ((<>) 0)
    |> Option.defaultValue 0

let getToolVersion () : string =
    let entryAssembly = Assembly.GetEntryAssembly()

    let informationalVersion =
        entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()

    if isNull informationalVersion then
        entryAssembly.GetName().Version.ToString()
    else
        informationalVersion.InformationalVersion.Split('+')[0]

let run (options: RunOptions) : int =
    let removedFolderCount =
        findProjectDirectories options.CurrentDirectory |> Seq.sumBy deepClean

    printfn "Removed %d folders." removedFolderCount

    if removedFolderCount > 0 && not options.SkipRestore then
        restoreSolutions options.CurrentDirectory
    else
        0
