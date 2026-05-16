The user might execute `git add`/`git restore` actively to observable/review your working progress, so DO NOT only rely on git status or related command to check files edits.

# Project Structure

```
src
├── Snap.Nicole
├── Snap.Nicole.Native
└── Snap.Nicole.SourceGeneration
```

- Snap.Nicole: The main project. It primarily uses C#/WinUI 3 to build an agentic, multifunctional toolbox application.
- Snap.Nicole.Native: A supporting project that uses C++/WinRT to help the main project interact with the Windows system and to polyfill features that WinUI 3 does not provide.
- Snap.Nicole.SourceGeneration: A supporting project and Roslyn source generator that reduces the overhead of writing repetitive code in the main project.

## Snap.Nicole

- Extensively adopt Microsoft.Extensions.Hosting and Microsoft.Extensions.DependencyInjection to manage the lifetime of applications, services, and objects.
- Extensively adopt the Model-View-ViewModel (MVVM) pattern to handle data presentation and user interactions.
- All class/struct members should be `internal` or `private`, unless XAML requires them to be `public` (for example, attached DependencyProperties and the `Application` class).

### How to build `Snap.Nicole`

Snap.Nicole depends on the Snap.Nicole.Native c++ project, so dotnet build won't work properly.
In order to build the project correctly and reduce build fails, you need to:

1. Run cmd `where /r "C:\Program Files\Microsoft Visual Studio" msbuild` to locate msbuild
2. Run cmd `"path/to/msbuild" "path/to/Snap.Nicole.csproj" -restore -t:Build -p:Configuration=Debug -p:Platform=x64 -nologo -verbosity:minimal -clp:ErrorsOnly`

If CMD is not available and PowerShell is, the script above may be converted to PowerShell instead.

## Snap.Nicole.Native

No information provided yet.

## Snap.Nicole.SourceGeneration

No information provided yet.

# Line Ending Normalization

`normalize-line-endings` skill requires be load into your context and you may follow the instructions in that skill before completing the givin task.
If you can not load skill by tool call, you can read the skill spec in `.agents\skills\normalize-line-endings.md`.