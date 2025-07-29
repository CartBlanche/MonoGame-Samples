# Game State Management Sample

This project demonstrates a cross-platform game state management system using MonoGame. It supports Windows (DirectX), DesktopGL, Android, and iOS platforms, with a shared core game logic project.

## Project Structure

- **/Core**: Shared game logic and screens
- **/Platforms/Windows**: Windows (DirectX) entry point and project
- **/Platforms/Desktop**: DesktopGL entry point and project
- **/Platforms/Android**: Android entry point and project
- **/Platforms/iOS**: iOS entry point and project

## Building and Running

### Prerequisites
- .NET 8 SDK or later
- MonoGame 3.8+ (NuGet packages are referenced in each platform project)
- Platform-specific build tools (e.g., Android/iOS SDKs for mobile targets)

### Windows (DirectX)
```
dotnet build Platforms/Windows/GameStateManagement.Windows.csproj
```

### DesktopGL
```
dotnet build Platforms/Desktop/GameStateManagement.DesktopGL.csproj
```

### Android
```
dotnet build Platforms/Android/GameStateManagement.Android.csproj
```

### iOS
```
dotnet build Platforms/iOS/GameStateManagement.iOS.csproj
```

## Notes
- All platform projects reference the shared core logic in `/Core`.
- Content pipeline warnings may appear if no MonoGame content is referenced. Add a `.mgcb` file if you need custom content.
- For Android/iOS, ensure you have the required SDKs and emulators/simulators installed.

## License
This sample is based on the Microsoft XNA Community Game Platform samples.
