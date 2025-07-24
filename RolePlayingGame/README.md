# Role-Playing Game Starter Kit

## Introduction
The Role-Playing Game Starter Kit is a complete XNA Game Studio game. The project comes ready to compile and run, and it's easy to customize with a little bit of C# programming. You are free to use the source code as the basis for your own XNA Game Studio game projects, and to share your work with others.

This starter kit contains a two-dimensional, tile-based, single-player, role-playing game. This game is complete with character classes, multiple party members, items, and quests. It also features a turn-based combat engine with a side-on presentation and animated characters and effects. All of the game data is exposed in XML files, making it easy to write your own quests, create your own towns, or even write your own storyline right into the game.

> **Note:** This documentation assumes that you have a basic knowledge of programming concepts and the Visual C# environment. You can learn more about these topics in the product documentation by clicking one of the **Help** menu items. Another way to learn is to position the mouse cursor on language keywords or user interface elements such as windows or dialog boxes and then press F1.

---

## Features
This starter kit provides a complete XNA Game Studio game. It includes source code and game content such as models, textures, and sounds. The starter kit documentation describes the general layout and controls for the Role-Playing Game Starter Kit.

### Tile Engine
- Two-dimensional overhead view of the world
- World layout controlled by four two-dimensional arrays of integers:
  - Base tiles
  - Fringe tiles
  - Objects
  - Collision
- Arbitrary player movement within tiles
- Calculation of collisions with the environment at the tile level
- Animated sprite support
- Visibility checks to make sure that only the tiles onscreen are submitted to the graphics device

### Turn-Based Combat Engine
- Two-dimensional view of the combat arena, with the player’s party lined up against the enemy forces
- Turn-based combat where each combatant chooses an action and executes it immediately
- Multiple supported actions per character, including attacking, defending, spell-casting, and item use
- Artificial intelligence that adapts to each enemy’s statistics and supported actions
- Animated combat actions and spell effects

### External Game Data
Nearly all game data is exposed in XML files outside of the game code, which means there is support for extensive modifications that don’t require any programming knowledge.

Supported game data types include:
- Maps
- Quests
- Party members
- Non-player characters
- Monsters
- Character classes
- Spells
- Equipment
- Items

These types have direct correlations in the RolePlayingGameData class library. These types have very little game logic in them to make them as easy to maintain and modify as possible.

### User Interface
- Features numerous screens, including inventory and dialogue menus
- Implemented using the Game State Management sample, which is available on the XNA Creators Club Online Web site

### Saving and Loading
- Save and load from any time when the party is in the overhead view
- Extensive serialization and deserialization system supports complete and efficient save games
- Side-by-side serialization types separate loading and saving code from gameplay code

---

## Project Structure

The project is organized for modern .NET and MonoGame development, with clear separation between shared and platform-specific code:

```
RolePlayingGame/
├── Core/                       # Shared game logic and systems
│   ├── Combat/                 # Combat system
│   ├── GameScreens/            # Game UI screens
│   ├── MenuScreens/            # Menu system
│   ├── Session/                # Save/load system
│   ├── TileEngine/             # 2D tile rendering
│   ├── ScreenManager/          # Screen management system
│   ├── AudioManager.cs         # Audio manager
│   ├── ...                     # Other shared files
│   └── RolePlayingGame.Core.csproj
├── Platforms/
│   ├── Windows/                # Windows DirectX platform
│   │   ├── Program.cs
│   │   └── RolePlayingGame.Windows.csproj
│   ├── Desktop/                # Cross-platform OpenGL
│   │   ├── Program.cs
│   │   └── RolePlayingGame.Desktop.csproj
│   ├── Android/                # Android platform
│   │   ├── Program.cs
│   │   └── RolePlayingGame.Android.csproj
│   └── iOS/                    # iOS platform
│       ├── Program.cs
│       └── RolePlayingGame.iOS.csproj
├── Content/                    # Game assets (textures, audio, data)
├── RolePlayingGameData/        # Shared game data library
│   └── RolePlayingGameData.csproj
├── README.md
└── RolePlayingGame.sln         # Solution file
```

