
# NetworkStateManagement

NetworkStateManagement is a MonoGame-based project demonstrating network state management in a game. It supports the following platforms:
- Windows
- DesktopGL
- Android
- iOS

## Directory Structure

- `/Core` - Shared game logic and code
- `/Platforms/Windows` - Windows-specific entry point and project
- `/Platforms/Desktop` - DesktopGL-specific entry point and project
- `/Platforms/Android` - Android-specific entry point and project
- `/Platforms/iOS` - iOS-specific entry point and project

## Prerequisites
- .NET 8.0 SDK or later
- MonoGame 3.8.*
- Visual Studio Code or Visual Studio

## Building and Running

### Windows
1. Open the project folder in Visual Studio Code.
2. Use the `build-windows` task to build the project.
3. Use the `Launch Windows` configuration in the debugger to run the project.

### DesktopGL
1. Open the project folder in Visual Studio Code.
2. Use the `build-desktopgl` task to build the project.
3. Use the `Launch DesktopGL` configuration in the debugger to run the project.

### Android
1. Open the project folder in Visual Studio Code or Visual Studio.
2. Use the `build-android` task to build the project.
3. Use the `Launch Android` configuration in the debugger to run the project.

### iOS
1. Open the project folder in Visual Studio Code or Visual Studio.
2. Use the `build-ios` task to build the project.
3. Use the `Launch iOS` configuration in the debugger to run the project.

## Notes
- The project uses precompiled `.xnb` files for content, located in the `Content` folder.
- All platform-specific code is separated into its own directory to avoid `#if/#endif` blocks.
- Shared code is referenced from `/Core`.
