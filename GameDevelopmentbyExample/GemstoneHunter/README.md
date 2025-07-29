# Gemstone Hunter

Gemstone Hunter is a cross-platform 2D platformer game built with MonoGame 3.8.* and .NET 8.0. The project is organized for modern .NET SDK-style development, supporting Windows (DX), DesktopGL, Android, and iOS platforms.

## Project Structure

- **/Core**: Shared game logic and code, referenced by all platforms.
- **/Platforms/Windows**: Windows-specific project using MonoGame.Framework.WindowsDX.
- **/Platforms/Desktop**: DesktopGL project using MonoGame.Framework.DesktopGL.
- **/Platforms/Android**: Android project using MonoGame.Framework.Android.
- **/Platforms/iOS**: iOS project using MonoGame.Framework.iOS.

## Building and Running

### Prerequisites
- .NET 8.0 SDK or newer
- MonoGame 3.8.* NuGet packages (restored automatically)
- Visual Studio 2022+ (for Windows/iOS/Android) or VSCode (all platforms)

### Windows
```
dotnet build Platforms/Windows/GemstoneHunter.Windows.csproj
```
Run:
```
dotnet run --project Platforms/Windows/GemstoneHunter.Windows.csproj
```

### DesktopGL
```
dotnet build Platforms/Desktop/GemstoneHunter.DesktopGL.csproj
```
Run:
```
dotnet run --project Platforms/Desktop/GemstoneHunter.DesktopGL.csproj
```

### Android
```
dotnet build Platforms/Android/GemstoneHunter.Android.csproj
```

### iOS
```
dotnet build Platforms/iOS/GemstoneHunter.iOS.csproj
```

## Content
This project uses pre-built .xnb files in the `Content/` directory. No Content.mgcb file is required.

## VSCode Usage
- Use the provided `.vscode/tasks.json` and `.vscode/launch.json` for building and running Windows and DesktopGL targets.

## Solution File
Open `GemstoneHunter.sln` in Visual Studio for full IDE support.

---

For more details, see the source code and platform-specific project files.

