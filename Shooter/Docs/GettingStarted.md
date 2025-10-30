# Getting Started with MonoGame FPS

This guide will help you understand the project structure and start building your first FPS game with MonoGame.

## Prerequisites

Before you begin, make sure you have:

1. **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download)
2. **Visual Studio 2022** or **Visual Studio Code**
3. **MonoGame 3.8.2+** - Install via NuGet

## Project Structure Overview

The solution is organized into several projects:

### 1. Shooter.Core (Core Engine)
The foundation of the game engine. Contains:
- **Components/**: Base component system (`GameComponent`, `Transform3D`)
- **Entities/**: Entity management system
- **Events/**: Event bus for decoupled communication
- **Plugins/**: Interfaces for physics and graphics providers
- **Services/**: Service locator and core services
- **Scenes/**: Scene management and JSON loading

### 2. Shooter.Physics (Physics Systems)
Physics provider implementations:
- **Bepu/**: BepuPhysics integration
- **Interfaces/**: Physics abstractions

### 3. Shooter.Graphics (Rendering)
Graphics and rendering systems:
- **Providers/**: Rendering implementations
- **Camera/**: Camera controllers
- **Primitives/**: Debug shapes and basic meshes

### 4. Shooter.Gameplay (Game Logic)
Game-specific components:
- **Player/**: Player controller, movement, weapons
- **AI/**: Enemy AI, detection, pathfinding
- **Combat/**: Health, damage, projectiles
- **Objectives/**: Mission systems

### 5. Shooter.UI (User Interface)
GUM-based UI:
- **HUD/**: Health bars, ammo counters, crosshair
- **Menus/**: Main menu, pause menu, game over screens

### 6. Shooter (Main Game)
The executable project that ties everything together.

## Understanding the Component System

Coming from Unity, the biggest shift is from `MonoBehaviour` to our custom component system.

### Creating a Component

```csharp
using Shooter.Core.Components;

namespace Shooter.Gameplay.Player
{
    /// <summary>
    /// Example: Health component for entities that can take damage
    /// </summary>
    public class Health : GameComponent
    {
        public float MaxHealth { get; set; } = 100f;
        public float CurrentHealth { get; private set; }
        
        public override void Initialize()
        {
            // Called once when component is added (like Awake/Start)
            CurrentHealth = MaxHealth;
        }
        
        public override void Update(GameTime gameTime)
        {
            // Called every frame
            // Access deltaTime: gameTime.ElapsedGameTime.TotalSeconds
        }
        
        public void TakeDamage(float amount)
        {
            CurrentHealth -= amount;
            
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            // Publish death event
            EventBus.Publish(new EntityDiedEvent 
            { 
                Entity = Owner 
            });
        }
        
        public override void OnDestroy()
        {
            // Clean up when component is removed
        }
    }
}
```

### Creating an Entity

```csharp
// Create a new entity
var enemy = new Entity("Enemy");

// Add components
var transform = enemy.AddComponent<Transform3D>();
transform.Position = new Vector3(10, 0, 10);

enemy.AddComponent<Health>().MaxHealth = 50f;
enemy.AddComponent<EnemyController>();

// Initialize (calls Initialize() on all components)
enemy.Initialize();

// Add to scene
scene.AddEntity(enemy);
```

## Working with Scenes

### Creating a Scene via JSON

Create a file in `Shooter/Content/Scenes/MyLevel.json`:

```json
{
  "name": "MyLevel",
  "entities": [
    {
      "name": "Player",
      "tag": "Player",
      "components": [
        {
          "type": "Transform3D",
          "position": [0, 2, 0]
        },
        {
          "type": "PlayerController"
        }
      ]
    }
  ]
}
```

### Loading a Scene

```csharp
var sceneManager = new SceneManager();

// Register custom component types
sceneManager.RegisterComponentType<PlayerController>("PlayerController");
sceneManager.RegisterComponentType<EnemyController>("EnemyController");

// Load the scene
await sceneManager.LoadSceneAsync("Content/Scenes/MyLevel.json");

// In your game loop
sceneManager.Update(gameTime);
sceneManager.Draw(gameTime);
```

## Using the Plugin System

### Physics Provider

```csharp
// In your Game.Initialize()
var physicsProvider = new BepuPhysicsProvider();
physicsProvider.Initialize();
ServiceLocator.Register<IPhysicsProvider>(physicsProvider);

// Later, in any component:
var physics = ServiceLocator.Get<IPhysicsProvider>();

// Create a physics body
var body = physics.CreateBody(new BodyDescription
{
    Position = transform.Position,
    BodyType = BodyType.Dynamic,
    Shape = new SphereShape(0.5f),
    Mass = 1.0f
});

// Raycast
if (physics.Raycast(origin, direction, 100f, out var hit))
{
    var hitEntity = hit.UserData as Entity;
    Console.WriteLine($"Hit: {hitEntity?.Name}");
}
```

### Graphics Provider

```csharp
// Register graphics provider
var graphicsProvider = new ForwardGraphicsProvider(GraphicsDevice);
ServiceLocator.Register<IGraphicsProvider>(graphicsProvider);

// Set up lighting
var lighting = new LightingConfiguration();
lighting.DirectionalLights.Add(new DirectionalLight
{
    Direction = new Vector3(-0.5f, -1, -0.5f),
    Color = new Vector3(1, 1, 1),
    Intensity = 1.0f
});
graphicsProvider.SetLighting(lighting);
```

## Event System

### Publishing Events

```csharp
// When player takes damage
EventBus.Publish(new DamageDealtEvent
{
    Target = playerEntity,
    Source = enemyEntity,
    Damage = 10f,
    HitPosition = hitPoint
});
```

### Subscribing to Events

```csharp
public class HealthBar : GameComponent
{
    public override void Initialize()
    {
        // Subscribe to damage events
        EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
    }
    
    private void OnDamageDealt(DamageDealtEvent evt)
    {
        // Update health bar UI
        if (evt.Target == Owner)
        {
            UpdateHealthDisplay();
        }
    }
    
    public override void OnDestroy()
    {
        // IMPORTANT: Always unsubscribe to prevent memory leaks!
        EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
    }
}
```

## Input Handling

```csharp
var input = ServiceLocator.Get<IInputService>();

// Get movement input (WASD or left stick)
Vector2 movement = input.GetMovementInput();

// Check for actions
if (input.IsJumpPressed())
{
    Jump();
}

if (input.IsFireHeld())
{
    FireWeapon();
}
```

## Common Patterns

### Finding Entities

```csharp
// By name
var player = scene.FindEntityByName("Player");

// By tag
var enemies = scene.FindEntitiesByTag("Enemy");

// By component
var damageable = scene.FindEntitiesWithComponent<Health>();
```

### Creating Prefabs (Entity Templates)

While Unity has prefabs, we use JSON templates:

```json
// WeaponTemplates/Rifle.json
{
  "name": "Rifle",
  "components": [
    {
      "type": "Transform3D"
    },
    {
      "type": "WeaponController",
      "fireRate": 10,
      "damage": 25,
      "range": 100
    }
  ]
}
```

Then instantiate programmatically:
```csharp
// Load template and create instance
var rifle = LoadEntityTemplate("WeaponTemplates/Rifle.json");
player.GetComponent<WeaponManager>().AddWeapon(rifle);
```

## Next Steps

1. **Read the Documentation**: Check `Docs/UnityToMonoGame.md` for detailed Unity-to-MonoGame mappings

2. **Explore Examples**: Look at the example scene in `Content/Scenes/ExampleScene.json`

3. **Build a Feature**: Try implementing:
   - A simple moving platform
   - A pickup item
   - A basic enemy AI

4. **Extend the System**: Add your own:
   - Custom components
   - Event types
   - Physics provider
   - Rendering techniques

## Tips for Unity Developers

1. **No Automatic Lifecycle**: You must explicitly call `Initialize()`, `Update()`, and `Draw()`

2. **Manual Memory Management**: No garbage collector magic - pool your objects!

3. **Explicit Dependencies**: Use `ServiceLocator` instead of singletons

4. **Code-First**: Less editor, more code. This is a feature, not a bug!

5. **Full Control**: You own the game loop, rendering pipeline, and physics integration

## Debugging

### Console Logging
```csharp
Console.WriteLine($"Player health: {health.CurrentHealth}");
```

### Debug Drawing
```csharp
var graphics = ServiceLocator.Get<IGraphicsProvider>();
graphics.DrawDebugPrimitive(new DebugPrimitive
{
    Type = DebugPrimitiveType.Line,
    Position = start,
    End = end,
    Color = new Vector4(1, 0, 0, 1), // Red
    Duration = 0 // One frame
});
```

### Performance Profiling
Use Visual Studio's profiling tools or add custom timers:

```csharp
var stopwatch = Stopwatch.StartNew();
// ... code to profile ...
stopwatch.Stop();
Console.WriteLine($"Took: {stopwatch.ElapsedMilliseconds}ms");
```

## Common Errors and Solutions

### "Service not registered"
**Solution**: Make sure you register all services in `Game.Initialize()`:
```csharp
ServiceLocator.Register<IInputService>(inputService);
```

### "Component type not found in factory"
**Solution**: Register custom components with SceneManager:
```csharp
sceneManager.RegisterComponentType<MyComponent>("MyComponent");
```

### Entities not updating
**Solution**: Ensure you're calling `sceneManager.Update(gameTime)` in your game loop

## Resources

- **MonoGame Documentation**: https://docs.monogame.net/
- **BepuPhysics v2**: https://github.com/bepu/bepuphysics2
- **Gum UI**: http://www.gumui.net/
- **This Project's Docs**: See `Docs/` folder for detailed guides

---

**Happy coding! Remember: With great power (full engine control) comes great responsibility (bugs are your fault) 😄**
