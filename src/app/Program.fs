open Argu
open System
open System.Diagnostics
open System.IO
open System.Reflection

let private dotnetProjectExtensions = set [ ".csproj"; ".fsproj"; ".vbproj" ]
let private solutionFileExtensions = set [ ".sln"; ".slnx" ]
let private buildArtifactDirectories = [ "bin"; "obj" ]

type CliArguments =
    | [<AltCommandLine("-V")>] Version

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Version -> "print the tool version."

type ConsoleExiter() =
    let writeColoredLine color (message: string) =
        let originalColor = Console.ForegroundColor
        Console.ForegroundColor <- color

        try
            Console.Error.WriteLine(message)
        finally
            Console.ForegroundColor <- originalColor

    interface IExiter with
        member _.Name = "console"

        member _.Exit(msg, errorCode) =
            match errorCode with
            | ErrorCode.HelpText ->
                Console.WriteLine(msg)
                Environment.Exit(0)
                Unchecked.defaultof<'T>
            | _ ->
                writeColoredLine ConsoleColor.Red msg
                Environment.Exit(1)
                Unchecked.defaultof<'T>

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
        if not (System.String.IsNullOrWhiteSpace(standardOutput)) then
            printf "%s" standardOutput

        if not (System.String.IsNullOrWhiteSpace(standardError)) then
            eprintf "%s" standardError

    restoreProcess.ExitCode

let restoreSolutions (rootPath: string) : int =
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

let runDeepClean (currentDirectory: string) : int =
    let removedFolderCount =
        findProjectDirectories currentDirectory |> Seq.sumBy deepClean

    printfn "Removed %d folders." removedFolderCount

    if removedFolderCount > 0 then
        restoreSolutions currentDirectory
    else
        0

[<EntryPoint>]
let main argv =
    let parser =
        ArgumentParser.Create<CliArguments>(
            programName = "dotnet-deepclean",
            errorHandler = ConsoleExiter()
        )

    let results = parser.ParseCommandLine argv

    if results.Contains Version then
        printfn "%s" (getToolVersion ())
        0
    else
        runDeepClean (Directory.GetCurrentDirectory())
