# CollisionSample (MonoGame 3.8.4, .NET 8)

This project demonstrates various forms of collision detection for primitives in MonoGame, including oriented bounding boxes and triangles. It is structured for modern cross-platform .NET 8 development, with a clean separation between shared game logic and platform-specific entry points.

## Directory Structure

- **Core/**: Shared game logic and content (.xnb files)
- **Platforms/Windows/**: Windows-specific project (net8.0-windows)
- **Platforms/Desktop/**: DesktopGL (cross-platform) project (net8.0)
- **Platforms/Android/**: Android project (net8.0-android)
- **Platforms/iOS/**: iOS project (net8.0-ios)

## Prerequisites
- .NET 8 SDK
- MonoGame 3.8.4 NuGet packages (restored automatically)
- For Android/iOS: Appropriate .NET workloads and platform SDKs (e.g., Android Studio, Xcode)

## Building and Running

### Windows
```
dotnet build Platforms/Windows/CollisionSample.Windows.csproj
```
Run the resulting executable from `bin/Debug/net8.0-windows/`.

### DesktopGL (Cross-platform)
```
dotnet build Platforms/Desktop/CollisionSample.DesktopGL.csproj
```
Run the resulting executable from `bin/Debug/net8.0/`.

### Android
```
dotnet build Platforms/Android/CollisionSample.Android.csproj
```
Deploy the resulting APK to an Android device or emulator.

### iOS
```
dotnet build Platforms/iOS/CollisionSample.iOS.csproj
```
Deploy using Xcode or the .NET iOS workload on a Mac.

## Notes
- No .mgcb file is used; the project loads pre-built .xnb content directly.
- Platform-specific entry points are in their respective folders.
- All shared code is in the `Core` project.
- If you encounter missing SDK/platform errors, ensure you have installed the required .NET workloads for Android/iOS.

## License
This sample is based on Microsoft XNA Community Game Platform code and is provided for educational purposes.
