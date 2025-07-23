# Primitives Sample - MonoGame 3.8.*

This sample demonstrates how to use the `PrimitiveBatch` class to draw lines and points on screen using MonoGame. The sample recreates a retro-style space scene with stars, spaceships, and a sun, all drawn using primitive shapes.

## Features

- Drawing lines and points using `PrimitiveBatch`
- Dynamic star field generation
- Simple spaceship and sun rendering using line primitives
- Cross-platform support for Windows, DesktopGL, Android, and iOS


## New Project Structure

```
/Core
  Primitives.Core.csproj         # Shared game logic and rendering code
  PrimitivesSampleGame.cs
  PrimitiveBatch.cs

/Platforms/Windows
  Primitives.Windows.csproj      # Windows DirectX entry point
  Program.cs

/Platforms/Desktop
  Primitives.DesktopGL.csproj    # Cross-platform OpenGL entry point
  Program.cs

/Platforms/Android
  Primitives.Android.csproj      # Android entry point
  MainActivity.cs

/Platforms/iOS
  Primitives.iOS.csproj          # iOS entry point
  Program.cs
  AppDelegate.cs
```

All platform projects reference `/Core/Primitives.Core.csproj` for shared code. Platform-specific code and entry points are isolated in their respective directories, reducing the need for `#if` blocks.

All projects use modern .NET SDK-style project files and target .NET 8.0 with MonoGame 3.8.* NuGet packages.

## What's New in This Modernization

This project has been updated from the original MonoGame samples to use modern development practices:

- **SDK-style projects**: All `.csproj` files use the modern SDK format
- **NuGet packages**: Uses MonoGame 3.8.* NuGet packages instead of source references
- **.NET 8.0**: Targets the latest .NET 8.0 framework for each platform
- **Removed legacy platforms**: MacOS (classic) and other deprecated platforms removed
- **VS Code support**: Full debugging and build task support for VS Code
- **Modern Android**: Updated to use modern Android development practices
- **Cross-platform**: Builds on Windows, with Android and iOS support

## Prerequisites

- .NET 8.0 or later
- MonoGame 3.8.* (installed via NuGet)
- For Android development: Android SDK
- For iOS development: Xcode (macOS only)

## Building and Running

### Using Visual Studio

1. Open `Primitives.sln` in Visual Studio
2. Set your desired platform project as the startup project
3. Build and run (F5)

### Using Visual Studio Code

1. Open the folder in VS Code
2. Use Ctrl+Shift+P and run "Tasks: Run Task"
3. Select one of the available tasks:
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version
   - `build-all` - Build all projects
   - `run-windows` - Build and run Windows version
   - `run-desktopgl` - Build and run DesktopGL version
   - `clean` - Clean all build artifacts
   - `restore` - Restore NuGet packages

Alternatively, use F5 to debug the Windows or DesktopGL versions.

### Using Command Line

#### Windows/DesktopGL
```bash
# Build all projects
dotnet build Primitives.sln

# Build specific platform
dotnet build Primitives.Windows.csproj
dotnet build Primitives.DesktopGL.csproj

# Run Windows version
dotnet run --project Primitives.Windows.csproj

# Run DesktopGL version
dotnet run --project Primitives.DesktopGL.csproj
```

#### Android
```bash
# Build Android version
dotnet build Primitives.Android.csproj

# To deploy to device/emulator, use:
dotnet build Primitives.Android.csproj -t:Run
```

#### iOS (macOS only)
```bash
# Build iOS version
dotnet build Primitives.iOS.csproj
```

## Platform-Specific Notes

### Windows
- Uses DirectX for rendering
- Requires Windows with .NET 8.0

### DesktopGL
- Uses OpenGL for rendering
- Cross-platform (Windows, Linux, macOS)
- Best choice for cross-platform desktop development

### Android
- Minimum API level: 21 (Android 5.0)
- Target API level: 34 (Android 14)
- Requires Android SDK for building

### iOS
- Minimum iOS version: 11.0
- Requires macOS with Xcode for building
- Requires Apple Developer account for device deployment

## Code Overview

The main classes in this sample:

- **PrimitivesSampleGame** - Main game class that inherits from `Game`
- **PrimitiveBatch** - Utility class for drawing lines and points efficiently
- **Activity1** (Android) - Android activity entry point
- **AppDelegate** (iOS) - iOS application delegate

## Learning Points

1. **PrimitiveBatch Usage** - Learn how to efficiently draw lines and points
2. **Cross-Platform Development** - See how MonoGame enables multi-platform deployment
3. **Resource Management** - Understand how to manage graphics resources
4. **Game Loop** - Basic game update and draw cycle implementation

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure you have .NET 8.0 installed
2. **Missing MonoGame**: The NuGet packages should restore automatically
3. **Android Build Issues**: Verify Android SDK is properly configured
4. **iOS Build Issues**: Ensure Xcode is installed (macOS only)

### Platform-Specific Issues

- **Windows**: Ensure you have the latest Visual C++ redistributables
- **DesktopGL**: OpenGL drivers must be up to date
- **Android**: Enable Developer Options and USB Debugging on device
- **iOS**: Valid provisioning profile required for device deployment

## Additional Resources

- [MonoGame Documentation](https://docs.monogame.net/)
- [MonoGame Community](https://community.monogame.net/)
- [MonoGame GitHub](https://github.com/MonoGame/MonoGame)

## License

This sample is part of the MonoGame Samples collection and follows the same license terms.
