open System.IO

let private dotnetProjectExtensions = set [ ".csproj"; ".fsproj"; ".vbproj" ]
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

let deepClean (projectPath: string) : unit =
    buildArtifactDirectories
    |> List.iter (fun directoryName ->
        let directoryPath = Path.Combine(projectPath, directoryName)

        if Directory.Exists(directoryPath) then
            Directory.Delete(directoryPath, true)
    )

[<EntryPoint>]
let main _ =
    let currentDirectory = Directory.GetCurrentDirectory()

    findProjectDirectories currentDirectory
    |> Seq.iter deepClean

    0
