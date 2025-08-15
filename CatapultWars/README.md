# CatapultWars MonoGame Sample

This is a modernized, cross-platform MonoGame 3.8.4 sample project, organized for .NET 8 and Visual Studio/VS Code. It demonstrates a catapult game with support for Windows, DesktopGL, and Android (iOS project included, but not tested).

## Project Structure

```
CatapaultWars/
├── Core/                # Shared game logic, assets, and content
├── Platforms/
│   ├── Windows/         # Windows-specific entry point and project
│   ├── Desktop/         # DesktopGL-specific entry point and project
│   ├── Android/         # Android-specific entry point and project
│   └── iOS/             # iOS-specific entry point and project
├── Content/             # Pre-built .xnb assets (used directly)
├── .vscode/             # VS Code tasks and launch configs
└── README.md            # This file
```

## How to Build and Run

### Prerequisites
- .NET 8 SDK or newer
- MonoGame 3.8.4 NuGet packages (local or public feed)
- Visual Studio 2022+ or VS Code

### Windows
```
dotnet build Platforms/Windows/CatapultWars.Windows.csproj
# To run:
dotnet run --project Platforms/Windows/CatapultWars.Windows.csproj
```

### DesktopGL
```
dotnet build Platforms/Desktop/CatapultWars.DesktopGL.csproj
# To run:
dotnet run --project Platforms/Desktop/CatapultWars.DesktopGL.csproj
```

### Android
```
dotnet build Platforms/Android/CatapultWars.Android.csproj
```

### iOS
```
dotnet build Platforms/iOS/CatapultWars.iOS.csproj
```

### Visual Studio
- Open `CatapaultWars.sln` and set the desired platform project as startup.

### VS Code
- Use the provided `.vscode/tasks.json` and `.vscode/launch.json` for build/run/debug.

## Notes
- All shared code is in `/Core` and referenced by each platform project.
- No Content.mgcb file is used; the game loads pre-built `.xnb` assets directly from `/Core/Content`.
- Android and iOS projects may require additional setup for deployment/emulation.
- If you encounter missing entry point errors, ensure each `/Platforms/[Platform]/Program.cs` exists and is correct for that platform.

## Supported Platforms
- Windows (DirectX)
- DesktopGL (cross-platform)
- Android (entry point: `MainActivity.cs`)
- iOS (Info.plist included, not tested)

---

For any issues, please check the project structure and ensure all dependencies are restored from the correct NuGet source.
