# Sound MonoGame Sample

This project demonstrates a cross-platform MonoGame 3.8.* sample for playing sound and using pre-built .xnb content. It supports Windows, DesktopGL, Android, and iOS using .NET 8.0 platform-specific SDK-style projects.

**Project Modernization (2025):**
- All projects are now SDK-style and use .NET 8.0 TFMs.
- Platform-specific code and entry points are separated into `Platforms/` subdirectories.
- All MonoGame references are via NuGet (3.8.*).
- No Content.mgcb file is used; .xnb files are loaded directly.
- Only Windows, DesktopGL, Android, and iOS are supported (Linux, MacOS, and PSMobile are not).
- VSCode tasks/launch configurations are provided for Windows and DesktopGL.

## Project Structure

```
README.md
Sound.sln
Core/
    SoundSample.cs
    Content/
        Content.mgcb
        Explosion1.mp3
        Font.spritefont
Platforms/
    Android/
        AndroidManifest.xml
        MainActivity.cs
        Sound.Android.csproj
        Resources/
            Resource.Designer.cs
            Drawable/
                icon.png
                splash.png
            Values/
                Styles.xml
    DesktopGL/
        Program.cs
        Sound.DesktopGL.csproj
    iOS/
        Info.plist
        Program.cs
        Sound.iOS.csproj
    Windows/
        Program.cs
        Sound.Windows.csproj
```

- `Core/Content/`: Pre-built .xnb and audio files, plus the content project file (`Content.mgcb`).
- `Platforms/Windows`, `Platforms/DesktopGL`, `Platforms/Android`, `Platforms/iOS`: Platform-specific projects and entry points.
- Shared game logic is in `Core/SoundSample.cs`.

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022+ or VSCode
- MonoGame 3.8.* NuGet packages (restored automatically)

### Windows & DesktopGL (VSCode)
1. Open the folder in VSCode.
2. Use the built-in tasks (Ctrl+Shift+B) to build:
   - `build-windows` or `build-desktopgl`
3. Use the Run/Debug menu to launch:
   - `Windows` or `DesktopGL` configuration
4. Ensure all required .xnb files are present in the `Core/Content/` directory. If you see a runtime error about missing content, you must add or rebuild the missing .xnb files.

### Windows & DesktopGL (Visual Studio)
1. Open `Sound.sln` in Visual Studio.
2. Select the desired platform project and build/run as usual.

### Android & iOS
- Open the respective project in Visual Studio 2022+ (with Xamarin/MAUI workloads installed) and deploy to device/emulator.
- For Android, ensure all referenced resources (including styles and icons) are present and named in lowercase under `Platforms/Android/Resources/`.

## Notes
- No Content.mgcb file is used at runtime; .xnb files are loaded directly from `Core/Content/`.
- Platform-specific code is separated into subdirectories to minimize `#if` blocks.
- Only Windows, DesktopGL, Android, and iOS are supported (Linux, MacOS, and PSMobile are not).
- If you encounter missing content errors at runtime, check the `Core/Content/` directory for the required .xnb files.

## License
Copyright © 2011
