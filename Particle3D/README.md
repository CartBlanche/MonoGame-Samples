# Particle3D Sample - MonoGame 3.8

This is a modernized version of the MonoGame Particle3D sample, demonstrating advanced 3D particle effects using MonoGame 3.8.4 framework.

## Project Overview

The Particle3D Sample showcases various particle systems including:
- Explosions with smoke
- Fire effects
- Projectile trails
- Smoke plumes
- Various particle emitters and systems

The project demonstrates how to create custom particle effects using vertex buffers, custom shaders, and particle settings that can be configured via XML files.

## Project Structure

- **Game.cs** - Main game class handling initialization, updates, and rendering
- **ParticleSystem.cs** - Core particle system implementation
- **ParticleEmitter.cs** - Particle emission logic
- **ParticleVertex.cs** - Custom vertex structure for particles
- **Projectile.cs** - Projectile implementation with trail effects
- **ParticleSettings/** - Library containing particle configuration classes

## Supported Platforms

This modernized version supports the following platforms using .NET 8.0:


### Using Visual Studio Code

1. Open the workspace in VS Code.
2. Use the build/run tasks for your platform (see `.vscode/tasks.json` and `.vscode/launch.json`).
3. Select the desired launch configuration and press F5 to run.

### Project Structure (Post-Refactor)

- `/Core` - Shared game and particle system code
- `/Platforms/Windows` - Windows-specific entry point and project
- `/Platforms/Desktop` - DesktopGL (OpenGL) entry point and project
- `/Platforms/Android` - Android entry point and project
- `/Platforms/iOS` - iOS entry point and project

All platform projects reference `/Core` for shared logic. Platform-specific code and entry points are separated to minimize `#if/#endif` usage.
```bash
dotnet build Platforms/Windows/Particle3DSample.Windows.csproj
dotnet run --project Platforms/Windows/Particle3DSample.Windows.csproj
```

#### DesktopGL (Cross-platform OpenGL)
```bash
dotnet build Platforms/Desktop/Particle3DSample.DesktopGL.csproj
dotnet run --project Platforms/Desktop/Particle3DSample.DesktopGL.csproj
```

#### Android
```bash
dotnet build Platforms/Android/Particle3DSample.Android.csproj
dotnet run --project Platforms/Android/Particle3DSample.Android.csproj
```

#### iOS
```bash
dotnet build Platforms/iOS/Particle3DSample.iOS.csproj
dotnet run --project Platforms/iOS/Particle3DSample.iOS.csproj
```

### Using Visual Studio Code

1. Open the workspace in VS Code.
2. Use the build/run tasks for your platform (see `.vscode/tasks.json` and `.vscode/launch.json`).
3. Select the desired launch configuration and press F5 to run.

### Project Structure (Post-Refactor)

- `/Core` - Shared game and particle system code
- `/Platforms/Windows` - Windows-specific entry point and project
- `/Platforms/Desktop` - DesktopGL (OpenGL) entry point and project
- `/Platforms/Android` - Android entry point and project
- `/Platforms/iOS` - iOS entry point and project

All platform projects reference `/Core` for shared logic. Platform-specific code and entry points are separated to minimize `#if/#endif` usage.
dotnet build Particle3DSample.DesktopGL.csproj
dotnet run --project Particle3DSample.DesktopGL.csproj
```

#### Android
```bash
dotnet build Particle3DSample.Android.csproj
# Deploy to connected device or emulator
dotnet build Particle3DSample.Android.csproj -t:Run
```

### Using Visual Studio Code

1. Open the project folder in VS Code
2. Use the integrated terminal or Command Palette
3. Run tasks:
   - `Ctrl+Shift+P` → "Tasks: Run Task"
   - Select "build-windows", "build-desktopgl", or "build-android"
   - Or use "run-windows" or "run-desktopgl" to build and run

#### Available VS Code Tasks
- `build-windows` - Build Windows version
- `build-desktopgl` - Build DesktopGL version
- `build-android` - Build Android version
- `run-windows` - Build and run Windows version
- `run-desktopgl` - Build and run DesktopGL version
- `clean-all` - Clean all projects
- `restore-all` - Restore NuGet packages

## Controls

- **Mouse** - Look around
- **Left Click** - Fire projectiles with particle trails
- **Right Click** - Create explosion effects
- **WASD** - Move camera
- **Space/Shift** - Move up/down
- **Escape** - Exit application

## Technical Details

### Modern .NET Features
- Uses SDK-style project files for simplified configuration
- Targets .NET 8.0 for best performance and modern language features
- Uses MonoGame 3.8.* NuGet packages instead of project references
- Nullable reference types enabled for better code safety

### Content Pipeline
The project uses pre-built .xnb content files located in the `Content/` directory:
- Particle textures (explosion.xnb, fire.xnb, smoke.xnb)
- Particle settings XML files and compiled .xnb versions
- Font files for UI text
- 3D model files

### Particle System Features
- Custom vertex structures optimized for particle rendering
- Hardware-accelerated particle rendering using vertex buffers
- XML-based particle system configuration
- Multiple particle emitter types
- Physics-based particle movement and effects

## Troubleshooting

### Common Issues

1. **Missing MonoGame Dependencies**
   ```bash
   dotnet restore
   ```

2. **Content not found errors**
   - Ensure all .xnb files are in the Content directory
   - Check that content files are set to "Copy to Output Directory"

3. **Graphics/Shader Issues**
   - Try the DesktopGL version for better cross-platform compatibility
   - Ensure graphics drivers are up to date

4. **Android Build Issues**
   - Install Android workload: `dotnet workload install android`
   - Ensure Android SDK is properly configured

### Performance Tips
- The sample is optimized for desktop performance
- On mobile devices, consider reducing particle counts in settings XML files
- Use the DesktopGL version for better cross-platform compatibility

## Contributing

This is a sample project demonstrating MonoGame 3.8 features. Feel free to use it as a reference for your own particle system implementations.

## License

This sample is provided as-is for educational and reference purposes.
