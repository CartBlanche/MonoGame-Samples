# NetRumble

NetRumble is a classic multiplayer space combat game built with MonoGame 3.8.4. Players control spaceships in an asteroid field, engaging in combat with lasers, rockets, and mines while collecting power-ups.

## Overview

This is a modernized version of the NetRumble sample from the XNA Game Studio era, updated to use:
- .NET 8.0 modern SDK-style projects
- MonoGame 3.8.* NuGet packages
- Cross-platform support for Windows, DesktopGL, Android, and iOS

## Features

- Multiplayer space combat
- Asteroid environments with realistic physics
- Various weapons: lasers, rockets, and mines
- Power-ups: double laser, triple laser, and rocket enhancements
- Particle effects and explosions
- Cross-platform networking support

## Supported Platforms

- **Windows** (DirectX) - `NetRumble.Windows.csproj`
- **DesktopGL** (OpenGL - Cross-platform desktop) - `NetRumble.DesktopGL.csproj`  
- **Android** - `NetRumble.Android.csproj`
- **iOS** - `NetRumble.iOS.csproj`

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- For Android development: Android SDK and Android 13.0 (API 33) or later
- For iOS development: macOS with Xcode and iOS 11.0 or later

## Building and Running

### From Visual Studio Code

1. Open the project folder in VS Code
2. Use the Command Palette (Ctrl+Shift+P) and run:
   - `Tasks: Run Task` → `build-windows` to build for Windows
   - `Tasks: Run Task` → `build-desktopgl` to build for DesktopGL
   - `Tasks: Run Task` → `run-windows` to run the Windows version
   - `Tasks: Run Task` → `run-desktopgl` to run the DesktopGL version

### From Command Line

#### Windows Platform
```powershell
dotnet build NetRumble.Windows.csproj
dotnet run --project NetRumble.Windows.csproj
```

#### DesktopGL Platform  
```powershell
dotnet build NetRumble.DesktopGL.csproj
dotnet run --project NetRumble.DesktopGL.csproj
```

#### Android Platform
```powershell
dotnet build NetRumble.Android.csproj
# For deployment to device/emulator, use your preferred Android deployment method
```

### From Visual Studio

1. Open `NetRumble.sln` in Visual Studio 2022
2. Select your target platform from the dropdown
3. Build and run using F5 or Ctrl+F5


## Project Structure

The project is organized as follows:

- **Core/**
  - Main source code for game logic, rendering, screens, and managers
  - Subfolders:
    - **Gameplay/**: Ships, weapons, power-ups, asteroids, world logic
    - **ScreenManager/**: Menu and gameplay screen management
    - **Screens/**: Individual screens (main menu, lobby, gameplay, etc.)
    - **Rendering/**: Starfield, particles, and rendering utilities
    - **BloomPostprocess/**: Bloom post-processing effects
    - **Content/**: Pre-built game assets (.xnb, textures, audio, fonts)

- **Platforms/**
  - Platform-specific project files:
    - **Windows/**: `NetRumble.Windows.csproj` (DirectX)
    - **Desktop/**: `NetRumble.DesktopGL.csproj` (OpenGL)
    - **Android/**: `NetRumble.Android.csproj` (Android)
    - **iOS/**: `NetRumble.iOS.csproj` (iOS)

- **NetRumble.sln**: Solution file for Visual Studio
- **README.md**: Project documentation

## Content Pipeline

This project uses pre-built XNB content files located in the `Content/` directory. No Content.mgcb file is required as all assets are already processed and ready for use.

## Networking

The game supports network multiplayer using the included networking components. Players can host and join games over local networks.

## Development Notes

- Uses modern .NET 8.0 SDK-style project files
- MonoGame 3.8.* packages provide cross-platform compatibility
- No legacy XNA Framework dependencies
- Content files are pre-processed .xnb files for immediate use
- Cross-platform input handling for different device types

## License

This project is based on the original Microsoft XNA Community Game Platform sample and is provided for educational and demonstration purposes.
