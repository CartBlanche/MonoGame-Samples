# Graphics3DSample

Graphics3DSample is a cross-platform 3D graphics demo built with MonoGame, showcasing animated models, lighting, textures, and interactive UI elements. The project is based on the Microsoft XNA Community Game Platform and demonstrates modern game development techniques using MonoGame.

## Features
- 3D spaceship model rendering
- Animation system driven by XML definitions
- Per-pixel lighting and multiple light sources
- Interactive UI buttons (checkboxes) for toggling features
- Touch and gesture support (pinch to zoom, drag to rotate)
- Mouse support for camera rotation, zoom, and UI interaction
- Background textures and effects

## Controls

### Mouse (Desktop)
- **Left Mouse Drag:** Rotate camera (spaceship view)
- **Mouse Wheel:** Zoom in/out (change camera FOV)
- **Right Mouse Drag:** Pinch-like zoom (alternative to wheel)
- **Click UI Buttons:** Toggle lighting, animation, and background texture

### Touch (Mobile)
- **Free Drag Gesture:** Rotate camera
- **Pinch Gesture:** Zoom in/out (change camera FOV)
- **Tap UI Buttons:** Toggle lighting, animation, and background texture

## Project Structure
- `Core/` — Shared game logic, models, animation, and UI components
- `Platforms/Windows/` — Windows-specific project files
- `Platforms/Desktop/` — DesktopGL (cross-platform desktop) project files
- `Platforms/Android/` — Android project files
- `Platforms/iOS/` — iOS project files
- `Content/` — Game assets (textures, models, animations)

## Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MonoGame](https://www.monogame.net/)
- Platform-specific tools (Android SDK, Xcode for iOS, etc.)

## Building and Running

### Windows
1. Restore dependencies:
    ```pwsh
    dotnet restore
    ```
2. Build:
    ```pwsh
    dotnet build Platforms/Windows/Graphics3DSample.Windows.csproj
    ```
3. Run:
    ```pwsh
    dotnet run --project Platforms/Windows/Graphics3DSample.Windows.csproj
    ```

### DesktopGL (Cross-Platform Desktop)
1. Restore dependencies:
    ```pwsh
    dotnet restore
    ```
2. Build:
    ```pwsh
    dotnet build Platforms/Desktop/Graphics3DSample.DesktopGL.csproj
    ```
3. Run:
    ```pwsh
    dotnet run --project Platforms/Desktop/Graphics3DSample.DesktopGL.csproj
    ```

### Android
1. Restore dependencies:
    ```pwsh
    dotnet restore
    ```
2. Build:
    ```pwsh
    dotnet build Platforms/Android/Graphics3DSample.Android.csproj
    ```
3. Deploy/run using your preferred Android deployment method (e.g., Visual Studio, device/emulator).

### iOS
1. Restore dependencies:
    ```pwsh
    dotnet restore
    ```
2. Build:
    ```pwsh
    dotnet build Platforms/iOS/Graphics3DSample.iOS.csproj
    ```
3. Deploy/run using Xcode or your preferred iOS deployment method.

## Cleaning the Project
To clean all build outputs:
```pwsh
dotnet clean
```

## Notes
- All platform projects share the core game logic in `Core/`.
- Content assets are located in `Core/Content/` and referenced by each platform project.
- For mobile platforms, ensure you have the necessary SDKs and emulators installed.

## License
This project is based on the Microsoft XNA Community Game Platform sample code.
