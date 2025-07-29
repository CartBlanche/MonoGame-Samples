# MonoGame Orientation Sample

This is a MonoGame 3.8.4 sample project that demonstrates orientation handling in games across multiple platforms. The sample shows how to handle different screen orientations and scaling in MonoGame applications.

## Project Overview

This sample demonstrates:
- Orientation handling (Portrait/Landscape)
- Dynamic orientation locking/unlocking
- Cross-platform compatibility
- Proper scaling and layout management

## Supported Platforms

- **Windows** (DirectX) - `net8.0-windows`
- **DesktopGL** (OpenGL) - `net8.0`
- **Android** - `net8.0-android`
- **iOS** - `net8.0-ios`

## Prerequisites

- .NET 8.0 SDK or later
- For Android development: Android SDK with API level 21+
- For iOS development: Xcode and iOS 11.0+
- Visual Studio 2022 or Visual Studio Code

## Project Structure

```
/Core
    MonoGame.Orientation.Core.csproj
    OrientationSample.cs
    LayoutSample.cs
/Platforms
    /Windows
        Orientation.Windows.csproj
        Program.cs
    /Desktop
        Orientation.DesktopGL.csproj
        Program.cs
    /Android
        Orientation.Android.csproj
        MainActivity.cs
    /iOS
        Orientation.iOS.csproj
        AppDelegate.cs
        Main.cs
Assets/Content/    # Game content files (.xnb format)
Resources/         # Android resources
Properties/        # Platform-specific configuration files
```

## Building the Project

### Using Visual Studio

1. Open `Orientation.sln` in Visual Studio 2022
2. Select the target platform from the dropdown
3. Build and run the project

### Using Visual Studio Code

1. Open the project folder in VS Code
2. Use the provided tasks to build and run:
   - `Ctrl+Shift+P` → "Tasks: Run Task"
   - Select from available tasks:
     - "build-windows" - Build Windows DirectX version
     - "build-desktopgl" - Build cross-platform OpenGL version
     - "build-android" - Build Android version
     - "build-ios" - Build iOS version
     - "run-windows" - Build and run Windows version
     - "run-desktopgl" - Build and run DesktopGL version
     - "run-android" - Build and run Android version
     - "run-ios" - Build and run iOS version

### Using Command Line

```bash
# Build Windows version
cd Platforms/Windows
 dotnet build Orientation.Windows.csproj

# Build DesktopGL version
cd ../Desktop
 dotnet build Orientation.DesktopGL.csproj

# Build Android version
cd ../Android
 dotnet build Orientation.Android.csproj

# Build iOS version
cd ../iOS
 dotnet build Orientation.iOS.csproj
```

## Running the Project

### Windows/DesktopGL
Simply run the executable or use `dotnet run --project <csproj>` with the appropriate project file.

### Android
Deploy to an Android device or emulator using Visual Studio or via command line:
```bash
cd Platforms/Android
 dotnet build Orientation.Android.csproj -f net8.0-android
```

### iOS
Deploy to an iOS device or simulator using Visual Studio for Mac or Xcode.

## Content Pipeline

This project uses pre-built XNB content files located in `Assets/Content/`:
- `directions.xnb` - Direction graphics
- `Font.xnb` - Sprite font for text rendering

## Dependencies

- MonoGame Framework 3.8.*
- .NET 8.0

All dependencies are managed via NuGet package references in the project files.

## Troubleshooting

### Build Issues
- Ensure you have .NET 8.0 SDK installed
- Verify platform-specific SDKs are installed (Android SDK, Xcode for iOS)
- Check NuGet package restore completed successfully

### Runtime Issues
- Ensure content files are being copied to output directory
- Check that the appropriate MonoGame runtime is installed
- Verify platform-specific permissions and configurations

## License

This sample is based on Microsoft XNA Community Game Platform samples and is provided for educational purposes.
