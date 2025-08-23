# RenderTarget2D Sample

This is a MonoGame 3.8 sample that demonstrates how to use RenderTarget2D to render graphics to a texture and then manipulate that texture before drawing it to the screen. This sample showcases render-to-texture techniques that are commonly used for post-processing effects, screen capture, and dynamic texture generation.

## Project Overview

The RenderTarget2D sample creates a scene with textures (checker pattern and logo), renders them to a render target, and then displays the result on the screen with various transformations and effects.

## Supported Platforms

This project has been modernized to use .NET 8.0 and MonoGame 3.8.* with support for the following platforms:

- **Windows** (`net8.0-windows`) - DirectX backend
- **DesktopGL** (`net8.0`) - OpenGL backend for cross-platform desktop
- **Android** (`net8.0-android`) - Mobile Android devices
- **iOS** (`net8.0-ios`) - Mobile iOS devices

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- For Android development: Android SDK with API Level 21+ (Android 5.0)
- For iOS development: Xcode and iOS 11.0+ support
- Visual Studio 2022 or Visual Studio Code with C# extension

## Building and Running

### Using Visual Studio

1. Open `RenderTarget2DSample.sln` in Visual Studio 2022
2. Select your target platform project as the startup project
3. Build and run (F5)

### Using Visual Studio Code

1. Open the project folder in VS Code
2. Use the Command Palette (Ctrl+Shift+P) and run "Tasks: Run Task"
3. Choose from available tasks:
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version
   - `run-windows` - Build and run Windows version
   - `run-desktopgl` - Build and run DesktopGL version

### Using Command Line

#### Windows DirectX Version
```bash
dotnet build RenderTarget2D.Windows.csproj
dotnet run --project RenderTarget2D.Windows.csproj
```

#### DesktopGL Version (Cross-platform)
```bash
dotnet build RenderTarget2D.DesktopGL.csproj
dotnet run --project RenderTarget2D.DesktopGL.csproj
```

#### Android Version
```bash
dotnet build RenderTarget2D.Android.csproj
# Deploy to connected Android device/emulator
dotnet publish RenderTarget2D.Android.csproj -f net8.0-android
```

#### iOS Version
```bash
dotnet build RenderTarget2D.iOS.csproj
# Requires Xcode and Mac for deployment
```

## Project Structure

- `Game1.cs` - Main game class with render target logic
- `Program.cs` - Entry points for different platforms
- `MainActivity.cs` - Android-specific activity
- `Content/` - Game assets (.xnb files)
- `Properties/` - Platform-specific manifests and settings
- `Resources/` - Android-specific resources

## Content Pipeline

This project uses pre-compiled .xnb content files located in the `../CompiledContent/` directory. The content includes:
- `checker.xnb` - Checker pattern texture
- `logo.xnb` - Logo texture

The project does **not** include a Content.mgcb file and uses the existing compiled content directly.

## Key Features Demonstrated

- **RenderTarget2D Creation** - How to create and configure render targets
- **Render-to-Texture** - Drawing graphics to a texture instead of the screen
- **Texture Manipulation** - Using rendered textures as input for further rendering
- **Cross-Platform Compatibility** - Single codebase supporting multiple platforms

## MonoGame 3.8 Features Used

- Modern .NET 8.0 target frameworks
- NuGet package references for MonoGame framework
- Platform-specific optimizations
- Updated graphics pipeline

## Troubleshooting

### Missing Content Files
If you get content loading errors, ensure that the `../CompiledContent/` directory exists with the required .xnb files for your target platform.

### Build Errors
- Ensure you have the correct .NET 8.0 SDK installed
- For Android: Verify Android SDK and build tools are properly configured
- For iOS: Ensure Xcode and iOS development tools are installed (Mac only)

### Performance Issues
- Try the DesktopGL version if Windows DirectX version has issues
- Ensure your graphics drivers are up to date

## License

This sample is part of the MonoGame samples collection and follows the same licensing terms as MonoGame.
