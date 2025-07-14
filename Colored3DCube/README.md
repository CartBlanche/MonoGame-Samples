# Colored 3D Cube - MonoGame Sample

This is a modernized MonoGame 3.8.* sample demonstrating a rotating colored 3D cube, now structured for cross-platform development with .NET 8.0 and VSCode/Visual Studio integration.

## Project Structure

- **Core/**
  - `Colored3DCube.Core.csproj` — Shared game logic (e.g., `Game1.cs`)
- **Platforms/DesktopGL/**
  - `Colored3DCube.DesktopGL.csproj` — Cross-platform OpenGL version
  - `Platform.cs` — DesktopGL entry point
- **Platforms/Windows/**
  - `Colored3DCube.Windows.csproj` — Windows DirectX version
  - `Platform.cs` — Windows entry point
- **Platforms/Android/**
  - `Colored3DCube.Android.csproj` — Android version (scaffolded)
  - `MainActivity.cs` — Android entry point
  - `AndroidManifest.xml` — Android manifest
- **Platforms/iOS/**
  - `Colored3DCube.iOS.csproj` — iOS version (scaffolded)
  - `Program.cs` — iOS entry point
  - `Info.plist` — iOS app info
- **.vscode/**
  - `tasks.json` and `launch.json` — Build/run integration for VSCode

## Requirements

- .NET 8.0 SDK or later
- MonoGame 3.8.* (restored via NuGet)
- For Android/iOS: Xamarin/MAUI/MonoGame toolchain (see MonoGame docs)

## Building and Running

### Visual Studio Code (VSCode)
- Use the built-in tasks and launch configurations:
  - **Run DesktopGL**: Builds and runs the cross-platform version
  - **Run Windows**: Builds and runs the Windows DirectX version
- Press `F5` or select a configuration from the Run/Debug panel.

### Command Line
- **DesktopGL**:
  ```sh
  dotnet build Platforms/DesktopGL/Colored3DCube.DesktopGL.csproj
  dotnet run --project Platforms/DesktopGL/Colored3DCube.DesktopGL.csproj
  ```
- **Windows**:
  ```sh
  dotnet build Platforms/Windows/Colored3DCube.Windows.csproj
  dotnet run --project Platforms/Windows/Colored3DCube.Windows.csproj
  ```
- **Android/iOS**:
  - Open the solution in Visual Studio (Windows or Mac) and select the Android or iOS project. (Requires Xamarin/MAUI/MonoGame for mobile targets.)

## Controls

- **Arrow Keys** — Rotate the cube
- **Escape** — Exit the application

## Features

- Modern .NET 8.0 SDK-style projects
- Shared game logic in `Core` project
- Platform-specific entry points and projects
- Procedural geometry (no content pipeline required)
- Cross-platform: DesktopGL, Windows, Android, iOS
- VSCode and Visual Studio integration

## Notes

- No Content Pipeline (.mgcb) is used; all geometry/colors are generated in code.
- Android/iOS projects are scaffolding only; add icons/assets as needed.
- For mobile, ensure you have the correct build tools and emulators installed.
