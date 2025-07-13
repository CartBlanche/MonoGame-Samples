Flood Control
=============


This project contains the Flood Control game, modernized for MonoGame 3.8.* and .NET 8, with a clean cross-platform structure.

## Directory Structure

- `Core/` — Shared game logic and code
- `Platforms/Windows/` — Windows-specific project and entry point
- `Platforms/Desktop/` — DesktopGL (cross-platform desktop) project and entry point
- `Platforms/Android/` — Android project and entry point
- `Platforms/iOS/` — iOS project and entry point
- `Content/` — Pre-built .xnb content files used directly by all platforms

## Building and Running

### Prerequisites
- .NET 8 SDK
- MonoGame 3.8.* (NuGet packages are referenced automatically)
- Visual Studio 2022+ or VSCode (with C# Dev Kit recommended)

### With Visual Studio
Open `FloodControl.sln` and build/run the desired platform project (Windows, DesktopGL, Android, or iOS).

### With VSCode
Use the built-in tasks and launch configurations:
- Press `Ctrl+Shift+B` to build (choose the platform-specific build task)
- Press `F5` to run (choose the platform-specific launch config: Windows, DesktopGL, Android, or iOS)

### Notes
- No Content.mgcb file is used; the game loads .xnb files directly from the Content folder.
- Platform-specific code is separated into platform folders; shared logic is in Core.
- Android and iOS require appropriate emulators or devices and platform SDKs.

---
This project was originally from the book XNA 4.0 Game Development by Example: Beginner's Guide by Kurt Jaegers (PACKT Publishing).
See: http://www.packtpub.com/xna-4-0-game-development-by-example-beginners-guide/book
