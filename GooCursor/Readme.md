GooCursor
=========

GooCursor is a cross-platform MonoGame sample demonstrating a custom, physics-based cursor inspired by World of Goo.

## Supported Platforms
- Windows (Desktop)
- DesktopGL (Cross-platform)
- Android
- iOS

## Desktop Controls

| Key(s)                | Action                        |
|-----------------------|-------------------------------|
| Q / A                 | Increase / Decrease Start Scale |
| W / S                 | Increase / Decrease End Scale   |
| E / D                 | Increase / Decrease Lerp Exponent |
| R / F                 | Increase / Decrease Border Size  |
| T / G                 | Increase / Decrease Trail Stiffness |
| Y / H                 | Increase / Decrease Trail Damping |
| U / J                 | Increase / Decrease Trail Node Mass |
| Left Shift + Enter    | Toggle Fullscreen               |

## Project Structure
- `/Core` — Shared game logic and assets (`Game1.cs`, `Cursor.cs`, etc.)
- `/Platforms/<Platform>` — Platform-specific entry points and project files

## How to Build & Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [MonoGame 3.8.*](https://www.monogame.net/)
- (Optional) Android/iOS build tools for mobile platforms

### Windows & DesktopGL
1. Open the solution (`GooCursor.sln`) in Visual Studio or VS Code.
2. Select the desired platform project under `Platforms/` (e.g., `Platforms/Windows/GooCursor.Windows.csproj`).
3. Build and run:
   - **VS Code:** Use the provided tasks (`Ctrl+Shift+B`) or F5 to debug.
   - **Command Line:**
     - Windows: `dotnet run --project Platforms/Windows/GooCursor.Windows.csproj`
     - DesktopGL: `dotnet run --project Platforms/Desktop/GooCursor.DesktopGL.csproj`

### Android & iOS
1. Open the solution in Visual Studio (Windows or Mac) or use the command line.
2. Build the platform project:
   - Android: `dotnet build Platforms/Android/GooCursor.Android.csproj`
   - iOS: `dotnet build Platforms/iOS/GooCursor.iOS.csproj`
3. Deploy to device/emulator as appropriate.

### Linux & macOS
1. Build the respective project:
   - Linux: `dotnet run --project Platforms/Linux/GooCursor.Linux.csproj`
   - macOS: `dotnet run --project Platforms/MacOS/GooCursor.MacOS.csproj`

## Credits
- Original concept: [Catalin Zima](http://www.catalinzima.com/samples/game-features-replicator/world-of-goo-cursor/)
- XNA 4.0 port: Kenneth J. Pouncey
- Modernization & cross-platform: CartBlanche