- All shared game logic is in `/Core` and referenced by each platform project.
- Each platform has its own entry point and project file in `/Platforms/{Platform}`.
- Content files are shared and loaded by all platforms.

---

## Supported Platforms

- **Windows** (.NET 8.0-windows) - Uses MonoGame.Framework.WindowsDX
- **DesktopGL** (.NET 8.0) - Cross-platform using MonoGame.Framework.DesktopGL
- **Android** (.NET 8.0-android) - Uses MonoGame.Framework.Android
- **iOS** (.NET 8.0-ios) - Uses MonoGame.Framework.iOS

---

## Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or Visual Studio Code
- For Android development: Android SDK and workload
- For iOS development: Xcode and iOS workload (macOS only)

### Requirements
**Minimum Shader Profile:**
- Vertex Shader Model 1.1
- Pixel Shader Model 2.0

## Building the Project

### Using Visual Studio

1. Open `RolePlayingGame.sln` in Visual Studio 2022
2. Set your desired platform project as the startup project
3. Build and run (F5)

### Using Visual Studio Code

1. Open the project folder in VS Code
2. Use Ctrl+Shift+P and run "Tasks: Run Task"
3. Choose from available tasks:
   - `build-windows` - Build Windows version
   - `build-desktopgl` - Build DesktopGL version
   - `build-android` - Build Android version
   - `run-windows` - Build and run Windows version
   - `run-desktopgl` - Build and run DesktopGL version

### Using Command Line

```powershell
# Restore NuGet packages
dotnet restore

# Build specific platform
dotnet build Platforms/Windows/RolePlayingGame.Windows.csproj
dotnet build Platforms/DesktopGL/RolePlayingGame.DesktopGL.csproj
dotnet build Platforms/Android/RolePlayingGame.Android.csproj

# Run specific platform
dotnet run --project Platforms/Windows/RolePlayingGame.Windows.csproj
dotnet run --project Platforms/DesktopGL/RolePlayingGame.DesktopGL.csproj
```

## Debugging in VS Code

1. Press F5 or go to Run > Start Debugging
2. Choose between:
   - **Launch Windows** - Debug Windows version
   - **Launch DesktopGL** - Debug DesktopGL version
   - **Launch iOS** - Debug iOS version
   - **Launch Android** - Debug Android version

## Content Pipeline

This project uses pre-built .xnb content files located in the `Content/` directory. The content includes:

- **Audio**: Background music and sound effects
- **Textures**: Sprites, UI elements, and backgrounds  
- **Fonts**: Various game fonts
- **Data**: Character classes, spells, maps, quests (XML format)

## Platform-Specific Notes

### Windows
- Uses DirectX for optimal Windows performance
- Supports Windows-specific features like window controls

### DesktopGL  
- Cross-platform compatibility (Windows, macOS, Linux)
- Uses OpenGL for rendering
- Recommended for development and testing

### Android
- Requires Android SDK and Android workload for .NET
- Assets are stored in the Android Assets folder
- Supports various Android device configurations

### iOS
- Requires Xcode and iOS workload for .NET (macOS only)
- Uses bundle resources for content
- Supports iPhone and iPad

---

## Playing the Role-Playing Game

### Role-Playing Game Screens
The Role-Playing Game begins at the Main Menu.

#### Main Menu
From the Main Menu, you can start a **New Game**, **Load Game**, check the **Controls**, access **Help**, or **Exit**.

| Action                        | Controller                | Keyboard         |
|-------------------------------|---------------------------|------------------|
| Highlight a menu option       | D-Pad UP or D-Pad DOWN    | UP ARROW/DOWN ARROW |
| Select a highlighted option   | **A**                     | ENTER            |
| Exit the game                 | **BACK**                  | ESC              |

