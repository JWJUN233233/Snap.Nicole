# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Snap Nicole (莱恩自动机) is a Windows desktop application built with WinUI 3 (Windows App SDK 2.0) and .NET 10. It features a system tray (notify icon) with context menu, window lifecycle management, and native Win32 interop via a C++ DLL.

## Build Commands

```bash
# Build the entire solution (from src/ directory)
dotnet build Snap.Nicole.slnx

# Build only the main C# project
dotnet build Snap.Nicole/Snap.Nicole.csproj

# Build for specific platform
dotnet build Snap.Nicole/Snap.Nicole.csproj -p:Platform=x64
dotnet build Snap.Nicole/Snap.Nicole.csproj -p:Platform=x86
dotnet build Snap.Nicole/Snap.Nicole.csproj -p:Platform=ARM64

# Build in Release mode
dotnet build Snap.Nicole/Snap.Nicole.csproj -c Release

# The C++ native project (Snap.Nicole.Native) must be built separately via Visual Studio or MSBuild
# as dotnet CLI cannot build .vcxproj files directly
```

## Solution Structure

The solution (`src/Snap.Nicole.slnx`) contains three projects:

- **Snap.Nicole** — Main WinUI 3 C# application (.NET 10, `net10.0-windows10.0.19041.0`)
- **Snap.Nicole.Native** — C++ DLL providing Win32 native interop (COM objects for notify icons, window subclassing, window utilities)
- **Snap.Nicole.SourceGeneration** — Roslyn incremental source generators (resx code generation, unmanaged function pointer wrapper generation)

## Architecture

### Application Bootstrap (DI + Hosting)

The app uses `Microsoft.Extensions.Hosting` for dependency injection. Entry point is `Program.Main()` which:
1. Initializes COM wrappers for WinRT
2. Configures the DI container via `IHostBuilder`
3. Registers XAML windows, services, and ViewModels
4. Starts the WinUI `Application` with a custom `SynchronizationContextPolyfill`

Window registration uses `AddXamlWindows()` / `AddXamlWindow<T>()` extension methods. Each window gets an `IWindowLifeTime<T>` wrapper that manages creation, activation, subclassing, and close handling.

### Window Lifecycle

`WindowLifeTime<T>` (implements `IWindowLifeTime<T>`) is the core window manager:
- Lazily creates windows on first `Show()` call
- Attaches a native window subclass via `NicoleNativeWindowSubclass` for intercepting Win32 messages
- Handles `IXamlWindowCloseHandler` to allow canceling window close
- Supports `IXamlWindowEraseBackground` to suppress background erase flicker
- Generates persisted window placement IDs from type names for position restoration

### Native Interop (Snap.Nicole.Native)

The C++ DLL exposes three COM interfaces via WRL (`Microsoft::WRL`):
- `INicoleNative` — Factory for creating notify icons and window subclasses
- `INicoleNativeNotifyIcon` — System tray icon management (create, recreate on taskbar restart, destroy, check promotion status)
- `INicoleNativeWindowSubclass` — Win32 window subclassing with `SetWindowSubclass`/`RemoveWindowSubclass`

The C# side uses hand-written vtable structs and `[DllImport]` P/Invoke to call into the native DLL. The `NicoleNative` class is a singleton accessed via `NicoleNative.Default`.

Window utilities (`WindowUtilities.cs`) provide P/Invoke wrappers for: `SwitchToWindow`, `AddExtendedStyleLayered`, `SetLayeredWindowTransparency`, `AddExtendedStyleToolWindow`, `GetRasterizationScaleForWindow`, `SetTaskbarProgress`, etc.

### System Tray (NotifyIcon)

`NotifyIcon` implements `INotifyIcon` and manages the system tray icon lifecycle:
- Creates a hidden `NotifyIconXamlHostWindow` (fully transparent, click-through, always-on-top) to host XAML flyouts
- Shows `NotifyIconContextMenuFlyout` as the context menu when the tray icon is clicked
- Handles double-click to show the main window
- Uses `GCHandle<T>` to pass managed references to native callbacks safely
- Handles `TaskbarCreated` message to recreate the icon when explorer.exe restarts

### Source Generators

Two Roslyn incremental source generators in `Snap.Nicole.SourceGeneration`:

1. **ResxGenerator** — Reads `.resx` files and generates:
   - A static resource class with `GetString()`, `GetObject()`, `GetStream()` methods and per-entry properties
   - An enum type (`SRName`) for type-safe resource name references
   - Format methods for entries containing composite format strings

2. **UnmanagedFunctionPointerGenerator** — Generates constructors and `Create()` factory methods for structs annotated with `[GeneratedUnmanagedFunctionPointer]`, used for native callback wrappers (e.g., `NicoleNativeNotifyIcon.Callback`, `NicoleNativeWindowSubclass.Callback`)

### Key Interfaces

- `IWindowLifeTime<TWindow>` — Window lifecycle (Show/Close, lazy creation)
- `IApplicationLifeTime` — Application shutdown coordination (`IsExiting`, `ShowdownAsync()`)
- `INotifyIcon` — System tray icon operations
- `IXamlWindowCloseHandler` — Intercept window close to cancel or perform cleanup
- `IXamlWindowEraseBackground` — Marker interface to suppress WM_ERASEBKGND

### XAML Markup Extensions

- `StringResourceExtension` (`{snuxm:StringResource Name=...}`) — Binds to localized strings from SR.resx via `StringResourceProxy`
- `FontIconExtension` (`{snuxm:FontIcon Glyph=...}`) — Creates `FontIcon` instances inline in XAML

## Conventions

- File-scoped namespaces throughout
- `internal` visibility by default for all types unless XAML requires `public`
- C# 13+ features used freely: `extension` blocks, primary constructors, collection expressions, `field` keyword
- `[DisableRuntimeMarshalling]` on the assembly for native interop
- `AllowUnsafeBlocks` is enabled — unsafe code is used extensively for native interop vtable access
- Chinese (zh-CN) is the default/neutral language; resources are in `Resources/SR.resx`
- Naming: PascalCase for types and members, `I` prefix for interfaces
- The `.editorconfig` enforces file-scoped namespaces, `var` only when type is apparent, and explicit access modifiers
