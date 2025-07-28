# TexturedQuad MonoGame Sample

This project demonstrates a cross-platform textured quad using MonoGame 3.8.x and .NET 8.0. The codebase is organized for modern multiplatform development, with a shared Core project and platform-specific launchers.

## Project Structure

- **Core/**: Shared game logic (`Game1.cs`, `Quad.cs`).
- **Platforms/Windows/**: Windows-specific entry point and project file.
- **Platforms/DesktopGL/**: DesktopGL (cross-platform) entry point and project file.
- **Platforms/Android/**: Android entry point, manifest, and project file.
- **Platforms/iOS/**: iOS entry point, Info.plist, and project file.
- **Content/**: Prebuilt .xnb assets used directly by the game.

## Requirements
- .NET 8.0 SDK or newer
- MonoGame 3.8.x NuGet packages (restored automatically)
- Visual Studio 2022+ or VSCode (with C# Dev Kit recommended)

## Building & Running

### Visual Studio
- Open `TexturedQuad.sln`.
- Set the desired platform project as startup (e.g., Windows or DesktopGL).
- Build and run (F5).

### VSCode
- Use the built-in tasks and launch configurations:
    - Press `Ctrl+Shift+B` to build (choose Windows or DesktopGL).
    - Press `F5` to debug (choose Windows or DesktopGL from the debug menu).

### Command Line
- Build: `dotnet build Platforms/Windows/TexturedQuad.Windows.csproj -c Debug`
- Run:   `dotnet run --project Platforms/Windows/TexturedQuad.Windows.csproj -c Debug`
- Replace `Windows` with `DesktopGL` for cross-platform.

## Android & iOS
- Android and iOS projects are included and follow the same shared code structure.
- You may need to open the solution in Visual Studio 2022+ (Windows or Mac) with Xamarin/MAUI workloads installed to build and deploy to devices or emulators.

## Notes
- No `Content.mgcb` file is used; the game loads prebuilt `.xnb` files directly from the `Content/` directory.
- Platform-specific code is minimized; all game logic is in the Core project.
- If you add new content, use the MonoGame Pipeline Tool to build `.xnb` files and place them in `Content/`.

## License
This sample is provided for educational purposes and is based on the original Microsoft XNA Community Game Platform samples.
