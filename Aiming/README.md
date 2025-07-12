# Aiming MonoGame Sample

This is a cross-platform MonoGame 3.8.4 sample project demonstrating how to aim one object towards another (e.g., a spotlight tracking a cat). The project is organized for modern .NET 8 SDK-style builds and supports Windows, DesktopGL, Android, and iOS.

## Project Structure

```
Aiming/
├── Core/                # Shared game logic (AimingGame.cs, Aiming.Core.csproj)
├── Platforms/
│   ├── Windows/         # Windows-specific entry point and csproj
│   ├── DesktopGL/       # DesktopGL (cross-platform OpenGL) entry point and csproj
│   ├── Android/         # Android entry point, csproj, and manifest
│   └── iOS/             # iOS entry point, csproj, and Info.plist
├── Content/             # Shared content (PNG, XNB, etc.)
├── .vscode/             # VS Code tasks and launch configs
├── README.md            # This file
└── Aiming.sln           # Visual Studio solution
```

## Prerequisites
- .NET 8 SDK
- MonoGame 3.8.4 (NuGet packages are referenced automatically)
- For Android/iOS: Xamarin/MAUI workloads and platform SDKs
- Visual Studio 2022+ or VS Code

## Building and Running

### With Visual Studio
- Open `Aiming.sln`
- Set the desired platform project as startup (Windows, DesktopGL, Android, or iOS)
- Build and run

### With VS Code
- Use the provided tasks and launch configurations:
  - **Build:**
    - `build-windows` — Windows
    - `build-desktopgl` — DesktopGL
    - `build-android` — Android
    - `build-ios` — iOS
  - **Run/Debug:**
    - `Run Windows` — Launches Windows build
    - `Run DesktopGL` — Launches DesktopGL build
- Press `Ctrl+Shift+B` to build, or use the Run/Debug panel

### Command Line
```
dotnet build Platforms/Windows/Aiming.Windows.csproj
# or
dotnet build Platforms/DesktopGL/Aiming.DesktopGL.csproj
# or
 dotnet build Platforms/Android/Aiming.Android.csproj
# or
 dotnet build Platforms/iOS/Aiming.iOS.csproj
```

## Notes
- Content is loaded as `.xnb` or `.png` files directly, depending on platform.
- Android/iOS builds require the appropriate SDKs and emulators/devices.
- If you encounter issues with Android entry points, check the MonoGame 3.8.4 documentation for the latest supported base class for `MainActivity`.

## License
MIT (see LICENSE if present)
