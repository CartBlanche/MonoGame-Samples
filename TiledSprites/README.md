# TiledSprites MonoGame Sample

This project demonstrates a variety of common sprite operations using MonoGame 3.8.* and .NET 8.0. It is organized for cross-platform builds with a shared Core library and platform-specific projects.

## Structure

## Folder Structure

```
TiledSprites.sln
README.md
Core/
  AnimatedSprite.cs
  Camera2D.cs
  SpriteSheet.cs
  TiledSprites.Core.csproj
  TiledSprites.cs
  TileGrid.cs
  Content/
    ball.png
    clouds.png
    Content.mgcb
    Default.png
    font.spritefont
    GameThumbnail.png
    ground.png
    bin/
      DesktopGL/Content/
      Windows/Content/
  bin/
    Debug/net8.0/
  obj/
    Debug/net8.0/
Platforms/
  Windows/
    Program.cs
    TiledSprites.Windows.csproj
    bin/
    obj/
  DesktopGL/
    Program.cs
    TiledSprites.DesktopGL.csproj
    bin/
    obj/
  Android/
    AndroidManifest.xml
    MainActivity.cs
    TiledSprites.Android.csproj
    bin/
    obj/
  iOS/
    Info.plist
    Program.cs
    TiledSprites.icns
    TiledSprites.iOS.csproj
    bin/
    obj/
```

**Core/**: Shared game logic, engine code, and assets.
**Core/Content/**: Game assets (sprites, fonts, etc.) and content pipeline files.
**Platforms/**: Platform-specific entry points and projects for Windows, DesktopGL, Android, and iOS.
**bin/** and **obj/**: Build output and intermediate files for each project.

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- MonoGame 3.8.* NuGet packages (restored automatically)
- Visual Studio 2022+ or VSCode (with C# Dev Kit recommended)

### Windows
- Build: `dotnet build Platforms/Windows/TiledSprites.Windows.csproj`
- Run: `dotnet run --project Platforms/Windows/TiledSprites.Windows.csproj`

### DesktopGL
- Build: `dotnet build Platforms/DesktopGL/TiledSprites.DesktopGL.csproj`
- Run: `dotnet run --project Platforms/DesktopGL/TiledSprites.DesktopGL.csproj`

### Android
- Build: `dotnet build Platforms/Android/TiledSprites.Android.csproj`
- Deploy/run using your preferred Android tooling.

### iOS
- Build: `dotnet build Platforms/iOS/TiledSprites.iOS.csproj`
- Deploy/run using your preferred iOS tooling (e.g., Visual Studio for Mac, Xcode integration).

## Notes
- Content is loaded from prebuilt .xnb files in the `Content/` directory.
- No Content.mgcb file is used; assets are referenced directly.
- Platform-specific code is separated to minimize `#if` blocks.

---

For more details, see the source code in each directory.
