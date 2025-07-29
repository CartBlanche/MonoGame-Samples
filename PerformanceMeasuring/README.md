# Performance Measuring Sample

This is a MonoGame 3.8.* sample project that demonstrates how to use the GameDebugTools to measure the performance of a game, as well as how the number of objects and interactions between them can affect performance.

## Project Structure


## Solution Structure

The solution is organized into platform-specific projects and a shared core library:

- **Core/PerformanceMeasuring.Core.csproj** – Shared game logic, primitives, and debug tools
- **Platforms/Windows/PerformanceMeasuring.Windows.csproj** – Windows DirectX version (net8.0-windows)
- **Platforms/DesktopGL/PerformanceMeasuring.DesktopGL.csproj** – Cross-platform OpenGL version (net8.0)
- **Platforms/Android/PerformanceMeasuring.Android.csproj** – Android version (net8.0-android)
- **Platforms/iOS/PerformanceMeasuring.iOS.csproj** – iOS version (net8.0-ios)

Each platform project references the shared Core project and includes only platform-specific entry points and configuration.

## Features

- Real-time performance monitoring using GameDebugTools
- Dynamic sphere simulation with collision detection
- Configurable number of objects to test performance impact
- Cross-platform compatibility

## Controls

### Desktop (Windows/DesktopGL)
- **X** - Toggle collisions on/off
- **Up Arrow** - Increase number of spheres
- **Down Arrow** - Decrease number of spheres

### Android
- **Tap** - Toggle collisions on/off
- **Drag up/down** - Change number of spheres

## Building and Running

### Prerequisites

- .NET 8.0 SDK or later
- For Android: Android SDK and workload installed (`dotnet workload install android`)

### Building from Command Line


#### Windows
```powershell
dotnet build Platforms/Windows/PerformanceMeasuring.Windows.csproj
dotnet run --project Platforms/Windows/PerformanceMeasuring.Windows.csproj
```

#### DesktopGL (Cross-platform)
```powershell
dotnet build Platforms/DesktopGL/PerformanceMeasuring.DesktopGL.csproj
dotnet run --project Platforms/DesktopGL/PerformanceMeasuring.DesktopGL.csproj
```

#### Android
```powershell
dotnet build Platforms/Android/PerformanceMeasuring.Android.csproj
```

#### iOS
```powershell
dotnet build Platforms/iOS/PerformanceMeasuring.iOS.csproj
```

### Building with Visual Studio

Open `PerformanceMeasuring.sln` in Visual Studio and build/run the desired platform project.

### Building with VS Code

The project includes VS Code tasks and launch configurations:

1. **Build tasks** (Ctrl+Shift+P → "Tasks: Run Task"):
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version
   - `clean` - Clean all projects

2. **Run/Debug** (F5):
   - "Launch Windows" - Run Windows version with debugger
   - "Launch DesktopGL" - Run DesktopGL version with debugger

## Content Pipeline

This project uses pre-built .xnb content files located in the `Content/` folder. No Content Pipeline rebuild is required.


## Dependencies

- MonoGame.Framework.WindowsDX 3.8.* (Windows)
- MonoGame.Framework.DesktopGL 3.8.* (DesktopGL)
- MonoGame.Framework.Android 3.8.* (Android)
- MonoGame.Framework.iOS 3.8.* (iOS)


## Platform Support

- ✅ **Windows** – Fully supported, builds and runs
- ✅ **DesktopGL** – Fully supported, builds and runs (Linux, macOS, Windows)
- ✅ **Android** – Builds successfully (requires Android SDK for deployment)
- 🟡 **iOS** – Entry point and project included (requires .NET iOS workload for deployment)

## Performance Monitoring

The sample includes several debug tools:

- **FPS Counter** - Real-time frame rate display
- **Time Ruler** - Performance profiling bars
- **Debug Manager** - Command system for runtime debugging

Use these tools to observe how increasing the number of spheres and enabling/disabling collisions affects performance.
