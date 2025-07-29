# PacMan MonoGame Cross-Platform Project

This is a modernized, cross-platform MonoGame 3.8.* implementation of Pac-Man, supporting Windows, DesktopGL, Android, and iOS. Shared game logic is in `/Core`, with platform-specific entry points in `/Platforms`.

## Project Structure

- Core/
  - Main game logic and classes (GameLoop.cs, Player.cs, Ghost.cs, etc.)
  - PacMan.Core.csproj
  - Content/
    - Game assets (sprites, audio, fonts, etc.)
    - Content.mgcb
    - Audio/, bonus/, sprites/
- Platforms/
  - Android/
    - PacMan.Android.csproj
    - Resources/
  - DesktopGL/
    - PacMan.DesktopGL.csproj
    - Program.cs
  - iOS/
    - PacMan.iOS.csproj
    - Info.plist
  - Windows/
    - PacMan.Windows.csproj
    - Program.cs
- README.md
- PacMan.sln

## Building

### Prerequisites
- .NET 8.0 SDK or newer
- MonoGame 3.8.* (NuGet packages are referenced automatically)
- For Android/iOS: Xamarin/MAUI workloads and platform SDKs

### Build Commands

- **Windows:**
  ```sh
  dotnet build Platforms/Windows/PacMan.Windows.csproj
  ```
- **DesktopGL:**
  ```sh
  dotnet build Platforms/DesktopGL/PacMan.DesktopGL.csproj
  ```
- **Android:**
  ```sh
  dotnet build Platforms/Android/PacMan.Android.csproj
  ```
- **iOS:**
  ```sh
  dotnet build Platforms/iOS/PacMan.iOS.csproj
  ```

## Running

- **Windows:**
  ```sh
  dotnet run --project Platforms/Windows/PacMan.Windows.csproj
  ```
- **DesktopGL:**
  ```sh
  dotnet run --project Platforms/DesktopGL/PacMan.DesktopGL.csproj
  ```

## Content
- This project currently uses pre-built `.xnb` files in the `Content/` directory. The Content.mgcb file has not been updated yet.
- If you add new assets, you must build them to `.xnb` format using the MonoGame Pipeline Tool.

## Troubleshooting
- If you see errors about missing content (e.g., `Content/Audio/YEPAudio.xgs`), ensure the `Content/` directory is copied to the output directory for each platform. You may need to update your `.csproj` files to copy the `Content` folder on build.
- For Android/iOS, ensure you have the correct SDKs and emulators installed.

## Platform Notes
- Windows and DesktopGL are the best-supported for desktop development and debugging.
- Android/iOS require additional setup and are best built/deployed from Visual Studio or with the appropriate workloads installed.

---

Enjoy Pac-Man on your favorite platform!
