# ChaseAndEvade MonoGame Sample

This project demonstrates simple chase, evade, and wander AI behaviors using MonoGame 3.8.* and .NET 8.0. It is organized for cross-platform development with a clean separation between shared game logic and platform-specific code.

## Directory Structure

- `/Core` — Shared game logic and main game class (`ChaseAndEvadeGame`)
- `/Platforms/Windows` — Windows-specific entry point and project
- `/Platforms/Desktop` — DesktopGL (cross-platform desktop) entry point and project
- `/Platforms/Android` — Android entry point, manifest, and project
- `/Platforms/iOS` — iOS entry point, Info.plist, and project
- `/Content` — Prebuilt .xnb content files used directly by the game

## Building and Running

### Prerequisites
- .NET 8.0 SDK
- MonoGame 3.8.* (NuGet packages are referenced automatically)
- Visual Studio 2022+ or VSCode (with C# Dev Kit recommended)

### Windows
1. Open `ChaseAndEvade.sln` in Visual Studio, or use VSCode.
2. Build and run the `ChaseAndEvade.Windows` project.

### DesktopGL
1. Open `ChaseAndEvade.sln` or use VSCode.
2. Build and run the `ChaseAndEvade.DesktopGL` project.

### Android
1. Open `ChaseAndEvade.sln` in Visual Studio (with Xamarin/MAUI workload).
2. Build and deploy the `ChaseAndEvade.Android` project to an emulator or device.

### iOS
1. Open `ChaseAndEvade.sln` in Visual Studio for Mac or Windows (with Xamarin/MAUI workload and Mac build agent).
2. Build and deploy the `ChaseAndEvade.iOS` project.

## Notes
- All shared code is in the `Core` project and referenced by each platform.
- Platform-specific entry points are in their respective directories.
- The game uses prebuilt `.xnb` files in the `Content` directory; no `Content.mgcb` is required.

## VSCode Tasks and Launch
- Use the provided `tasks.json` and `launch.json` for building and running Windows and DesktopGL projects directly from VSCode.

---

For more details, see the source code and comments in each project.
