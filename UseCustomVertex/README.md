# UseCustomVertex MonoGame Sample

This project demonstrates the use of custom vertex types in MonoGame 3.8.* across multiple platforms.

## Supported Platforms
- Windows (DirectX)
- DesktopGL
- Android
- iOS

## Project Structure
- `Core/` — Shared game logic, custom vertex types, and assets
- `Core/Content/` — Pre-built .xnb and image assets (used directly, no Content.mgcb)
- `Platforms/Windows/` — Windows-specific entry point (`Program.cs`) and project
- `Platforms/DesktopGL/` — DesktopGL-specific entry point (`Program.cs`) and project
- `Platforms/Android/` — Android-specific entry point (`MainActivity.cs`), manifest, and project
- `Platforms/iOS/` — iOS-specific entry point (`Program.cs`), Info.plist, and project

All platform projects reference the shared code in `Core/`. Platform-specific code and entry points are isolated in their respective directories.

## Building and Running

### Prerequisites
- .NET 8.0 SDK or later
- MonoGame 3.8.* NuGet packages (restored automatically)
- Visual Studio 2022+ or VSCode (with C# Dev Kit recommended)

### Windows & DesktopGL
- Open the solution (`UseCustomVertex.sln`) in Visual Studio and select the desired platform project.
- Or, in VSCode, use the provided tasks/launch configurations to build and run Windows or DesktopGL.

### Android & iOS
- Open the solution in Visual Studio 2022+ (Windows: Android, Mac: Android/iOS).
- Ensure Xamarin/MAUI workloads are installed for mobile targets.

## Notes
- All shared code is in `Core/` and referenced by each platform project.
- Platform-specific code and entry points are in their respective directories.
- No Content.mgcb is used; .xnb files are loaded directly.
