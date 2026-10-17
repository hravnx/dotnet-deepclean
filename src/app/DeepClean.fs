module DeepClean

open System.IO

let private solutionFileExtensions = set [ ".sln"; ".slnx" ]

type RunOptions = {
    CurrentDirectory: string
    SkipRestore: bool
}

type RunResult = {
    RemovedFolderCount: int
    ExitCode: int
}

let run (restoreSolution: string -> int) (options: RunOptions) : RunResult =
    let rootPath = Path.GetFullPath(options.CurrentDirectory)
    let removedFolderCount = Cleanup.clean rootPath

    let exitCode =
        if removedFolderCount > 0 && not options.SkipRestore then
            Directory.EnumerateFiles(rootPath)
            |> Seq.filter (fun path -> solutionFileExtensions.Contains(Path.GetExtension(path)))
            |> Seq.sort
            |> Seq.map restoreSolution
            |> Seq.tryFind ((<>) 0)
            |> Option.defaultValue 0
        else
            0

    {
        RemovedFolderCount = removedFolderCount
        ExitCode = exitCode
    }
