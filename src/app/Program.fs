open System.IO

let private dotnetProjectExtensions = set [ ".csproj"; ".fsproj"; ".vbproj" ]

let findProjectDirectories (rootPath: string) : seq<string> =
    seq {
        for directory in Directory.EnumerateDirectories(rootPath, "*", SearchOption.AllDirectories) do
            let containsProjectFile =
                Directory.EnumerateFiles(directory)
                |> Seq.exists (fun filePath ->
                    let extension = Path.GetExtension(filePath)
                    dotnetProjectExtensions.Contains(extension))

            if containsProjectFile then
                yield directory
    }

[<EntryPoint>]
let main _ = 0
