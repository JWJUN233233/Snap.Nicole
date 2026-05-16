The user might execute `git add`/`git restore` actively to observable/review your working progress, so DO NOT only rely on git status or related command to check files edits.

## How to build `Snap.Nicole`

Snap.Nicole depends on the Snap.Nicole.Native c++ project, so dotnet build won't work properly.
In order to build the project correctly and reduce build fails, you need to:

1. Run cmd `where /r "C:\Program Files\Microsoft Visual Studio" msbuild` to locate msbuild
2. Run cmd `"path/to/msbuild" "path/to/Snap.Nicole.csproj" -restore -t:Build -p:Configuration=Debug -p:Platform=x64 -nologo -verbosity:minimal -clp:ErrorsOnly`

If CMD is not available and PowerShell is, the script above may be converted to PowerShell instead.

## Line Ending Normalization

`normalize-line-endings` skill requires be load into your context and you may follow the instructions in that skill before completing the givin task.
If you can not load skill by tool call, you can read the skill spec in `.agents\skills\normalize-line-endings.md`.