#### Exploring the World
You will explore the game world with your party by controlling your party leader from a top-down perspective.

| Action                        | Controller                | Keyboard         |
|-------------------------------|---------------------------|------------------|
| Move Up/Down/Left/Right       | Left Thumbstick Up/Down/Left/Right | UP/DOWN/LEFT/RIGHT |
| Enter Character Management    | Y                         | SPACE            |
| Return to the Main Menu       | START                     | TAB              |
| Exit the Game                 | BACK                      | ESC              |

#### Combat
When your characters enter combat, the perspective will shift to a side-on view. Control your characters in turn-based combat to defeat enemies.

| Action                        | Controller                | Keyboard         |
|-------------------------------|---------------------------|------------------|
| Change Active Character       | Left Thumbstick Left/Right| LEFT/RIGHT       |
| Change Menu Selection Up/Down | Left Thumbstick Up/Down   | UP/DOWN          |
| Select Option                 | A                         | ENTER            |
| Cancel Option                 | B                         | ESC              |
| Win Combat (Debug Only)       | Right Shoulder            | W                |

---

## Extending the Role-Playing Game
There are many ways to add your own functionality to the Role-Playing Game:

### Game Content
All of the gameplay content is contained in XML files loaded by the XNA Content Pipeline. These can be altered, or new files can be added and integrated into the game.

For example, you can edit `Content/MainGameDescription.xml` to add more members to the party at the start of the game, or edit `Content/Characters/Players/Kolatt.xml` to give him better equipment at the start of the game.

Additional changes could be made to the code to support additional features built on top of these data types. For example, you could change the quest system to support rewarding the party with a new party member.

All of the animations are implemented as sprite sheets, combined with animation definitions in the game types that use them (spells, characters, etc.).

There is a lot of extra content provided with this starter kit than is used in the pre-existing game. For example, walking animations for the map sprites for your party members are provided, and you could use these to draw all of the party members trailing behind the leader.

Additional content will be made available on the [XNA Creators Club Online](http://creators.xna.com) website that will describe some of these content-creation scenarios in more detail.

### Combat and Artificial Intelligence
The player chooses combat actions for the party members, and the code in `Combat/ArtificialIntelligence.cs` chooses combat actions for the monsters. This AI code can be customized to provide advanced behaviors, like healing fellow monsters or performing statistic-damaging attacks.

With more alterations to the combat engine, defined in `Combat/CombatEngine.cs`, the player's party can have NPCs of its own, fighting under AI control.

Additional combat actions could be added by deriving new classes from `CombatAction`, using the existing actions as a reference. The combat engine, in `Combat/CombatEngine.cs`, and the HUD (heads-up-display) menu, in `GameScreens/Hud.cs`, would have to be updated to support this additional action. For example, a Steal action would add a fun dimension to combat.

Another possibility is to extend the size of the party beyond the current four-member limit, and create a new menu to designate which party members will fight in combat.

The current combat system chooses sides randomly, and each side completes all of their moves before the other side may take their turn. You could interleave the combat turns and make the order deterministic by adding a dexterity or speed attribute to fighting characters.

### Re-using Game Systems
Many of the game systems represent universal concepts to role-playing games, such as quests and characters, that could be re-used in an original game engine. This would leverage the existing XNA Content Pipeline-based data management system while using these types in entirely new ways.

The game execution systems could also be decoupled from the rest of the starter kit and reused in new games. The tile engine, defined in `TileEngine.cs`, could be used in many different kinds of games.


### Tutorials
- [Tutorial 1: Quests](./Tutorial_1_Quest.md) - Learn how to add new quests and quest lines to expand your game's storyline and objectives.
- [Tutorial 2: Reusing the RPG Engine](./Tutorial_2_Engine.md) - Discover how to extract and reuse the tile engine for rendering and collision in your own projects.

---