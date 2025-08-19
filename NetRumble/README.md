# Net Rumble Sample

Net Rumble is a two-dimensional shooter, pitting up to sixteen players against one another in an arena filled with asteroids and power-ups.

## Table of Contents
- [Introduction to Net Rumble](#introduction-to-net-rumble)
- [What's New](#whats-new)
- [Goals](#goals)
- [System Requirements](#system-requirements)
- [Getting Started](#getting-started)
- [Game Controls](#game-controls)
- [Implementation Notes](#implementation-notes)
- [Extending Net Rumble](#extending-net-rumble)

---

## Introduction to Net Rumble
Net Rumble is a complete XNA Game Studio sample game. The project comes ready to compile and run, and it's easy to customize with a little bit of C# programming. You are free to use the source code as the basis for your own XNA Game Studio game projects, and to share your work with others.

Net Rumble is a two-dimensional shooter, pitting up to sixteen players against one another in an arena filled with asteroids and power-ups.

This is a modernized version of the NetRumble sample from the XNA Game Studio era, updated to use:
- .NET 8.0 modern SDK-style projects
- MonoGame 3.8.* NuGet packages
- Cross-platform support for Windows, DesktopGL, Android, and iOS

---

## What's New
The following improvements have been made in the XNA Game Studio 4.0-compatible version of Net Rumble:
- Support for Xbox LIVE invites
- Support for trial mode: indicator and purchase option
- Support for rich presence
- Audio functionality migration from XACT to SoundEffect and MediaLibrary implementation

## Supported Platforms

- **Windows** (DirectX) - `NetRumble.Windows.csproj`
- **DesktopGL** (OpenGL - Cross-platform desktop) - `NetRumble.DesktopGL.csproj`  
- **Android** - `NetRumble.Android.csproj`
- **iOS** - `NetRumble.iOS.csproj`

---

## Goals
This game demonstrates the following features:
- System Link and LIVE multiplayer
- Game entity and state management
- Two-dimensional rendering with bloom post-processing effects

---

## System Requirements
The Windows version of this sample requires a minimum desktop resolution of 1280×720 pixels.

### Multiplayer Requirements
System Link and Games for Windows - LIVE multiplayer require that each Windows computer participating in the game be connected to a common network. In addition, each player using Games for Windows - LIVE must have a Gamertag associated with a valid App Hub membership.

> **Note:**
> In order for two instances of Net Rumble to connect to one another, the Guid property in AssemblyInfo.cs in all projects must match. If you create a Windows project and an Xbox 360 project by using the **New Project** dialog box, you will need to copy the Guid from one project to the other in order to connect.

---

## Getting Started
Follow these procedures to get started.

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- For Android development: Android SDK and Android 13.0 (API 33) or later
- For iOS development: macOS with Xcode and iOS 11.0 or later

### From Visual Studio Code

1. Open the project folder in VS Code
2. Use the Command Palette (Ctrl+Shift+P) and run:
   - `Tasks: Run Task` → `build-windows` to build for Windows
   - `Tasks: Run Task` → `build-desktopgl` to build for DesktopGL
   - `Tasks: Run Task` → `run-windows` to run the Windows version
   - `Tasks: Run Task` → `run-desktopgl` to run the DesktopGL version

### From Command Line

#### Windows Platform
```powershell
dotnet build NetRumble.Windows.csproj
dotnet run --project NetRumble.Windows.csproj
```

#### DesktopGL Platform  
```powershell
dotnet build NetRumble.DesktopGL.csproj
dotnet run --project NetRumble.DesktopGL.csproj
```

#### Android Platform
```powershell
dotnet build NetRumble.Android.csproj
# For deployment to device/emulator, use your preferred Android deployment method
```

### From Visual Studio

1. Open `NetRumble.sln` in Visual Studio 2022
2. Select your target platform from the dropdown
3. Build and run using F5 or Ctrl+F5


### Project Structure

The project is organized as follows:

- **Core/**
  - Main source code for game logic, rendering, screens, and managers
  - Subfolders:
    - **Gameplay/**: Ships, weapons, power-ups, asteroids, world logic
    - **ScreenManager/**: Menu and gameplay screen management
    - **Screens/**: Individual screens (main menu, lobby, gameplay, etc.)
    - **Rendering/**: Starfield, particles, and rendering utilities
    - **BloomPostprocess/**: Bloom post-processing effects
    - **Content/**: Pre-built game assets (.xnb, textures, audio, fonts)

- **Platforms/**
  - Platform-specific project files:
    - **Windows/**: `NetRumble.Windows.csproj` (DirectX)
    - **Desktop/**: `NetRumble.DesktopGL.csproj` (OpenGL)
    - **Android/**: `NetRumble.Android.csproj` (Android)
    - **iOS/**: `NetRumble.iOS.csproj` (iOS)

- **NetRumble.sln**: Solution file for Visual Studio
- **README.md**: Project documentation

---

## Game Controls
Net Rumble uses the following keyboard and gamepad controls.

| Action                        | Keyboard Control           | Gamepad Control                  |
|-------------------------------|----------------------------|----------------------------------|
| Select a menu entry           | UP ARROW, DOWN ARROW       | Left thumb stick, D-Pad up/down  |
| Accept the menu selection     | SPACEBAR, ENTER            | **A**, **START**                 |
| Cancel the menu               | ESC                        | **B**, **BACK**                  |
| Move the ship                 | None (gamepad required)    | Left thumb stick                 |
| Fire the current weapon       | None (gamepad required)    | Right thumb stick                |
| Fire a mine behind the ship   | None (gamepad required)    | Right Trigger                    |
| Pause the game                | ESC                        | **START**, **BACK**              |

---

## Implementation Notes
Note the following areas when implementing the sample.

### Networking
The flow of a networked game, from creation/join to lobby to game and back again, is implemented with screens derived from the Game State Management sample code. Packets are sent between the players, each starting with a 32-bit integer value containing the packet type, defined in an enumeration in the `World` class. Each game represents a new *World object*, and the initial state of the game is generated by the host.

### Entity Management
The entity management and collision systems in Net Rumble are simple, given the small number of objects in any game. The systems use polymorphism to ensure a consistent set of interactions between all in-game objects, all of which derive from the `GameplayObject` class.

### CollectCollection
The `CollectCollection` class allows the game loop to target `GameplayObject` objects for removal from the game without actually removing them from the list. Removing an item from a list invalidates all iterators, which would disrupt the update loop. Objects to be removed are added to the Garbage list, and when the `CollectCollection.Collect` method is called, the items in the Garbage list are removed from the main list (and the Garbage list is cleared).

### Reuse of Existing Samples
Net Rumble uses the game screen management architecture from the Game State Management sample. The `BackgroundScreen` class animates a `Starfield` object while it overlays various menus, and the `GameplayScreen` owns the `World object` object that drives gameplay.

This game also uses the bloom post-processing component from the Bloom Postprocess sample. It is not added to the `Game` object's component list, as doing so would add bloom to all elements rendered by the screen management system, including the user interface elements. The `GameplayScreen` object creates and manages the component, such that the game elements are processed by the component but not the user interface.

### Gameplay Constants
You can alter gameplay in many important ways by adjusting the constant variables, which you can find in the relevant classes.

---

## Extending Net Rumble
There are many possible ways to improve on or extend Net Rumble.

- Add more weapons or power-ups by using new classes that mirror the existing ones.
- Customize the `World.GenerateWorld` function to create any number of possible level configurations. Other possibilities include adding a randomly generated maze, assuming you can guarantee that the players are never sealed away from one another.
- Add new levels, where victory is scored and the play moves on to a different level. This will require additional game-state management code to handle the additional game flow.
- Add interesting new strategies to the game by adding projectile interactions, such as projectiles that bounce off the walls.
- The collision system treats most of the in-game objects as circular, leading to some graphical anomalies, such as asteroids colliding outside their visible shape. Consider using the tutorial "Collision Series 2: 2D Per-Pixel Collision" to help create a pixel-accurate collision system. Note that momentum-transferring collisions, such as those between asteroids, ships, and the walls, may be greatly complicated by this procedure.
- Implement artificial-intelligence "bots" to fill up the gameplay session. Start by separating the control of the ship from the gamepad-handling techniques, generalizing to allow any source—gamepads, an artificial intelligence algorithm, or even network data—to control the ships.
- Support more than one player at once per machine. One possibility would be to display both ships at once using split-screen rendering.

There are also many possible optimizations that you could make. Many of these were not made because the current code is simpler, and the design of the game as it is today did not require them.

- The game allocates many objects on the heap during gameplay, which leads to occasional collections during gameplay. These collections can be noticeable on the Xbox 360 console. In the game as it is today, these are very slight, but it's generally good practice to make allocations before the main game loop begins. The major culprits are `ParticleSystem` objects and `Projectile` class-derived objects. One possible solution would be to add factories for these objects—factories that could recycle these objects when they are no longer needed.
- The collision system currently checks every object against every other object. A broad-pass collision check, to narrow the list of possible colliders, should improve performance. A common broad-pass method is spatial partitioning, which should work well in a game like Net Rumble. Note that the overhead from this additional pass is not free, and with as few objects as Net Rumble has, this optimization may or may not be worthwhile. If Net Rumble was modified to be more complex, then this optimization could be significant.
- The network session supports up to 16 players, but the current implementation may be sending too much traffic to support that many players on a high-latency connection. There are many ways to reduce the network traffic, potentially at the expense of fidelity (player warping, and so on).
- There are many small optimizations that you could make throughout the code. In general, the greatest gains will be made on high-frequency calls; for example, reducing operations that occurred for every particle in every frame would result in a large overall reduction, because of the large number of particles used.

---

## License

This project is based on the original Microsoft XNA Community Game Platform sample and is provided for educational and demonstration purposes.