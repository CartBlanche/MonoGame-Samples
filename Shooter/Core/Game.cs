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
public partial class ShooterGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch? _spriteBatch;
    private SceneManager? _sceneManager;
    private Gameplay.Systems.PickupSystem? _pickupSystem;
    private Gameplay.Components.GameFlowManager? _gameFlowManager;
    private Texture2D? _pauseOverlayTexture; // Reusable 1x1 texture for pause overlay

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
        ServiceLocator.Register<SceneManager>(_sceneManager);

        // TODO: Register custom component types
        // _sceneManager.RegisterComponentType<PlayerController>("PlayerController");

        base.Initialize();
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

        // Add Rigidbody for physics collision (so enemies can hit the player)
        var playerRigidbody = playerEntity.AddComponent<Core.Components.Rigidbody>();
        playerRigidbody.BodyType = Core.Plugins.Physics.BodyType.Kinematic; // Kinematic so FPS controller handles movement
        playerRigidbody.Shape = new Core.Plugins.Physics.BoxShape(1.0f, 1.8f, 1.0f); // Box: width 1.0, height 1.8, depth 1.0
        playerRigidbody.Mass = 70.0f;

        // Add HUD component to player
        var hud = playerEntity.AddComponent<Gameplay.Components.HUD>();

        // Add GameFlowManager to player (for game state management)
        _gameFlowManager = playerEntity.AddComponent<Gameplay.Components.GameFlowManager>();

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

        // Player setup complete

        // Build multi-room level (inspired by Unity's Room_01, Room_02, Room_03 structure)
        BuildMultiRoomLevel(scene, playerEntity);

        // Create pickup system
        _pickupSystem = new Gameplay.Systems.PickupSystem(playerEntity);

        // Initialize the scene
        scene.Initialize();

        // Make it the active scene (hacky but works for testing)
        var activeSceneField = typeof(SceneManager).GetField("_activeScene",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        activeSceneField?.SetValue(_sceneManager, scene);
    }


    /// <summary>
    /// Build a multi-room dungeon level inspired by Unity's Room_01, Room_02, Room_03 structure.
    /// Creates 3 connected rooms with walls, floors, doorways, enemies, and pickups.
    /// </summary>
    private void BuildMultiRoomLevel(Core.Scenes.Scene scene, Core.Entities.Entity playerEntity)
    {
        // Colors for visual distinction
        var floorColor = new System.Numerics.Vector4(0.3f, 0.4f, 0.5f, 1.0f);   // Bluish-gray floor
        var wallColor = new System.Numerics.Vector4(0.5f, 0.3f, 0.3f, 1.0f);    // Reddish-brown walls
        var doorFrameColor = new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 1.0f); // Dark gray doorframes

        // ROOM 01 - Starting room (6x6 units, similar to Unity's Floor_6x6)
        // Player starts here at (0, 2, 10)
        CreateFloor(scene, "Room01_Floor", new System.Numerics.Vector3(0, -1, 0), 6f, 6f, floorColor);

        // Room 01 walls (4 sides, with opening on north side)
        CreateWall(scene, "Room01_Wall_South", new System.Numerics.Vector3(0, 1, 3), 6f, 3f, 0.5f, wallColor); // South wall
        CreateWall(scene, "Room01_Wall_West", new System.Numerics.Vector3(-3, 1, 0), 0.5f, 3f, 6f, wallColor); // West wall
        CreateWall(scene, "Room01_Wall_East", new System.Numerics.Vector3(3, 1, 0), 0.5f, 3f, 6f, wallColor);  // East wall
        // North wall split into two pieces with doorway in center
        CreateWall(scene, "Room01_Wall_North_Left", new System.Numerics.Vector3(-1.75f, 1, -3), 2.5f, 3f, 0.5f, wallColor);
        CreateWall(scene, "Room01_Wall_North_Right", new System.Numerics.Vector3(1.75f, 1, -3), 2.5f, 3f, 0.5f, wallColor);

        // Room 01 enemies (2 starting enemies close to player)
        CreateEnemy(scene, playerEntity, "Room01_Enemy1", new System.Numerics.Vector3(-2, 1.5f, 2), 1.0f, 0.0f, 0.0f);
        CreateEnemy(scene, playerEntity, "Room01_Enemy2", new System.Numerics.Vector3(2, 1.5f, 2), 1.0f, 0.0f, 0.0f);

        // ROOM 02 - Medium room (9x9 units, connected via Opening_01)
        // Center at (0, 0, -12) so it's north of Room 01
        CreateFloor(scene, "Room02_Floor", new System.Numerics.Vector3(0, -1, -12), 9f, 9f, floorColor);

        // Room 02 walls
        CreateWall(scene, "Room02_Wall_South_Left", new System.Numerics.Vector3(-3f, 1, -7.5f), 3f, 3f, 0.5f, wallColor); // South wall left
        CreateWall(scene, "Room02_Wall_South_Right", new System.Numerics.Vector3(3f, 1, -7.5f), 3f, 3f, 0.5f, wallColor); // South wall right
        CreateWall(scene, "Room02_Wall_West", new System.Numerics.Vector3(-4.5f, 1, -12), 0.5f, 3f, 9f, wallColor); // West wall
        CreateWall(scene, "Room02_Wall_East", new System.Numerics.Vector3(4.5f, 1, -12), 0.5f, 3f, 9f, wallColor); // East wall
        // North wall with opening on east side
        CreateWall(scene, "Room02_Wall_North_Left", new System.Numerics.Vector3(-2.5f, 1, -16.5f), 4f, 3f, 0.5f, wallColor);
        CreateWall(scene, "Room02_Wall_North_Center", new System.Numerics.Vector3(2f, 1, -16.5f), 3f, 3f, 0.5f, wallColor);

        // Room 02 enemies (3 enemies spread across the room)
        CreateEnemy(scene, playerEntity, "Room02_Enemy1", new System.Numerics.Vector3(-2, 1.5f, -10), 1.0f, 0.5f, 0.0f); // Orange
        CreateEnemy(scene, playerEntity, "Room02_Enemy2", new System.Numerics.Vector3(2, 1.5f, -14), 1.0f, 0.5f, 0.0f);
        CreateEnemy(scene, playerEntity, "Room02_Enemy3", new System.Numerics.Vector3(0, 1.5f, -12), 1.0f, 0.5f, 0.0f);

        // Room 02 pickups
        CreateHealthPickup(scene, "Room02_HealthPickup", new System.Numerics.Vector3(-3, 1.5f, -12), 25f);

        // ROOM 03 - Large room (15x15 units, final challenge room)
        // Center at (12, 0, -28) so it's northeast of Room 02
        CreateFloor(scene, "Room03_Floor", new System.Numerics.Vector3(12, -1, -28), 15f, 15f, floorColor);

        // Room 03 walls (fully enclosed for final battle)
        CreateWall(scene, "Room03_Wall_South", new System.Numerics.Vector3(12, 1, -20.5f), 15f, 3f, 0.5f, wallColor);
        CreateWall(scene, "Room03_Wall_North", new System.Numerics.Vector3(12, 1, -35.5f), 15f, 3f, 0.5f, wallColor);
        CreateWall(scene, "Room03_Wall_West_Upper", new System.Numerics.Vector3(4.5f, 1, -30), 0.5f, 3f, 7f, wallColor);
        CreateWall(scene, "Room03_Wall_West_Lower", new System.Numerics.Vector3(4.5f, 1, -24), 0.5f, 3f, 6f, wallColor);
        CreateWall(scene, "Room03_Wall_East", new System.Numerics.Vector3(19.5f, 1, -28), 0.5f, 3f, 15f, wallColor);

        // Room 03 enemies (5 enemies - harder final room)
        CreateEnemy(scene, playerEntity, "Room03_Enemy1", new System.Numerics.Vector3(8, 1.5f, -24), 1.0f, 0.0f, 0.0f);
        CreateEnemy(scene, playerEntity, "Room03_Enemy2", new System.Numerics.Vector3(16, 1.5f, -24), 1.0f, 0.0f, 0.0f);
        CreateEnemy(scene, playerEntity, "Room03_Enemy3", new System.Numerics.Vector3(8, 1.5f, -32), 1.0f, 0.0f, 0.0f);
        CreateEnemy(scene, playerEntity, "Room03_Enemy4", new System.Numerics.Vector3(16, 1.5f, -32), 1.0f, 0.0f, 0.0f);
        CreateEnemy(scene, playerEntity, "Room03_Enemy5", new System.Numerics.Vector3(12, 1.5f, -28), 1.0f, 0.0f, 0.0f); // Center enemy

        // Room 03 pickups (health and ammo for final room)
        CreateHealthPickup(scene, "Room03_HealthPickup1", new System.Numerics.Vector3(10, 1.5f, -26), 50f);
        CreateHealthPickup(scene, "Room03_HealthPickup2", new System.Numerics.Vector3(14, 1.5f, -30), 50f);
        CreateAmmoPickup(scene, "Room03_AmmoPickup", new System.Numerics.Vector3(12, 1.5f, -34), 50);

        Console.WriteLine("[Game] Multi-room level created: Room 01 (6x6), Room 02 (9x9), Room 03 (15x15)");
        Console.WriteLine("[Game] Total enemies: 10 | Total health pickups: 3 | Total ammo pickups: 1");
    }

    /// <summary>
    /// Create a floor plane for a room
    /// </summary>
    private void CreateFloor(Core.Scenes.Scene scene, string name, System.Numerics.Vector3 position,
        float width, float depth, System.Numerics.Vector4 color)
    {
        var entity = new Core.Entities.Entity(name);
        var transform = entity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;
        transform.LocalScale = new System.Numerics.Vector3(width, 0.5f, depth);

        var renderer = entity.AddComponent<Core.Components.MeshRenderer>();
        renderer.SetCube(1.0f, color);

        var rigidbody = entity.AddComponent<Core.Components.Rigidbody>();
        rigidbody.BodyType = Core.Plugins.Physics.BodyType.Static;
        rigidbody.Shape = new Core.Plugins.Physics.BoxShape(width, 0.5f, depth);

        scene.AddEntity(entity);
    }

    /// <summary>
    /// Create a wall section
    /// </summary>
    private void CreateWall(Core.Scenes.Scene scene, string name, System.Numerics.Vector3 position,
        float width, float height, float depth, System.Numerics.Vector4 color)
    {
        var entity = new Core.Entities.Entity(name);
        var transform = entity.AddComponent<Core.Components.Transform3D>();
        transform.Position = position;
        transform.LocalScale = new System.Numerics.Vector3(width, height, depth);

        var renderer = entity.AddComponent<Core.Components.MeshRenderer>();
        renderer.SetCube(1.0f, color);

        var rigidbody = entity.AddComponent<Core.Components.Rigidbody>();
        rigidbody.BodyType = Core.Plugins.Physics.BodyType.Static;
        rigidbody.Shape = new Core.Plugins.Physics.BoxShape(width, height, depth);

        scene.AddEntity(entity);
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

        // Set weapon AFTER entity is added and initialized
        enemyController.SetWeapon(eyeLazers);
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

        // Create 1x1 white texture for pause overlay (reusable)
        _pauseOverlayTexture = new Texture2D(GraphicsDevice, 1, 1);
        _pauseOverlayTexture.SetData(new[] { Color.White });

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

            // Load HUD content (SpriteBatch, textures, font)
            var hud = playerEntity.GetComponent<Gameplay.Components.HUD>();
            if (hud != null && _spriteBatch != null)
            {
                hud.LoadContent(GraphicsDevice, _spriteBatch, hudFont);
                Console.WriteLine("[Game] HUD content loaded");
            }

            // Load GameFlowManager content (for fade transitions and "You Died" message)
            if (_gameFlowManager != null && _spriteBatch != null)
            {
                _gameFlowManager.LoadContent(GraphicsDevice, _spriteBatch, hudFont);
                Console.WriteLine("[Game] GameFlowManager content loaded");
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

        // Skip game updates when paused or during scene transition
        if (_isPaused)
        {
            base.Update(gameTime);
            return;
        }

        // Skip scene updates during fade transitions (but allow GameFlowManager to update for fade animation)
        if (_gameFlowManager != null && _gameFlowManager.IsTransitioning)
        {
            // Only update the GameFlowManager during transitions
            var coreGameTime = new Core.Components.GameTime(
                gameTime.TotalGameTime,
                gameTime.ElapsedGameTime
            );
            _gameFlowManager.Update(coreGameTime);

            base.Update(gameTime);
            return;
        }

        // Update active scene
        var coreGameTime2 = new Core.Components.GameTime(
            gameTime.TotalGameTime,
            gameTime.ElapsedGameTime
        );

        _sceneManager?.Update(coreGameTime2);

        // Update pickup system
        if (_pickupSystem != null && _sceneManager?.ActiveScene != null)
        {
            var pickups = _sceneManager.ActiveScene.Entities
                .SelectMany(e => e.GetComponents<Gameplay.Components.Pickup>())
                .Where(p => !p.IsCollected);
            _pickupSystem.Update(coreGameTime2, pickups);
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

        // Draw fade overlay (MUST be last so it's on top of everything)
        if (_gameFlowManager != null)
        {
            _gameFlowManager.Draw(coreGameTime);
        }

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
        if (_spriteBatch == null || _pauseOverlayTexture == null)
            return;

        // Use AlphaBlend to ensure proper transparency (not accumulative)
        _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;

        // Draw semi-transparent black overlay using persistent texture
        _spriteBatch.Draw(
            _pauseOverlayTexture,
            new Rectangle(0, 0, screenWidth, screenHeight),
            Color.Black * 0.7f // 70% opacity
        );

        // Draw pause menu text
        try
        {
            var font = Content.Load<SpriteFont>("font");
            string pauseTitle = "PAUSED";
            string resumeText = "Press ESC to Resume";

            var titleSize = font.MeasureString(pauseTitle);
            var resumeSize = font.MeasureString(resumeText);

            // Draw centered title
            _spriteBatch.DrawString(
                font,
                pauseTitle,
                new Microsoft.Xna.Framework.Vector2(
                    screenWidth / 2 - titleSize.X / 2,
                    screenHeight / 2 - 30
                ),
                Color.White
            );

            // Draw resume instruction
            _spriteBatch.DrawString(
                font,
                resumeText,
                new Microsoft.Xna.Framework.Vector2(
                    screenWidth / 2 - resumeSize.X / 2,
                    screenHeight / 2 + 20
                ),
                Color.LightGray
            );
        }
        catch
        {
            // Font not loaded, skip text rendering
        }

        _spriteBatch.End();
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

        base.UnloadContent();
    }
}
