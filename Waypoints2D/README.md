# Waypoint MonoGame Sample

This is a cross-platform MonoGame 3.8.* sample project, targeting Windows, DesktopGL, Android, and iOS using .NET 8.0. Shared game logic is in the `Core` project, with platform-specific entry points in their respective directories.

## Directory Structure

```
README.md
Waypoint.sln
Core/
    Tank.cs
    Waypoint.Core.csproj
    WaypointList.cs
    WaypointSample.cs
    Behaviors/
        Behavior.cs
        LinearBehavior.cs
        SteeringBehavior.cs
    Content/
        Content.mgcb
        cursor.png
        Default.png
        dot.png
        font.spritefont
        GameThumbnail.png
        Icon.ico
        tank.png
Platforms/
    Android/
        AndroidManifest.xml
        MainActivity.cs
        Waypoint.Android.csproj
    DesktopGL/
        Program.cs
        Waypoint.DesktopGL.csproj
    iOS/
        Info.plist
        Program.cs
        Waypoint.iOS.csproj
    Windows/
        Program.cs
        Waypoint.Windows.csproj
```

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022+ (with MonoGame and Xamarin workloads) or VSCode

### Windows & DesktopGL
- Open the solution in Visual Studio and set the desired platform project as startup, or
- In VSCode, use the provided tasks/launch configurations to build and run.

### Android & iOS
- Requires Visual Studio 2022+ with Xamarin/MAUI workloads and appropriate emulators or devices.

## Content Pipeline
- All game assets are managed in `Core/Content/Content.mgcb`.
- Textures, fonts, and other resources are processed for each platform.

## Platforms
- **Android**: `Platforms/Android/Waypoint.Android.csproj`
- **DesktopGL**: `Platforms/DesktopGL/Waypoint.DesktopGL.csproj`
- **iOS**: `Platforms/iOS/Waypoint.iOS.csproj`
- **Windows**: `Platforms/Windows/Waypoint.Windows.csproj`

## Core Logic
- Main game logic and behaviors are in the `Core` directory.

---

For more details, see the individual platform project files.
