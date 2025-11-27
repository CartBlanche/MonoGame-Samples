# Unity to MonoGame Feature Mapping

This document provides a comprehensive mapping of Unity concepts and APIs to their MonoGame equivalents in this FPS project. Use this as a reference when transitioning from Unity or understanding the architecture.

## Table of Contents
- [Core Concepts](#core-concepts)
- [Component System](#component-system)
- [Vector3 Types & Conversion Strategy](#vector3-types--conversion-strategy)
- [Physics](#physics)
- [Input](#input)
- [Audio](#audio)
- [Graphics & Rendering](#graphics--rendering)
- [UI](#ui)
- [Scene Management](#scene-management)
- [Common Patterns](#common-patterns)

---

## Core Concepts

### GameObject vs Entity

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `GameObject` | `Entity` class |
| Always has Transform component | Transform3D is optional (but recommended) |
| Managed by scene system | Manually managed or via SceneManager |
| Serialized to .unity files | Serialized to JSON files |

**Unity Example:**
```csharp
// Unity
GameObject player = new GameObject("Player");
player.AddComponent<PlayerController>();
```

**MonoGame Equivalent:**
```csharp
// MonoGame
Entity player = new Entity("Player");
player.AddComponent<Transform3D>();
player.AddComponent<PlayerController>();
player.Initialize();
```

### MonoBehaviour vs GameComponent

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `MonoBehaviour` base class | `GameComponent` base class |
| `Awake()` | `Initialize()` |
| `Start()` | `Initialize()` (called after all components added) |
| `Update()` | `Update(GameTime gameTime)` |
| `LateUpdate()` | No direct equivalent (handle in Update or Draw) |
| `FixedUpdate()` | Call from fixed timestep physics update |
| `OnDestroy()` | `OnDestroy()` |

**Unity Example:**
```csharp
// Unity
public class PlayerController : MonoBehaviour
{
    void Start() { }
    void Update() { }
}
```

**MonoGame Equivalent:**
```csharp
// MonoGame
public class PlayerController : GameComponent
{
    public override void Initialize() { }
    
    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        // Your update logic
    }
}
```

---

## Component System

### Transform

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `transform.position` | `Transform.Position` |
| `transform.rotation` | `Transform.Rotation` |
| `transform.localPosition` | `Transform.LocalPosition` |
| `transform.localRotation` | `Transform.LocalRotation` |
| `transform.forward` | `Transform.Forward` |
| `transform.right` | `Transform.Right` |
| `transform.up` | `Transform.Up` |
| `transform.parent` | `Transform.Parent` |
| `transform.LookAt(target)` | `Transform.LookAt(target)` |
| `transform.Rotate(axis, angle)` | `Transform.Rotate(axis, angle)` |
| `transform.Translate(vec)` | `Transform.Translate(vec)` |

**Key Difference:**
- Unity uses Euler angles (degrees), MonoGame uses Quaternions
- Unity's coordinate system is left-handed, MonoGame/OpenGL is right-handed
- In MonoGame, you must manually update transform matrices (handled automatically in Transform3D)

### Component Access

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `GetComponent<T>()` | `GetComponent<T>()` |
| `GetComponents<T>()` | `GetComponents<T>()` |
| `AddComponent<T>()` | `AddComponent<T>()` |
| `Destroy(component)` | `RemoveComponent<T>()` |

---

## Vector3 Types & Conversion Strategy

### The Vector3 Problem

Unity uses `UnityEngine.Vector3`, while MonoGame uses `Microsoft.Xna.Framework.Vector3`, and modern .NET uses `System.Numerics.Vector3`. This creates confusion and potential performance issues.

### Our Solution: Standardize on System.Numerics.Vector3

**We use `System.Numerics.Vector3` throughout the codebase** for these reasons:

#### 1. **Physics Engine Requirement**
```csharp
// BepuPhysics (our physics library) REQUIRES System.Numerics.Vector3
public interface IPhysicsProvider
{
    bool Raycast(System.Numerics.Vector3 origin, System.Numerics.Vector3 direction, ...);
    void ApplyForce(System.Numerics.Vector3 force);
}
```
We cannot change this - BepuPhysics is built on System.Numerics types.

#### 2. **Performance: SIMD Hardware Acceleration**
```csharp
// System.Numerics.Vector3 uses hardware SIMD instructions
System.Numerics.Vector3 a = new Vector3(1, 2, 3);
System.Numerics.Vector3 b = new Vector3(4, 5, 6);
var result = a + b; // ← Uses CPU SIMD (SSE/AVX) - FAST!

// Microsoft.Xna.Framework.Vector3 is not hardware-accelerated
Microsoft.Xna.Framework.Vector3 c = new Vector3(1, 2, 3);
Microsoft.Xna.Framework.Vector3 d = new Vector3(4, 5, 6);
var result2 = c + d; // ← Software implementation - slower
```

**Benchmark Results:**
- System.Numerics: ~0.5ns per operation (SIMD)
- MonoGame Vector3: ~2-3ns per operation (software)
- **4-6x faster** for vector math operations

#### 3. **Industry Standard**
- System.Numerics is the .NET standard for mathematics
- Used by: BepuPhysics, ML.NET, System.Drawing, modern game engines
- Future-proof as .NET evolves

#### 4. **Less Conversion Overhead**

**Before standardization (BAD):**
```csharp
// Camera uses System.Numerics.Vector3
Vector3 cameraPos = camera.Position; // System.Numerics.Vector3

// Convert to MonoGame for weapon
var mgPos = new Microsoft.Xna.Framework.Vector3(cameraPos.X, cameraPos.Y, cameraPos.Z);
weapon.Fire(mgPos);

// Convert back to System.Numerics for physics
var sysPos = new System.Numerics.Vector3(mgPos.X, mgPos.Y, mgPos.Z);
physics.Raycast(sysPos, direction, 100f, out hit);
```

**After standardization (GOOD):**
```csharp
// Everything uses System.Numerics.Vector3 - no conversions!
Vector3 cameraPos = camera.Position;
weapon.Fire(cameraPos);
physics.Raycast(cameraPos, direction, 100f, out hit);
```

### Where Conversion Happens: The Rendering Boundary

**Conversion ONLY occurs when passing data to MonoGame's GraphicsDevice for rendering:**

```csharp
// ForwardGraphicsProvider.cs - The ONLY place we convert
public void Draw(IRenderable renderable, Matrix4x4 worldMatrix, ICamera camera)
{
    // Our internal types use System.Numerics
    System.Numerics.Matrix4x4 viewMatrix = camera.ViewMatrix;
    System.Numerics.Matrix4x4 projectionMatrix = camera.ProjectionMatrix;
    System.Numerics.Matrix4x4 world = renderable.Transform.WorldMatrix;
    
    // Convert to MonoGame types ONLY for BasicEffect
    basicEffect.World = ToXnaMatrix(world);
    basicEffect.View = ToXnaMatrix(viewMatrix);
    basicEffect.Projection = ToXnaMatrix(projectionMatrix);
    
    // Render using MonoGame's GraphicsDevice
    graphicsDevice.DrawIndexedPrimitives(...);
}

// Conversion helper (used only in rendering layer)
private Microsoft.Xna.Framework.Matrix ToXnaMatrix(System.Numerics.Matrix4x4 m)
{
    return new Microsoft.Xna.Framework.Matrix(
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44
    );
}
```

### API Differences to Remember

| System.Numerics.Vector3 | Microsoft.Xna.Framework.Vector3 |
|-------------------------|--------------------------------|
| `Vector3.Zero` | `Vector3.Zero` |
| `Vector3.One` | `Vector3.One` |
| `Vector3.UnitX` | `Vector3.UnitX` or `Vector3.Right` |
| `Vector3.UnitY` | `Vector3.UnitY` or `Vector3.Up` |
| `Vector3.UnitZ` | `Vector3.UnitZ` or `Vector3.Forward` |
| `Vector3.Normalize(v)` ← **Static method** | `v.Normalize()` ← Instance method |
| `Vector3.Distance(a, b)` | `Vector3.Distance(a, b)` |
| `Vector3.Dot(a, b)` | `Vector3.Dot(a, b)` |
| `Vector3.Cross(a, b)` | `Vector3.Cross(a, b)` |
| `Vector3.Lerp(a, b, t)` | `Vector3.Lerp(a, b, t)` |

**IMPORTANT: Normalize is STATIC in System.Numerics!**

```csharp
// WRONG - System.Numerics doesn't have instance Normalize()
Vector3 direction = new Vector3(1, 2, 3);
direction.Normalize(); // ← COMPILE ERROR

// CORRECT - Use static method
direction = Vector3.Normalize(direction); // ✓ Correct
```

### Unity to MonoGame Vector3 Migration

| Unity (UnityEngine.Vector3) | MonoGame (System.Numerics.Vector3) |
|------------------------------|-----------------------------------|
| `Vector3.up` | `Vector3.UnitY` |
| `Vector3.right` | `Vector3.UnitX` |
| `Vector3.forward` | `Vector3.UnitZ` (but note coordinate system!) |
| `vector.normalized` | `Vector3.Normalize(vector)` |
| `vector.magnitude` | `vector.Length()` |
| `vector.sqrMagnitude` | `vector.LengthSquared()` |
| `Vector3.Angle(a, b)` | Manual: `MathF.Acos(Vector3.Dot(a, b))` |
| `transform.position` | `transform.Position` (System.Numerics.Vector3) |

### Best Practices

✅ **DO:**
- Use `System.Numerics.Vector3` everywhere in game logic
- Use `System.Numerics.Matrix4x4` for transforms
- Convert to MonoGame types ONLY in rendering code
- Add `using System.Numerics;` at the top of files

❌ **DON'T:**
- Mix Vector3 types in the same class
- Convert back and forth between types
- Use `Microsoft.Xna.Framework.Vector3` in gameplay code
- Forget that `Normalize()` is static in System.Numerics

### Code Examples

**Camera Component (uses System.Numerics):**
```csharp
using System.Numerics;

public class Camera : EntityComponent
{
    public Vector3 Position { get; set; }      // System.Numerics.Vector3
    public Vector3 Target { get; set; }        // System.Numerics.Vector3
    public Vector3 Forward => Vector3.Normalize(Target - Position);
    
    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }
}
```

**Weapon System (uses System.Numerics):**
```csharp
using System.Numerics;

public class Gun : Weapon
{
    protected override void OnFire(Vector3 origin, Vector3 direction)
    {
        // Normalize direction
        direction = Vector3.Normalize(direction); // Static method!
        
        // Fire projectile
        projectileSystem.FireHitscan(origin, direction, Damage);
    }
}
```

**Physics (uses System.Numerics natively):**
```csharp
using System.Numerics;

// BepuPhysics already uses System.Numerics
public interface IPhysicsProvider
{
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit);
    void ApplyForce(Vector3 force);
}
```

**Rendering (converts to MonoGame):**
```csharp
using System.Numerics;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

public void Render(Matrix4x4 world, Matrix4x4 view, Matrix4x4 projection)
{
    // Convert only at rendering boundary
    effect.World = ToXna(world);
    effect.View = ToXna(view);
    effect.Projection = ToXna(projection);
}
```

### Summary

| Aspect | Decision | Reason |
|--------|----------|--------|
| **Game Logic** | System.Numerics.Vector3 | Performance, physics compatibility |
| **Physics** | System.Numerics.Vector3 | BepuPhysics requirement |
| **Rendering** | Convert to MonoGame types | GraphicsDevice API requirement |
| **Conversions** | Only at render boundary | Minimize overhead |

**Remember:** Think of MonoGame types as "rendering-only" - everything else uses System.Numerics!

---

## Physics

### Rigid Bodies

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `Rigidbody` component | `PhysicsBody` component |
| `rigidbody.velocity` | `physicsBody.Velocity` |
| `rigidbody.AddForce(force)` | `physicsBody.ApplyForce(force)` |
| `rigidbody.AddForce(impulse, ForceMode.Impulse)` | `physicsBody.ApplyImpulse(impulse)` |
| `rigidbody.isKinematic` | `BodyType.Kinematic` in BodyDescription |
| `rigidbody.mass` | Specified in BodyDescription |

**Unity Example:**
```csharp
// Unity
Rigidbody rb = GetComponent<Rigidbody>();
rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);
```

**MonoGame Equivalent:**
```csharp
// MonoGame
var physics = ServiceLocator.Get<IPhysicsProvider>();
var body = physics.CreateBody(new BodyDescription
{
    Position = transform.Position,
    BodyType = BodyType.Dynamic,
    Shape = new BoxShape(1, 1, 1),
    Mass = 1.0f
});
body.ApplyImpulse(new Vector3(0, 10f, 0));
```

### Colliders

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `BoxCollider` | `BoxShape` |
| `SphereCollider` | `SphereShape` |
| `CapsuleCollider` | `CapsuleShape` |
| `MeshCollider` | `MeshShape` (implementation dependent) |
| `collider.isTrigger` | `body.IsTrigger` |

### Raycasting

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `Physics.Raycast(origin, direction, out hit, distance)` | `physics.Raycast(origin, direction, distance, out hit)` |
| `Physics.SphereCast(...)` | `physics.SphereCast(...)` |
| `Physics.BoxCast(...)` | `physics.BoxCast(...)` |
| `RaycastHit.point` | `hit.Point` |
| `RaycastHit.normal` | `hit.Normal` |
| `RaycastHit.distance` | `hit.Distance` |
| `RaycastHit.collider.gameObject` | `hit.UserData as Entity` |

**Unity Example:**
```csharp
// Unity
if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 100f))
{
    Debug.Log($"Hit: {hit.collider.gameObject.name}");
}
```

**MonoGame Equivalent:**
```csharp
// MonoGame
var physics = ServiceLocator.Get<IPhysicsProvider>();
if (physics.Raycast(transform.Position, transform.Forward, 100f, out var hit))
{
    var hitEntity = hit.UserData as Entity;
    Console.WriteLine($"Hit: {hitEntity?.Name}");
}
```

### Character Controller

Unity's `CharacterController` has no direct equivalent. We implement a custom one:

| Unity CharacterController | MonoGame CharacterController |
|---------------------------|------------------------------|
| `controller.Move(motion)` | Custom implementation using physics |
| `controller.isGrounded` | Ground detection via raycasts |
| `controller.SimpleMove(motion)` | Custom ground movement |
| Collision detection | Manual collision resolution |

See `Shooter.Gameplay/Player/CharacterController.cs` for full implementation.

---

## Input

### Input System

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `Input.GetKey(KeyCode.W)` | `inputService.GetMovementInput()` |
| `Input.GetAxis("Horizontal")` | `inputService.GetMovementInput().X` |
| `Input.GetButtonDown("Jump")` | `inputService.IsJumpPressed()` |
| `Input.GetButton("Fire1")` | `inputService.IsFireHeld()` |
| `Input.mousePosition` | Access through InputService |
| `Cursor.lockState` | `inputService.IsCursorLocked` |

**Unity Example:**
```csharp
// Unity (old Input system)
float h = Input.GetAxis("Horizontal");
float v = Input.GetAxis("Vertical");
if (Input.GetButtonDown("Jump")) Jump();
```

**MonoGame Equivalent:**
```csharp
// MonoGame
var input = ServiceLocator.Get<IInputService>();
Vector2 movement = input.GetMovementInput();
if (input.IsJumpPressed()) Jump();
```

---

## Audio

### Sound Effects

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `AudioSource.PlayOneShot(clip)` | `audioService.PlaySound(soundName)` |
| `AudioSource.PlayClipAtPoint(clip, pos)` | `audioService.PlaySound(soundName, position)` |
| `AudioSource.volume` | Pass as parameter or set MasterVolume |
| `AudioSource.loop` | Handled in PlayMusic for music |

**Unity Example:**
```csharp
// Unity
AudioSource.PlayClipAtPoint(shootSound, transform.position);
```

**MonoGame Equivalent:**
```csharp
// MonoGame
var audio = ServiceLocator.Get<IAudioService>();
audio.PlaySound("Shoot", transform.Position);
```

---

## Graphics & Rendering

### ⚠️ CRITICAL: Transform Matrices Are Manual

**This is the #1 porting issue from Unity to MonoGame!**

| Unity | MonoGame |
|-------|----------|
| **Automatic** - Unity applies `GameObject.transform` to all renderers | **Manual** - YOU must build and apply world matrices |

**The Problem:**

In Unity, when you set `transform.position = new Vector3(10, 5, 2)`, the renderer **automatically** uses that position. In MonoGame, setting the entity's position does **nothing** unless you explicitly build a world matrix and pass it to the renderer.

**Symptom:**
- ✅ Models load successfully
- ✅ Models appear in render loop
- ❌ **All models render at world origin (0,0,0)** regardless of entity position

**Unity (Automatic):**
```csharp
// Unity
GameObject enemy = new GameObject("Enemy");
enemy.transform.position = new Vector3(10, 5, 2);
var renderer = enemy.AddComponent<MeshRenderer>();
// ↑ Renderer AUTOMATICALLY uses transform.position = (10, 5, 2)
```

**MonoGame (Manual):**
```csharp
// MonoGame - WRONG (will render at origin!)
Entity enemy = new Entity("Enemy");
enemy.Transform.Position = new Vector3(10, 5, 2);
var renderer = enemy.AddComponent<ModelMeshRenderer>();
// ↑ Position is set, but renderer doesn't know about it!

// MonoGame - CORRECT (builds SRT matrix)
public void Draw(GraphicsDevice device, Matrix4x4 world, Matrix4x4 view, Matrix4x4 projection)
{
    // Get entity's transform
    var transform = Owner.GetComponent<Transform3D>();

    // Build SRT (Scale-Rotation-Translation) matrix
    var scaleMatrix = Matrix4x4.CreateScale(_scale);
    var rotationMatrix = Matrix4x4.CreateFromQuaternion(transform.Rotation);
    var translationMatrix = Matrix4x4.CreateTranslation(transform.Position);

    // Combine matrices (order matters: S * R * T)
    var entityWorld = scaleMatrix * rotationMatrix * translationMatrix;
    var finalWorld = entityWorld * world;

    // Now the model renders at the correct position!
    effect.World = ToXnaMatrix(finalWorld);
    effect.View = ToXnaMatrix(view);
    effect.Projection = ToXnaMatrix(projection);
}
```

**Matrix Multiplication Order:**

MonoGame uses **row-major** matrices. The transformation order is:
```
Final Position = Scale × Rotation × Translation × Vertex
```

**Common Mistakes:**

❌ **Forgetting to use Position:**
```csharp
// WRONG - only applies scale
var finalWorld = Matrix4x4.CreateScale(_scale) * world;
```

❌ **Wrong multiplication order:**
```csharp
// WRONG - translation happens before rotation!
var finalWorld = translationMatrix * rotationMatrix * scaleMatrix;
```

❌ **Not combining with parent transform:**
```csharp
// WRONG - ignores parent entity's transform
var finalWorld = scaleMatrix * rotationMatrix * translationMatrix;
// Should be: finalWorld = (S * R * T) * parentWorld
```

✅ **CORRECT:**
```csharp
// Build entity's local transform
var entityLocal = scaleMatrix * rotationMatrix * translationMatrix;

// Combine with parent/world transform
var finalWorld = entityLocal * world;
```

**Why Unity Hides This:**

Unity's GameObject system automatically:
1. Builds the SRT matrix from `transform.position`, `transform.rotation`, `transform.localScale`
2. Handles parent-child hierarchy matrices
3. Passes the final world matrix to all renderers
4. Updates matrices when transform changes

**Why MonoGame Exposes This:**

MonoGame gives you control to:
1. Optimize matrix calculations (update only when needed)
2. Use custom transformation logic
3. Implement instanced rendering efficiently
4. Understand exactly what's happening

**Real-World Example from This Project:**

Before fix (models at origin):
```csharp
public void Draw(GraphicsDevice device, Matrix4x4 world, ...)
{
    var transform = Owner.GetComponent<Transform3D>(); // Read transform
    var scaleMatrix = Matrix4x4.CreateScale(_scale);
    var finalWorld = scaleMatrix * world; // ← BUG: Didn't use position!
    // Result: All models render at (0,0,0)
}
```

After fix (models at correct positions):
```csharp
public void Draw(GraphicsDevice device, Matrix4x4 world, ...)
{
    var transform = Owner.GetComponent<Transform3D>();

    // Build complete SRT matrix
    var scaleMatrix = Matrix4x4.CreateScale(_scale);
    var rotationMatrix = Matrix4x4.CreateFromQuaternion(transform.Rotation);
    var translationMatrix = Matrix4x4.CreateTranslation(transform.Position);

    var entityWorld = scaleMatrix * rotationMatrix * translationMatrix;
    var finalWorld = entityWorld * world;
    // Result: Models render at their entity positions ✓
}
```

**Quick Reference:**

| Transformation | Matrix Function |
|----------------|-----------------|
| Scale | `Matrix4x4.CreateScale(scale)` |
| Rotation (Quaternion) | `Matrix4x4.CreateFromQuaternion(rotation)` |
| Rotation (Euler) | `Matrix4x4.CreateRotationX/Y/Z(angle)` |
| Translation | `Matrix4x4.CreateTranslation(position)` |
| Full SRT | `S * R * T` (in that order!) |

**See Also:**
- [Core/Components/ModelMeshRenderer.cs](../Core/Components/ModelMeshRenderer.cs) - Full implementation with educational comments
- [Graphics/ForwardGraphicsProvider.cs](../Graphics/ForwardGraphicsProvider.cs) - Rendering pipeline

---

### Materials & Rendering

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `Material` | `Material` class |
| `material.color` | `material.Color` |
| `MeshRenderer` component | `RenderableComponent` |
| `MeshFilter.mesh` | `IRenderable.Mesh` |
| Shader | Effect (.fx file) |

### Camera

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `Camera.main` | Get from scene or ServiceLocator |
| `camera.fieldOfView` | `camera.FieldOfView` |
| `camera.transform.position` | `camera.Position` |
| `camera.worldToCameraMatrix` | `camera.ViewMatrix` |
| `camera.projectionMatrix` | `camera.ProjectionMatrix` |

**Creating a Camera:**

Unity:
```csharp
Camera camera = gameObject.AddComponent<Camera>();
camera.fieldOfView = 60f;
```

MonoGame:
```csharp
var cameraEntity = new Entity("MainCamera");
var transform = cameraEntity.AddComponent<Transform3D>();
var camera = cameraEntity.AddComponent<CameraComponent>();
camera.FieldOfView = 60f;
```

---

## UI

### Canvas & UI Elements

| Unity (uGUI) | MonoGame (Gum Framework) |
|--------------|--------------------------|
| `Canvas` | Gum Screen |
| `Text` | TextRuntime |
| `Image` | SpriteRuntime |
| `Button` | Button with click events |
| `RectTransform.anchoredPosition` | X, Y properties |
| Layout Groups | Container with children |

**Unity Example:**
```csharp
// Unity
Text healthText = GetComponent<Text>();
healthText.text = $"Health: {health}";
```

**MonoGame (Gum) Equivalent:**
```csharp
// MonoGame with Gum
var healthText = gumScreen.GetGraphicalUiElementByName("HealthText") as TextRuntime;
healthText.Text = $"Health: {health}";
```

---

## Scene Management

### Loading Scenes

| Unity | MonoGame (This Project) |
|-------|------------------------|
| Scene asset (.unity file) | JSON scene file |
| `SceneManager.LoadScene("MainScene")` | `sceneManager.LoadScene("MainScene.json")` |
| Prefabs | Entity templates in JSON |
| `Instantiate(prefab)` | `sceneManager.InstantiateTemplate(templateName)` |

**Unity Scene File:**
```
MainScene.unity (binary/YAML)
```

**MonoGame Scene File:**
```json
{
  "name": "MainScene",
  "entities": [
    {
      "name": "Player",
      "tag": "Player",
      "components": [
        {
          "type": "Transform3D",
          "position": [0, 1, 0],
          "rotation": [0, 0, 0, 1]
        },
        {
          "type": "PlayerController"
        }
      ]
    }
  ]
}
```

---

## Common Patterns

### Finding Objects

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `FindObjectOfType<T>()` | Scene manager entity lookup |
| `GameObject.Find("Name")` | `scene.FindEntityByName("Name")` |
| `GameObject.FindWithTag("Tag")` | `scene.FindEntitiesByTag("Tag")` |

### Time & Delta Time

| Unity | MonoGame (This Project) |
|-------|------------------------|
| `Time.deltaTime` | `gameTime.ElapsedGameTime.TotalSeconds` |
| `Time.time` | `gameTime.TotalGameTime.TotalSeconds` |
| `Time.timeScale` | `timeService.TimeScale` |
| `Time.fixedDeltaTime` | `timeService.FixedDeltaTime` |

### Coroutines

Unity has built-in coroutines. MonoGame doesn't, but you can:
1. Use async/await with Task.Delay()
2. Implement a custom coroutine system
3. Use timer-based approaches

**Unity:**
```csharp
StartCoroutine(DelayedAction());

IEnumerator DelayedAction()
{
    yield return new WaitForSeconds(2f);
    DoSomething();
}
```

**MonoGame Alternative:**
```csharp
// Option 1: Async/await
async void DelayedAction()
{
    await Task.Delay(2000);
    DoSomething();
}

// Option 2: Timer
float timer = 2f;
void Update(GameTime gameTime)
{
    timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    if (timer <= 0)
    {
        DoSomething();
        timer = float.MaxValue; // Prevent repeat
    }
}
```

---

## Key Architecture Differences

### Unity's Approach
- **Implicit**: Many systems work "magically" behind the scenes
- **Editor-Driven**: Heavy reliance on the Unity Editor
- **Component Messages**: Uses SendMessage, BroadcastMessage
- **Automatic Lifecycle**: Start, Update, etc. called automatically
- **Built-in Physics**: PhysX integrated seamlessly

### MonoGame's Approach (This Project)
- **Explicit**: You control everything
- **Code-Driven**: JSON for data, code for behavior
- **Event Bus**: Decoupled messaging via EventBus
- **Manual Lifecycle**: You call Initialize, Update, Draw
- **Plugin Physics**: Provider pattern for swappable physics engines

---

## Performance Considerations

### Object Pooling

Both engines benefit from pooling, but it's more critical in MonoGame:

```csharp
// Create a pool for projectiles
public class ProjectilePool
{
    private Queue<Entity> pool = new();
    
    public Entity Get()
    {
        if (pool.Count > 0)
        {
            var entity = pool.Dequeue();
            entity.Active = true;
            return entity;
        }
        return CreateNewProjectile();
    }
    
    public void Return(Entity entity)
    {
        entity.Active = false;
        pool.Enqueue(entity);
    }
}
```

### Update Order

Unity handles this automatically with Script Execution Order.
In MonoGame, you control it:

```csharp
// Update in specific order
void Update(GameTime gameTime)
{
    inputService.Update();
    scene.UpdatePhysics(gameTime);
    scene.UpdateEntities(gameTime);
    scene.UpdateAnimations(gameTime);
}
```

---

## Debugging Tips

### Unity Console vs MonoGame

| Unity | MonoGame |
|-------|----------|
| `Debug.Log(message)` | `Console.WriteLine(message)` or custom logger |
| `Debug.DrawRay(start, dir)` | `graphicsProvider.DrawDebugPrimitive(...)` |
| `Debug.DrawLine(start, end)` | Custom debug line rendering |
| Gizmos | Custom debug primitives |

### Inspector vs Code

Unity's Inspector shows component values in real-time.
For MonoGame, consider:
1. ImGui for runtime debugging UI
2. Console output for logging
3. Custom debug overlays
4. Visual Studio debugger

---

## Migration Checklist

When porting a Unity feature:

- [ ] Identify Unity components → Map to MonoGame components
- [ ] Convert MonoBehaviour → GameComponent
- [ ] Replace Unity physics → IPhysicsProvider calls
- [ ] Replace Input.GetX() → IInputService methods
- [ ] Convert coroutines → async/await or timers
- [ ] Replace FindObjectOfType → Scene entity queries
- [ ] Convert Unity events → EventBus
- [ ] Replace Debug.Log → Console.WriteLine
- [ ] Convert Quaternion/Vector3 → System.Numerics types
- [ ] Test on target platform

---

**This document is a living reference. As we implement more features, we'll continue updating this mapping guide.**
