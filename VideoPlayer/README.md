# MonoGame VideoPlayer Sample

This project demonstrates video playback using MonoGame 3.8.* across multiple platforms: Windows, DesktopGL, Android, and iOS. It is structured for modern .NET 8 SDK-style projects and is ready for use in both Visual Studio and VSCode.

## Project Structure

 - **/Core**: Shared game logic (`VideoPlayerGame.cs`)
 - **/Platforms/Windows**: Windows-specific entry point and project
 - **/Platforms/DesktopGL**: DesktopGL-specific entry point and project
 - **/Platforms/Android**: Android entry point, manifest, and project
 - **/Platforms/iOS**: iOS entry point, Info.plist, and project
 - **/Content**: Game assets, including fonts and video files (e.g., .mp4)

## Building and Running

### Prerequisites
- .NET 8 SDK
- MonoGame 3.8.* NuGet packages (restored automatically)
- For Android/iOS: Appropriate workloads and emulators/simulators

### Windows & DesktopGL (VSCode)
1. Open the folder in VSCode.
2. Use the built-in tasks (`Ctrl+Shift+B`) to build for Windows or DesktopGL.
3. Use the Run/Debug menu to launch the desired platform.

### Windows & DesktopGL (Visual Studio)
1. Open `VideoPlayer.sln` in Visual Studio 2022 or later.
2. Set the desired startup project (Windows or DesktopGL).
3. Build and run.

### Android/iOS
- Open the solution in Visual Studio 2022+ (Windows or Mac) with Xamarin/MAUI workloads installed.
- Set the platform project as startup, build, and deploy to device/emulator.

## Notes

### Video Files (.mp4)
- Video files are **not** processed by the MonoGame Content Pipeline and should **not** be listed in `Content.mgcb`.
- Video files (e.g., `sintel_trailer.mp4`) are copied to the output directory using a `<Content>` entry in each platform's `.csproj` file:
  ```xml
  <Content Include="..\..\Core\Content\**\*.mp4" Link="Content\%(RecursiveDir)%(Filename)%(Extension)">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
  ```
- At runtime, load videos using `Content.Load<Video>("sintel_trailer")` (without file extension).

### Other Notes
- Platform-specific code is isolated in each platform directory for clarity and maintainability.
- Only Windows, DesktopGL, Android, and iOS are supported. Linux, MacOS, and PSMobile are not supported in MonoGame 3.8.*.

## License
Copyright Savage Software Solutions Ltd.
