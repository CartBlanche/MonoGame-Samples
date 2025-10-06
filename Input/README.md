# MonoGame Input Sample

This project demonstrates input handling in MonoGame 3.8.4, including keyboard, gamepad, and touch input across multiple platforms.

## Project Overview

The Input sample showcases:
- Keyboard input detection
- GamePad controller input
- Touch input with gesture recognition (tap and double-tap)
- Cross-platform input handling
- Real-time display of input coordinates and screen information

## Supported Platforms

- **Windows** (.NET 8.0 with Windows Forms)
- **DesktopGL** (.NET 8.0 cross-platform)
- **Android** (.NET 8.0 Android)
- **iOS** (.NET 8.0 iOS)

## Building the Project

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- For Android: Android SDK with API level 21 or higher
- For iOS: Xcode and iOS SDK 11.0 or higher

### Using Visual Studio

1. Open `Input.sln` in Visual Studio
2. Select your target platform from the solution configuration
3. Build and run the project

### Using VS Code

1. Open the project folder in VS Code
2. Use Ctrl+Shift+P and run "Tasks: Run Task"
3. Choose from:
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version
   - `build-solution` - Build entire solution

### Using Command Line

#### Build Windows version:
```bash
dotnet build Input.Windows.csproj
```

#### Build DesktopGL version:
```bash
dotnet build Input.DesktopGL.csproj
```

#### Build Android version:
```bash
dotnet build Input.Android.csproj
```

#### Build entire solution:
```bash
dotnet build Input.sln
```

## Running the Project

### Windows/DesktopGL
```bash
dotnet run --project Input.Windows.csproj
# or
dotnet run --project Input.DesktopGL.csproj
```

### Using VS Code Debug
- Press F5 to launch with debugger
- Choose "Launch Windows" or "Launch DesktopGL" configuration

## Project Structure

- `Game1.cs` - Main game class with input handling logic
- `Program.Windows.cs` - Windows platform entry point
- `Program.DesktopGL.cs` - DesktopGL platform entry point
- `Program.iOS.cs` - iOS platform entry point
- `MainActivity.cs` - Android activity
- `Content/` - Game assets (sprite fonts, textures)
- `Properties/AndroidManifest.xml` - Android app configuration
- `Info.iOS.plist` - iOS app configuration

## Features Demonstrated

1. **Touch Input**: Tap to change background color, double-tap for different color
2. **Keyboard Input**: Real-time keyboard state detection
3. **GamePad Input**: Controller input handling with back button exit
4. **Screen Information**: Display of screen dimensions and orientation
5. **Multi-touch**: Support for multiple simultaneous touch points

## Technical Details

- Built with MonoGame 3.8.4 framework
- Uses SDK-style project files for modern .NET development
- Target framework: .NET 8.0 for all platforms
- Content pipeline: Uses pre-built .xnb files directly
- Cross-platform input abstraction through MonoGame APIs

## Notes

- The project uses existing compiled content (.xnb files) instead of a content pipeline
- Android minimum SDK version: API 21 (Android 5.0)
- iOS minimum version: 11.0
- All platforms support landscape and portrait orientations
