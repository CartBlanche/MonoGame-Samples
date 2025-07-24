# RockRain - MonoGame Sample

RockRain is a classic arcade-style space shooter game built with MonoGame 3.8.4. The player controls a spaceship and must avoid meteors while collecting power-ups to survive as long as possible.

## Features

- Classic arcade gameplay
- Particle explosion effects
- Power-ups and scoring system
- Background music and sound effects
- Multiple platform support

## Supported Platforms


This project has been modernized to use .NET 8.0 SDK-style projects and supports the following platforms:

- **Windows (DirectX)** - `Platforms/Windows/RockRain.Windows.csproj`
- **DesktopGL (Cross-platform)** - `Platforms/Desktop/RockRain.DesktopGL.csproj`
- **Android** - `Platforms/Android/RockRain.Android.csproj`
- **iOS** - `Platforms/iOS/RockRain.iOS.csproj`

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- For Android development: Android SDK and workloads
- For iOS development: Xcode and iOS workloads (macOS only)

## Building and Running

### Windows (DirectX)

```bash
# Build
dotnet build RockRain.Windows.csproj

# Run
dotnet run --project RockRain.Windows.csproj
```

### DesktopGL (Cross-platform)

```bash
# Build  
dotnet build RockRain.DesktopGL.csproj

# Run
dotnet run --project RockRain.DesktopGL.csproj
```

### Android

```bash
# Build
dotnet build RockRain.Android.csproj

# Deploy to device/emulator
dotnet build RockRain.Android.csproj -t:Run
```

### iOS

```bash
# Build
dotnet build RockRain.iOS.csproj

# Deploy to device/simulator (requires macOS and Xcode)
dotnet build RockRain.iOS.csproj -t:Run
```

## Visual Studio Code

This project includes VS Code configuration for building and debugging:

### Tasks Available:
- `build-windows` - Build Windows version
- `build-desktopgl` - Build DesktopGL version  
- `build-android` - Build Android version
- `build-ios` - Build iOS version
- `run-windows` - Run Windows version
- `run-desktopgl` - Run DesktopGL version
- `clean` - Clean all projects
- `restore` - Restore NuGet packages

### Debugging:
- Use `F5` to start debugging
- Choose "Launch Windows (DirectX)" or "Launch DesktopGL" configuration

## Visual Studio

Open `RockRain.sln` in Visual Studio to build and run all platform projects.

## Game Controls

- **Arrow Keys / WASD** - Move spaceship
- **Spacebar** - Fire (if power-up collected)
- **Escape** - Return to menu


## Project Structure

- `/Core/` - Shared game logic and components (Game1, GameScene, Sprite, ParticleSystem, etc.)
- `/Core/RockRain.Core.csproj` - Shared code project referenced by all platforms
- `/Content/` - Game assets (textures, sounds, fonts)
- `/Platforms/Windows/` - Windows-specific entry point and project files
- `/Platforms/Desktop/` - DesktopGL-specific entry point and project files
- `/Platforms/Android/` - Android-specific entry point, manifest, and project files
- `/Platforms/iOS/` - iOS-specific entry point, plist, and project files
- Each platform directory contains its own `Program.cs` (or `MainActivity.cs` for Android) and `RockRain.[Platform].csproj`

## Modernization Notes

This project has been modernized from an older MonoGame version to 3.8.4:

- **Mobile-specific APIs removed**: `GamePad.Visible`, `ButtonDefinition`, `ThumbStickDefinition`, and `Accelerometer` APIs that were specific to older mobile platforms have been commented out
- **Content Pipeline**: Uses pre-compiled .xnb files directly instead of requiring a Content.mgcb file
- **Platform-specific entry points**: Each platform now has its own entry point in `/Platforms/[Platform]/` (e.g., `Program.cs` for Windows/DesktopGL/iOS, `MainActivity.cs` for Android)

## Known Limitations

- **Font Support**: The original XNB font files (menuSmall.xnb, menuLarge.xnb, score.xnb) are incompatible with MonoGame 3.8.4 and have been disabled. Text rendering is currently not functional.
- **Touch/virtual gamepad support**: Has been disabled for compatibility with MonoGame 3.8.4
- **Accelerometer input**: Not available in desktop versions
- **Content Pipeline Warning**: The "No Content References Found" warning is expected since we're using pre-compiled XNB files instead of a Content.mgcb file

## Content Compatibility

To fully restore text rendering functionality, the XNB font files would need to be rebuilt with MonoGame 3.8.4's content pipeline, or the original font source files (.spritefont) would need to be compiled through a proper Content.mgcb file.

## MonoGame Content

This project uses pre-compiled .xnb content files located in the `/Content/` directory. No Content.mgcb file is needed as the content has already been processed.

## License

This is a MonoGame sample project provided for educational purposes.
