# Audio3D MonoGame Sample

This project demonstrates 3D audio using MonoGame 3.8.* and .NET 8.0. It is structured for cross-platform builds with shared code in `/Core` and platform-specific projects in `/Platforms`.

## Structure
- `/Core`: Shared game logic and assets
- `/Platforms/Windows`: Windows-specific entry point and project
- `/Platforms/DesktopGL`: DesktopGL-specific entry point and project
- `/Platforms/Android`: Android-specific entry point and project
- `/Platforms/iOS`: iOS-specific entry point and project

## Building
- **Windows & DesktopGL**: Use VSCode or Visual Studio 2022+ to open the solution and build the desired platform project.
- **Android & iOS**: Open the solution in Visual Studio 2022+ with Xamarin/MAUI workloads installed.

## Running
- Use the provided `launch.json` and `tasks.json` for building and running from VSCode.
- Use the solution file for Visual Studio.

## Content
- Uses pre-built `.xnb` files in `/Content`.

## Requirements
- .NET 8.0 SDK
- MonoGame 3.8.* NuGet packages

## Platforms Supported
- Windows
- DesktopGL
- Android
- iOS

## Not Supported
- Linux, MacOS, PSMobile (per MonoGame 3.8.*)

---
For more details, see the individual platform project files.
