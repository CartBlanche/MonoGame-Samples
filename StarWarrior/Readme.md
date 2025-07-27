StarWarrior
===========

StarWarrior is a cross-platform MonoGame 3.8.x sample game demonstrating an entity-component-system (ECS) architecture. The codebase is organized for modern .NET SDK-style projects and supports Windows, DesktopGL, Android, and iOS platforms.

## Project Structure
- **Core/**: Shared game logic, components, systems, and ECS code. References MonoGame and Artemis ECS.
- **Platforms/Windows/**: Windows-specific entry point and project (`StarWarrior.Windows.csproj`).
- **Platforms/DesktopGL/**: DesktopGL (cross-platform) entry point and project (`StarWarrior.DesktopGL.csproj`).
- **Platforms/Android/**: Android entry point, project (`StarWarrior.Android.csproj`), and manifest.
- **Platforms/iOS/**: iOS entry point, project (`StarWarrior.iOS.csproj`), and Info.plist.
- **Content/**: Pre-built .xnb assets used directly by the game.
- **Artemis/PC/**: ECS library DLL and XML doc (must be .NET 8.0 compatible).

## Requirements
- .NET 8.0 SDK or newer
- Visual Studio 2022+ or VSCode with C# Dev Kit
- MonoGame 3.8.x NuGet packages (restored automatically)
- Artemis ECS DLL (must be .NET 8.0 compatible)

## Building and Running
### Visual Studio
- Open `StarWarrior.sln`.
- Set your desired platform project as startup (Windows, DesktopGL, Android, or iOS).
- Build and run.

### VSCode
- Use the provided `.vscode/tasks.json` and `.vscode/launch.json` for build/run/debug.
- Example: Press `F5` to launch Windows, DesktopGL, Android, or iOS version.

### CLI
- Build: `dotnet build Platforms/Windows/StarWarrior.Windows.csproj`
- Run: `dotnet run --project Platforms/Windows/StarWarrior.Windows.csproj`
- Build Android: `dotnet build Platforms/Android/StarWarrior.Android.csproj`
- Build iOS: `dotnet build Platforms/iOS/StarWarrior.iOS.csproj`

## Notes
- The project uses pre-built `.xnb` content and does not require a Content.mgcb file.
- Linux, MacOS, and PSMobile are no longer supported in MonoGame 3.8.x.
- Ensure `Artemis/PC/artemis.dll` is compatible with .NET 8.0. If not, consider porting or replacing Artemis ECS.

## Solution File
The solution (`StarWarrior.sln`) includes:
- Core/StarWarrior.Core.csproj
- Platforms/Windows/StarWarrior.Windows.csproj
- Platforms/DesktopGL/StarWarrior.DesktopGL.csproj
- Platforms/Android/StarWarrior.Android.csproj
- Platforms/iOS/StarWarrior.iOS.csproj

## VSCode Tasks and Launch
- `.vscode/tasks.json` and `.vscode/launch.json` are set up for Windows, DesktopGL, Android, and iOS.
- You can build and run any supported platform from VSCode.

## Credits
Ported to MonoGame by Kenneth J. Pouncey from the original here: 
https://github.com/thelinuxlich/starwarrior_CSharp
