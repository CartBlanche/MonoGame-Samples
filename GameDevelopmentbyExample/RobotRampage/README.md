# Robot Rampage - MonoGame 3.8.4 Project

This is a modernized version of the Robot Rampage game from the XNA Game Development by Example book, updated to use MonoGame 3.8.4 and .NET 8.0.

Originally from the Book "XNA 4.0 Game Development by Example: Beginner's Guide" by Kurt Jaegers
Published by PACKT Publishing: http://www.packtpub.com/xna-4-0-game-development-by-example-beginners-guide/book

## Project Structure

The project has been converted to use modern SDK-style project files and supports multiple platforms:

- **RobotRampage.DesktopGL.csproj** - Cross-platform desktop version using OpenGL
- **RobotRampage.Windows.csproj** - Windows-specific version using DirectX
- **RobotRampage.Android.csproj** - Android version
- **RobotRampage.iOS.csproj** - iOS version

## Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 (recommended) or Visual Studio Code
- For Android development: Android SDK
- For iOS development: Xcode (macOS only)

## Building

### Using Visual Studio
Open `RobotRampage.sln` in Visual Studio and build the desired project.

### Using .NET CLI
```bash
# Build Windows version
dotnet build RobotRampage.Windows.csproj

# Build DesktopGL version
dotnet build RobotRampage.DesktopGL.csproj

# Build Android version
dotnet build RobotRampage.Android.csproj
```

### Using Visual Studio Code
Open the folder in VS Code and use the following tasks:
- `Ctrl+Shift+P` → "Tasks: Run Task" → Choose build task
- Available tasks: build-windows, build-desktopgl, build-android

## Running

### Using .NET CLI
```bash
# Run Windows version
dotnet run --project RobotRampage.Windows.csproj

# Run DesktopGL version
dotnet run --project RobotRampage.DesktopGL.csproj
```

### Using Visual Studio Code
- `F5` to debug with Launch DesktopGL or Launch Windows configurations
- `Ctrl+F5` to run without debugging

## Content Pipeline

The project uses existing compiled content (.xnb files) from the Content folder. No additional content pipeline setup is required as the .xnb files are directly copied to the output directory.

## Changes Made

1. **Removed all #region/#endregion directives** for cleaner, modern code
2. **Updated to SDK-style project files** using .NET 8.0 target frameworks
3. **Added MonoGame 3.8.* NuGet package references** for all platforms
4. **Fixed namespace consistency** (changed from Robot_Rampage to RobotRampage)
5. **Removed obsolete XNA references** (like GamerServices)
6. **Added VSCode configuration files** for building and debugging
7. **Updated Program.cs** to modern .NET style without platform-specific conditionals

## Platform-Specific Notes

### Windows
- Uses MonoGame.Framework.WindowsDX
- Supports both windowed and fullscreen modes
- Requires Windows 10 or later

### DesktopGL
- Uses MonoGame.Framework.DesktopGL
- Cross-platform compatible (Windows, Linux, macOS)
- Uses OpenGL for rendering

### Android
- Uses MonoGame.Framework.Android
- Minimum SDK version: 21 (Android 5.0)
- Target SDK version: 34 (Android 14)
- Includes AndroidManifest.xml configuration

### iOS
- Uses MonoGame.Framework.iOS
- Minimum iOS version: 11.0
- Requires Xcode and macOS for building

## Troubleshooting

1. **Content Warning**: The "No Content References Found" warning is expected since we're using pre-compiled .xnb files instead of a .mgcb content pipeline project.

2. **Build Errors**: Ensure you have the correct .NET 8.0 SDK installed and all required workloads for your target platform.

3. **Android Build Issues**: Make sure you have the Android SDK installed and configured properly in Visual Studio.

## Game Controls

- Arrow keys or WASD: Move player
- Mouse: Aim turret
- Left click: Fire weapon
- Escape: Exit game

## License

This code is based on the examples from "XNA Game Development by Example" and has been modernized for educational purposes.

