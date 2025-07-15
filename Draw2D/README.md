# Draw2D MonoGame Sample

This project demonstrates 2D graphics rendering using MonoGame 3.8.* and .NET 8.0 for Windows, DesktopGL, Android, and iOS platforms. It is structured for modern cross-platform development and easy maintenance.

## Project Structure

```
/Core                # Shared game logic and components
/Platforms/Windows   # Windows-specific entry point and project
/Platforms/Desktop   # DesktopGL (cross-platform desktop) entry point and project
/Platforms/Android   # Android entry point and project
/Platforms/iOS       # iOS entry point and project
```

## Building and Running

### Prerequisites
- .NET 8.0 SDK or newer
- MonoGame 3.8.* NuGet packages (restored automatically)
- Visual Studio 2022+ or VS Code
- For Android/iOS: Xamarin/MAUI workloads and platform SDKs

### Windows & DesktopGL
- **Build:**
  - `dotnet build Platforms/Windows/Draw2D.Windows.csproj`
  - `dotnet build Platforms/Desktop/Draw2D.DesktopGL.csproj`
- **Run:**
  - `dotnet run --project Platforms/Windows/Draw2D.Windows.csproj`
  - `dotnet run --project Platforms/Desktop/Draw2D.DesktopGL.csproj`
- **VS Code:**
  - Use the provided `.vscode/tasks.json` and `.vscode/launch.json` for build/run/debug.

### Android & iOS
- **Build:**
  - `dotnet build Platforms/Android/Draw2D.Android.csproj`
  - `dotnet build Platforms/iOS/Draw2D.iOS.csproj`
- **Run:**
  - Deploy using Visual Studio or appropriate device/emulator tools.

## Content
- No `Content.mgcb` file is used. Precompiled `.xnb` files are copied to the output directory for each platform.
- Update content paths in the platform `.csproj` files if you add or move assets.

## Notes
- Windows and DesktopGL are fully supported and tested.
- Android and iOS projects are included but may require additional setup or MonoGame compatibility fixes for .NET 8.0.
- All shared code is in `/Core` and referenced by each platform project.
- Platform-specific code and entry points are isolated in their respective folders.

## License
MIT or as specified by the repository owner.
