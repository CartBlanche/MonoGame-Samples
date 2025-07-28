# MonoGame Storage Sample

This repository demonstrates cross-platform save game and storage management using MonoGame. It includes implementations for Windows, DesktopGL, Android, and iOS platforms.

## Project Structure
- `Core/` - Shared game logic and storage code.
- `Platforms/Windows/` - Windows-specific project.
- `Platforms/DesktopGL/` - DesktopGL (cross-platform desktop) project.
- `Platforms/Android/` - Android-specific project.
- `Platforms/iOS/` - iOS-specific project.

## Building

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- MonoGame (see platform-specific requirements)
- Visual Studio 2022 or VS Code recommended

### Windows
```
dotnet build Platforms/Windows/Storage.Windows.csproj
```

### DesktopGL
```
dotnet build Platforms/DesktopGL/Storage.DesktopGL.csproj
```

### Android
```
dotnet build Platforms/Android/Storage.Android.csproj
```

### iOS
```
dotnet build Platforms/iOS/Storage.iOS.csproj
```

## Running
You can use the provided VS Code tasks and launch configurations for each platform. See `.vscode/tasks.json` and `.vscode/launch.json` for details.

## Notes
- Each platform project builds its own executable or app package.
- Shared logic is in the `Core` project.
- For mobile platforms, ensure you have the required SDKs and emulators installed.

## License
MIT
