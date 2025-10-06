# WpfInteropSample

This sample demonstrates how to display MonoGame graphics inside a WPF application using a D3DImage. It leverages MonoGame 3.8.x and SharpDX for Direct3D interop.

## Features
- Host MonoGame rendering in a WPF window
- Uses D3D11 and D3D9 interop via SharpDX
- Modern .NET 6.0 Windows Desktop SDK project

## Prerequisites
- .NET 6.0 SDK or later
- Windows OS
- Visual Studio Code (or Visual Studio 2022+)

## How to Build and Run

### Using Visual Studio Code
1. Open this folder in VS Code.
2. Press `Ctrl+Shift+B` to build (or run the `build` task).
3. Press `F5` to launch and debug the app.

### Using Command Line
```sh
# Restore dependencies
 dotnet restore
# Build the project
 dotnet build
# Run the app
 dotnet run --project WpfInteropSample.csproj
```

## Supported Platforms
- Windows (WPF, .NET 6.0+)

## Dependencies
- [MonoGame.Framework.WindowsDX 3.8.x](https://www.nuget.org/packages/MonoGame.Framework.WindowsDX)
- [SharpDX 4.2.0](https://www.nuget.org/packages/SharpDX)

---
This project is a modernized version of the original sample, now using NuGet packages and SDK-style .csproj for easier development.
