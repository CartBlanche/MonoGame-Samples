# TouchGesture MonoGame Sample

This is a cross-platform MonoGame 3.8.4 sample demonstrating touch gesture and mouse support. The project uses modern .NET 8.0+ SDK-style projects, with shared game logic in `/Core` and platform-specific launchers in `/Platforms`.

## Supported Platforms
- Windows (net8.0-windows)
- DesktopGL (net8.0)
- Android (net8.0-android)
- iOS (net8.0-ios)

## Directory Structure
```
/Core                  # Shared game logic (Game1, Sprite, etc.)
/Platforms/Windows     # Windows-specific entry point and project
/Platforms/DesktopGL   # DesktopGL-specific entry point and project
/Platforms/Android     # Android entry point, manifest, and project
/Platforms/iOS         # iOS entry point, Info.plist, and project
/Content               # Prebuilt .xnb content files (used by Windows & DesktopGL)
.vscode               # VSCode tasks and launch configuration
TouchGesture.sln       # Solution file for Visual Studio
README.md              # This file
```

## Content (.xnb) Files
- **Windows & DesktopGL:**
  - Use the `/Content` directory directly for all .xnb assets (e.g., `cat.xnb`, `Font.xnb`).
- **Android:**
  - The project copies .xnb files from `/Content` into the Android output at build time.
- **iOS:**
  - The project copies .xnb files from `../../../CompiledContent/iOS/Content/Textures/cat.xnb` and `../../../CompiledContent/iOS/Content/Fonts/Font.xnb` into the app bundle as `Content/cat.xnb` and `Content/Font.xnb`.
  - This allows for platform-optimized content if needed.

## Input Support
- **Touch (Mobile):**
  - All original gestures (hold, tap, drag, flick, pinch) are supported.
- **Mouse (Desktop/Windows):**
  - Left click: select or create sprite
  - Right click: remove sprite
  - Drag: move selected sprite
  - Mouse wheel: scale selected sprite
  - Middle click: change color
  - Mouse cursor is visible by default

## Building and Running

### Prerequisites
- .NET 8.0 SDK or newer
- Visual Studio 2022+ or VSCode
- MonoGame 3.8.4+ (NuGet packages are referenced automatically)

### Windows & DesktopGL
- Open the solution (`TouchGesture.sln`) in Visual Studio and set the desired platform project as startup, then build and run.
- Or, in VSCode, use the Run/Debug options for Windows or DesktopGL, or run from terminal:
  ```
  dotnet run --project Platforms/Windows/TouchGesture.Windows.csproj -c Debug
  dotnet run --project Platforms/DesktopGL/TouchGesture.DesktopGL.csproj -c Debug
  ```

### Android & iOS
- Open the solution in Visual Studio 2022+ (with Xamarin/MAUI workloads installed) and build/deploy the Android/iOS projects.
- Ensure the referenced .xnb files exist in the expected locations for each platform.

## Notes
- No Content.mgcb file is used; the game loads prebuilt .xnb files from the Content directory or platform-specific locations.
- All shared code is in `/Core` and referenced by each platform project.
- Platform-specific code is minimal and isolated in each platform directory.
- If you add new content, ensure it is built to .xnb for each platform and placed in the correct location.

---

For more details, see the source code in each directory.
