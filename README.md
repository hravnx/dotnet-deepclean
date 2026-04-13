# Dotnet DeepClean

A simple tool to clean up .NET projects by removing unnecessary files and
folders, such as `bin`, `obj`, and `packages`. This can help free up disk space
and improve performance when working with large projects.

# Usage
To use Dotnet DeepClean, simply run the following command in your terminal:

```bash
dotnet deepclean
```

This will remove any `bin` and `obj` folders from the current directory and all
its subdirectories. Note that it will only remove these folders if they are in a
.NET project root, i.e a directory that contains a `.csproj`, `.fsproj`, or
`.vbproj` file.

After cleanup completes, the tool runs `dotnet restore` for each solution file
(`.sln` or `.slnx`) found in the current directory.

# Installation

You can install Dotnet DeepClean as a global tool using the following command:
```bash
dotnet tool install -g dotnet-deepclean
```
