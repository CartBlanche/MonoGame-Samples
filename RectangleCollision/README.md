# Rectangle Collision Sample

This is a MonoGame 3.8.* sample project that demonstrates rectangle collision detection. The game features a player character that must avoid falling blocks in a simple 2D environment.

## Project Description

The Rectangle Collision Sample showcases:
- Basic 2D sprite rendering
- Rectangle collision detection
- Player input handling (keyboard)
- Random block spawning
- Game state management (collision detection triggers)

The player controls a character that can move left and right using the keyboard. Blocks fall from the top of the screen, and the game detects when the player collides with any block.

## Project Status

✅ **Windows** - Building and running successfully  
✅ **DesktopGL** - Building and running successfully  
✅ **Android** - Building successfully (requires Android SDK to deploy)  
⚠️ **iOS** - Project created (requires Mac + Xcode to build and test)

## Building and Running

### Prerequisites

- .NET 8.0 SDK or later
- For iOS: Xcode and Mac development environment
- For Android: Android SDK


### Directory Structure

```
RectangleCollisionSample/
│
├── Core/
│   ├── Game1.cs
│   ├── Program.Core.cs
│   ├── RectangleCollisionSample.Core.csproj
│   └── Content/
│       ├── Block.xnb
│       └── Person.xnb
│
├── Platforms/
│   ├── Windows/
│   │   ├── Program.cs
│   │   ├── RectangleCollisionSample.Windows.csproj
│   │   └── app.manifest
│   ├── Desktop/
│   │   ├── Program.cs
│   │   └── RectangleCollisionSample.DesktopGL.csproj
│   ├── Android/
│   │   ├── MainActivity.cs
│   │   ├── RectangleCollisionSample.Android.csproj
│   │   └── AndroidManifest.xml
│   └── iOS/
│       ├── Program.cs
│       ├── RectangleCollisionSample.iOS.csproj
│       └── Info.plist
│
├── .vscode/
│   ├── launch.json
│   └── tasks.json
│
├── RectangleCollisionSample.sln
└── README.md
```

### Building and Running

#### Using Visual Studio
1. Open `RectangleCollisionSample.sln` in Visual Studio
2. Select the desired project under `Platforms/` (Windows, Desktop, Android, iOS) as startup project
3. Build and run (F5)

#### Using VS Code
1. Open the project folder in VS Code
2. Use Ctrl+Shift+P to open command palette
3. Run "Tasks: Run Task" and select:
   - `build-windows`, `build-desktopgl`, or `build-android` to build
   - `run-windows` or `run-desktopgl` to run
4. Or use F5 to debug with the configured launch profiles

#### Using Command Line
```bash
# Build Windows version
dotnet build Platforms/Windows/RectangleCollisionSample.Windows.csproj

# Run Windows version
dotnet run --project Platforms/Windows/RectangleCollisionSample.Windows.csproj

# Build DesktopGL version
dotnet build Platforms/Desktop/RectangleCollisionSample.DesktopGL.csproj

# Run DesktopGL version
dotnet run --project Platforms/Desktop/RectangleCollisionSample.DesktopGL.csproj

# Build Android version
dotnet build Platforms/Android/RectangleCollisionSample.Android.csproj

# Build iOS version (on Mac)
dotnet build Platforms/iOS/RectangleCollisionSample.iOS.csproj
```

## Game Controls

- **Left Arrow / A**: Move player left
- **Right Arrow / D**: Move player right
- **Escape**: Exit game

## Content Files

The project uses pre-built XNB content files located in the `Content/` folder:
- `Person.xnb` - Player character sprite
- `Block.xnb` - Falling block sprite

These files are automatically copied to the output directory during build.

## Architecture Notes

- Uses MonoGame 3.8.* NuGet packages instead of source references
- Modern SDK-style project files for all platforms
- Shared source code across all platforms
- Platform-specific configurations and manifests where needed
- No Content Pipeline (.mgcb) file - uses pre-built XNB files directly

## Dependencies

- MonoGame.Framework.Windows 3.8.*
- MonoGame.Framework.DesktopGL 3.8.*
- MonoGame.Framework.iOS 3.8.*
- MonoGame.Framework.Android 3.8.*
- MonoGame.Content.Builder.Task 3.8.*
