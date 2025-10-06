# XNA2DShaderExamples

This project demonstrates the use of 2D shaders in MonoGame 3.8.* across multiple platforms: Windows, DesktopGL, Android, and iOS. It uses pre-built .xnb content files and is structured for modern .NET 8 SDK-style projects.

## Project Structure

- `/Core` — Shared game logic and assets
- `/Platforms/Windows` — Windows-specific entry point and project
- `/Platforms/DesktopGL` — DesktopGL-specific entry point and project
- `/Platforms/Android` — Android-specific entry point and project
- `/Platforms/iOS` — iOS-specific entry point and project

## Building and Running

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022+ or VSCode
- MonoGame 3.8.* NuGet packages (restored automatically)

### Windows & DesktopGL
- Open the solution in Visual Studio or VSCode
- Build and run the desired platform project
- Or use VSCode tasks/launch configurations for build/run

### Android & iOS
- Open the solution in Visual Studio (Windows for Android, Mac for iOS)
- Build and deploy to device or emulator

## Notes
- No `Content.mgcb` file is used; .xnb files are loaded directly.
- Platform-specific code is separated to avoid `#if` blocks.

## Controls
- Use Up/Down arrows to cycle through shader effects
- Press Escape to exit (Windows/DesktopGL)

---

For more details, see the source code in each platform and the `Core` directory.
