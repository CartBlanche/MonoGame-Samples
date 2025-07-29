
# Tetris (MonoGame 3.8.4, .NET 8)

## Project Summary
This is a cross-platform Tetris clone built with MonoGame 3.8.4 and .NET 8.0. It supports Windows, DesktopGL (cross-platform desktop), Android, and iOS. The game features classic Tetris gameplay, persistent high scores, and platform-specific launchers. All core game logic and assets are shared across platforms.


## Project Structure
- **Core/**: Shared game logic and assets (Content.mgcb, TetrisGame.cs, Board.cs, etc.)
- **Platforms/Windows/**: Windows-specific entry point and project (`Tetris.Windows.csproj`)
- **Platforms/DesktopGL/**: DesktopGL (cross-platform desktop) entry point and project (`Tetris.DesktopGL.csproj`)
- **Platforms/Android/**: Android entry point, manifest, and project (`Tetris.Android.csproj`)
- **Platforms/iOS/**: iOS entry point, Info.plist, and project (`Tetris.iOS.csproj`)


## Building and Running

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MonoGame 3.8.4](https://www.monogame.net/downloads/)
- For Android/iOS: Xamarin/MAUI workloads and platform SDKs


### Windows
Build:
```
dotnet build Platforms/Windows/Tetris.Windows.csproj
```
Run:
```
dotnet run --project Platforms/Windows/Tetris.Windows.csproj
```

### DesktopGL
Build:
```
dotnet build Platforms/DesktopGL/Tetris.DesktopGL.csproj
```
Run:
```
dotnet run --project Platforms/DesktopGL/Tetris.DesktopGL.csproj
```

### Android
Build:
```
dotnet build Platforms/Android/Tetris.Android.csproj
```
Deploy/run using your preferred IDE or CLI tools (Visual Studio, Rider, etc.).

### iOS
Build:
```
dotnet build Platforms/iOS/Tetris.iOS.csproj
```
Deploy/run using your preferred IDE or CLI tools (Visual Studio for Mac, Rider, etc.).

### VSCode
- Use the provided `.vscode/tasks.json` for building Windows and DesktopGL targets.


## Content
- The shared `Core/Content/Content.mgcb` is referenced by all platform projects and builds platform-specific `.xnb` files automatically.


## Notes
- Linux, MacOS, and PSMobile are not supported in MonoGame 3.8.* and are not included.
- All platform-specific code is isolated in its respective directory.
- Shared code is in `Core/` and referenced by all platforms.
