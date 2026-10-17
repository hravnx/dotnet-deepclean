open System

// Write more than a pipe buffer before closing stdout. Sequential reads hang.
Console.Error.Write(String('e', 1024 * 1024))
Console.Out.Write(String('o', 1024 * 1024))
Console.Out.Write("|" + Environment.CurrentDirectory)
Console.Out.Write("|" + fsi.CommandLineArgs[1])
Environment.Exit(23)
