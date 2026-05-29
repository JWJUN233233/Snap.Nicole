---
name: ilspycmd-find-implementation
description: Use ilspycmd to locate, decompile, and inspect external .NET library method or type implementations that are not present in the current repository. Use when you needs to trace a call into a NuGet package, framework assembly, or other referenced DLL; resolve which external assembly contains a symbol; or explain external behavior from decompiled code.
---

# ILSpycmd Find Implementation

- Answer "where is this external .NET method implemented?" with concrete evidence from the restored package graph and decompiled assembly output.
- Inspect the external type/method implementaion details to write better codes for the current task.

## Workflow

1. Identify the symbol precisely before decompiling.
   - Search the repo first with `rg` for the method, type, namespace, and call site.
   - Capture the declaring type if available from compiler errors, stack traces, `using` directives, XML docs, or metadata names.
   - Treat extension methods as static methods on static classes, and property/event accessors as generated `get_`, `set_`, `add_`, or `remove_` methods when needed.

2. Choose the restore graph that matches the investigation.
   - Prefer `obj\project.assets.json`
   - Use publish-specific assets only for publish-output questions, such as `obj\publish\win-x64\project.assets.json`.

3. Locate candidate DLLs from the actual restored assets.
   - Use the bundled helper when the package or assembly name is known:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .agents\skills\ilspycmd-find-implementation\scripts\Find-ExternalAssembly.ps1 `
  -AssetsPath src\Snap.Nicole\obj\project.assets.json `
  -Query "CommunityToolkit.Mvvm"
```

   - The helper searches compile/runtime DLLs in the NuGet roots recorded by `project.assets.json`. It does not prove that a type is inside the DLL; it narrows the assembly candidates.
   - If the helper returns several candidates, prefer the compile asset that matches the call site target framework, then runtime assets that match the app runtime identifier.

4. Verify `ilspycmd` availability and syntax.
   - Run `ilspycmd --help` because option names can vary by installed version.
   - If `ilspycmd` is missing and the task requires decompilation, ask for permission before installing the tool via `dotnet tool install --global ilspycmd`.

5. Decompile narrowly first.  
Here are some demonstrations about how to use the tool:

- Use type-level decompilation when the declaring type is known:

```powershell
ilspycmd -t "Namespace.TypeName" "C:\path\to\Package.dll"
```

- Use IL output when the high-level decompiler hides details important to the question:

```powershell
ilspycmd --ilcode -t "Namespace.TypeName" "C:\path\to\Package.dll"
```

- If the type is unknown, decompile to a scratch directory and search the output:

```powershell
ilspycmd -o "$env:TEMP\codex-ilspy\PackageName" "C:\path\to\Package.dll"
rg -n "MethodName|TypeName" "$env:TEMP\codex-ilspy\PackageName"
```

6. Interpret the result carefully.
   - Interfaces, abstract methods, extern methods, P/Invoke declarations, COM projections, and WinRT projections may not have a managed body.
   - Async methods and iterators may appear as high-level methods in C# output but as generated state-machine `MoveNext` methods in IL.
   - Generic and explicit interface implementations may have names that differ from the source call syntax.
   - Native or packaged WinUI behavior may require native symbols or XAML templates instead of managed decompilation.

## Script Resource

`scripts\Find-ExternalAssembly.ps1` locates restored package DLL candidates by package, assembly, or asset-path fragment. Use `-Json` when another script or agent step needs structured output.
