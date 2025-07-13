Asteroid Belt Assault
=====================

This project contains the Asteroid Belt Assault game as it is at the end of Chapter 5. 

This was taken from the Book XNA 4.0 Game Development by Example: Beginner's Guide by Kurt Jaegers
Published by PACKT Publishing which can be found here 
http://www.packtpub.com/xna-4-0-game-development-by-example-beginners-guide/book

For a complete discussion of the code please buy the book as it is worth the money.

## About

Asteroid Belt Assault is a cross-platform 2D space shooter game originally developed for XNA 4.0, now ported to MonoGame. The project demonstrates game development concepts such as sprite management, collision detection, particle effects, and sound, following the structure from the book "XNA 4.0 Game Development by Example: Beginner's Guide" by Kurt Jaegers.

## Supported Platforms
- Windows (DirectX)
- DesktopGL (cross-platform)
- Android
- iOS

## Building and Running

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [MonoGame](https://www.monogame.net/) (if not included via NuGet)
- For Android: Android SDK & emulator
- For iOS: macOS with Xcode & simulator (or networked Mac for Windows users)

### Windows
1. Build: Use the VS Code task `build-windows` or run:
   ```pwsh
   dotnet build Platforms/Windows/AsteroidBeltAssault.Windows.csproj
   ```
2. Run: Use the VS Code launch config `Run Windows` or run the built `.exe` from `Platforms/Windows/bin/Debug/net8.0-windows/`.

### DesktopGL
1. Build: Use the VS Code task `build-desktopgl` or run:
   ```pwsh
   dotnet build Platforms/Desktop/AsteroidBeltAssault.DesktopGL.csproj
   ```
2. Run: Use the VS Code launch config `Run Desktop` or run the built binary from `Platforms/Desktop/bin/Debug/net8.0/`.

### Android
1. Build: Use the VS Code task `build-android` or run:
   ```pwsh
   dotnet build Platforms/Android/AsteroidBeltAssault.Android.csproj
   ```
2. Deploy & Run: Use the VS Code launch config `Deploy and Launch Android` or run:
   ```pwsh
   dotnet android deploy --project Platforms/Android/AsteroidBeltAssault.Android.csproj
   ```
   The app will be deployed to the default Android emulator.

### iOS
1. Build: Use the VS Code task `build-ios` or run:
   ```pwsh
   dotnet build Platforms/iOS/AsteroidBeltAssault.iOS.csproj
   ```
2. Deploy & Run: Use the VS Code launch config `Deploy and Launch iOS` or run:
   ```pwsh
   dotnet ios deploy --project Platforms/iOS/AsteroidBeltAssault.iOS.csproj
   ```
   The app will be deployed to the default iOS simulator.

---
For more details, see the book or the source code in the `Core` and `Platforms` directories.
