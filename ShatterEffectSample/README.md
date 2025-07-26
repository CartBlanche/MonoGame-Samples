# Shatter Effect Sample

This project demonstrates how to apply a shatter effect to any model in your game using MonoGame 3.8.*. The effect is simulated with a vertex shader. The codebase is organized for cross-platform support with shared logic in the `Core` project and platform-specific entry points for Windows, DesktopGL, Android, and iOS.

## Project Structure
- `Core/` — Shared game logic and content pipeline files
- `Core/Content/` — Prebuilt .xnb content and related assets
- `Platforms/Windows/` — Windows-specific project (net8.0-windows)
- `Platforms/DesktopGL/` — DesktopGL cross-platform project (net8.0)
- `Platforms/Android/` — Android project (net8.0-android)
- `Platforms/iOS/` — iOS project (net8.0-ios)

## Building and Running

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MonoGame 3.8.*](https://www.monogame.net/)
- Visual Studio 2022+ or VSCode with C# Dev Kit

### Windows & DesktopGL
- Open the solution in Visual Studio and set the desired platform project as startup, or
- In VSCode, use the provided `launch.json` and `tasks.json` to build and run:
  - Press `Ctrl+Shift+B` to build (choose `build-windows` or `build-desktopgl`)
  - Press `F5` to launch

### Android & iOS
- Build using Visual Studio or `dotnet build` from the command line:
  - `dotnet build Platforms/Android/ShatterEffectSample.Android.csproj`
  - `dotnet build Platforms/iOS/ShatterEffectSample.iOS.csproj`
- Deploy to device/emulator as per your platform requirements.

## Notes
- No Content.mgcb file is used; the game loads .xnb files directly from the `Content/` folder.
- Platform-specific code is separated to avoid `#if` blocks.
- Only Windows, DesktopGL, Android, and iOS are supported. Linux, MacOS, and PSMobile are not supported in MonoGame 3.8.*.

## License
Copyright © Microsoft

---
For more details, see the source code and comments in each project.
