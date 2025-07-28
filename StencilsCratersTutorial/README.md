# StencilCraters

A cross-platform MonoGame 3.8.* sample demonstrating stencil buffer craters on a planet surface. Supports Windows, DesktopGL, Android, and iOS using .NET 8.0 SDK-style projects.

## Project Structure
- `Core/` — Shared game logic (`Game1.cs`), cross-platform code, and assets
- `Core/Content/` — Game content (textures, icons, etc.)
- `Platforms/Windows/` — Windows-specific entry point and project (`StencilCraters.Windows.csproj`)
- `Platforms/DesktopGL/` — DesktopGL-specific entry point and project (`StencilCraters.DesktopGL.csproj`)
- `Platforms/Android/` — Android entry point, project, and manifest (`StencilCraters.Android.csproj`)
- `Platforms/iOS/` — iOS entry point, project, and Info.plist (`StencilCraters.iOS.csproj`)
- `Documentation/` — Tutorials, images, and technical docs

## Building and Running

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022+ or VSCode (with C# Dev Kit recommended)
- For Android/iOS: Xamarin/MAUI workload and platform SDKs

### Windows & DesktopGL
- Open `StencilsCraters.sln` in Visual Studio, or use VSCode tasks/launch.
- Build and run either the Windows or DesktopGL project.

### Android
- Open solution in Visual Studio (Windows or Mac) with Android workload.
- Build and deploy `StencilCraters.Android` to an emulator or device.

### iOS
- Open solution in Visual Studio for Mac with iOS workload.
- Build and deploy `StencilCraters.iOS` to a simulator or device.

## Notes
- All platform projects reference the shared `Core` project.
- Content is loaded directly from `.xnb` files in the `Core/Content/` directory.
- No `Content.mgcb` file is required.

## Documentation

- See [`Documentation/Stencil Buffer Craters.md`](Documentation/Stencil%20Buffer%20Craters.md) for a detailed tutorial on stencil buffer craters and GPU texture modification.

Additional images and reference material are in the `Documentation/` folder.
