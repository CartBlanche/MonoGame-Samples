# LensFlare MonoGame Sample

This is a MonoGame 3.8.4 sample that demonstrates how to implement a lens flare effect using occlusion queries to hide the flares when the sun is hidden behind the landscape.

## Features

- **Lens Flare Effect**: Visual lens flare effects that appear when looking at the sun
- **Occlusion Queries**: Smart occlusion detection to hide flares when the sun is blocked by terrain
- **3D Terrain**: Sample 3D terrain rendering
- **Cross-Platform**: Supports multiple platforms through MonoGame

## Supported Platforms

- **Windows** (DirectX)
- **DesktopGL** (OpenGL - Windows, Linux, macOS)
- **Android** (OpenGL ES)
- **iOS** (OpenGL ES)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- For Android development: Android SDK with API level 21+
- For iOS development: Xcode and iOS SDK 11.0+

## Building the Project

### Using Visual Studio

1. Open `LensFlare.sln` in Visual Studio
2. Select the desired platform project (Windows, DesktopGL, Android, or iOS)
3. Build and run using F5

### Using Visual Studio Code

1. Open the project folder in VS Code
2. Use Ctrl+Shift+P and run tasks:
   - **Build Windows**: `Tasks: Run Task` → `build-windows`
   - **Build DesktopGL**: `Tasks: Run Task` → `build-desktopgl`
   - **Build Android**: `Tasks: Run Task` → `build-android`
   - **Run Windows**: `Tasks: Run Task` → `run-windows`
   - **Run DesktopGL**: `Tasks: Run Task` → `run-desktopgl`

3. For debugging, use F5 or Ctrl+F5 to launch the configured debug sessions

### Using Command Line

#### Windows Platform
```powershell
dotnet build LensFlare.Windows.csproj
dotnet run --project LensFlare.Windows.csproj
```

#### DesktopGL Platform
```powershell
dotnet build LensFlare.DesktopGL.csproj
dotnet run --project LensFlare.DesktopGL.csproj
```

#### Android Platform
```powershell
dotnet build LensFlare.Android.csproj
# Deploy to connected Android device/emulator:
dotnet publish LensFlare.Android.csproj -f net8.0-android
```

#### iOS Platform
```powershell
dotnet build LensFlare.iOS.csproj
# Deploy requires Xcode and connected iOS device or simulator
```

## Controls

- **Arrow Keys / WASD**: Move camera around the scene
- **Mouse**: Look around (if mouse controls are implemented)
- **Gamepad**: Use gamepad for movement and camera control


## Project Structure

```
LensFlare/
├── Core/                   # Shared game logic and components
│   ├── Game.cs             # Main game class
│   ├── LensFlareComponent.cs # Lens flare effect implementation
│   └── Content/            # Shared content (textures, models)
├── Platforms/              # Platform-specific code
│   ├── Windows/            # Windows-specific entry point
│   ├── DesktopGL/          # DesktopGL-specific entry point
│   ├── Android/            # Android-specific code and manifest
│   └── iOS/                # iOS-specific code
├── bin/                    # Build output
├── obj/                    # Build intermediates
├── *.csproj                # Project files
└── LensFlare.sln           # Visual Studio solution file
```

## Technical Details

This sample demonstrates:

- **MonoGame 3.8.4**: Modern SDK-style project format
- **Cross-platform development**: Single codebase, multiple platforms
- **GPU Occlusion Queries**: Hardware-accelerated visibility testing
- **3D Graphics**: Model loading, texturing, and rendering
- **Component-based architecture**: Clean separation of concerns

## License

This sample is based on the original Microsoft XNA Community Game Platform samples and has been updated for MonoGame 3.8.4.

## Contributing

Feel free to contribute improvements, bug fixes, or platform-specific optimizations through pull requests.
