using Microsoft.Xna.Framework.Input;
using Shooter.Core.Services;
using Shooter.Core.Plugins.Physics;
using Shooter.Core.Plugins.Graphics;
using Shooter.Graphics;
using Shooter.Physics;
using Shooter.Gameplay.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Shooter.Core.Scenes;

namespace Shooter;

/// <summary>
/// Main game class for MonoGame FPS.
/// This is the entry point for the game, similar to Unity's main scene setup.
/// 
/// UNITY COMPARISON:
/// Unity doesn't have a single "Game" class. Instead:
/// - This replaces Unity's Application class
/// - Initialize() = Scene load + Awake()
/// - Update() = Update() across all GameObjects
/// - Draw() = Camera rendering (handled by Unity automatically)
/// </summary>
public class ShooterGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private SceneManager? _sceneManager;

    public ShooterGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;
        
        // Set up window
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
        _graphics.IsFullScreen = false;
        _graphics.SynchronizeWithVerticalRetrace = true;
        
        Window.Title = "MonoGame FPS - Educational 3D Shooter";
    }

    /// <summary>
    /// Initialize the game.
    /// Similar to Unity's Awake() but for the entire application.
    /// </summary>
    protected override void Initialize()
    {
        // Initialize service locator
        ServiceLocator.Initialize();
        
        // Phase 1: Register core services
        var inputService = new InputService();
        var timeService = new TimeService();
        var audioService = new AudioService();
        var physicsProvider = new BepuPhysicsProvider();
        var graphicsProvider = new ForwardGraphicsProvider();
        
        // Phase 2: Register gameplay systems
        var projectileSystem = new Gameplay.Systems.ProjectileSystem(physicsProvider);
        
        // Initialize input service with screen center for mouse locking
        var screenCenter = new Microsoft.Xna.Framework.Point(
            _graphics.PreferredBackBufferWidth / 2,
            _graphics.PreferredBackBufferHeight / 2
        );
        inputService.Initialize(screenCenter);
        
        // Initialize physics and graphics providers
        physicsProvider.Initialize();
        
        ServiceLocator.Register<IInputService>(inputService);
        ServiceLocator.Register<ITimeService>(timeService);
        ServiceLocator.Register<IAudioService>(audioService);
        ServiceLocator.Register<IPhysicsProvider>(physicsProvider);
        ServiceLocator.Register<IGraphicsProvider>(graphicsProvider);
        ServiceLocator.Register<Gameplay.Systems.ProjectileSystem>(projectileSystem);
        
        // Initialize graphics provider with our GraphicsDevice
        graphicsProvider.SetGraphicsDevice(GraphicsDevice);
        
        // Create scene manager
        _sceneManager = new SceneManager();
        
        // TODO: Register custom component types
        // _sceneManager.RegisterComponentType<PlayerController>("PlayerController");
        
        base.Initialize();
        
        Console.WriteLine("MonoGame FPS initialized!");
        Console.WriteLine("Phase 1 services registered:");
        Console.WriteLine("  - InputService (keyboard, mouse, gamepad)");
        Console.WriteLine("  - TimeService (delta time, time scaling)");
        Console.WriteLine("  - AudioService (placeholder for Phase 4)");
        Console.WriteLine("  - BepuPhysicsProvider (multi-threaded physics)");
        Console.WriteLine("  - ForwardGraphicsProvider (BasicEffect rendering)");
    }
    
    /// <summary>
    /// Create a simple test scene for Phase 1 validation.
    /// This demonstrates:
    /// - Camera setup
    /// - Static and dynamic physics bodies
    /// - Mesh rendering with different colors
    /// - Basic 3D scene composition
    /// 
    /// PHASE 2 UPDATE:
    /// - Player entity with FirstPersonController for movement
    /// - Mouse look and WASD controls enabled
    /// </summary>
    private void CreateTestScene()
    {
        // Create a new scene
        var scene = new Core.Scenes.Scene("Phase2TestScene");
        _sceneManager!.UnloadScene();
        
        // Create Player Entity with FPS Controller
        var playerEntity = new Core.Entities.Entity("Player");
        playerEntity.Tag = "Player";
        
        var playerTransform = playerEntity.AddComponent<Core.Components.Transform3D>();
        playerTransform.Position = new System.Numerics.Vector3(0, 2, 10); // Start above ground, back from origin
        
        // Add Camera to player
        var camera = playerEntity.AddComponent<Core.Components.Camera>();
        camera.FieldOfView = 75f;
        camera.NearPlane = 0.1f;
        camera.FarPlane = 100f;
        
        // Add FirstPersonController for movement (kinematic movement, no physics yet)
        var fpsController = playerEntity.AddComponent<Gameplay.Components.FirstPersonController>();
        fpsController.MoveSpeed = 50.0f; // Tuned for comfortable movement
        fpsController.MouseSensitivity = 50.0f; // Higher sensitivity for normalized delta (pixel delta normalized to -1 to 1 range)
        fpsController.JumpForce = 8.0f;
        
        // Add WeaponController for weapon management
        var weaponController = playerEntity.AddComponent<Gameplay.Components.WeaponController>();
        
        // Add Health to player
        var playerHealth = playerEntity.AddComponent<Gameplay.Components.Health>();
        playerHealth.MaxHealth = 100f;
        
        // Add HUD component to player
        var hud = playerEntity.AddComponent<Gameplay.Components.HUD>();
        
        scene.AddEntity(playerEntity);
        
        // Initialize camera AFTER entity is added and components are initialized
        camera.Position = playerTransform.Position;
        camera.Target = new System.Numerics.Vector3(0, 2, 0); // Look toward origin at same height
        
        // Equip weapons for testing
        var pistol = Gameplay.Weapons.Gun.CreatePistol();
        var rifle = Gameplay.Weapons.Gun.CreateAssaultRifle();
        weaponController.EquipWeapon(pistol, 0);   // Slot 1 (press '1' to select)
        weaponController.EquipWeapon(rifle, 1);    // Slot 2 (press '2' to select)
        
        Console.WriteLine("[Phase 2 Test Scene] Player created with FirstPersonController");
        Console.WriteLine("  Controls: WASD = Move, Mouse = Look, Space = Jump, Shift = Sprint, Escape = Release Mouse");
        Console.WriteLine("  Weapons: 1 = Pistol, 2 = Assault Rifle, LMB = Fire, R = Reload");
        
        // Create Ground Plane (large flat box)
        var groundEntity = new Core.Entities.Entity("Ground");
        var groundTransform = groundEntity.AddComponent<Core.Components.Transform3D>();
        groundTransform.Position = new System.Numerics.Vector3(0, -1, 0);
        groundTransform.LocalScale = new System.Numerics.Vector3(20, 0.5f, 20); // Wide and flat
        var groundRenderer = groundEntity.AddComponent<Core.Components.MeshRenderer>();
        groundRenderer.SetCube(1.0f, new System.Numerics.Vector4(0.3f, 0.5f, 0.3f, 1.0f)); // Green ground
        Console.WriteLine($"[DEBUG] Ground cube material color: {groundRenderer.Material.Color}");
        var groundRigid = groundEntity.AddComponent<Core.Components.Rigidbody>();
        groundRigid.BodyType = Core.Plugins.Physics.BodyType.Static;
        groundRigid.Shape = new Core.Plugins.Physics.BoxShape(20, 0.5f, 20);
        scene.AddEntity(groundEntity);
        
        // Create a few colored cubes (larger for easier targeting)
    CreateCube(scene, "RedCube", new System.Numerics.Vector3(-3, 2, 0), 2.0f, Color.Red);
    CreateCube(scene, "BlueCube", new System.Numerics.Vector3(0, 4, 0), 2.0f, Color.Blue);
    CreateCube(scene, "YellowCube", new System.Numerics.Vector3(3, 3, 0), 2.0f, Color.Yellow);
        
        // Create enemy entities for AI testing (bright red, taller/skinnier to distinguish from cubes)
        CreateEnemy(scene, playerEntity, "Enemy1", new System.Numerics.Vector3(-5, 2, -5), 1.0f, 0.0f, 0.0f);
        CreateEnemy(scene, playerEntity, "Enemy2", new System.Numerics.Vector3(5, 2, -5), 1.0f, 0.0f, 0.0f);
        
        // Initialize the scene
        scene.Initialize();
        
        Console.WriteLine($"[DEBUG] Scene has {scene.Entities.Count} entities after initialization");
        foreach (var ent in scene.Entities)
        {
            Console.WriteLine($"[DEBUG]   - {ent.Name}: Active={ent.Active}, ComponentCount={ent.GetType().GetProperty("Components")?.GetValue(ent)}");
        }
        
        // Make it the active scene (hacky but works for testing)
        var activeSceneField = typeof(SceneManager).GetField("_activeScene", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        activeSceneField?.SetValue(_sceneManager, scene);
        
        Console.WriteLine("[Phase 2 Test Scene] Created:");
        Console.WriteLine("  - Player with FPS controller, weapons, health (100 HP), and HUD at (0, 2, 10)");
        Console.WriteLine("  - Ground plane (20x0.5x20 static box)");
        Console.WriteLine("  - 3 colored cubes with Health (50 HP each) for target practice");
        Console.WriteLine("  - 2 enemy entities with AI (20 HP, 10 damage melee attacks)");
        Console.WriteLine("  - Controls: WASD=Move, Mouse=Look, Space=Jump, Shift=Sprint, Esc=Menu");
        Console.WriteLine("  - Weapons: 1=Pistol, 2=Assault Rifle, LMB=Fire, R=Reload");
    }
    
    /// <summary>
    /// Helper to create a cube entity.
    /// </summary>
    private void CreateCube(Core.Scenes.Scene scene, string name, 
    System.Numerics.Vector3 position, float size, Color color)
    {
        var entity = new Core.Entities.Entity(name);
        var transform = entity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;
        transform.LocalScale = new System.Numerics.Vector3(size);
        
    var renderer = entity.AddComponent<Core.Components.MeshRenderer>();
    renderer.SetCube(1.0f, new System.Numerics.Vector4(color.R / 255f, color.G / 255f, color.B / 255f, 1.0f));
        
        var rigidbody = entity.AddComponent<Core.Components.Rigidbody>();
        rigidbody.BodyType = Core.Plugins.Physics.BodyType.Static; // Static so cubes don't fall
        rigidbody.Shape = new Core.Plugins.Physics.BoxShape(size, size, size);
        rigidbody.Mass = 1.0f;
        
        // Add Health component for damage testing
        var health = entity.AddComponent<Gameplay.Components.Health>();
        health.MaxHealth = 50f;
        health.DestroyOnDeath = false; // Keep cube visible after death for testing
        
        // Subscribe to death event
        health.OnDeath += (damageInfo) =>
        {
            Console.WriteLine($"[Test Scene] {name} has been destroyed!");
        };
        
        scene.AddEntity(entity);
    }

    /// <summary>
    /// Helper to create an enemy entity with AI.
    /// </summary>
    private void CreateEnemy(Core.Scenes.Scene scene, Core.Entities.Entity player, string name, 
        System.Numerics.Vector3 position, float r, float g, float b)
    {
        var entity = new Core.Entities.Entity(name);
        entity.Tag = "Enemy";
        
        var transform = entity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;
        transform.LocalScale = new System.Numerics.Vector3(0.8f, 1.8f, 0.8f); // Taller, skinnier - more humanoid
        
        var renderer = entity.AddComponent<Core.Components.MeshRenderer>();
        renderer.SetCube(1.0f, new System.Numerics.Vector4(r, g, b, 1.0f));
        
        var rigidbody = entity.AddComponent<Core.Components.Rigidbody>();
        rigidbody.BodyType = Core.Plugins.Physics.BodyType.Static; // Static for now (Phase 3 will add dynamic movement)
        rigidbody.Shape = new Core.Plugins.Physics.BoxShape(0.8f, 1.8f, 0.8f);
        rigidbody.Mass = 70.0f; // Human-like weight
        
        // Add Health component
        var health = entity.AddComponent<Gameplay.Components.Health>();
        health.MaxHealth = 20f;
        health.DestroyOnDeath = false; // Keep visible after death for testing
        
        // Subscribe to death event
        health.OnDeath += (damageInfo) =>
        {
            Console.WriteLine($"[Test Scene] {name} was killed by {damageInfo.Attacker?.Name ?? "unknown"}!");
        };
        
        // Add EnemyAI component
        var enemyAI = entity.AddComponent<Gameplay.Components.EnemyAI>();
        enemyAI.Target = player; // Set player as target
        enemyAI.DetectionRange = 15f;
        enemyAI.AttackRange = 2.5f;
        enemyAI.MoveSpeed = 2.5f;
        enemyAI.AttackDamage = 10f;
        enemyAI.AttackCooldown = 2.0f;
        
        scene.AddEntity(entity);
        
        Console.WriteLine($"[Test Scene] Created enemy '{name}' at {position} targeting player");
    }

    /// <summary>
    /// Load content (textures, models, sounds).
    /// Similar to Unity's resource loading but more manual.
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // TODO Phase 1: Load content via Content Pipeline
        // TODO Phase 5: Load Gum UI screens
        
        Console.WriteLine("Content loaded (placeholder - Phase 1 will add actual content)");
        
        // Create Phase 2 test scene AFTER all services are initialized
        CreateTestScene();
        
        // Load HUD content after scene is created
        var playerEntity = _sceneManager?.ActiveScene?.FindEntitiesByTag("Player").FirstOrDefault();
        if (playerEntity != null)
        {
            var hud = playerEntity.GetComponent<Gameplay.Components.HUD>();
            hud?.LoadContent(GraphicsDevice, _spriteBatch);
            Console.WriteLine("[Game] HUD content loaded");
        }
    }

    /// <summary>
    /// Update game logic.
    /// This is called every frame, similar to Update() in Unity.
    /// 
    /// EDUCATIONAL NOTE - Fixed Timestep Physics:
    /// We run physics on a fixed timestep (60 updates/sec) while
    /// rendering runs at variable framerate. This ensures:
    /// - Deterministic physics simulation
    /// - Consistent behavior across different hardware
    /// - Decoupled physics from rendering performance
    /// Unity does this automatically. In MonoGame we manage it manually.
    /// </summary>
    /// <param name="gameTime">Provides timing information</param>
    protected override void Update(GameTime gameTime)
    {
        // Exit on Escape (temporary - will be replaced with proper menu)
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        // Update core services
        var timeService = ServiceLocator.Get<ITimeService>();
        var inputService = ServiceLocator.Get<IInputService>();
        var physicsProvider = ServiceLocator.Get<IPhysicsProvider>();
        
        timeService.Update(gameTime);
        inputService.Update();
        
        // Update active scene
        var coreGameTime = new Core.Components.GameTime(
            gameTime.TotalGameTime,
            gameTime.ElapsedGameTime
        );
        
        _sceneManager?.Update(coreGameTime);
        
        // Step physics simulation with fixed timestep
        // We use the scaled delta time from TimeService to support slow-motion/time effects
        // Guard against zero/negative timestep on first frame
        if (timeService.DeltaTime > 0)
        {
            physicsProvider.Step(timeService.DeltaTime);
        }

        base.Update(gameTime);
    }

    /// <summary>
    /// Draw/render the game.
    /// MonoGame requires you to explicitly handle rendering.
    /// Unity does this automatically based on cameras.
    /// 
    /// EDUCATIONAL NOTE - Rendering Pipeline:
    /// 1. Clear backbuffer (prevent artifacts from previous frame)
    /// 2. BeginFrame - Set up render state
    /// 3. RenderScene - Draw all renderable entities
    /// 4. EndFrame - Present to screen
    /// Unity's rendering is automatic via Camera components.
    /// MonoGame gives you full control (and responsibility).
    /// </summary>
    /// <param name="gameTime">Provides timing information</param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Get graphics provider
        var graphics = ServiceLocator.Get<IGraphicsProvider>();
        
        // Begin frame
        graphics.BeginFrame();
        
        // Find active camera in scene (attached to Player entity in Phase 2)
        // TODO: Improve camera selection (support multiple cameras, camera priorities)
        Core.Components.Camera? camera = null;
        
        // Try to find camera on Player entity first
        var playerEntity = _sceneManager?.ActiveScene?.FindEntitiesByTag("Player").FirstOrDefault();
        if (playerEntity != null)
        {
            camera = playerEntity.GetComponent<Core.Components.Camera>();
        }
        
        // Fallback to MainCamera tag if no player camera found
        if (camera == null)
        {
            var cameraEntity = _sceneManager?.ActiveScene?.FindEntitiesByTag("MainCamera").FirstOrDefault();
            camera = cameraEntity?.GetComponent<Core.Components.Camera>();
        }
        
        if (camera != null)
        {
            // Collect all renderable entities from the scene
            var renderables = new System.Collections.Generic.List<IRenderable>();
            
            if (_sceneManager?.ActiveScene != null)
            {
                foreach (var entity in _sceneManager.ActiveScene.Entities)
                {
                    // MeshRenderer implements IRenderable
                    var meshRenderer = entity.GetComponent<Core.Components.MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        renderables.Add(meshRenderer);
                    }
                }
            }
            
            // Render the scene
            graphics.RenderScene(camera, renderables);
        }
        else
        {
            Console.WriteLine("[Render] ERROR: No camera found!");
        }
        
        // End frame (presents to screen)
        graphics.EndFrame();
        
        // Draw crosshair (simple 2D overlay)
        DrawCrosshair();
        
        // Draw scene entities (UI/debug overlay - Phase 5)
        var coreGameTime = new Core.Components.GameTime(
            gameTime.TotalGameTime,
            gameTime.ElapsedGameTime
        );
        
        _sceneManager?.Draw(coreGameTime);

        base.Draw(gameTime);
    }

    /// <summary>
    /// Draw a simple crosshair in the center of the screen.
    /// This helps with aiming when the mouse cursor is hidden.
    /// </summary>
    private void DrawCrosshair()
    {
        if (_spriteBatch == null) return;

        _spriteBatch.Begin();
        
        // Get screen center
        int centerX = _graphics.PreferredBackBufferWidth / 2;
        int centerY = _graphics.PreferredBackBufferHeight / 2;
        
        // Crosshair size
        int crosshairSize = 10;
        int thickness = 2;
        int gap = 3;
        
        // Create a 1x1 white pixel texture for drawing lines
        Texture2D pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        
        // Draw horizontal line (left and right from center)
        _spriteBatch.Draw(pixel, new Rectangle(centerX - crosshairSize - gap, centerY - thickness / 2, crosshairSize, thickness), Color.White);
        _spriteBatch.Draw(pixel, new Rectangle(centerX + gap, centerY - thickness / 2, crosshairSize, thickness), Color.White);
        
        // Draw vertical line (top and bottom from center)
        _spriteBatch.Draw(pixel, new Rectangle(centerX - thickness / 2, centerY - crosshairSize - gap, thickness, crosshairSize), Color.White);
        _spriteBatch.Draw(pixel, new Rectangle(centerX - thickness / 2, centerY + gap, thickness, crosshairSize), Color.White);
        
        _spriteBatch.End();
        
        pixel.Dispose();
    }
    
    /// <summary>
    /// Cleanup when game exits.
    /// Similar to Unity's OnApplicationQuit() or OnDestroy().
    /// </summary>
    protected override void UnloadContent()
    {
        // Unload scene
        _sceneManager?.UnloadScene();
        
        // Shutdown services
        ServiceLocator.Get<IPhysicsProvider>()?.Shutdown();
        ServiceLocator.Get<IGraphicsProvider>()?.Shutdown();
        
        ServiceLocator.Clear();
        
        Console.WriteLine("MonoGame FPS shutdown complete");
        
        base.UnloadContent();
    }
}
