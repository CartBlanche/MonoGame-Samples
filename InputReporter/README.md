# InputReporter - MonoGame Sample

A MonoGame 3.8.4 sample project that displays live input values for all connected game controllers. This project demonstrates how to handle gamepad input, display controller capabilities, and provide an interactive interface for testing controller features.

## Features

- Real-time display of gamepad input values (buttons, triggers, thumbsticks)
- Support for up to 4 connected controllers
- Controller capability detection and display
- Dead zone configuration and visualization
- Cross-platform support for Windows, DesktopGL, and Android

## Project Structure

This project has been modernized to use .NET 8.0 SDK-style projects with MonoGame 3.8.4 NuGet packages:

- `InputReporter.Windows.csproj` - Windows DirectX version (net8.0-windows)
- `InputReporter.DesktopGL.csproj` - Cross-platform OpenGL version (net8.0)
- `InputReporter.Android.csproj` - Android version (net8.0-android)

## Solution Files

Due to .NET SDK limitations with multi-targeting across different platforms, separate solution files are provided:

- `InputReporter.sln` - Main solution with DesktopGL (cross-platform)
- `InputReporter.Windows.sln` - Windows-specific solution
- `InputReporter.Android.sln` - Android-specific solution

## Building and Running

### Prerequisites

- .NET 8.0 SDK or later
- For Android: Android SDK and workload (`dotnet workload install android`)

### Building

#### Main Cross-Platform Version (Recommended)
```bash
dotnet build InputReporter.sln
```

#### Specific Platform
```bash
# Windows
dotnet build InputReporter.Windows.sln

# DesktopGL (Cross-platform)
dotnet build InputReporter.sln

# Android
dotnet build InputReporter.Android.sln
```

#### Individual Projects
```bash
# Windows
dotnet build InputReporter.Windows.csproj

# DesktopGL
dotnet build InputReporter.DesktopGL.csproj

# Android
dotnet build InputReporter.Android.csproj
```

### Running

#### DesktopGL (Recommended - Cross-platform)
```bash
dotnet run --project InputReporter.DesktopGL.csproj
```

#### Windows
```bash
dotnet run --project InputReporter.Windows.csproj
```

#### Android
Deploy to device or emulator using Visual Studio or command line tools:
```bash
dotnet build InputReporter.Android.csproj -t:Run
```

### Visual Studio Code

This project includes VS Code configuration for building and debugging:

1. Open the folder in VS Code
2. Use `Ctrl+Shift+P` and run "Tasks: Run Task"
3. Select the appropriate build/run task:
   - `build-desktopgl` - Build cross-platform version
   - `build-windows` - Build Windows version
   - `build-android` - Build Android version
   - `run-desktopgl` - Run cross-platform version
   - `run-windows` - Run Windows version
4. Use F5 to debug with the configured launch configurations

### Visual Studio

Open the appropriate solution file in Visual Studio:
- `InputReporter.sln` for cross-platform development
- `InputReporter.Windows.sln` for Windows-specific development
- `InputReporter.Android.sln` for Android development

## Controls

- **DPAD/Left Stick**: Navigate between connected controllers
- **A Button**: Toggle dead zone settings
- **B Button**: Exit application
- **Right Trigger**: Charge exit switch (hold to exit)

## Content

The project uses pre-built XNB content files located in the `Content` folder. No Content Pipeline build step is required as the project has been configured to use existing compiled assets.

## Platform Notes

### DesktopGL (Recommended)
- Uses MonoGame.Framework.DesktopGL
- Cross-platform (Windows, Linux, macOS)
- Requires OpenGL support
- Most compatible across different systems

### Windows
- Uses MonoGame.Framework.WindowsDX
- Windows-only with DirectX support
- Better performance on Windows systems

### Android
- Uses MonoGame.Framework.Android
- Minimum Android API level 21
- Includes AndroidManifest.xml configuration
- Content files are included as Android assets
- Requires Android development workload

## Dependencies

- MonoGame.Framework 3.8.*
- MonoGame.Content.Builder.Task 3.8.*
- .NET 8.0

## Modernization Notes

This project has been updated from the original MonoGame sample to use:
- .NET 8.0 SDK-style projects
- MonoGame 3.8.4 NuGet packages
- Modern project structure with platform-specific entry points
- Removed legacy #region directives for cleaner code
- VS Code integration with tasks and launch configurations

## License

This sample is provided as-is for educational and reference purposes.
