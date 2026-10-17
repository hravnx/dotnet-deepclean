module Cleanup

open System.IO

let private projectExtensions = set [ ".csproj"; ".fsproj"; ".vbproj" ]
let private artifactDirectories = set [ "bin"; "obj" ]

let private isLink (path: string) =
    File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint)

/// Clean the root and its descendants without traversing directory links.
let rec clean (rootPath: string) : int =
    if isLink rootPath then
        0
    else
        let containsProject =
            Directory.EnumerateFiles(rootPath)
            |> Seq.exists (fun path -> projectExtensions.Contains(Path.GetExtension(path)))

        // Snapshot children before deleting any of them, and never recurse into
        // artifacts belonging to a project that is being cleaned.
        Directory.GetDirectories(rootPath)
        |> Array.sumBy (fun directory ->
            if isLink directory then
                0
            elif containsProject && artifactDirectories.Contains(Path.GetFileName(directory)) then
                Directory.Delete(directory, true)
                1
            else
                clean directory
        )
