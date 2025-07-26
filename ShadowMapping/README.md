# ShadowMapping MonoGame Sample

This project demonstrates basic shadow mapping from a directional light using MonoGame 3.8.* and .NET 8.0. The solution is organized for modern cross-platform development, with each platform using its own SDK-style .csproj file.

## Project Structure
- **Platforms/Windows/ShadowMapping.Windows.csproj**: Windows-specific project (DirectX, `net8.0-windows`)
- **Platforms/DesktopGL/ShadowMapping.DesktopGL.csproj**: Cross-platform OpenGL project (`net8.0`)
- **Platforms/Android/ShadowMapping.Android.csproj**: Android project (`net8.0-android`)
- **Platforms/iOS/ShadowMapping.iOS.csproj**: iOS project (`net8.0-ios`)
- **Core/**: Shared game logic (`ShadowMapping.cs`)
- **Content/**: Pre-built .xnb assets used directly by the game

## How to Build & Run

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
- [MonoGame 3.8.*](https://www.monogame.net/)
- For Android/iOS: Android/iOS SDK & emulator/device

### Windows & DesktopGL (VSCode)
1. Open the folder in VSCode.
2. Use the provided tasks to build for Windows or DesktopGL:
   - `Build Windows` or `Build DesktopGL`
3. Use the provided tasks to run Windows or DesktopGL:
   - `Run Windows` or `Run DesktopGL`

### Visual Studio
1. Open `ShadowMapping.sln`.
2. Set the desired platform project as startup (Windows, DesktopGL, Android, or iOS).
3. Build and run.

### Android/iOS
1. Open the respective project in Visual Studio or use `dotnet build`.
2. Deploy to an emulator or device.

## Notes
- All platform projects use SDK-style .csproj files; legacy `AssemblyInfo.cs` is no longer used.
- No Content.mgcb file is used; .xnb files are loaded directly.
- Platform-specific code is minimized and organized into subdirectories.
- Only Windows, DesktopGL, Android, and iOS are supported. Linux, MacOS, and PSMobile are not supported in MonoGame 3.8.*.
