# StateObject Sample (MonoGame 3.8.*)

This project demonstrates a cross-platform MonoGame sample, structured for modern .NET 8 and MonoGame 3.8.*. It is organized for easy development and deployment on Windows, DesktopGL, Android, and iOS.

## Structure
- `Core/` — Shared game logic (`StateObject.Core.csproj`, e.g., `StateObjectGame.cs`)
- `Platforms/Windows/` — Windows-specific entry point and project (`StateObject.Windows.csproj`)
- `Platforms/DesktopGL/` — DesktopGL-specific entry point and project (`StateObject.DesktopGL.csproj`)
- `Platforms/Android/` — Android entry point, manifest, and project (`StateObject.Android.csproj`)
- `Platforms/iOS/` — iOS entry point, Info.plist, and project (`StateObject.iOS.csproj`)

## Building & Running

### Prerequisites
- .NET 8 SDK
- MonoGame 3.8.* NuGet packages (restored automatically)
- Visual Studio 2022+ or VSCode (with C# Dev Kit)

### Windows & DesktopGL
- Open the solution (`StateObject.sln`) in Visual Studio or VSCode.
- Select the desired platform project (`StateObject.Windows` or `StateObject.DesktopGL`) as startup.
- Build and run (F5 or `dotnet run --project Platforms/Windows/StateObject.Windows.csproj` or `dotnet run --project Platforms/DesktopGL/StateObject.DesktopGL.csproj`).

### Android
- Requires Android SDK and MonoGame.Android NuGet package.
- Build and deploy from Visual Studio or with `dotnet build Platforms/Android/StateObject.Android.csproj`.

### iOS
- Requires macOS, Xcode, and MonoGame.iOS NuGet package.
- Build and deploy from Visual Studio for Mac or with `dotnet build Platforms/iOS/StateObject.iOS.csproj`.

## Notes
- No `Content.mgcb` file is used; `.xnb` assets are loaded directly.
- Platform-specific code is isolated in each platform directory.
- Shared code is in `Core` and referenced by all platforms.

## Troubleshooting
- Ensure all NuGet packages restore successfully.
- For Android/iOS, ensure you have the correct SDKs and emulators installed.

---
(C) Microsoft 2010, modernized for MonoGame 3.8.* and .NET 8 by the community.
