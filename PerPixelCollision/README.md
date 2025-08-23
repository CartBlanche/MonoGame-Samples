# Per Pixel Collision Sample

This is a MonoGame sample that demonstrates per-pixel collision detection between sprites. The game features a person character that can be moved around the screen while blocks fall from the top. When the person touches a block, a collision is detected using per-pixel accuracy rather than simple bounding box collision.


## Project Structure

```
PerPixelCollisionSample/
│
├── Core/
│   ├── Game1.cs
│   ├── Content/
│   ├── GameThumbnail.png
│   └── PerPixelCollisionSample.Core.csproj
│
├── Platforms/
│   ├── Windows/
│   │   ├── PerPixelCollisionSample.Windows.csproj
│   │   └── app.manifest
│   ├── Desktop/
│   │   └── PerPixelCollisionSample.DesktopGL.csproj
│   ├── Android/
│   │   ├── PerPixelCollisionSample.Android.csproj
│   │   ├── Program.cs
│   │   ├── AndroidManifest.xml
│   │   └── Resources/
│   └── iOS/
│       ├── PerPixelCollisionSample.iOS.csproj
│       ├── Program.cs
│       └── Info.plist
│
├── PerPixelCollisionSample.sln
├── README.md
└── .vscode/
```

**Platforms:**
- **Windows** (`Platforms/Windows/PerPixelCollisionSample.Windows.csproj`) - Uses MonoGame.Framework.WindowsDX
- **DesktopGL** (`Platforms/Desktop/PerPixelCollisionSample.DesktopGL.csproj`) - Cross-platform OpenGL version
- **iOS** (`Platforms/iOS/PerPixelCollisionSample.iOS.csproj`) - iOS version targeting net8.0-ios
- **Android** (`Platforms/Android/PerPixelCollisionSample.Android.csproj`) - Android version targeting net8.0-android

## Building and Running

### Prerequisites

- .NET 8.0 SDK or later
- For iOS development: Xcode and iOS development tools
- For Android development: Android SDK and development tools

### Building from Command Line


#### Windows
```bash
dotnet build Platforms/Windows/PerPixelCollisionSample.Windows.csproj
dotnet run --project Platforms/Windows/PerPixelCollisionSample.Windows.csproj
```

#### DesktopGL (Cross-platform)
```bash
dotnet build Platforms/Desktop/PerPixelCollisionSample.DesktopGL.csproj
dotnet run --project Platforms/Desktop/PerPixelCollisionSample.DesktopGL.csproj
```

#### Android
```bash
dotnet build Platforms/Android/PerPixelCollisionSample.Android.csproj
```

#### iOS
```bash
dotnet build Platforms/iOS/PerPixelCollisionSample.iOS.csproj
```

### Building with Visual Studio

Open `PerPixelCollisionSample.sln` in Visual Studio and build/run the desired platform target.

### Building with VS Code

This project includes VS Code configuration files:

1. **Build Tasks**: Use Ctrl+Shift+P → "Tasks: Run Task" and select:
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version
   - `build-ios` - Build iOS version

2. **Run/Debug**: Use F5 or the Debug panel to run:
   - "Launch Windows" - Run Windows version
   - "Launch DesktopGL" - Run DesktopGL version
   - "Launch Android" - Run Android version
   - "Launch iOS" - Run iOS version

## Game Controls

- **Arrow Keys** or **WASD** - Move the person character around the screen
- **Escape** - Exit the game

## Technical Details

### Per-Pixel Collision

The sample demonstrates how to implement per-pixel collision detection by:

1. Loading texture color data into arrays
2. Transforming sprite rectangles based on position
3. Checking overlapping pixels for transparency
4. Only registering a collision when non-transparent pixels overlap

### Content Pipeline

The project uses pre-built `.xnb` content files located in the `Content/` folder:
- `Block.xnb` - The falling block texture
- `Person.xnb` - The player character texture

These files are automatically copied to the output directory and loaded at runtime.

## Platform-Specific Notes

### Windows
- Uses DirectX graphics backend
- Includes application manifest for DPI awareness

### DesktopGL
- Uses OpenGL graphics backend
- Runs on Windows, macOS, and Linux

### iOS
- Requires iOS 11.0 or later
- Configured for landscape orientation
- Uses the iOS-specific Program.cs in `Platforms/iOS/`

### Android
- Requires Android API level 21 or later
- Uses Android-specific Activity in `Platforms/Android/`
- Includes AndroidManifest.xml configuration

## Dependencies

All platforms use MonoGame 3.8.* NuGet packages:
- Windows: `MonoGame.Framework.WindowsDX`
- DesktopGL: `MonoGame.Framework.DesktopGL`
- iOS: `MonoGame.Framework.iOS`
- Android: `MonoGame.Framework.Android`
