# MonoGame Samples Content Builder

This project demonstrates how to build and load content assets using MonoGame 3.8.* with the modern .NET SDK project system. It serves as a comprehensive example of content pipeline usage for various asset types including textures, fonts, models, audio, and effects.

## Project Structure

- **Core** - Contains shared game logic (`Game1.cs`) and common project references (`SamplesContentBuilder.Core.csproj`)
- **Platforms/Windows** - Windows DirectX platform target (`Platforms/Windows/SamplesContentBuilder.Windows.csproj`)
- **Platforms/DesktopGL** - Cross-platform OpenGL platform target (`Platforms/DesktopGL/SamplesContentBuilder.DesktopGL.csproj`)
- **Platforms/iOS** - iOS platform target (`Platforms/iOS/SamplesContentBuilder.iOS.csproj`)
- **Platforms/Android** - Android platform target (`Platforms/Android/SamplesContentBuilder.Android.csproj`)
- **Content** - Centralized content assets with platform-specific Content.mgcb files

## Content Assets

The project includes sample assets of various types:

- **Effects** - HLSL shader files (.fx)
- **Fonts** - SpriteFont definition files (.spritefont)  
- **Models** - 3D models (.fbx, .x)
- **Music** - Audio files (.mp3)
- **Textures** - Image files (.png)
- **Sprites** - Game sprite images
- **Bonus** - Additional sample textures

## Content Organization

The project uses a centralized content approach with platform-specific build configurations:

### Centralized Source Assets
All raw content assets (textures, fonts, models, audio, etc.) are stored once in the `Content/` directory structure:
- `Content/Effects/` - Shader files
- `Content/Fonts/` - Font definitions  
- `Content/Models/` - 3D models
- `Content/Music/` - Audio files
- `Content/Textures/` - Images and sprites
- `Content/bonus/` - Additional sample textures

### Platform-Specific Build Configuration
Each platform has its own Content.mgcb file that references the same source assets but builds with platform-specific settings:
- `Content/Content-Windows.mgcb` - DirectX optimized content
- `Content/Content-DesktopGL.mgcb` - OpenGL optimized content
- `Content/Content-iOS.mgcb` - iOS optimized content
- `Content/Content-Android.mgcb` - Android optimized content

### Benefits of This Approach
- **No Content Duplication** - Source assets exist in only one location
- **Platform Optimization** - Each platform builds content with appropriate settings
- **Isolated Output** - Platform-specific .xnb files are kept separate
- **Easy Maintenance** - Update source assets once, all platforms benefit
- **Build Efficiency** - Only necessary content is built for each platform

## Building and Running

### Prerequisites

- .NET 8.0 SDK or later
- MonoGame 3.8.* (installed via NuGet packages)

### Visual Studio

1. Open `SamplesContentBuilder.sln`
2. Build the solution (Ctrl+Shift+B)
3. Set your desired platform project as startup project
4. Run (F5)


### Visual Studio Code

1. Open the project folder in VS Code
2. Use the Command Palette (Ctrl+Shift+P) and run "Tasks: Run Task"
3. Select one of the available tasks:
   - `build-content-windows` - Build Windows content assets only
   - `build-content-desktopgl` - Build DesktopGL content assets only
   - `build-content-ios` - Build iOS content assets only
   - `build-content-android` - Build Android content assets only
   - `build-windows` - Build Windows platform (legacy layout)
   - `build-desktopgl` - Build DesktopGL platform (legacy layout)
   - `build-platform-windows` - Build Windows platform (modern layout)
   - `build-platform-desktopgl` - Build DesktopGL platform (modern layout)
   - `build-platform-ios` - Build iOS platform
   - `build-platform-android` - Build Android platform
   - `clean-content` - Clean content build outputs
   - `clean-all` - Clean all build outputs

4. To run and debug:
   - Press F5 or use "Run and Debug" view
   - Select from available launch configurations for each platform project

### Command Line

Build content for specific platforms:
```bash
dotnet mgcb -@ Content/Content-Windows.mgcb
dotnet mgcb -@ Content/Content-DesktopGL.mgcb
dotnet mgcb -@ Content/Content-iOS.mgcb
dotnet mgcb -@ Content/Content-Android.mgcb
```

Build specific platform projects:
```bash
# Platform projects
dotnet build Platforms/Windows/SamplesContentBuilder.Windows.csproj
dotnet build Platforms/DesktopGL/SamplesContentBuilder.DesktopGL.csproj
dotnet build Platforms/iOS/SamplesContentBuilder.iOS.csproj
dotnet build Platforms/Android/SamplesContentBuilder.Android.csproj
dotnet build Core/SamplesContentBuilder.Core.csproj
```

Run:
```bash
# Platform projects
dotnet run --project Platforms/Windows
dotnet run --project Platforms/DesktopGL
dotnet run --project Platforms/iOS
dotnet run --project Platforms/Android
dotnet run --project Core
```

## Supported Platforms

- **Windows** - DirectX backend (net8.0-windows)
- **DesktopGL** - OpenGL backend, cross-platform (net8.0)
- **iOS** - iOS backend (net8.0-ios)
- **Android** - Android backend (net8.0-android)

## Content Pipeline

The project uses MonoGame's Content Builder (MGCB) to process raw assets into optimized .xnb files. Content is centralized in the `Content/` directory but built to platform-specific output directories:

- **Content/Content-Windows.mgcb** - Windows content configuration → `Content/bin/Windows/`
- **Content/Content-DesktopGL.mgcb** - DesktopGL content configuration → `Content/bin/DesktopGL/`
- **Content/Content-iOS.mgcb** - iOS content configuration → `Content/bin/iOS/`
- **Content/Content-Android.mgcb** - Android content configuration → `Content/bin/Android/`

This approach eliminates content duplication while maintaining platform-specific optimizations. Each platform references the same source assets but builds them with platform-appropriate settings and outputs them to isolated directories.

Content processing is handled automatically through the `MonoGame.Content.Builder.Task` NuGet package during build.

## Modernization Changes

This project has been modernized from legacy XNA/MonoGame format to use:

- .NET SDK-style project files
- MonoGame 3.8.* NuGet packages
- .NET 8.0 target frameworks
- Unified content building with Content.mgcb
- VS Code support with tasks and launch configurations

## Learning Objectives

This sample demonstrates:

- Setting up MonoGame 3.8.* projects with modern tooling
- Content pipeline usage for different asset types  
- Cross-platform game development
- Integration with Visual Studio and VS Code
- Modern .NET SDK project structure
