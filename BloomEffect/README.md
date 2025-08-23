# BloomSample

This is a cross-platform MonoGame 3.8.* sample demonstrating bloom post-processing effects. The project is organized for modern .NET SDK-style builds and supports Windows, DesktopGL, Android, and iOS platforms.

## Directory Structure

- `/Core` — Shared game logic and code
- `/Platforms/Windows` — Windows-specific entry point and project
- `/Platforms/DesktopGL` — DesktopGL-specific entry point and project
- `/Platforms/Android` — Android-specific entry point, project, and manifest
- `/Platforms/iOS` — iOS-specific entry point, project, and Info.plist

## Building and Running

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022+ (for Windows/iOS/Android) or VSCode
- MonoGame 3.8.* NuGet packages (restored automatically)

### Windows & DesktopGL
- Open the solution in Visual Studio, or use VSCode.
- To build and run from VSCode:
  - Select the desired platform in the Run/Debug panel.
  - Use the provided tasks/launch configurations.

### Android & iOS
- Open the solution in Visual Studio 2022+ (Windows: Android, Mac: iOS).
- Build and deploy to a device or emulator.

### Content
- This sample uses pre-built `.xnb` files directly from the `bin/` directory. No `Content.mgcb` file is required.

## Notes
- Only Windows, DesktopGL, Android, and iOS are supported. Linux, MacOS, and PSMobile are not supported in MonoGame 3.8.*.
- All shared code is in `/Core` and referenced by each platform project.
