# GameComponents MonoGame Sample

This project demonstrates a cross-platform MonoGame 3.8.* sample, structured for modern .NET 8+ development. It supports Windows, DesktopGL, Android, and iOS platforms, using a clean separation of shared and platform-specific code.

## Project Structure

```
/Core                # Shared game logic and components
/Platforms/Windows   # Windows-specific entry point and project
/Platforms/Desktop   # DesktopGL-specific entry point and project
/Platforms/Android   # Android-specific entry point, manifest, and project
/Platforms/iOS       # iOS-specific entry point, Info.plist, and project
/Content             # Pre-built .xnb content files
```

## Building and Running

### Prerequisites
- .NET 8 SDK or newer
- Visual Studio 2022+ (for Android/iOS) or VSCode
- MonoGame 3.8.* NuGet packages (restored automatically)
- Android/iOS build tools for mobile platforms

### With Visual Studio
1. Open `GameComponents.sln`.
2. Select the desired platform project (Windows, DesktopGL, Android, or iOS).
3. Build and run as usual.

### With VSCode
1. Open the root folder in VSCode.
2. Use the provided tasks (see below) to build or run each platform:
   - **Windows**: `dotnet run --project Platforms/Windows/GameComponents.Windows.csproj`
   - **DesktopGL**: `dotnet run --project Platforms/Desktop/GameComponents.DesktopGL.csproj`
   - **Android**: `dotnet build Platforms/Android/GameComponents.Android.csproj`
   - **iOS**: `dotnet build Platforms/iOS/GameComponents.iOS.csproj`

### Content
- Uses pre-built `.xnb` files in `/Content`.
- No `Content.mgcb` is required.

## Notes
- All shared code is in `/Core` and referenced by each platform project.
- Platform-specific code and entry points are in their respective `/Platforms/*` directories.
- Only Windows, DesktopGL, Android, and iOS are supported (no Linux, MacOS, or PSMobile).

## Troubleshooting
- Ensure all required SDKs and build tools are installed for your target platform.
- If you encounter build errors, run `dotnet restore` first.

---

For more details, see the individual project files and platform directories.
