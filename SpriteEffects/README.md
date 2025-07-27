# SpriteEffects MonoGame Sample

This project demonstrates advanced sprite effects using MonoGame 3.8.* and .NET 8.0 for Windows, DesktopGL, Android, and iOS platforms.


## Project Structure
- `Core/` — Shared game logic (`SpriteEffectsGame`), content, and effects
- `Platform/`
  - `Windows/` — Windows-specific entry point and project
  - `DesktopGL/` — DesktopGL-specific entry point and project
  - `Android/` — Android entry point, manifest, and project
  - `iOS/` — iOS entry point, Info.plist, and project
- `Processor/` — Custom content pipeline processors (e.g., `NormalMapProcessor`, `TexturePlusAlphaProcessor`)
- `Content/` — Prebuilt .xnb assets and effect files (e.g., images, .fx shaders)

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- MonoGame 3.8.* NuGet packages (restored automatically)
- Visual Studio 2022+ or VSCode (with C# Dev Kit recommended)

### Windows
- Open `SpriteEffects.sln` in Visual Studio and set `SpriteEffects.Windows` as startup.
- Or, in VSCode, use the provided tasks/launch configurations to build and run.

### DesktopGL
- Open `SpriteEffects.sln` and set `SpriteEffects.DesktopGL` as startup.
- Or, in VSCode, use the provided tasks/launch configurations.

### Android/iOS
- Open the solution in Visual Studio 2022+ (Windows: Android, Mac: iOS) and deploy to device/emulator.

## Notes
- No Content.mgcb file is used; .xnb files are loaded directly from the Content folder.
- Platform-specific code is isolated to minimize #if/#endif usage.
