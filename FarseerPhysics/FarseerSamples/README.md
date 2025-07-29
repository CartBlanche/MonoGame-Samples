# Farseer Physics MonoGame Samples

This repository contains cross-platform samples for the Farseer Physics Engine using MonoGame 3.8.* and .NET 8.0. The codebase is organized for modern SDK-style projects, with shared logic in a Core library and platform-specific entry points for Windows, DesktopGL, Android, and iOS.

## Project Structure

```
FarseerSamples/
├── Core/                # Shared game logic, physics, and rendering code
│   └── FarseerSamples.Core.csproj
├── Platforms/
│   ├── Windows/         # Windows-specific entry point and project
│   │   └── FarseerSamples.Windows.csproj
│   ├── Desktop/         # DesktopGL (cross-platform desktop) entry point and project
│   │   └── FarseerSamples.DesktopGL.csproj
│   ├── Android/         # Android entry point and project
│   │   └── FarseerSamples.Android.csproj
│   └── iOS/             # iOS entry point and project
│       └── FarseerSamples.iOS.csproj
├── Content/             # Pre-built .xnb assets used directly by all platforms
└── FarseerSamples.sln   # Solution file referencing all projects
```

## Prerequisites
- .NET 8.0 SDK
- MonoGame 3.8.* (NuGet packages are referenced automatically)
- Visual Studio 2022+ or VS Code (with C# Dev Kit recommended)
- For Android/iOS: Xamarin/MAUI workloads and appropriate emulators or devices

## Building and Running

### Visual Studio
- Open `FarseerSamples.sln`.
- Set the desired platform project as the startup project (Windows, DesktopGL, Android, or iOS).
- Build and run as usual.

### VS Code
- Use the built-in tasks and launch configurations:
    - `build-windows`, `build-desktopgl`, `build-android`, `build-ios`
    - Launch configurations for Windows, DesktopGL, Android, and iOS are available in `.vscode/launch.json`.
- Example (Windows):
    1. Run the `build-windows` task.
    2. Start debugging with the "Launch Windows" configuration.

### Android/iOS
- You may need to deploy manually to a device or emulator. See platform-specific documentation for details.

## Notes
- All shared code is in `/Core` and referenced by each platform project.
- Pre-built `.xnb` content is used directly; no content pipeline build is required.
- Only Windows, DesktopGL, Android, and iOS are supported (no Linux, MacOS, or PSMobile).

## License
Copyright © Farseer Physics 2023. See LICENSE for details.
