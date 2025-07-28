# VirtualGamePad MonoGame Sample

This project demonstrates a cross-platform virtual gamepad using MonoGame 3.8.* and .NET 8.0. The codebase is structured for maximum code sharing and minimal platform-specific code, supporting Windows, DesktopGL, Android, and iOS.

## Directory Structure

- **/Core**: Shared game logic (Game1, etc.)
- **/Platforms/Windows**: WindowsDX entry point and project
- **/Platforms/DesktopGL**: DesktopGL entry point and project
- **/Platforms/Android**: Android entry point, manifest, and project
- **/Platforms/iOS**: iOS entry point, Info.plist, and project
- **/Core/Content**: Font and Texture assets
- **/.vscode**: VSCode tasks and launch configurations

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- MonoGame 3.8.* NuGet packages (restored automatically)
- Visual Studio 2022+ or VSCode

### Windows & DesktopGL (VSCode)
1. Open the folder in VSCode.
2. Use the built-in tasks (Ctrl+Shift+B) to build for Windows or DesktopGL.
3. Use the Run/Debug menu to launch the desired platform.

### Windows & DesktopGL (Visual Studio)
1. Open `VirtualGamePad.sln`.
2. Set the desired platform project as startup.
3. Build and run.

### Android/iOS
- Open the solution in Visual Studio 2022+ (Windows or Mac) with Xamarin/MAUI workloads installed.
- Set the Android or iOS project as startup and deploy to a device or emulator.

## Notes
- All shared code is in `/Core` and referenced by each platform project.
- Platform-specific entry points are minimal and located in `/Platforms/{Platform}`.
- No Content.mgcb file is used; .xnb files are loaded directly from `/Core/Content`.
- Unsupported platforms (Linux, MacOS, PSMobile) are not included.

---

For any issues, please check your MonoGame and .NET SDK installations, and ensure all NuGet packages restore successfully.
