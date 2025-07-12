# BatteryStatus MonoGame Sample

This project demonstrates a cross-platform MonoGame 3.8.* sample for displaying battery and power status information. The codebase is structured for .NET 8 and supports Windows, DesktopGL, Android, and iOS platforms.

## Directory Structure

```
/Core                # Shared game logic (Game1, PowerStatus, etc.)
/Platforms/Windows   # Windows-specific entry point and implementation
/Platforms/DesktopGL # DesktopGL-specific entry point and implementation
/Platforms/Android   # Android-specific entry point and implementation
/Platforms/iOS       # iOS-specific entry point and implementation
/Content             # Pre-built .xnb content files
```

## Building and Running

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022+ or VSCode
- MonoGame 3.8.* NuGet packages (restored automatically)

### Windows & DesktopGL
- Open the solution (`BatteryStatus.sln`) in Visual Studio and build/run the desired platform project.
- Or, in VSCode, use the provided tasks and launch configurations:
  - Press `Ctrl+Shift+B` to build (`build-windows` or `build-desktopgl`).
  - Press `F5` to run/debug (`Windows` or `DesktopGL`).

### Android & iOS
- Open the solution in Visual Studio 2022+ (with Xamarin/MAUI workloads installed) and build/deploy the respective platform project.

## Notes
- No `Content.mgcb` is used; the project uses pre-built `.xnb` files directly.
- Platform-specific code is separated to avoid `#if` blocks.
- Battery status is only implemented for Windows; other platforms return stub values.

## License
MIT or as specified by CartBlanche.
