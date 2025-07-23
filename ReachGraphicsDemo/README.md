# Reach Graphics Demo - MonoGame 3.8.4

This project demonstrates various graphics techniques using MonoGame 3.8.4, including alpha blending, dual texturing, environment mapping, particle systems, and skinned animation.

## Project Structure

The solution contains multiple platform-specific projects:

- **Platforms/Windows/ReachGraphicsDemo.Windows.csproj** - Windows DirectX platform (net8.0-windows)
- **Platforms/DesktopGL/ReachGraphicsDemo.DesktopGL.csproj** - Cross-platform OpenGL (net8.0)
- **Platforms/Android/ReachGraphicsDemo.Android.csproj** - Android platform (net8.0-android)
- **Platforms/iOS/ReachGraphicsDemo.iOS.csproj** - iOS platform (net8.0-ios)
- **Core/ReachGraphicsDemo.Core.csproj** - Shared game logic and assets

## Features

- **Alpha Demo**: Demonstrates alpha blending and transparency effects
- **Basic Demo**: Shows basic 3D rendering techniques
- **Dual Demo**: Illustrates dual texturing and multi-pass rendering
- **Environment Mapping Demo**: Shows environment/reflection mapping
- **Particle Demo**: Demonstrates particle system effects
- **Skinned Demo**: Shows animated character with bone-based animation

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code with C# extension
- For Android: Android SDK and workload (`dotnet workload install android`)
- For iOS: Xcode and workload (`dotnet workload install ios`)

## Building the Project

### Using Visual Studio

1. Open `ReachGraphicsDemo.sln` in Visual Studio 2022
2. Select your target project (Windows, DesktopGL, Android, or iOS) under the `Platforms/` directory
3. Build and run (F5)

### Using .NET CLI

**Windows:**
```powershell
cd Platforms/Windows
 dotnet build ReachGraphicsDemo.Windows.csproj
 dotnet run --project ReachGraphicsDemo.Windows.csproj
```

**DesktopGL (Cross-platform):**
```powershell
cd Platforms/DesktopGL
 dotnet build ReachGraphicsDemo.DesktopGL.csproj
 dotnet run --project ReachGraphicsDemo.DesktopGL.csproj
```

**Android:**
```powershell
cd Platforms/Android
 dotnet build ReachGraphicsDemo.Android.csproj
```

**iOS:**
```powershell
cd Platforms/iOS
 dotnet build ReachGraphicsDemo.iOS.csproj
```

### Using VS Code

1. Open the project folder in VS Code
2. Use Ctrl+Shift+P to open the command palette
3. Run tasks:
   - "build-windows" - Build Windows version
   - "build-desktopgl" - Build DesktopGL version
   - "build-android" - Build Android version
   - "run-windows" - Build and run Windows version
   - "run-desktopgl" - Build and run DesktopGL version

## Platform-Specific Notes

### Windows
- Uses DirectX backend
- Requires Windows 10 or later
- Hardware acceleration recommended

### DesktopGL
- Uses OpenGL backend
- Cross-platform (Windows, Linux, macOS)
- Requires OpenGL 3.0+ support

### Android
- Minimum API level 21 (Android 5.0)
- Uses OpenGL ES 2.0
- AndroidManifest.xml configured for landscape orientation

### iOS
- Minimum iOS 10.0
- Uses OpenGL ES 2.0
- Info.plist configured for landscape orientation

## Content Pipeline

This project uses pre-compiled XNB content files located in the `Core/Content/` directory. The content includes:

- Fonts (BigFont.xnb, font.xnb)
- Textures (various .xnb files)
- 3D Models (model.xnb, tank.xnb, etc.)
- Audio and other assets

## Controls

- Navigate menus using keyboard or touch input
- Each demo has specific controls displayed on screen
- ESC to return to main menu

## Troubleshooting

### Build Issues
- Ensure .NET 8.0 SDK is installed
- Restore NuGet packages: `dotnet restore`
- Clean and rebuild: `dotnet clean && dotnet build`

### Missing Workloads
```powershell
# Install Android workload
dotnet workload install android

# Install iOS workload (macOS only)
dotnet workload install ios
```

### Graphics Issues
- Ensure graphics drivers are up to date
- For DesktopGL: Verify OpenGL 3.0+ support
- For Android: Check OpenGL ES 2.0 support

## License

This project is based on Microsoft XNA Community Game Platform samples. See individual source files for license information.
