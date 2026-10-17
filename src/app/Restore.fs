module Restore

open System.Diagnostics
open System.IO

type ProcessResult = {
    ExitCode: int
    StandardOutput: string
    StandardError: string
}

let execute (startInfo: ProcessStartInfo) : ProcessResult =
    startInfo.UseShellExecute <- false
    startInfo.RedirectStandardOutput <- true
    startInfo.RedirectStandardError <- true
    use child = new Process(StartInfo = startInfo)
    child.Start() |> ignore

    // Both pipes must be drained while the child is running: either can fill.
    let standardOutput = child.StandardOutput.ReadToEndAsync()
    let standardError = child.StandardError.ReadToEndAsync()
    child.WaitForExit()

    {
        ExitCode = child.ExitCode
        StandardOutput = standardOutput.GetAwaiter().GetResult()
        StandardError = standardError.GetAwaiter().GetResult()
    }

let solution (solutionPath: string) : ProcessResult =
    let startInfo =
        ProcessStartInfo(
            FileName = "dotnet",
            WorkingDirectory = Path.GetDirectoryName(solutionPath)
        )

    startInfo.ArgumentList.Add("restore")
    startInfo.ArgumentList.Add(solutionPath)
    execute startInfo
