# MultiTouch Sample

A MonoGame 3.8.* sample demonstrating multi-touch input and drawing functionality. This sample allows users to draw on the screen using touch input or mouse, with support for multiple simultaneous touch points.

## Features

- Multi-touch drawing support
- Colorful brush strokes for multiple touch points
- Screen clearing functionality (Space key or gamepad A button)
- Cross-platform support for Windows, DesktopGL, iOS, and Android

## Platforms & Structure

This sample uses a modern, multi-project structure:

- `/Core` — Shared game logic (`Game1.cs`, `MultiTouch.Core.csproj`)
- `/Platforms/Windows` — Windows DirectX entry point and project
- `/Platforms/Desktop` — DesktopGL entry point and project
- `/Platforms/Android` — Android entry point and project
- `/Platforms/iOS` — iOS entry point and project
- `/Content` — Game assets (textures)

Each platform folder contains its own `.csproj` and entry point, referencing the shared `/Core` project.

## Prerequisites

- .NET 8.0 or later
- MonoGame 3.8.* (installed via NuGet packages)
- For Android: Android SDK and appropriate build tools
- For iOS: Xcode and iOS development tools (macOS only)

## Building and Running

### Visual Studio

1. Open `MultiTouch.sln` in Visual Studio
2. Set the desired platform project as the startup project (e.g., `Platforms/Windows/MultiTouch.Windows.csproj`)
3. Build and run (F5)

### VS Code

1. Open the project folder in VS Code
2. Use Ctrl+Shift+P and run "Tasks: Run Task"
3. Select one of:
   - `build-windows` / `run-windows`
   - `build-desktopgl` / `run-desktopgl`
   - `build-android` / `run-android`
   - `build-ios` / `run-ios`

Or use the launch configurations in `.vscode/launch.json` for debugging.

### Command Line

#### Windows
```pwsh
 dotnet build Platforms/Windows/MultiTouch.Windows.csproj
 dotnet run --project Platforms/Windows/MultiTouch.Windows.csproj
```
#### DesktopGL
```pwsh
 dotnet build Platforms/Desktop/MultiTouch.DesktopGL.csproj
 dotnet run --project Platforms/Desktop/MultiTouch.DesktopGL.csproj
```
#### Android
```pwsh
 dotnet build Platforms/Android/MultiTouch.Android.csproj
 # Deploy to connected device or emulator
 dotnet build Platforms/Android/MultiTouch.Android.csproj -t:Run
```
#### iOS (macOS only)
```pwsh
 dotnet build Platforms/iOS/MultiTouch.iOS.csproj
 # Deploy to connected device or simulator
 dotnet build Platforms/iOS/MultiTouch.iOS.csproj -t:Run
```

## Controls

- **Touch/Mouse**: Draw on the screen
- **Space** or **Gamepad A**: Clear the screen
- **Escape** or **Gamepad Back**: Exit the application

## Project Structure

```
/Core
    Game1.cs
    MultiTouch.Core.csproj
/Platforms
    /Windows
        MultiTouch.Windows.csproj
        Program.cs
    /Desktop
        MultiTouch.DesktopGL.csproj
        Program.cs
    /Android
        MultiTouch.Android.csproj
        MainActivity.cs
    /iOS
        MultiTouch.iOS.csproj
        Program.cs
/Content
    sqbrush.png
    circle.png
```

## Content

The sample uses the following content files:
- `sqbrush.png` — Brush texture for drawing
- `circle.png` — Circle texture (unused in current implementation)

These are included as `.xnb` files and loaded directly without requiring MonoGame Content Pipeline compilation.

## Notes

- This project has been modernized to use:
  - .NET 8.0 target frameworks
  - SDK-style project files
  - MonoGame 3.8.* NuGet packages
  - Modern C# code patterns
  - Platform-specific folders and entry points (no more `#if/#endif` blocks)
- The accelerometer functionality from the original sample has been replaced with keyboard/gamepad input for cross-platform compatibility
