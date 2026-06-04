---
name: normalize-line-endings
description: Execute a C# script to normalize line endings in files within a specified directory.
---

Different agent prefer different line endings (LF vs CRLF). This script helps ensure that all files in a directory have consistent line endings, which can be important for version control.
You may run a simple C# script that recursively traverses the specified directory for files and normalizes the line endings of each file using `String.ReplaceLineEndings()`.
The script is inside `./scripts/normalize.cs` and can be run in most CLI environments.
It is generally acceptable to use a broader directory scope than you expect, since the script only modifies files with inconsistent line endings.

## Usage

Run in PowerShell:

```powershell
dotnet run .\normalize.cs -- <target path>
```

Example:

```powershell
dotnet run .\normalize.cs -- C:\Projects\MySolution
```