open Argu
open System
open System.IO

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
        printfn "dotnet-deepclean %s" (DeepClean.getToolVersion ())
        0
    else
        DeepClean.run {
            CurrentDirectory = Directory.GetCurrentDirectory()
            SkipRestore = results.Contains Skip_Restore
        }
