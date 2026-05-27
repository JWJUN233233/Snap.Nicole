---
name: winui-generic-xaml
description: Locate and inspect WinUI 3 default styles in Microsoft.WindowsAppSDK.WinUI generic.xaml files from the local NuGet package cache. Use when you needs to find default WinUI control templates, theme resources, VisualState definitions, style setters, or compare app XAML against WinUI's packaged generic.xaml.
---

# WinUI Generic XAML

Use the restored NuGet assets as the source of truth. Do not guess the package cache path or version.

## Locate generic.xaml

1. Find the relevant `project.assets.json`, usually under the main project's `obj` folder. Use publish-specific assets such as `obj\publish\win-x64\project.assets.json` only when investigating publish behavior.
2. Read `packageFolders` from the assets file. These are the local NuGet roots that restore actually used.
3. Find the `libraries` entry whose name starts with `Microsoft.WindowsAppSDK.WinUI/`. Use its `path` value rather than reconstructing the lower-case package folder manually.
4. Combine each package root with the package version currently using (e.g. `2.1.0` -> `~\.nuget\packages\microsoft.windowsappsdk.winui\2.1.0\lib`) and check whether the directory exists.
5. Under that directory, choose the framework-specific `generic.xaml` first when the app targets .NET:
   `lib\<target-framework>\Microsoft.WinUI\Themes\generic.xaml`
6. Fall back to the native packaged XAML only when the framework-specific file is absent or the investigation is native-packaging-specific:
   `lib\native\Microsoft.UI\Themes\generic.xaml`

## Inspect Safely

`generic.xaml` is large. Prefer targeted search before opening broad ranges.

- Search for a control key or type name with `rg`, for example `rg -n "DefaultButtonStyle|TargetType=\"Button\"|x:Key=\"Button"` on the resolved file.
- Read a narrow range around the match after finding the line number.
- Preserve the distinction between `DefaultStyleKey`, implicit styles, keyed styles, `BasedOn`, theme dictionaries, and `VisualStateManager.VisualStateGroups`.
- When comparing with app XAML, copy only the relevant setters, resources, or template parts. Do not fork an entire default template unless the requested change requires it.
