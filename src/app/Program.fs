open Argu
open System
open System.IO
open System.Reflection

let private getToolVersion () =
    let assembly = Assembly.GetExecutingAssembly()
    let version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()

    if isNull version then
        assembly.GetName().Version.ToString()
    else
        version.InformationalVersion.Split('+')[0]

let private restoreSolution (solutionPath: string) =
    printfn "Running dotnet restore for %s" (Path.GetFileName(solutionPath))
    let result = Restore.solution solutionPath

    if result.ExitCode <> 0 then
        if not (String.IsNullOrWhiteSpace(result.StandardOutput)) then
            printf "%s" result.StandardOutput

        if not (String.IsNullOrWhiteSpace(result.StandardError)) then
            eprintf "%s" result.StandardError

    result.ExitCode

type CliArguments =
    | [<AltCommandLine("-V")>] Version
    | Skip_Restore

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Version -> "print the tool version."
            | Skip_Restore -> "skip the final dotnet restore step."

[<EntryPoint>]
let main argv =
    let parser =
        let colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red

        ArgumentParser.Create<CliArguments>(
            programName = "dotnet-deepclean",
            errorHandler = ProcessExiter(colorizer = colorizer)
        )

    let results = parser.ParseCommandLine argv

    if results.Contains Version then
        printfn "dotnet-deepclean %s" (getToolVersion ())
        0
    else
        let result =
            DeepClean.run restoreSolution {
                CurrentDirectory = Directory.GetCurrentDirectory()
                SkipRestore = results.Contains Skip_Restore
            }

        printfn "Removed %d folders." result.RemovedFolderCount
        result.ExitCode
