# ParticleSample - MonoGame 3.8.* Sample

This is a MonoGame sample project that demonstrates particle systems with explosions, smoke plumes, and various particle effects. The project has been modernized to use SDK-style project files and MonoGame 3.8.* NuGet packages.

## Project Overview

The ParticleSample demonstrates how to create and manage different types of particle systems in MonoGame:

- **Explosion Particle System**: Creates dramatic explosion effects
- **Explosion Smoke Particle System**: Adds smoke effects to explosions  
- **Smoke Plume Particle System**: Creates rising smoke plume effects

Key features:
- Multiple particle system types with different behaviors
- Touch/mouse input for triggering effects
- Optimized particle rendering and management
- Cross-platform compatible code

## Project Structure

The project contains the following main classes:

- `ParticleSampleGame.cs` - Main game class that manages the particle systems
- `ParticleSystem.cs` - Base class for all particle system implementations
- `Particle.cs` - Individual particle data structure
- `ExplosionParticleSystem.cs` - Explosion particle effects
- `ExplosionSmokeParticleSystem.cs` - Smoke effects for explosions
- `SmokePlumeParticleSystem.cs` - Rising smoke plume effects
- `Program.cs` - Entry point with platform-specific initialization

## Supported Platforms

This project supports the following platforms with MonoGame 3.8.*:

- **Windows** (.NET 8.0) - Uses DirectX backend
- **DesktopGL** (.NET 8.0) - Cross-platform OpenGL backend
- **Android** (.NET 8.0) - Mobile Android devices
- **iOS** (.NET 8.0) - iOS devices (requires macOS for building)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- For Android: Android SDK and Java Development Kit
- For iOS: Xcode and macOS (building iOS requires a Mac)

## Building the Project

### Using Visual Studio

1. Open `ParticleSample.sln` in Visual Studio 2022
2. Select your target platform (Windows, DesktopGL, Android, or iOS)
3. Build the solution (Ctrl+Shift+B)
4. Run the project (F5)

### Using Visual Studio Code

1. Open the project folder in VS Code
2. Install the C# extension if not already installed
3. Use the Command Palette (Ctrl+Shift+P) and run "Tasks: Run Task"
4. Select one of the build tasks:
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version

### Using Command Line

#### Windows Platform
```powershell
dotnet build ParticleSample.Windows.csproj -c Debug
dotnet run --project ParticleSample.Windows.csproj -c Debug
```

## Directory Structure
```
ParticleSample/
├── Core/                # Shared game logic and particle system code
│   └── ParticleSample.Core.csproj
├── Platforms/
│   ├── Windows/         # Windows-specific entry point and project
│   │   └── ParticleSample.Windows.csproj
│   ├── Desktop/         # DesktopGL (OpenGL) entry point and project
│   │   └── ParticleSample.DesktopGL.csproj
│   ├── Android/         # Android entry point and project
│   │   └── ParticleSample.Android.csproj
│   └── iOS/             # iOS entry point and project
│       └── ParticleSample.iOS.csproj
├── Content/             # Game assets (images, fonts, etc.)
├── .vscode/             # VS Code tasks and launch configs
└── ParticleSample.sln   # Solution file referencing all projects
```
#### DesktopGL Platform (Cross-platform)
```powershell
dotnet build ParticleSample.DesktopGL.csproj -c Debug
dotnet run --project ParticleSample.DesktopGL.csproj -c Debug
```

#### Android Platform
```powershell
dotnet build ParticleSample.Android.csproj -c Debug
# Deploy to connected Android device or emulator
dotnet run --project ParticleSample.Android.csproj -c Debug
```

#### iOS Platform (requires macOS)
```bash
dotnet build ParticleSample.iOS.csproj -c Debug
# Deploy to connected iOS device or simulator
dotnet run --project ParticleSample.iOS.csproj -c Debug
```

## Running the Project

### From Visual Studio Code

Use the Debug panel (Ctrl+Shift+D) and select:
- "Launch Windows" - Run the Windows DirectX version
- "Launch DesktopGL" - Run the cross-platform OpenGL version

### Controls

- **Mouse/Touch**: Click or tap anywhere on the screen to trigger particle effects
- **Multiple Effects**: Different click locations may trigger different particle systems
- **Real-time Rendering**: Watch as particles are created, animated, and fade away

## Content Pipeline

This project uses pre-built .xnb content files located in the `Content/` directory:
- `explosion.xnb` - Explosion particle texture
- `smoke.xnb` - Smoke particle texture  
- `font.xnb` - Font for text rendering

The project does not include a Content.mgcb file as it directly uses the compiled .xnb files.

## Platform-Specific Notes

### Windows
- Uses DirectX 11 backend
- Requires Windows 7 SP1 or later
- Best performance on Windows systems

### DesktopGL  
- Uses OpenGL backend
- Runs on Windows, Linux, and macOS
- Slightly lower performance than DirectX on Windows
- Better compatibility across different systems

### Android
- Minimum API level 21 (Android 5.0)
- Requires Android SDK for building
- Touch input optimized for mobile devices

### iOS
- Minimum iOS version 11.0
- Requires Xcode and macOS for building
- Touch input optimized for mobile devices

## Troubleshooting

### Build Issues
- Ensure .NET 8.0 SDK is installed
- Run `dotnet restore` to restore NuGet packages
- Clean and rebuild if encountering cache issues

### Content Pipeline Warnings
The projects may show warnings about "No Content References Found" - this is expected since we're using pre-built .xnb files directly instead of a Content.mgcb pipeline.

### Platform-Specific Issues
- **Android**: 
  - Ensure Android SDK is properly configured
  - May require Android SDK API level 34 for full compatibility
  - The manifest is configured for basic functionality
- **iOS**: Requires valid Apple Developer account for device deployment
- **DesktopGL**: May require additional OpenGL drivers on some systems
- **Windows**: May show DPI-related warnings which can be ignored for this sample

## Project Tasks (VSCode)

When using VSCode, you can use the following tasks:
- **build-windows**: Build the Windows DirectX version
- **build-desktopgl**: Build the cross-platform DesktopGL version  
- **build-android**: Build the Android version
- **run-windows**: Build and run the Windows version
- **run-desktopgl**: Build and run the DesktopGL version

Use Ctrl+Shift+P → "Tasks: Run Task" to access these tasks.

## License

This sample is based on Microsoft XNA Community Game Platform samples and is provided for educational purposes.
