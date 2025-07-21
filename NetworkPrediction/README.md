# NetworkPrediction - MonoGame Sample

A MonoGame sample demonstrating network prediction and smoothing techniques to compensate for network latency and low packet send rates.

## Project Overview

This sample shows how to use prediction and smoothing to compensate for the effects of network latency, and for the low packet send rates needed to conserve network bandwidth. The project has been modernized to use:

- .NET 8.0 SDK-style projects
- MonoGame 3.8.* NuGet packages
- NetRumble MonoGame.Xna.Framework.Net library for full XNA 4.0 networking compatibility

## Platform Support


The project includes support for multiple platforms, each with its own folder and entry point:

- **Windows** (`Platforms/Windows/NetworkPrediction.Windows.csproj`, `Program.cs`) - Uses DirectX backend
- **DesktopGL** (`Platforms/Desktop/NetworkPrediction.DesktopGL.csproj`, `Program.cs`) - Cross-platform OpenGL backend
- **Android** (`Platforms/Android/NetworkPrediction.Android.csproj`, `Program.cs`) - Android mobile platform
- **iOS** (`Platforms/iOS/NetworkPrediction.iOS.csproj`, `Program.cs`) - iOS mobile platform

All platform projects reference the shared core logic in `Core/NetworkPrediction.Core.csproj`.

**Folder Structure:**

```
NetworkPrediction.sln
README.md
Core/
    NetworkPrediction.Core.csproj
    NetworkPredictionGame.cs
    RollingAverage.cs
    Tank.cs
    Content/
Platforms/
    Windows/
        NetworkPrediction.Windows.csproj
        Program.cs
    Desktop/
        NetworkPrediction.DesktopGL.csproj
        Program.cs
    Android/
        NetworkPrediction.Android.csproj
        Program.cs
    iOS/
        NetworkPrediction.iOS.csproj
        Program.cs
```

## Current Status

✅ **Completed:**
- Modernized all project files to SDK-style format
- Updated to use MonoGame 3.8.* NuGet packages
- Integrated NetRumble MonoGame.Xna.Framework.Net for full XNA 4.0 networking compatibility
- Consolidated all XNA networking APIs - no code duplication
- Created VS Code tasks and launch configurations
- Removed obsolete macOS project (no longer supported in MonoGame 3.8.*)
- Updated solution file with new project structure
- Fixed all critical build errors and platform compatibility issues
- All platforms build successfully (Windows, DesktopGL, Android, iOS)

✅ **XNA 4.0 API Compatibility:**
- Full Microsoft.Xna.Framework.GamerServices support (SignedInGamer, Guide, etc.)
- Full Microsoft.Xna.Framework.Net support (NetworkSession, NetworkGamer, PacketReader/Writer, etc.)
- All networking APIs match XNA 4.0 specifications
- Shared implementation eliminates duplicate code

⚠️ **Minor Notes:**
- Content pipeline warnings (no .mgcb file, using prebuilt .xnb files - this is intentional)
- iOS platform compatibility warnings (CA1416 - informational only, builds successfully)
- Game.Exit() disabled on iOS per platform policy

## Building the Project

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension

### Command Line Build

```powershell
# Build Windows version
dotnet build NetworkPrediction.Windows.csproj

# Build DesktopGL version
dotnet build NetworkPrediction.DesktopGL.csproj

# Build Android version
dotnet build NetworkPrediction.Android.csproj

# Build iOS version
dotnet build NetworkPrediction.iOS.csproj

# Build all projects
dotnet build NetworkPrediction.sln
```

### Visual Studio

Open `NetworkPrediction.sln` in Visual Studio and build the solution.

### VS Code

Use the Command Palette (Ctrl+Shift+P) and run:
- `Tasks: Run Task` → `build-windows` or `build-desktopgl`
- `Debug: Start Debugging` → Choose "Launch Windows" or "Launch DesktopGL"

## Running the Project

### Windows
```powershell
dotnet run --project NetworkPrediction.Windows.csproj
```

### DesktopGL (Cross-platform)
```powershell
dotnet run --project NetworkPrediction.DesktopGL.csproj
```

### Android & iOS
Use platform-specific deployment tools (Android Studio, Xcode) or dotnet publish commands for mobile platforms.

## Content

The project uses pre-built XNB content files located in the `Content/` directory:
- `Font.xnb` - Sprite font for UI text
- `Tank.xnb` - Tank sprite texture
- `Turret.xnb` - Turret sprite texture

## Controls

- **Arrow Keys / WASD** - Move tank
- **Mouse / Right Stick** - Aim turret
- **A** - Create network session
- **B** - Join network session
- **Escape** - Exit game

## Networking

The project uses the NetRumble MonoGame.Xna.Framework.Net library which provides a fully compatible implementation of the Microsoft.Xna.Framework.Net and Microsoft.Xna.Framework.GamerServices namespaces. This shared implementation:

- Maintains full XNA 4.0 API compatibility
- Eliminates code duplication across projects  
- Provides all required networking classes (NetworkSession, NetworkGamer, PacketReader/Writer, etc.)
- Includes gamer services functionality (SignedInGamer, Guide, etc.)

## Architecture

The project follows a consolidated architecture:
- **NetworkPrediction** - Main game sample (this project)
- **NetRumble\MonoGame.Xna.Framework.Net** - Shared XNA 4.0 networking implementation
- No duplicate networking code between projects

## Contributing

To contribute to this project:

1. Test networking functionality in real multiplayer scenarios
2. Add additional platform-specific optimizations
3. Enhance the prediction algorithms
4. Add more comprehensive error handling

## License

This project is based on the original Microsoft XNA Community Game Platform samples and follows the same licensing terms.
