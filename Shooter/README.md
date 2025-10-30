# MonoGame FPS - Educational 3D Shooter

A complete 3D first-person shooter built with MonoGame, ported from Unity's FPS Microgame. This project is designed as a learning resource for developers transitioning from Unity to MonoGame or learning 3D game development.

## 🎯 Project Goals

- **Educational**: Comprehensive comments explaining MonoGame concepts
- **Modular**: Plugin-based architecture for physics and graphics
- **Complete**: Full FPS implementation (player, weapons, AI, objectives)
- **Hackable**: Easy to extend and modify for learning

## 🏗️ Architecture Overview

### Plugin System
The project uses a provider-based plugin architecture allowing you to swap:
- **Physics Engines**: BepuPhysics (default), custom implementations
- **Graphics Renderers**: Basic forward renderer (default), extensible for advanced features
- **Navigation**: A* pathfinding, waypoint-based AI

### Core Systems
- **Entity-Component System**: Replaces Unity's GameObject/MonoBehaviour
- **Event Bus**: Decoupled communication between systems
- **Scene Management**: JSON-based level configuration
- **Service Locator**: Access to core services (Physics, Audio, Input, etc.)

## 📂 Project Structure

```
Shooter/
├── Shooter.Core/          # Core engine systems
│   ├── Components/            # Component base classes
│   ├── Entities/              # Entity management
│   ├── Events/                # Event system
│   ├── Plugins/               # Plugin interfaces
│   └── Services/              # Service locator & core services
├── Shooter.Physics/       # Physics provider implementations
│   ├── Bepu/                  # BepuPhysics provider
│   └── Interfaces/            # Physics abstractions
├── Shooter.Graphics/      # Rendering systems
│   ├── Providers/             # Graphics provider implementations
│   ├── Camera/                # Camera systems
│   └── Primitives/            # Debug rendering (colored shapes)
├── Shooter.Gameplay/      # Game-specific logic
│   ├── Player/                # Player controller, weapons
│   ├── AI/                    # Enemy controllers, detection
│   ├── Combat/                # Health, damage, projectiles
│   └── Objectives/            # Mission objectives
├── Shooter.UI/            # GUM-based user interface
│   ├── HUD/                   # In-game HUD
│   └── Menus/                 # Menu screens
├── Shooter/               # Main game project
│   ├── Content/               # Assets (models, textures, shaders)
│   └── Scenes/                # Level JSON files
└── Docs/                      # Documentation
    └── UnityToMonoGame.md     # Feature mapping reference
```

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code
- MonoGame 3.8.2+

### NuGet Packages
- `MonoGame.Framework.DesktopGL`
- `MonoGame.Content.Builder.Task`
- `MonoGame.Extended` (cameras, utilities)
- `BepuPhysics` (physics engine)
- `Gum.MonoGame` (UI framework)
- `Newtonsoft.Json` (scene serialization)

### Build & Run
```bash
dotnet restore
dotnet build
dotnet run --project Shooter/Shooter.csproj
```

## 🎓 Learning Path

1. **Start with Core**: Understand the component system (`Shooter.Core`)
2. **Physics Integration**: See how BepuPhysics integrates via plugins
3. **Player Controller**: Study first-person movement and camera
4. **Weapon System**: Learn projectile spawning and hit detection
5. **AI Basics**: Examine enemy detection and pathfinding
6. **UI with GUM**: Explore the HUD and menu systems

## 📖 Unity to MonoGame Mapping

See `Docs/UnityToMonoGame.md` for detailed feature comparisons and implementation strategies.

### Quick Reference
| Unity | MonoGame Equivalent |
|-------|---------------------|
| `MonoBehaviour` | `GameComponent` (custom) |
| `GameObject` | `Entity` (custom) |
| `Transform` | `Transform3D` component |
| `Rigidbody` | `PhysicsBody` component (Bepu wrapper) |
| `CharacterController` | `CharacterController` (custom) |
| Scene files | JSON scene data |
| Prefabs | Entity templates (JSON) |

## 🔧 Extending the Project

### Adding a New Weapon
1. Create weapon definition in `Content/Data/Weapons/`
2. Inherit from `WeaponBase` in `Shooter.Gameplay/Weapons/`
3. Define projectile behavior
4. Add weapon pickup to scene JSON

### Creating a New Enemy
1. Define enemy data in JSON
2. Create AI behavior class inheriting `EnemyController`
3. Configure detection and attack parameters
4. Add navigation waypoints

### Custom Physics Provider
1. Implement `IPhysicsProvider` interface
2. Register in `ServiceLocator`
3. Swap provider in `Game.Initialize()`

## 🤝 Contributing

This is an educational resource. Feel free to:
- Add more weapons, enemies, or features
- Improve documentation
- Create tutorial videos or guides
- Share your modifications

## 📜 License

This project is based on Unity's FPS Microgame sample. 
Code is provided for educational purposes.

## 🙏 Credits

- Original Unity FPS Microgame by Unity Technologies
- MonoGame by MonoGame Team
- BepuPhysics by Ross Nordby
- GUM UI by FlatRedBall

---

**Built with ❤️ for the MonoGame and game development learning community**
