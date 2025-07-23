# Peer2Peer Sample - MonoGame 3.8.4

A MonoGame sample demonstrating peer-to-peer multiplayer networking using a simple tank game. This project showcases how to implement network sessions with multiple players sharing game state in real-time.

## Project Overview

This sample implements a multiplayer tank game where players can:
- Join or create network sessions
- Control tanks with keyboard/gamepad input
- See other players' tanks in real-time
- Experience synchronized gameplay across multiple devices

The project uses the Lidgren.Network library for peer-to-peer networking and demonstrates MonoGame's networking capabilities.

## New Project Structure

```
Peer2PeerSample/
├── Core/                       # Shared game logic and classes
│   ├── Peer2PeerSample.Core.csproj
│   ├── PeerToPeerGame.cs
│   └── ... (other shared files)
├── Platforms/
│   ├── Windows/
│   │   ├── Peer2PeerSample.Windows.csproj
│   │   └── Program.cs
│   ├── Desktop/
│   │   ├── Peer2PeerSample.DesktopGL.csproj
│   │   └── Program.cs
│   ├── Android/
│   │   ├── Peer2Peer.Android.csproj
│   │   └── MainActivity.cs
│   └── iOS/
│       ├── Peer2Peer.iOS.csproj
│       ├── Program.cs
│       └── AppDelegate.cs
├── Content/                    # Pre-built .xnb assets
├── Resources/                  # Android resources
├── Properties/                 # AndroidManifest.xml, etc.
├── README.md
└── Peer2PeerSample.sln         # Solution file referencing all projects
```

## Supported Platforms
- **Windows** (.NET 8.0-windows with DirectX)
- **DesktopGL** (.NET 8.0 with OpenGL - cross-platform)
- **Android** (.NET 8.0-android, minimum API 21)
- **iOS** (.NET 8.0-ios, minimum iOS 11.0)

## Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- For Android: Android SDK and emulator/device
- For iOS: Xcode and iOS device/simulator (macOS only)

## Building the Project

### Using Visual Studio
1. Open `Peer2PeerSample.sln`
2. Select your target platform (Windows, DesktopGL, Android, or iOS)
3. Build and run (F5)

### Using VS Code
1. Open the project folder in VS Code
2. Use Ctrl+Shift+P and run "Tasks: Run Task"
3. Choose from available tasks:
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version
   - `run-windows` - Build and run Windows version
   - `run-desktopgl` - Build and run DesktopGL version

### Using Command Line

```bash
# Build Windows version
dotnet build Peer2PeerSample.Windows.csproj

# Build DesktopGL version  
dotnet build Peer2PeerSample.DesktopGL.csproj

# Build Android version
dotnet build Peer2Peer.Android.csproj

# Run Windows version
dotnet run --project Peer2PeerSample.Windows.csproj

# Run DesktopGL version
dotnet run --project Peer2PeerSample.DesktopGL.csproj
```

## Content Pipeline

This project uses pre-built .xnb content files located in the `Content/` folder:
- `Font.xnb` - Sprite font for UI text
- `Tank.xnb` - Tank texture
- `Turret.xnb` - Tank turret texture
- `gamepad.png` - Virtual gamepad for mobile platforms

No Content.mgcb file is needed as the project uses the existing compiled content directly.

## Project Structure

```
├── PeerToPeerGame.cs          # Main game class
├── Program.cs                 # Platform-specific entry points
├── Activity1.cs               # Android activity
├── Tank.cs                    # Tank game object
├── Content/                   # Game assets (.xnb files)
├── Properties/                # Platform manifests
│   └── AndroidManifest.xml
├── Resources/                 # Android resources
├── Info.plist                 # iOS app info
└── *.csproj                   # Platform-specific projects
```

## Key Dependencies

- **MonoGame.Framework.DesktopGL** 3.8.* - Cross-platform OpenGL
- **MonoGame.Framework.WindowsDX** 3.8.* - Windows DirectX
- **MonoGame.Framework.Android** 3.8.* - Android support
- **MonoGame.Framework.iOS** 3.8.* - iOS support
- **Lidgren.Network** 1.0.2 - Peer-to-peer networking

## Controls

- **Keyboard**: Arrow keys or WASD to move tank
- **Gamepad**: Left stick to move, right stick to aim
- **Mobile**: Touch virtual gamepad controls

## Networking

The game creates or joins network sessions automatically. Multiple instances can be run on the same machine or across different devices on the same network to test multiplayer functionality.

## Troubleshooting

- Ensure firewall allows the application through for networking
- For Android, ensure Internet permission is granted
- For iOS, ensure network permissions in Info.plist
- Use DesktopGL version for best cross-platform compatibility
