using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shooter.Core.Plugins.Graphics;
using Shooter.Core.Plugins.Physics;
using Shooter.Core.Scenes;
using Shooter.Core.Services;
using Shooter.Graphics;
using Shooter.Physics;
using System;
using System.Linq;

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
    private Gameplay.Systems.PickupSystem? _pickupSystem;

    // Hit marker state
    private bool _showHitMarker = false;
    private float _hitMarkerTimer = 0f;
    private const float HIT_MARKER_DURATION = 0.15f; // 150ms

    // Pause state
    private bool _isPaused = false;
    private bool _escapeWasPressed = false;

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
        
        Window.Title = "Unity Shooter in MonoGame";
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

        // Subscribe to hit events for hit markers
        projectileSystem.OnHitEntity += (hitEntity) =>
        {
            TriggerHitMarker();
        };

        // Subscribe to impact events for particle effects
        projectileSystem.OnImpact += (position, normal) =>
        {
            SpawnImpactParticles(position, normal);
        };

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

        // Store player entity reference for later (for weapon firing events)
        _playerEntity = playerEntity;

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

        // Create enemy entities for AI testing (bright red, taller/skinnier)
        // Position them right next to the weapon for visibility testing
        CreateEnemy(scene, playerEntity, "Enemy1", new System.Numerics.Vector3(-1, 1.5f, 2), 1.0f, 0.0f, 0.0f);
        CreateEnemy(scene, playerEntity, "Enemy2", new System.Numerics.Vector3(1, 1.5f, 2), 1.0f, 0.0f, 0.0f);

        // Create pickups for testing
        CreateHealthPickup(scene, "HealthPickup1", new System.Numerics.Vector3(-5, 1.5f, 0), 25f);
        CreateHealthPickup(scene, "HealthPickup2", new System.Numerics.Vector3(5, 1.5f, 0), 50f);
        CreateAmmoPickup(scene, "AmmoPickup1", new System.Numerics.Vector3(0, 1.5f, -5), 30);

        // Create pickup system
        _pickupSystem = new Gameplay.Systems.PickupSystem(playerEntity);

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
        Console.WriteLine("  - 2 enemy entities with AI (20 HP, 10 damage melee attacks)");
        Console.WriteLine("  - 3 pickups (2 health, 1 ammo) with auto-collect");
        Console.WriteLine("  - Controls: WASD=Move, Mouse=Look, Space=Jump, Shift=Sprint, Esc=Pause");
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
        health.MaxHealth = 100f; // Unity HoverBot health
        health.DestroyOnDeath = false; // Keep visible after death for testing

        // Create weapon for enemy (Eye Lazers)
        var eyeLazers = Gameplay.Weapons.Gun.CreateEnemyEyeLazers();

        // Hook up weapon firing to projectile system
        if (eyeLazers is Gameplay.Weapons.Gun gun)
        {
            gun.OnHitscanFired += (origin, direction, damage) =>
            {
                var projectileSystem = ServiceLocator.Get<Gameplay.Systems.ProjectileSystem>();
                projectileSystem?.FireHitscan(origin, direction, damage, 100f, entity);
            };
        }

        // Add EnemyController for weapon management
        var enemyController = entity.AddComponent<Gameplay.Components.EnemyController>();
        enemyController.SetWeapon(eyeLazers);

        // Add EnemyAI component
        var enemyAI = entity.AddComponent<Gameplay.Components.EnemyAI>();
        enemyAI.Target = player; // Set player as target
        enemyAI.DetectionRange = 20f; // Unity default
        enemyAI.AttackRange = 10f; // Unity default
        enemyAI.AttackStopDistanceRatio = 0.5f; // Stop at 50% of attack range
        enemyAI.MoveSpeed = 2.5f;

        // Subscribe to death event (after enemyAI is created so we can reference it)
        health.OnDeath += (damageInfo) =>
        {
            Console.WriteLine($"[Test Scene] {name} was killed by {damageInfo.Attacker?.Name ?? "unknown"}!");

            // Death effect: change color to dark gray (corpse color)
            renderer.Material.Color = new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f);

            // Death effect: rotate/fall down (tilt forward)
            transform.LocalRotation = System.Numerics.Quaternion.CreateFromAxisAngle(
                new System.Numerics.Vector3(1, 0, 0), // Rotate around X-axis
                (float)(System.Math.PI / 2) // 90 degrees forward
            );

            // Death effect: lower to ground (half height)
            var currentPos = transform.Position;
            transform.Position = new System.Numerics.Vector3(currentPos.X, currentPos.Y - 0.9f, currentPos.Z);

            // Death effect: disable rigidbody collisions (corpse shouldn't block movement)
            rigidbody.BodyType = Core.Plugins.Physics.BodyType.Static;
        };
        
        scene.AddEntity(entity);
        
        Console.WriteLine($"[Test Scene] Created enemy '{name}' at {position} targeting player");
    }

    /// <summary>
    /// Helper to create a health pickup
    /// </summary>
    private void CreateHealthPickup(Core.Scenes.Scene scene, string name, System.Numerics.Vector3 position, float healAmount)
    {
        var entity = new Core.Entities.Entity(name);
        entity.Tag = "Pickup";

        var transform = entity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;
        transform.LocalScale = new System.Numerics.Vector3(0.5f, 0.5f, 0.5f);

        var renderer = entity.AddComponent<Core.Components.MeshRenderer>();
        renderer.SetCube(1.0f, new System.Numerics.Vector4(0.0f, 1.0f, 0.0f, 1.0f)); // Green for health

        // No physics collider needed - proximity detection handled by PickupSystem

        var pickup = entity.AddComponent<Gameplay.Components.Pickup>();
        pickup.Type = Gameplay.Components.PickupType.Health;
        pickup.Value = healAmount;
        pickup.PickupRadius = 2.0f;

        scene.AddEntity(entity);
        Console.WriteLine($"[Test Scene] Created health pickup '{name}' (+{healAmount} HP) at {position}");
    }

    /// <summary>
    /// Helper to create an ammo pickup
    /// </summary>
    private void CreateAmmoPickup(Core.Scenes.Scene scene, string name, System.Numerics.Vector3 position, int ammoAmount)
    {
        var entity = new Core.Entities.Entity(name);
        entity.Tag = "Pickup";

        var transform = entity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;
        transform.LocalScale = new System.Numerics.Vector3(0.5f, 0.5f, 0.5f);

        var renderer = entity.AddComponent<Core.Components.MeshRenderer>();
        renderer.SetCube(1.0f, new System.Numerics.Vector4(1.0f, 0.8f, 0.0f, 1.0f)); // Yellow/gold for ammo

        var pickup = entity.AddComponent<Gameplay.Components.Pickup>();
        pickup.Type = Gameplay.Components.PickupType.Ammo;
        pickup.Value = ammoAmount;
        pickup.PickupRadius = 2.0f;

        scene.AddEntity(entity);
        Console.WriteLine($"[Test Scene] Created ammo pickup '{name}' (+{ammoAmount} rounds) at {position}");
    }

    /// <summary>
    /// Load content (textures, models, sounds).
    /// Similar to Unity's resource loading but more manual.
    ///
    /// EDUCATIONAL NOTE - CONTENT LOADING:
    /// Unity automatically loads assets when you reference them in prefabs.
    /// MonoGame requires explicit loading via Content.Load<T>(assetName).
    ///
    /// The Content Manager:
    /// - Loads .xnb files (compiled assets from Content.mgcb)
    /// - Caches loaded assets (calling Load twice returns the same instance)
    /// - Handles asset dependencies automatically
    ///
    /// Common asset types:
    /// - Model: 3D models from FBX files
    /// - Texture2D: Images (PNG, JPG)
    /// - SpriteFont: Fonts for text rendering
    /// - SoundEffect: Audio files
    /// </summary>
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load SpriteFont for HUD
        SpriteFont hudFont = Content.Load<SpriteFont>("font");

        // Initialize AudioService with ContentManager
        var audioService = ServiceLocator.Get<IAudioService>() as AudioService;
        if (audioService != null)
        {
            audioService.Initialize(Content);

            // Pre-load commonly used sounds to avoid first-shot delay
            audioService.LoadSound("Audio/SFX/Weapons/Blaster_Shot");
            audioService.LoadSound("Audio/SFX/Weapons/Shotgun_Shot");
            audioService.LoadSound("Audio/SFX/Pickups/Pickup_Health");
            audioService.LoadSound("Audio/SFX/Pickups/Pickup_Weapon_Small");
            audioService.LoadSound("Audio/SFX/Player/Damage_Tick");

            Console.WriteLine("[Game] AudioService initialized with ContentManager");
        }

        // Create Phase 2 test scene AFTER all services are initialized
        CreateTestScene();

        // Wire up HUD overlay in ForwardGraphicsProvider
        var graphicsProvider = ServiceLocator.Get<IGraphicsProvider>() as ForwardGraphicsProvider;
        var playerEntity = _sceneManager?.ActiveScene?.FindEntitiesByTag("Player").FirstOrDefault();
        if (graphicsProvider != null && playerEntity != null)
        {
            var weaponController = playerEntity.GetComponent<Gameplay.Components.WeaponController>();
            if (weaponController != null)
            {
                graphicsProvider.SetHUDFont(hudFont);
                graphicsProvider.SetPlayerWeaponController(weaponController);
                Console.WriteLine("[Game] HUD overlay wired up");
            }

            // Load and attach weapon model to player's view
            LoadWeaponModel(playerEntity);

            Console.WriteLine("[Game] Weapon model loaded and attached to player camera");
        }

        // Load enemy models for all enemy entities
        LoadEnemyModels();
    }

    /// <summary>
    /// Load and attach a weapon model to the player's camera for first-person view.
    ///
    /// UNITY COMPARISON - WEAPON VIEW MODEL:
    /// In Unity FPS games, weapons are typically:
    /// 1. Child objects of the camera GameObject
    /// 2. Use a separate "Weapon" layer rendered by a second camera
    /// 3. Have custom FOV to prevent distortion
    ///
    /// In MonoGame, we:
    /// 1. Create a child entity of the player
    /// 2. Position it relative to the camera's view
    /// 3. Render it in the normal scene (no separate camera needed for this demo)
    ///
    /// EDUCATIONAL NOTE - FPS WEAPON POSITIONING:
    /// The weapon model needs careful positioning:
    /// - Too close: fills the screen
    /// - Too far: looks tiny
    /// - Wrong rotation: points at weird angles
    ///
    /// Unity's values (from FPS Microgame):
    /// - Position: (0.3, -0.2, 0.5) relative to camera
    /// - Rotation: Slightly tilted for visual interest
    /// - Scale: Often enlarged because camera is so close
    /// </summary>
    // Store weapon viewmodel entities for switching
    private Core.Entities.Entity? _primaryWeaponViewModel;
    private Core.Entities.Entity? _secondaryWeaponViewModel;
    private int _lastWeaponIndex = -1;
    private Core.Entities.Entity? _playerEntity;

    private void LoadWeaponModel(Core.Entities.Entity playerEntity)
    {
        try
        {
            var scene = _sceneManager?.ActiveScene;

            // The camera is on the player entity, not a separate entity
            var cameraEntity = playerEntity;

            // Load PRIMARY weapon viewmodel (Pistol - slot 0)
            _primaryWeaponViewModel = CreateWeaponViewModel(
                "PrimaryWeaponViewModel",
                "Models/Mesh_Weapon_Primary",
                cameraEntity,
                new System.Numerics.Vector3(0.5f, -0.5f, -1.5f),
                0.3f);

            // Load SECONDARY weapon viewmodel (Assault Rifle - slot 1)
            _secondaryWeaponViewModel = CreateWeaponViewModel(
                "SecondaryWeaponViewModel",
                "Models/Mesh_Weapon_Secondary",
                cameraEntity,
                new System.Numerics.Vector3(0.5f, -0.5f, -1.5f),
                0.3f);

            // Add both to scene
            if (scene != null)
            {
                scene.AddEntity(_primaryWeaponViewModel);
                _primaryWeaponViewModel.Initialize();

                scene.AddEntity(_secondaryWeaponViewModel);
                _secondaryWeaponViewModel.Initialize();

                // Start with primary weapon visible
                SetActiveWeaponViewModel(0);

                // Hook up muzzle flash to weapon firing
                HookupMuzzleFlashEvents(playerEntity);

                Console.WriteLine("[LoadContent] Loaded both weapon viewmodels with camera tracking and muzzle flash");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LoadContent] Failed to load weapon models: {ex.Message}");
            Console.WriteLine("  Make sure Content.mgcb has been built and weapon FBX files are included");
        }
    }

    /// <summary>
    /// Hook up weapon firing events to trigger muzzle flash on viewmodels
    /// </summary>
    private void HookupMuzzleFlashEvents(Core.Entities.Entity playerEntity)
    {
        var weaponController = playerEntity.GetComponent<Gameplay.Components.WeaponController>();
        if (weaponController == null) return;

        // Get the weapons and hook up their firing events
        var pistol = weaponController.GetWeapon(0);
        var rifle = weaponController.GetWeapon(1);

        if (pistol is Gameplay.Weapons.Gun pistolGun)
        {
            pistolGun.OnHitscanFired += (origin, direction, damage) =>
            {
                // Trigger muzzle flash on primary viewmodel
                var muzzleFlash = _primaryWeaponViewModel?.GetComponent<Gameplay.Components.MuzzleFlash>();
                muzzleFlash?.Flash();

                // Add muzzle flash particles
                SpawnMuzzleFlashParticles(origin, direction);
            };
        }

        if (rifle is Gameplay.Weapons.Gun rifleGun)
        {
            rifleGun.OnHitscanFired += (origin, direction, damage) =>
            {
                // Trigger muzzle flash on secondary viewmodel
                var muzzleFlash = _secondaryWeaponViewModel?.GetComponent<Gameplay.Components.MuzzleFlash>();
                muzzleFlash?.Flash();

                // Add muzzle flash particles
                SpawnMuzzleFlashParticles(origin, direction);
            };
        }
    }

    /// <summary>
    /// Helper to create a weapon viewmodel entity
    /// </summary>
    private Core.Entities.Entity CreateWeaponViewModel(
        string name,
        string modelPath,
        Core.Entities.Entity? cameraEntity,
        System.Numerics.Vector3 offset,
        float scale)
    {
        var weaponEntity = new Core.Entities.Entity(name);
        weaponEntity.AddComponent<Core.Components.Transform3D>();

        var viewModel = weaponEntity.AddComponent<Gameplay.Components.WeaponViewModel>();
        if (viewModel != null)
        {
            viewModel.CameraEntity = cameraEntity;
            viewModel.ViewmodelOffset = offset;
        }

        var weaponRenderer = weaponEntity.AddComponent<Core.Components.ModelMeshRenderer>();
        weaponRenderer.LoadModel(Content, modelPath);
        weaponRenderer.Scale = scale;
        weaponRenderer.TintColor = new System.Numerics.Vector4(1, 1, 1, 1);

        // Add muzzle flash effect
        var muzzleFlash = weaponEntity.AddComponent<Gameplay.Components.MuzzleFlash>();
        if (muzzleFlash != null)
        {
            muzzleFlash.BarrelOffset = new System.Numerics.Vector3(0f, -0.2f, -1.2f);
            muzzleFlash.FlashScale = 0.2f;
        }

        return weaponEntity;
    }

    /// <summary>
    /// Switch which weapon viewmodel is visible based on equipped weapon slot
    /// </summary>
    private void SetActiveWeaponViewModel(int weaponIndex)
    {
        // Hide all viewmodels first
        if (_primaryWeaponViewModel != null)
        {
            var primaryRenderer = _primaryWeaponViewModel.GetComponent<Core.Components.ModelMeshRenderer>();
            if (primaryRenderer != null)
                primaryRenderer.Visible = false;
        }

        if (_secondaryWeaponViewModel != null)
        {
            var secondaryRenderer = _secondaryWeaponViewModel.GetComponent<Core.Components.ModelMeshRenderer>();
            if (secondaryRenderer != null)
                secondaryRenderer.Visible = false;
        }

        // Show the active weapon viewmodel
        switch (weaponIndex)
        {
            case 0: // Primary weapon (Pistol)
                if (_primaryWeaponViewModel != null)
                {
                    var primaryRenderer = _primaryWeaponViewModel.GetComponent<Core.Components.ModelMeshRenderer>();
                    if (primaryRenderer != null)
                        primaryRenderer.Visible = true;
                }
                break;

            case 1: // Secondary weapon (Assault Rifle)
                if (_secondaryWeaponViewModel != null)
                {
                    var secondaryRenderer = _secondaryWeaponViewModel.GetComponent<Core.Components.ModelMeshRenderer>();
                    if (secondaryRenderer != null)
                        secondaryRenderer.Visible = true;
                }
                break;
        }
    }

    /// <summary>
    /// Load 3D models for all enemy entities in the scene.
    ///
    /// EDUCATIONAL NOTE - REPLACING PLACEHOLDER MESHES:
    /// During development, we use colored cubes as placeholders.
    /// Now we replace the MeshRenderer (cubes) with ModelMeshRenderer (FBX models).
    ///
    /// This is a common pattern:
    /// 1. Phase 1: Get gameplay working with primitives (cubes, spheres)
    /// 2. Phase 2: Replace with actual art assets
    /// 3. Phase 3: Add animations, effects, polish
    ///
    /// Unity hides this process because you usually import FBX files directly.
    /// In MonoGame, we make it explicit for learning purposes.
    /// </summary>
    private void LoadEnemyModels()
    {
        var scene = _sceneManager?.ActiveScene;
        if (scene == null)
            return;

        // Find all enemy entities (tagged as "Enemy")
        var enemies = scene.FindEntitiesByTag("Enemy");

        foreach (var enemy in enemies)
        {
            try
            {
                // Remove the old MeshRenderer (colored cube placeholder)
                var oldRenderer = enemy.GetComponent<Core.Components.MeshRenderer>();
                if (oldRenderer != null)
                {
                    enemy.RemoveComponent<Core.Components.MeshRenderer>();
                }

                // Add ModelMeshRenderer with HoverBot model
                var modelRenderer = enemy.AddComponent<Core.Components.ModelMeshRenderer>();
                modelRenderer.LoadModel(Content, "Models/HoverBot_Fixed");
                modelRenderer.Scale = 0.015f; // Scale down from Unity size (Unity units ~= 1m, adjust to fit MonoGame scene)
                modelRenderer.TintColor = new System.Numerics.Vector4(1, 0, 0, 1); // Red tint to distinguish enemies

                // Re-initialize the component
                modelRenderer.Initialize();

                var enemyPos = enemy.Transform.Position;
                Console.WriteLine($"[LoadContent] Loaded HoverBot model for enemy '{enemy.Name}' at position ({enemyPos.X}, {enemyPos.Y}, {enemyPos.Z})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoadContent] Failed to load model for enemy '{enemy.Name}': {ex.Message}");
                Console.WriteLine("  Enemy will remain as colored cube placeholder");
            }
        }

        Console.WriteLine($"[LoadContent] Loaded models for {enemies.Count()} enemy entities");
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
        // Handle pause toggle with Escape key
        bool escapePressed = Keyboard.GetState().IsKeyDown(Keys.Escape);
        if (escapePressed && !_escapeWasPressed)
        {
            _isPaused = !_isPaused;
            IsMouseVisible = _isPaused; // Show cursor when paused
        }
        _escapeWasPressed = escapePressed;

        // Update core services (always update input/time even when paused)
        var timeService = ServiceLocator.Get<ITimeService>();
        var inputService = ServiceLocator.Get<IInputService>();
        var physicsProvider = ServiceLocator.Get<IPhysicsProvider>();

        timeService.Update(gameTime);
        inputService.Update();

        // Skip game updates when paused
        if (_isPaused)
        {
            base.Update(gameTime);
            return;
        }

        // Update active scene
        var coreGameTime = new Core.Components.GameTime(
            gameTime.TotalGameTime,
            gameTime.ElapsedGameTime
        );

        _sceneManager?.Update(coreGameTime);

        // Update pickup system
        if (_pickupSystem != null && _sceneManager?.ActiveScene != null)
        {
            var pickups = _sceneManager.ActiveScene.Entities
                .SelectMany(e => e.GetComponents<Gameplay.Components.Pickup>())
                .Where(p => !p.IsCollected);
            _pickupSystem.Update(coreGameTime, pickups);
        }

        // Update hit marker timer
        if (_showHitMarker)
        {
            _hitMarkerTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_hitMarkerTimer <= 0f)
            {
                _showHitMarker = false;
            }
        }

        // Check for weapon switching (update viewmodel visibility)
        var playerEntity = _sceneManager?.ActiveScene?.FindEntitiesByTag("Player").FirstOrDefault();
        if (playerEntity != null)
        {
            var weaponController = playerEntity.GetComponent<Gameplay.Components.WeaponController>();
            if (weaponController != null && weaponController.CurrentWeaponIndex != _lastWeaponIndex)
            {
                SetActiveWeaponViewModel(weaponController.CurrentWeaponIndex);
                _lastWeaponIndex = weaponController.CurrentWeaponIndex;
            }
        }

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
            var modelRenderers = new System.Collections.Generic.List<Core.Components.ModelMeshRenderer>();

            if (_sceneManager?.ActiveScene != null)
            {
                foreach (var entity in _sceneManager.ActiveScene.Entities)
                {
                    // MeshRenderer implements IRenderable (procedural cubes/spheres)
                    var meshRenderer = entity.GetComponent<Core.Components.MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        renderables.Add(meshRenderer);
                    }

                    // ModelMeshRenderer for FBX models from Content Pipeline
                    var modelRenderer = entity.GetComponent<Core.Components.ModelMeshRenderer>();
                    if (modelRenderer != null)
                    {
                        modelRenderers.Add(modelRenderer);
                    }
                }
            }

            // Render the scene (procedural meshes first)
            graphics.RenderScene(camera, renderables);

            // Render FBX models
            if (graphics is ForwardGraphicsProvider forwardGraphics)
            {
                forwardGraphics.RenderModels(camera, modelRenderers);

                // Render muzzle flashes (after models, with additive blending)
                if (_sceneManager?.ActiveScene != null)
                {
                    var muzzleFlashes = _sceneManager.ActiveScene.Entities
                        .SelectMany(e => e.GetComponents<Gameplay.Components.MuzzleFlash>())
                        .Where(m => m.IsVisible);
                    forwardGraphics.RenderMuzzleFlashes(camera, muzzleFlashes);

                    // Render particles (after muzzle flashes, also additive)
                    var particleEmitters = _sceneManager.ActiveScene.Entities
                        .SelectMany(e => e.GetComponents<Gameplay.Components.ParticleEmitter>());
                    forwardGraphics.RenderParticles(camera, particleEmitters);
                }
            }
        }
        else
        {
            Console.WriteLine("[Render] ERROR: No camera found!");
        }
        
        // End frame (presents to screen)
        graphics.EndFrame();
        
        // Draw crosshair (simple 2D overlay)
        if (!_isPaused)
        {
            DrawCrosshair();
        }

        // Draw pause menu if paused
        if (_isPaused)
        {
            DrawPauseMenu();
        }

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
    /// <summary>
    /// Trigger hit marker feedback (call when player hits an enemy)
    /// </summary>
    public void TriggerHitMarker()
    {
        _showHitMarker = true;
        _hitMarkerTimer = HIT_MARKER_DURATION;
    }

    /// <summary>
    /// Spawn impact particles when bullets hit surfaces
    /// </summary>
    private void SpawnImpactParticles(System.Numerics.Vector3 position, System.Numerics.Vector3 normal)
    {
        if (_sceneManager?.ActiveScene == null)
            return;

        // Create a new entity for the particle effect
        var particleEntity = new Core.Entities.Entity("ImpactParticles");

        var transform = particleEntity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;

        // Add particle emitter
        var emitter = particleEntity.AddComponent<Gameplay.Components.ParticleEmitter>();
        emitter.EmissionPosition = position;
        emitter.StartColor = new System.Numerics.Vector4(1.0f, 0.8f, 0.3f, 1.0f); // Orange/yellow
        emitter.EndColor = new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 0.0f); // Fade to gray
        emitter.StartSize = 0.15f;
        emitter.EndSize = 0.05f;
        emitter.Lifetime = 0.3f;
        emitter.EmissionSpeed = 3f;
        emitter.Gravity = -5f;
        emitter.OneShot = true;

        // Emit particles in direction of surface normal
        emitter.Emit(15, position, normal);

        // Add to scene
        _sceneManager.ActiveScene.AddEntity(particleEntity);
    }

    /// <summary>
    /// Spawn muzzle flash particles when weapons fire
    /// </summary>
    private void SpawnMuzzleFlashParticles(System.Numerics.Vector3 position, System.Numerics.Vector3 direction)
    {
        if (_sceneManager?.ActiveScene == null)
            return;

        // Create a new entity for the particle effect
        var particleEntity = new Core.Entities.Entity("MuzzleFlashParticles");

        var transform = particleEntity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;

        // Add particle emitter
        var emitter = particleEntity.AddComponent<Gameplay.Components.ParticleEmitter>();
        emitter.EmissionPosition = position;
        emitter.StartColor = new System.Numerics.Vector4(1.0f, 0.9f, 0.5f, 1.0f); // Bright yellow/white
        emitter.EndColor = new System.Numerics.Vector4(1.0f, 0.3f, 0.0f, 0.0f); // Fade to orange
        emitter.StartSize = 0.2f;
        emitter.EndSize = 0.02f;
        emitter.Lifetime = 0.15f; // Short-lived flash
        emitter.EmissionSpeed = 8f; // Fast particles
        emitter.Gravity = 0f; // No gravity for muzzle flash
        emitter.OneShot = true;

        // Emit particles forward in firing direction (cone spread)
        emitter.Emit(20, position, direction);

        // Add to scene
        _sceneManager.ActiveScene.AddEntity(particleEntity);
    }

    private void DrawCrosshair()
    {
        if (_spriteBatch == null) return;

        _spriteBatch.Begin();

        // Get screen center
        int centerX = _graphics.PreferredBackBufferWidth / 2;
        int centerY = _graphics.PreferredBackBufferHeight / 2;

        // Crosshair size and color (changes when hitting)
        int crosshairSize = 10;
        int thickness = 2;
        int gap = 3;
        Color crosshairColor = Color.White;

        // Hit marker effect: red color and larger size
        if (_showHitMarker)
        {
            float progress = _hitMarkerTimer / HIT_MARKER_DURATION;
            crosshairColor = Color.Lerp(Color.White, Color.OrangeRed, progress);
            crosshairSize = (int)(10 + 4 * progress); // Grows from 10 to 14
            gap = (int)(3 + 2 * progress); // Gap grows too
        }

        // Create a 1x1 white pixel texture for drawing lines
        Texture2D pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });

        // Draw horizontal line (left and right from center)
        _spriteBatch.Draw(pixel, new Rectangle(centerX - crosshairSize - gap, centerY - thickness / 2, crosshairSize, thickness), crosshairColor);
        _spriteBatch.Draw(pixel, new Rectangle(centerX + gap, centerY - thickness / 2, crosshairSize, thickness), crosshairColor);

        // Draw vertical line (top and bottom from center)
        _spriteBatch.Draw(pixel, new Rectangle(centerX - thickness / 2, centerY - crosshairSize - gap, thickness, crosshairSize), crosshairColor);
        _spriteBatch.Draw(pixel, new Rectangle(centerX - thickness / 2, centerY + gap, thickness, crosshairSize), crosshairColor);

        _spriteBatch.End();

        pixel.Dispose();
    }

    /// <summary>
    /// Draw the pause menu overlay
    /// </summary>
    private void DrawPauseMenu()
    {
        if (_spriteBatch == null)
            return;

        _spriteBatch.Begin();

        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;

        // Draw semi-transparent black overlay
        var overlayTexture = new Texture2D(GraphicsDevice, 1, 1);
        overlayTexture.SetData(new[] { Color.Black });
        _spriteBatch.Draw(
            overlayTexture,
            new Rectangle(0, 0, screenWidth, screenHeight),
            Color.Black * 0.7f // 70% opacity
        );

        // Note: For a full pause menu with text, we'd need to load a SpriteFont
        // For now, we just show the dark overlay as visual feedback that the game is paused
        // The user can see the mouse cursor appears when paused

        _spriteBatch.End();

        overlayTexture.Dispose();
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