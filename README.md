# Dotnet DeepClean

A simple tool to clean up .NET projects by removing unnecessary files and
folders, such as `bin` and `obj`. This can help free up disk space
and improve performance when working with large projects.

# Installation

You can install Dotnet DeepClean as a global tool using the following command:

```bash
dotnet tool install -g Ravn.Dotnet.DeepClean
```

# Usage
To use Dotnet DeepClean, simply run the following command in your terminal:

```bash
dotnet deepclean
```

This will remove any `bin` and `obj` folders from the current directory and all
its subdirectories. Note that it will only remove these folders if they are in a
.NET project root, i.e a directory that contains a `.csproj`, `.fsproj`, or
`.vbproj` file. Directory symlinks and reparse points are skipped, including
linked `bin` and `obj` directories.

If cleanup removes any folders, the tool runs `dotnet restore` for each solution
file (`.sln` or `.slnx`) found in the current directory, stopping at the first
failure and returning its exit code.

To skip the final restore step, run:

```bash
dotnet deepclean --skip-restore
```

To print the installed tool version, run:

```bash
dotnet deepclean --version
```

# Tests

Run `just test` or `dotnet fsi --exec tests/Tests.fsx` with the .NET 10 SDK.
The tests use temporary directories and require permission to create directory
symlinks. They need no additional test packages.

# License

This project is licensed under the MIT License.
