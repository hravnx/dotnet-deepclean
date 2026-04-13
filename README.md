# Dotnet Super-Clean

A simple tool to clean up .NET projects by removing unnecessary files and
folders, such as `bin`, `obj`, and `packages`. This can help free up disk space
and improve performance when working with large projects.

# Usage
To use Dotnet Super-Clean, simply run the following command in your terminal:

```bash
dotnet super-clean
```

This will remove any `bin` and `obj` folders from the current directory and all
its subdirectories. Note that it will only remove these folders if they are in a
.NET project root, i.e a directory that contains a `.csproj`, `.fsproj`, or
`.vbproj` file.

# Installation

You can install Dotnet Super-Clean as a global tool using the following command:
```bash
dotnet tool install -g dotnet-super-clean
```
