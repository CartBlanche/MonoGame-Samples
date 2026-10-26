using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Shooter.Gameplay;
using Shooter.Audio;
using Shooter.Gameplay.Effects;
using Shooter.Gameplay.Enemies;
using Shooter.Gameplay.Pickups;
using Shooter.Gameplay.Weapons;
using Shooter.Rendering;
using Shooter.Scene;
using Shooter.Scene.Diagnostics;
using Shooter.UI;

namespace Shooter
{
    /// <summary>
    /// Entry point for the MonoGame port of Unity's FPS Microgame. In this phase it is a scene inspector:
    /// free-fly camera over MainScene plus a headless screenshot mode that reproduces Tools/UnityReference/Baseline.
    ///
    /// Command line:
    ///   --shot &lt;outDir&gt;     render every pose of poses.json to MainScene_&lt;pose&gt;.png (1280x720) and exit
    ///   --pose &lt;name&gt;       start the free-fly camera at a pose (or restrict --shot to it)
    ///   --poses &lt;file&gt;      poses.json location (default: Tools/UnityReference/Baseline/poses.json found upward from the exe)
    ///   --weapon            draw the weapon viewmodel in --shot mode (the Unity baseline has none)
    ///   --basic             force the BasicEffect fallback renderer
    ///   --by-index          match model parts to Unity materials by index instead of by name
    ///   --nocull            submit every item to the renderer
    ///   --backbuffer        --shot: capture the back buffer instead of a RenderTarget2D
    ///   --projectile        --shot: also draw a Blaster projectile 8 m ahead of each pose camera (visual check of its mesh/material)
    ///   --hud               --shot: also draw the in-game HUD (needs the collision world, so the shot run loads it)
    ///   --hud-time &lt;s&gt;     --shot --hud: HUD clock (default 1 = objective message up; >= 4.5 shows the objective toast instead)
    ///   --vfx [seconds]     --shot: spawn the Blaster muzzle flash and an impact 8 m ahead of each pose camera, aged by that many seconds (default 0.04)
    ///   --ammo <ratio>      --shot: draw the weapon with that ammo ratio (gem colour, fuel cells; steam and cylinders with --vfx). Default 1 = full, like Unity's first frame
    ///   --enemy-sim <s>     --shot: put the player at each pose, run the enemy AI for s seconds (patrol, detection, shots, animation), then render
    ///   --hud-health <r>    --shot --hud: the player takes damage down to ratio r just before drawing (flash 0.05 s old, steady critical vignette when r <= 0.3)
    ///   --alert <s>         --shot: play every enemy's detect systems (VFX_Alert, AngrySteam) and let them run for s seconds first
    ///   --bars              --shot: draw the enemies' world-space health bars (implies --hit-flash 0: the HoverBot is hurt by 10 first)
    ///   --pickups <s>       --shot: run the pickups (bob, spin, glow particles) for s seconds before rendering; --loot kills the HoverBot first (its Loot_Health appears where it died); --pick hurts the player and steps into the first pickup (heal, sound, VFX_HealthPickup burst) before the s seconds run
    ///   --icon <file>       renders the Blaster alone into a transparent 1024 x 1024 PNG (app icon master) and exits; --icon-yaw / --icon-pitch / --icon-roll <deg> orbit the camera (150 / 14 / 0), --icon-flash <s> adds the muzzle flash at that age (0.05, 0 = none), --icon-blast <m> a shortened bolt that far ahead of the muzzle (0 = none), --icon-size <px>
    ///   --jetpack <s>       --shot: put the player on the Jetpack pickup (it unlocks: sound, toast, pickup gone), then jump, press jump again in the air and hold it for s seconds; renders the pose camera with the HUD (counter, trails)
    ///   --hit-flash <s>     --shot: the HoverBot takes a player hit, then the enemies run for s seconds (the body flash is at its brightest around 0.05 s)
    ///   --hud-kills <n>     --shot --hud: kill the first n enemies before drawing (objective counter, notification, compass markers of the rest)
    ///   --hud-target        --shot --hud: draw the crosshair as if an enemy were in the sights (red, 40)
    ///   --intro             start at the intro screen (the default when no mode flag is given)
    ///   --freefly           start in the free-fly scene inspector instead of the intro
    ///   --play              start in first-person play mode (F2 toggles play / free-fly at runtime)
    ///   --test-physics      run the headless player-controller self-test (exit code = failed checks) and exit
    ///
    /// Camera conversion (Unity pose -> MonoGame): see Shooter.Scene.UnityConvert.CameraWorldFromUnityPose.
    /// </summary>
    public class ShooterGame : Game
    {
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        private readonly GraphicsDeviceManager _graphics;
        private readonly string[] _args;

        private string _shotDir;
        private string _poseName;
        private string _posesPath;
        private bool _showWeapon = true;
        private bool _weaponForced;
        private bool _forceBasic;
        private bool _byIndex;
        private bool _noCull;
        private bool _useBackBuffer;
        private bool _startPlay;
        private bool _shotProjectile;
        private bool _testPhysics;
        private bool _shotHud;
        private bool _shotVfx;
        private float _shotVfxTime = 0.04f;
        private bool _shotHudTarget;
        private int _shotHudKills;
        private float _shotHudHealth = -1f;
        private float _shotPickups = -1f;
        private bool _shotLoot;
        private bool _shotPick;
        private float _shotJetpack = -1f;
        private string _iconFile;
        private IconShot.Options _iconOptions = IconShot.Defaults;
        private float _shotHitFlash = -1f;
        private bool _shotBars;
        private float _shotHitDamage = 10f;
        private bool _shotHitAll;   // --hit-all: --hit-flash hurts the Turret too
        private float _shotAlert = -1f;
        private EnemyHealthBars _healthBars;
        private float _shotEnemySim;
        private float _shotHudTime = 1f;
        private Hud _hud;
        private int _testFailures;

        // play mode (Phase 4): first-person controller over the baked colliders
        private bool _play;
        private PlayerController _player;
        private PlayerSettings _playerSettings;
        private PlayerWeaponsManager _weapons;
        private ProjectileManager _projectiles;
        private EnemyDirector _director;
        private GameFlow _flow;            // GameFlowManager + ObjectiveManager
        private MenuScreens _menus;        // IntroMenu / WinScene / LoseScene
        private PauseMenu _pause;          // InGameMenu (Tab / P) and the framerate counter
        private bool _screenshotRequested;
        private bool _shotPause, _shotPauseControls;   // --pause [controls]: the in-game menu over the pose
        private Texture2D _screenshotPreview;
        private MouseState _prevMouse;
        private bool _startIntro;
        private bool _startFreeFly;
        private string _shotMenu;          // --menu intro|controls|win|lose
        private bool _shotMenuSelect;
        private readonly PlayerInputHandler _playerInput = new PlayerInputHandler();

        private SceneLoader _loader;
        private LoadedScene _scene;
        private SfxPlayer _sfx;
        private VfxLibrary _vfx;
        private PickupEffects _pickupEffects;
        private Jetpack _jetpack;
        private JetpackEffects _jetpackEffects;
        private EnemyEffects _enemyEffects;
        private WeaponEffects _effects;
        private OverheatVisuals _overheat;
        private float? _shotAmmo;
        private ISceneRenderer _renderer;
        private string _rendererName;
        private readonly List<PoseInfo> _poses = new List<PoseInfo>();
        private int _poseIndex = -1;

        private Vector3 _position;
        private float _yaw;     // radians, 0 = looking down -Z, positive = turning toward -X (counter-clockwise seen from above)
        private float _pitch;   // radians, positive = up
        private bool _captureMouse;
        private bool _warmup = true;
        private int _frames;
        private KeyboardState _prevKeys;
        private readonly List<RenderItem> _visible = new List<RenderItem>();

        private class PoseInfo
        {
            public string Name;
            public Vector3 UnityPosition;
            public Vector3 UnityEuler;
            public float Fov;
            public Matrix World;
        }

        public ShooterGame(string[] args)
        {
            _args = args ?? Array.Empty<string>();
            ParseArgs();

            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = ShotWidth,
                PreferredBackBufferHeight = ShotHeight,
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                GraphicsProfile = GraphicsProfile.HiDef,
                SynchronizeWithVerticalRetrace = _shotDir == null
            };

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "First Person Shooter";
        }

        private void ParseArgs()
        {
            for (int i = 0; i < _args.Length; i++)
            {
                string a = _args[i];
                string next = i + 1 < _args.Length ? _args[i + 1] : null;
                switch (a)
                {
                    case "--shot": _shotDir = next ?? "shots"; i++; break;
                    case "--pose": _poseName = next; i++; break;
                    case "--poses": _posesPath = next; i++; break;
                    case "--weapon": _showWeapon = true; _weaponForced = true; break;
                    case "--basic": _forceBasic = true; break;
                    case "--by-index": _byIndex = true; break;
                    case "--nocull": _noCull = true; break;
                    case "--backbuffer": _useBackBuffer = true; break;
                    case "--play": _startPlay = true; break;
                    case "--intro": _startIntro = true; break;
                    case "--freefly": _startFreeFly = true; break;
                    case "--menu": _shotMenu = next; i++; break;
                    case "--menu-select": _shotMenuSelect = true; break;
                    case "--pause": _shotPause = true; if (next == "controls") { _shotPauseControls = true; i++; } break;
                    case "--hud": _shotHud = true; break;
                    case "--ammo":
                        if (float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ar)) _shotAmmo = ar;
                        i++;
                        break;
                    case "--vfx":
                        _shotVfx = true;
                        if (next != null && float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float vt)) { _shotVfxTime = vt; i++; }
                        break;
                    case "--hud-time": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotHudTime); i++; break;
                    case "--hud-target": _shotHud = true; _shotHudTarget = true; break;
                    case "--enemy-sim": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotEnemySim); i++; break;
                    case "--hud-health": _shotHud = true; float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotHudHealth); i++; break;
                    case "--pickups": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotPickups); i++; break;
                    case "--hit-flash": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotHitFlash); i++; break;
                    case "--hit-all": _shotHitAll = true; if (_shotHitFlash < 0f) _shotHitFlash = 0f; break;
                    case "--alert": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotAlert); i++; break;
                    case "--bars":
                        _shotBars = true;
                        if (_shotHitFlash < 0f) _shotHitFlash = 0f;
                        if (next != null && float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float barDamage)) { _shotHitDamage = barDamage; i++; }
                        break;
                    case "--icon": _iconFile = next; _shotDir = _shotDir ?? "shots"; i++; break;
                    case "--icon-yaw": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _iconOptions.Yaw); i++; break;
                    case "--icon-pitch": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _iconOptions.Pitch); i++; break;
                    case "--icon-roll": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _iconOptions.Roll); i++; break;
                    case "--icon-blast": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _iconOptions.Blast); i++; break;
                    case "--icon-flash": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _iconOptions.Flash); i++; break;
                    case "--icon-size": int.TryParse(next, out _iconOptions.Size); i++; break;
                    case "--jetpack": float.TryParse(next, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _shotJetpack); _shotHud = true; i++; break;
                    case "--pick": _shotPick = true; if (_shotPickups < 0f) _shotPickups = 0f; break;
                    case "--loot": _shotLoot = true; if (_shotPickups < 0f) _shotPickups = 0f; break;
                    case "--hud-kills": _shotHud = true; int.TryParse(next, out _shotHudKills); i++; break;
                    case "--projectile": _shotProjectile = true; break;
                    case "--test-physics": _testPhysics = true; break;
                }
            }
            if (_shotDir != null && !_weaponForced)
            {
                _showWeapon = false;
            }
            // A normal launch (no flags) opens the intro screen; free-fly is opt-in via --freefly (or --pose / --poses).
            if (!_startPlay && !_startFreeFly && _poseName == null && _posesPath == null && _shotDir == null && !_testPhysics)
            {
                _startIntro = true;
            }
        }

        // ------------------------------------------------------------------ loading

        protected override void LoadContent()
        {
            var sw = Stopwatch.StartNew();
            var loader = _loader = new SceneLoader(GraphicsDevice, Content, new SceneLoadOptions { MatchMaterialsByIndex = _byIndex, BuildCollision = _shotDir == null || _shotHud || _shotVfx || _shotEnemySim > 0f || _shotPickups >= 0f || _shotHitFlash >= 0f || _shotAlert >= 0f });
            _scene = loader.Load();
            Console.WriteLine("Scene loaded in " + _scene.LoadMilliseconds + " ms (json parse " + _scene.ParseMilliseconds + " ms)");
            Console.WriteLine("  render items: " + _scene.Items.Count + " (" + _scene.RenderMeshes + " render meshes, " + _scene.DistinctVertexBuffers + " distinct vertex buffers), weapon pieces " + _scene.WeaponPieces.Count);
            Console.WriteLine("  materials in table: " + _scene.Data.Materials.Count + ", instantiated " + _scene.MaterialsInstantiated + ", textures loaded " + _scene.TexturesLoaded);
            Console.WriteLine("  colliders " + _scene.Data.Colliders.Count + ", enemies " + _scene.Data.Enemies.Count + ", pickups " + _scene.Data.Pickups.Count +
                              ", patrol paths " + _scene.Data.PatrolPaths.Count + ", items with mirrored (det<0) transforms " + _scene.NegativeDeterminantItems);
            foreach (KeyValuePair<string, int> kv in loader.MissingMaterialTags)
            {
                Console.WriteLine("  note: part tag not found among renderer materials (fell back to index): " + kv.Key + " x" + kv.Value);
            }

            LoadPoses();
            _renderer = CreateRenderer();
            Console.WriteLine("Renderer: " + _rendererName);
            _sfx = new SfxPlayer(Content);
            if (_scene.Data.Weapon != null)
            {
                _overheat = new OverheatVisuals(_scene.Data, _scene, _loader.GetMaterialByName);
                _overheat.Update(_shotAmmo ?? 1f, Matrix.Identity);   // OverheatBehavior's first Update applies the full-ammo colour
            }
            if (_renderer is SceneRenderer sr)
            {
                sr.Particles.AutoRemoveFinished = true;
                sr.Particles.MaterialByName = _loader.GetMaterialByName;   // trail materials
                _vfx = new VfxLibrary(sr.Particles, _loader.GetMaterialByName);
                // the scene's ambient emitters (VFX_Dust, VFX_Lava): looping systems placed in MainScene, playing from the start (the enemies' and pickups' systems are driven by their scripts)
                foreach (Shooter.Rendering.Particles.ParticleSystemData.Placement p in _vfx.Placements("MainScene"))
                {
                    if (p.Active && (p.Data.Name == "VFX_Dust" || p.Data.Name == "VFX_Lava"))
                    {
                        _vfx.AddSystem(p, p.World);
                    }
                }
            }

            if (_poseName != null)
            {
                int idx = _poses.FindIndex(p => p.Name == _poseName);
                if (idx < 0)
                {
                    Console.WriteLine("Unknown pose '" + _poseName + "'");
                }
                else
                {
                    GoToPose(idx);
                }
            }
            if (_poseIndex < 0)
            {
                SetViewFromWorld(_scene.StartCameraWorld);
            }
            InitPlayer();
            Console.WriteLine("LoadContent total " + sw.ElapsedMilliseconds + " ms");
        }

        private void InitPlayer()
        {
            if (_scene.Collision == null)
            {
                return;
            }
            Console.WriteLine("Collision: " + _scene.CollisionSummary);
            _playerSettings = PlayerSettings.FromScene(_scene.Data);
            _playerInput.LookSensitivity = _playerSettings.LookSensitivity;
            _playerInput.InvertX = _playerSettings.InvertXAxis;
            _playerInput.InvertY = _playerSettings.InvertYAxis;
            _player = new PlayerController(_scene.Collision, _playerSettings);
            _player.Landed += OnPlayerLanded;
            _player.Jumped += () => PlayPlayerSfx("JumpSfx");
            _player.Footstep += () => PlayPlayerSfx("FootstepSfx");
            CreateWeapons();
            Console.WriteLine("Player spawn " + _player.Position + " yaw " + _playerSettings.StartYawDegrees);
            if (_testPhysics)
            {
                _testFailures = PhysicsSelfTest.Run(_scene.Data, _scene.Collision, _scene);
                Environment.ExitCode = _testFailures == 0 ? 0 : 1;
            }
            else if (_startIntro && _shotDir == null)
            {
                EnsureMenus();
                ShowMenu(MenuScreen.Intro);
            }
            else if (_startPlay && _shotDir == null)
            {
                SetPlayMode(true);
            }
        }

        /// <summary>The player's own 2D AudioSource (volume 1, group PlayerMovement 0 dB): PlayOneShot(clip) with the clip field of PlayerCharacterController.</summary>
        private void PlayPlayerSfx(string field)
        {
            if (_sfx == null || _scene.Data.Player.Components == null || !_scene.Data.Player.Components.TryGetValue("PlayerCharacterController", out System.Text.Json.JsonElement pc))
            {
                return;
            }
            string clip = Shooter.Gameplay.JsonFields.AssetName(pc, field);
            if (clip != null)
            {
                _sfx.PlayOneShot(clip, 1f);
            }
        }

        /// <summary>PlayerCharacterController.Update on landing: FallDamageSfx when the fall hurt (damage > 0), else LandSfx; both through the player's 2D AudioSource (volume 1, group PlayerMovement 0 dB).</summary>
        private void OnPlayerLanded(float fallSpeed, float damage)
        {
            if (_sfx == null || _scene.Data.Player.Components == null || !_scene.Data.Player.Components.TryGetValue("PlayerCharacterController", out System.Text.Json.JsonElement pc))
            {
                return;
            }
            _sfx.PlayOneShot(Shooter.Gameplay.JsonFields.AssetName(pc, damage > 0f ? "FallDamageSfx" : "LandSfx"), 1f);
        }

        private void CreateWeapons()
        {
            // reloading the scene: enemies back at full health, objective counting from zero
            if (_director == null)
            {
                _director = new EnemyDirector(_scene);
            }
            else
            {
                _director.Reset();
            }
            _director.SetPlayer(_player, _playerSettings);
            _projectiles = new ProjectileManager();
            _projectiles.Owner = _director.PlayerActor;
            _projectiles.DamageableOf = tag => _director.Enemies.DamageableOf(tag);
            _weapons = new PlayerWeaponsManager(_scene.Data, _player, _playerSettings, _scene.Collision, _projectiles);
            _weapons.IsEnemyHit = hit => _director.Enemies.EnemyOf(hit.Owner) != null;
            if (_hud == null)
            {
                _hud = new Hud(GraphicsDevice, Content);
            }
            EnsureMenus();
            _hud.Bind(_player, _weapons);
            _hud.BindCombat(_director);
            _hud.BindHealth(_director.PlayerHealth);
            // a fresh Jetpack (locked again, like the scene loading); the pickup that unlocks it is back with the new EnemyDirector.Pickups
            _jetpack = new Jetpack(_player, JetpackSettings.FromScene(_scene.Data), _playerSettings.GravityDownForce);
            _director.Pickups.GetPlayerJetpack = () => _jetpack;
            _hud.BindJetpack(_jetpack);
            _flow.Bind(_director);
            _director.GameIsEnding = () => _flow.GameIsEnding;
            _director.PlayerHealth.Invincible = _pause.Invincible;   // the menu's toggle outlives a reload of the scene
            SoundEffect.MasterVolume = 1f;
            // PlayerCharacterController.OnDie: lower the weapon (a non-existing weapon index); the controller sets IsDead itself
            _director.PlayerHealth.OnDie += () => _weapons.SwitchToWeaponIndex(-1, true);
            if (_effects != null)
            {
                _effects.Dispose();
                _effects = null;
            }
            if (_pickupEffects != null)
            {
                _pickupEffects.Dispose();
                _pickupEffects = null;
            }
            if (_enemyEffects != null)
            {
                _enemyEffects.Dispose();
                _enemyEffects = null;
            }
            if (_jetpackEffects != null)
            {
                _jetpackEffects.Dispose();
                _jetpackEffects = null;
            }
            if (_vfx != null)
            {
                _jetpackEffects = new JetpackEffects(_jetpack, _player, _vfx, ((SceneRenderer)_renderer).Particles, _sfx);
                _enemyEffects = new EnemyEffects(_director, _vfx, _sfx, () => _player.CameraWorld);
                _pickupEffects = new PickupEffects(_scene, _director.Pickups, _vfx, ((SceneRenderer)_renderer).Particles, _sfx);
                _effects = new WeaponEffects(_scene.Data, _player, _weapons, _projectiles, _vfx, ((SceneRenderer)_renderer).Particles, _sfx);
                if (_overheat != null)
                {
                    _overheat.DetachParticles(((SceneRenderer)_renderer).Particles);   // a respawn reloads the weapon: fresh steam and cylinders
                    _overheat.AttachParticles(_vfx);
                    _effects.Overheat = _overheat;
                }
            }
        }

        private void EnsureMenus()
        {
            if (_menus == null)
            {
                _menus = new MenuScreens(GraphicsDevice, Content);
            }
            if (_pause == null)
            {
                _pause = new PauseMenu(GraphicsDevice, Content);
                _pause.LookSensitivity = _playerInput.LookSensitivity;
                _pause.ShadowsOn = _scene.Lighting.SunShadows;
                // InGameMenuManager.SetPauseMenuActivation: open = cursor free, Time.timeScale 0, master volume 0.5; close = cursor locked again, volume 1
                _pause.Opened += () => { _captureMouse = false; IsMouseVisible = true; SoundEffect.MasterVolume = PauseMenu.VolumeWhenMenuOpen; };
                _pause.Closed += () => { _captureMouse = true; IsMouseVisible = false; _warmup = true; SoundEffect.MasterVolume = _flow != null ? _flow.MasterVolume : 1f; };
                _pause.LookSensitivityChanged += v => _playerInput.LookSensitivity = v;
                _pause.ShadowsChanged += on => _scene.Lighting.SunShadows = on;
                _pause.InvincibleChanged += on => { if (_director != null) _director.PlayerHealth.Invincible = on; };
                _pause.ScreenshotRequested += () => _screenshotRequested = true;
            }
            if (_flow == null)
            {
                _flow = new GameFlow();
                _flow.DisplayMessage += (text, delay) => _hud.ShowMessage(text, delay);
                _flow.PlayVictory += () => { if (_sfx != null) _sfx.PlayOneShot(GameFlow.VictorySound, GameFlow.VictoryGain); };
                _flow.EndScreen += win => ShowMenu(win ? MenuScreen.Win : MenuScreen.Lose);   // SceneManager.LoadScene(WinScene / LoseScene)
            }
        }

        /// <summary>A menu scene is loaded: the cursor is unlocked and visible (GameFlowManager.EndGame / MenuNavigation.Start).</summary>
        private void ShowMenu(MenuScreen screen)
        {
            _menus.Show(screen);
            _captureMouse = false;
            IsMouseVisible = true;
        }

        private void UpdateMenus(GameTime gameTime, KeyboardState keys)
        {
            MouseState ms = Mouse.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MenuAction action = _menus.Update(dt, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, ms, _prevMouse, keys, _prevKeys);
            _prevMouse = ms;
            if (action == MenuAction.Menu)
            {
                ShowMenu(MenuScreen.Intro);
            }
            else if (action == MenuAction.Play || action == MenuAction.PlayAgain)
            {
                // LoadScene("MainScene"): a fresh scene with the player at the start and the cursor locked
                _menus.Hide();
                if (_play)
                {
                    RespawnPlayer();
                    _showWeapon = true;
                    _captureMouse = true;
                    IsMouseVisible = false;
                    _warmup = true;
                }
                else
                {
                    SetPlayMode(true);
                }
            }
        }

        /// <summary>Respawn at the baked start; like reloading the Unity scene it also resets the weapon (raised again) and clears projectiles.</summary>
        private void RespawnPlayer()
        {
            _player.Respawn(_playerSettings.StartPosition, _playerSettings.StartYawDegrees);
            CreateWeapons();
        }

        private void SetPlayMode(bool on)
        {
            if (on == _play || _player == null)
            {
                return;
            }
            _play = on;
            if (on)
            {
                RespawnPlayer();
                _showWeapon = true;
                _captureMouse = true;
                IsMouseVisible = false;
                _warmup = true;
            }
            else
            {
                SetViewFromWorld(_player.CameraWorld);   // continue free-fly from where the player's eyes were
                _captureMouse = false;
                IsMouseVisible = true;
            }
            Console.WriteLine(on ? "Play mode" : "Free-fly mode");
        }

        /// <summary>Single switch point between the real renderer and the fallback.</summary>
        private ISceneRenderer CreateRenderer()
        {
            if (!_forceBasic)
            {
                Type t = typeof(ShooterGame).Assembly.GetType("Shooter.Rendering.SceneRenderer");
                if (t != null)
                {
                    try
                    {
                        _rendererName = t.FullName;
                        return (ISceneRenderer)Activator.CreateInstance(t, GraphicsDevice, Content, 1024);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("SceneRenderer failed to initialise (" + (e.InnerException ?? e).Message + "); using BasicSceneRenderer");
                    }
                }
            }
            _rendererName = typeof(BasicSceneRenderer).FullName;
            return new BasicSceneRenderer(GraphicsDevice);
        }

        private void LoadPoses()
        {
            string path = _posesPath ?? FindUpward(Path.Combine("Tools", "UnityReference", "Baseline", "poses.json"));
            if (path == null || !File.Exists(path))
            {
                Console.WriteLine("poses.json not found");
                return;
            }
            using (JsonDocument doc = JsonDocument.Parse(File.ReadAllBytes(path)))
            {
                foreach (JsonElement p in doc.RootElement.GetProperty("poses").EnumerateArray())
                {
                    JsonElement pos = p.GetProperty("position");
                    JsonElement eul = p.GetProperty("eulerAngles");
                    var info = new PoseInfo
                    {
                        Name = p.GetProperty("name").GetString(),
                        UnityPosition = new Vector3((float)pos[0].GetDouble(), (float)pos[1].GetDouble(), (float)pos[2].GetDouble()),
                        UnityEuler = new Vector3((float)eul[0].GetDouble(), (float)eul[1].GetDouble(), (float)eul[2].GetDouble()),
                        Fov = p.TryGetProperty("fov", out JsonElement f) ? (float)f.GetDouble() : 60f
                    };
                    info.World = UnityConvert.CameraWorldFromUnityPose(info.UnityPosition, info.UnityEuler);
                    _poses.Add(info);
                }
            }
            Console.WriteLine("Loaded " + _poses.Count + " poses from " + path);
        }

        private static string FindUpward(string relative)
        {
            foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            {
                string dir = start;
                while (!string.IsNullOrEmpty(dir))
                {
                    string candidate = Path.Combine(dir, relative);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }
            return null;
        }

        // ------------------------------------------------------------------ camera

        private void SetViewFromWorld(Matrix world)
        {
            _position = world.Translation;
            Vector3 f = Vector3.TransformNormal(Vector3.Forward, world);
            _yaw = (float)Math.Atan2(-f.X, -f.Z);
            _pitch = (float)Math.Asin(MathHelper.Clamp(f.Y, -1f, 1f));
        }

        private Matrix FlyWorld()
        {
            return Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f) * Matrix.CreateTranslation(_position);
        }

        private void GoToPose(int index)
        {
            if (_poses.Count == 0)
            {
                return;
            }
            _poseIndex = ((index % _poses.Count) + _poses.Count) % _poses.Count;
            SetViewFromWorld(_poses[_poseIndex].World);
            Console.WriteLine("Pose " + _poses[_poseIndex].Name);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keys = Keyboard.GetState();
            if (_testPhysics)
            {
                Exit();
                return;
            }
            if (_shotDir != null)
            {
                base.Update(gameTime);
                return;
            }

            if (keys.IsKeyDown(Keys.Escape) && !(_pause != null && _pause.IsOpen))   // Cancel closes the pause menu instead
            {
                Exit();
            }
            if (_menus != null && _menus.Screen != MenuScreen.None)
            {
                UpdateMenus(gameTime, keys);
                _prevKeys = keys;
                base.Update(gameTime);
                return;
            }
            if (_play && _pause != null)
            {
                // InGameMenuManager: Tab / P / Cancel; the game is stopped while the menu is open
                MouseState pms = Mouse.GetState();
                float pdt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                _pause.CountFrame(pdt);
                _pause.Update(pdt, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, pms, _prevMouse, keys, _prevKeys);
                bool clickedOutside = !_pause.IsOpen && !_captureMouse && pms.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released;
                _prevMouse = pms;
                if (clickedOutside)
                {
                    _captureMouse = true;   // "Lock cursor when clicking outside of menu"
                    IsMouseVisible = false;
                    _warmup = true;
                }
                if (_pause.IsOpen)
                {
                    _prevKeys = keys;
                    base.Update(gameTime);
                    return;
                }
            }
            if (!_play && Pressed(keys, Keys.Tab))
            {
                _captureMouse = !_captureMouse;
                IsMouseVisible = !_captureMouse;
                if (_captureMouse)
                {
                    Mouse.SetPosition(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);
                }
            }
            if (Pressed(keys, Keys.F2)) { SetPlayMode(!_play); }
            if (_play)
            {
                UpdatePlay(gameTime, keys);
                _prevKeys = keys;
                base.Update(gameTime);
                return;
            }
            if (Pressed(keys, Keys.N)) { GoToPose(_poseIndex + 1); }
            if (Pressed(keys, Keys.P)) { GoToPose(_poseIndex - 1); }
            if (Pressed(keys, Keys.F)) { _showWeapon = !_showWeapon; }
            if (Pressed(keys, Keys.C)) { _noCull = !_noCull; }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_captureMouse && IsActive)
            {
                MouseState ms = Mouse.GetState();
                int cx = Window.ClientBounds.Width / 2, cy = Window.ClientBounds.Height / 2;
                if (!_warmup)
                {
                    _yaw -= (ms.X - cx) * 0.0025f;
                    _pitch = MathHelper.Clamp(_pitch - (ms.Y - cy) * 0.0025f, -1.55f, 1.55f);
                }
                Mouse.SetPosition(cx, cy);
                _warmup = false;
            }

            Matrix rot = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
            Vector3 move = Vector3.Zero;
            if (keys.IsKeyDown(Keys.W)) { move += rot.Forward; }
            if (keys.IsKeyDown(Keys.S)) { move -= rot.Forward; }
            if (keys.IsKeyDown(Keys.D)) { move += rot.Right; }
            if (keys.IsKeyDown(Keys.A)) { move -= rot.Right; }
            if (keys.IsKeyDown(Keys.E) || keys.IsKeyDown(Keys.Space)) { move += Vector3.Up; }
            if (keys.IsKeyDown(Keys.Q) || keys.IsKeyDown(Keys.LeftControl)) { move -= Vector3.Up; }
            if (move != Vector3.Zero)
            {
                move.Normalize();
                float speed = keys.IsKeyDown(Keys.LeftShift) ? 30f : 6f;
                _position += move * speed * dt;
            }
            _prevKeys = keys;

            if (gameTime.TotalGameTime.Milliseconds < 20)
            {
                Window.Title = "First Person Shooter [" + _rendererName + "] pos(" + _position.X.ToString("0.0") + "," + _position.Y.ToString("0.0") + "," + _position.Z.ToString("0.0") +
                               ") items " + _visible.Count + "/" + _scene.Items.Count;
            }
            base.Update(gameTime);
        }

        private void UpdatePlay(GameTime gameTime, KeyboardState keys)
        {
            Point delta = Point.Zero;
            if (_captureMouse && IsActive)
            {
                MouseState ms = Mouse.GetState();
                int cx = Window.ClientBounds.Width / 2, cy = Window.ClientBounds.Height / 2;
                if (!_warmup)
                {
                    delta = new Point(ms.X - cx, ms.Y - cy);
                }
                Mouse.SetPosition(cx, cy);
                _warmup = false;
            }
            if (Pressed(keys, Keys.R))
            {
                RespawnPlayer();
            }

            // like Unity, input is only processed while the cursor is locked (Tab releases it)
            PlayerInput input = _captureMouse ? _playerInput.Read(keys, Mouse.GetState(), delta) : _playerInput.Read(default(KeyboardState), default(MouseState), Point.Zero);
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_player.IsDead || _flow.GameIsEnding)
            {
                input = default(PlayerInput);   // GameFlowManager.GameIsEnding: PlayerInputHandler stops processing input
            }
            _hud.GameIsEnding = _player.IsDead || _flow.GameIsEnding;
            _player.Update(dt, input);
            _jetpack.Update(dt, input);
            if (_jetpackEffects != null)
            {
                _jetpackEffects.Update();
            }
            _weapons.Update(dt, input);
            _projectiles.Update(dt);
            _director.Update(dt);
            if (_pickupEffects != null)
            {
                _pickupEffects.Update(dt);
            }
            if (_enemyEffects != null)
            {
                _enemyEffects.Update(dt);
            }
            _weapons.LateUpdate(dt);
            if (_effects != null)
            {
                _effects.Update(dt);
            }
            _hud.Update(dt);
            _flow.Update(dt);
            SoundEffect.MasterVolume = _flow.MasterVolume;   // AudioUtility.SetMasterVolume

            if (gameTime.TotalGameTime.Milliseconds < 20)
            {
                Vector3 p = _player.Position;
                Window.Title = "First Person Shooter [play] pos(" + p.X.ToString("0.00") + "," + p.Y.ToString("0.00") + "," + p.Z.ToString("0.00") + ") yaw " + _player.YawDegrees.ToString("0") +
                               " speed " + _player.CharacterVelocity.Length().ToString("0.0") + (_player.IsGrounded ? " grounded" : " air") + (_player.IsCrouching ? " crouch" : "") + " ammo " + (_weapons.ActiveWeapon != null ? _weapons.ActiveWeapon.CurrentAmmo.ToString("0.0") : "-") + (_weapons.IsAiming ? " aim" : "") + " fov " + _weapons.Fov.ToString("0") + " proj " + _projectiles.Live.Count +  (_player.IsDead ? " DEAD" : "") + (_flow.GameIsEnding ? (_flow.Win ? " WIN (fading)" : " LOSE (fading)") : "");
            }
        }

        private bool Pressed(KeyboardState keys, Keys k)
        {
            return keys.IsKeyDown(k) && !_prevKeys.IsKeyDown(k);
        }

        // ------------------------------------------------------------------ drawing

        protected override void Draw(GameTime gameTime)
        {
            if (_shotDir != null)
            {
                _frames++;
                if (_frames >= 2)
                {
                    RunShots(gameTime);
                    Exit();
                }
                return;
            }

            if (_testPhysics)
            {
                return;
            }
            if (_menus != null && _menus.Screen != MenuScreen.None)
            {
                GraphicsDevice.Clear(Color.Black);
                _menus.Draw(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
                base.Draw(gameTime);
                return;
            }
            float aspect = GraphicsDevice.Viewport.AspectRatio;
            _renderer.Overlay = _play ? _hud.BuildLayer(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) : null;
            RenderFrame(gameTime, _play ? _player.CameraWorld : FlyWorld(), aspect, _play ? _weapons.Fov : _poseIndex < 0 ? _scene.Data.Camera.Fov : _poses[_poseIndex].Fov, _play);
            if (_play && _flow != null)
            {
                _menus.DrawFade(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, _flow.FadeAlpha);   // EndGameFadeCanvas
            }
            if (_play && _pause != null && (_pause.IsOpen || _pause.FramerateOn))
            {
                if (_screenshotRequested)
                {
                    TakeScreenshot();   // the frame without the menu, like TakeScreenshot hiding the canvas first
                }
                _pause.Draw(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            }
            base.Draw(gameTime);
        }

        /// <summary>TakeScreenshot: the last frame goes to Screenshots/Screenshot.png (same file every time) and the menu previews it.</summary>
        private void TakeScreenshot()
        {
            _screenshotRequested = false;
            try
            {
                int w = GraphicsDevice.PresentationParameters.BackBufferWidth, h = GraphicsDevice.PresentationParameters.BackBufferHeight;
                var data = new Color[w * h];
                GraphicsDevice.GetBackBufferData(data);
                for (int i = 0; i < data.Length; i++) data[i].A = 255;
                string dir = Path.Combine(AppContext.BaseDirectory, "Screenshots");
                Directory.CreateDirectory(dir);
                var tex = new Texture2D(GraphicsDevice, w, h);
                tex.SetData(data);
                using (FileStream fs = File.Create(Path.Combine(dir, "Screenshot.png")))
                {
                    tex.SaveAsPng(fs, w, h);
                }
                if (_screenshotPreview != null) _screenshotPreview.Dispose();
                _screenshotPreview = tex;
                _pause.Preview = tex;
                Console.WriteLine("Screenshot saved to " + Path.Combine(dir, "Screenshot.png"));
            }
            catch (Exception e)
            {
                Console.WriteLine("Screenshot failed: " + e.Message);
            }
        }

        private void RenderFrame(GameTime gameTime, Matrix cameraWorld, float aspect, float fovDegrees, bool playView = false)
        {
            Camera cam = _scene.Camera;
            cam.FieldOfViewY = MathHelper.ToRadians(fovDegrees);
            SceneLoader.SetCamera(cam, cameraWorld, aspect);
            Camera wcam = _scene.WeaponCamera;
            wcam.FieldOfViewY = MathHelper.ToRadians(playView ? _weapons.WeaponFov : _scene.Data.WeaponCamera.Fov);
            SceneLoader.SetCamera(wcam, cameraWorld, aspect);

            _visible.Clear();
            if (_noCull)
            {
                _visible.AddRange(_scene.Items);
                _scene.AddDynamic(_visible, null, default(BoundingSphere));
            }
            else
            {
                var frustum = new BoundingFrustum(cam.View * cam.Projection);
                _scene.Cull(frustum, cam.Position, _scene.Lighting.ShadowDistance, _visible);
            }
            if (playView)
            {
                if (_showWeapon && _weapons.WeaponVisible)
                {
                    _scene.BuildWeaponItems(cameraWorld, _weapons.SocketLocal, _visible);
                }
                foreach (ProjectileStandard p in _projectiles.Live)
                {
                    _scene.BuildProjectileItems(p.Position, p.Forward, _visible);
                }
                foreach (ProjectileStandard p in _director.EnemyProjectiles.Live)
                {
                    _scene.BuildEnemyProjectileItems(p.Position, p.Forward, _visible);
                }
            }
            else if (_showWeapon)
            {
                _scene.BuildWeaponItems(cameraWorld, _visible);
            }
            if (_shotProjectile && _shotDir != null)
            {
                Vector3 fwd = Vector3.TransformNormal(Vector3.Forward, cameraWorld);
                Vector3 right = Vector3.TransformNormal(Vector3.Right, cameraWorld);
                _scene.BuildProjectileItems(cameraWorld.Translation + fwd * 8f + right * 0.6f - Vector3.Up * 0.3f, fwd, _visible);
            }
            if ((playView || _shotBars) && _director != null)
            {
                if (_healthBars == null)
                {
                    _healthBars = new EnemyHealthBars(GraphicsDevice);
                }
                _healthBars.Build(_director.Enemies.Enemies, cameraWorld.Translation, _visible);
            }
            _renderer.Render(gameTime, _visible, _scene.Lighting, cam, wcam, _scene.Post);
        }

        /// <summary>--shot --menu intro|controls|win|lose [--menu-select]: one screen of the menu canvases (selected button = Down pressed once).</summary>
        private void RunMenuShot(int width, int height, RenderTarget2D rt)
        {
            EnsureMenus();
            MenuScreen screen = _shotMenu == "win" ? MenuScreen.Win : _shotMenu == "lose" ? MenuScreen.Lose : MenuScreen.Intro;
            _menus.Show(screen);
            _menus.Update(0f, width, height, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            if (_shotMenuSelect)
            {
                _menus.Update(1f, width, height, default(MouseState), default(MouseState), new KeyboardState(Keys.Down), default(KeyboardState));
            }
            if (_shotMenu == "controls")
            {
                _menus.SetControlsOpen(true);
            }
            _menus.Update(1f, width, height, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            if (rt != null)
            {
                GraphicsDevice.SetRenderTarget(rt);
            }
            GraphicsDevice.Clear(Color.Black);
            _menus.Draw(width, height);
            string file = Path.Combine(_shotDir, "Menu_" + _shotMenu + (_shotMenuSelect ? "_selected" : "") + ".png");
            if (rt != null)
            {
                GraphicsDevice.SetRenderTarget(null);
                using (FileStream fs = File.Create(file))
                {
                    rt.SaveAsPng(fs, width, height);
                }
            }
            Console.WriteLine("  wrote " + file);
        }

        /// <summary>
        /// --shot --jetpack: the player starts on the Jetpack pickup (so the first update unlocks it), stands still, jumps, presses jump again in the air
        /// and holds it for the requested time, with the same per-frame calls the play loop makes.
        /// </summary>
        private void RunJetpackShot(PoseInfo pose)
        {
            Pickup spot = null;
            foreach (Pickup p in _director.Pickups.All)
            {
                if (p.Kind == "Jetpack") spot = p;
            }
            if (spot == null)
            {
                Console.WriteLine("  no Jetpack pickup in the scene");
                return;
            }
            _player.Respawn(spot.Start, pose.UnityEuler.Y);
            CreateWeapons();
            const float dt = 1f / 60f;
            Action<PlayerInput> step = input =>
            {
                _player.Update(dt, input);
                _jetpack.Update(dt, input);
                if (_jetpackEffects != null) _jetpackEffects.Update();
                _director.Update(dt);
                if (_pickupEffects != null) _pickupEffects.Update(dt);
                if (_effects != null) _effects.Update(dt);   // ticks the particle manager
                _hud.Update(dt);
            };
            for (int i = 0; i < 30; i++) step(default(PlayerInput));                        // land, pick the jetpack up (inside its trigger)
            step(new PlayerInput { JumpDown = true, JumpHeld = true });                     // the jump
            for (int i = 0; i < 12; i++) step(new PlayerInput { JumpHeld = true });         // jump held but not pressed again: no thrust yet
            step(new PlayerInput { JumpDown = true, JumpHeld = true });                     // pressed again in the air: armed
            int steps = (int)Math.Round(_shotJetpack * 60f);
            for (int i = 0; i < steps; i++) step(new PlayerInput { JumpHeld = true });
            Console.WriteLine("  jetpack: unlocked " + _jetpack.IsJetpackUnlocked + ", fuel " + _jetpack.CurrentFillRatio.ToString("0.00") + ", in use " + _jetpack.IsInUse + ", player " + _player.Position + ", velocity y " + _player.CharacterVelocity.Y.ToString("0.0"));
        }

        private void RunShots(GameTime gameTime)
        {
            if (_iconFile != null)
            {
                IconShot.Render(GraphicsDevice, _scene, _renderer, gameTime, _vfx, ((SceneRenderer)_renderer).Particles, _iconFile, _iconOptions);
                return;
            }
            Directory.CreateDirectory(_shotDir);
            int width = ShotWidth, height = ShotHeight;
            RenderTarget2D rt = _useBackBuffer ? null : new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8);
            if (_shotMenu != null)
            {
                RunMenuShot(width, height, rt);
                rt?.Dispose();
                return;
            }
            if (_shotVfx && _weapons != null)
            {
                for (int i = 0; i < 300; i++)   // let the weapon finish raising so the muzzle sits where the viewmodel is drawn
                {
                    _weapons.Update(0.01f, default(PlayerInput));
                    _weapons.LateUpdate(0.01f);
                }
            }
            if (_shotPickups >= 0f && _director != null)
            {
                if (_shotLoot)
                {
                    foreach (Enemy en in _director.AllEnemies.ToArray())
                    {
                        if (en.Data.Kind == "HoverBot") en.Health.Kill();
                    }
                }
                if (_shotPick && _director.Pickups.All.Count > 0)
                {
                    _director.PlayerHealth.TakeDamage(50f, null);
                    _player.Respawn(_director.Pickups.All[0].Start, 0f);
                    _director.Pickups.Update(1f / 60f);
                    if (_pickupEffects != null) _pickupEffects.Update(1f / 60f);
                }
                int steps = (int)Math.Round(_shotPickups * 60f);
                for (int i = 0; i <= steps; i++)
                {
                    _director.Pickups.Update(1f / 60f);
                    if (_pickupEffects != null) _pickupEffects.Update(1f / 60f);
                    if (_enemyEffects != null) _enemyEffects.Update(1f / 60f);
                    if (_effects != null) _effects.Update(1f / 60f);   // ticks the particle manager
                }
            }
            if (_shotAlert >= 0f && _enemyEffects != null)
            {
                for (int i = 0; i < 30; i++) { _enemyEffects.Update(1f / 60f); if (_effects != null) _effects.Update(1f / 60f); }   // idle first, like play: the manager must not drop the stopped systems
                _enemyEffects.DebugAlert(true);   // the detect systems (alert mark, steam) as if every enemy had just seen the player
            }
            if (_shotHitFlash >= 0f && _director != null)
            {
                foreach (Enemy en in _director.AllEnemies)
                {
                    if (en.Data.Kind == "HoverBot" || _shotHitAll) en.Health.TakeDamage(_shotHitDamage, _director.PlayerActor);
                }
                int flashSteps = (int)Math.Round(_shotHitFlash * 60f);
                for (int i = 0; i < flashSteps; i++) { _director.Update(1f / 60f); if (_enemyEffects != null) _enemyEffects.Update(1f / 60f); if (_effects != null) _effects.Update(1f / 60f); }
            }
            if (_shotAlert > 0f && _enemyEffects != null)
            {
                int alertSteps = (int)Math.Round(_shotAlert * 60f);
                for (int i = 0; i < alertSteps; i++) { _enemyEffects.Update(1f / 60f); if (_effects != null) _effects.Update(1f / 60f); }
            }
            int count = 0;
            var sw = Stopwatch.StartNew();
            foreach (PoseInfo pose in _poses)
            {
                if (_poseName != null && pose.Name != _poseName)
                {
                    continue;
                }
                if (rt != null)
                {
                    GraphicsDevice.SetRenderTarget(rt);
                }
                // Throwaway pass so the post chain's previous view-projection equals this pose (no motion blur smear).
                _renderer.Overlay = null;
                if (_shotVfx && _effects != null)
                {
                    Vector3 fwd = Vector3.Normalize(Vector3.TransformNormal(Vector3.Forward, pose.World));
                    _effects.Preview(pose.World, pose.World.Translation + fwd * 8f, -fwd, _shotVfxTime, _shotAmmo ?? 1f);
                }
                if (_shotJetpack >= 0f && _player != null && _director != null)
                {
                    RunJetpackShot(pose);
                }
                if (_shotEnemySim > 0f && _player != null)
                {
                    Vector3 t = pose.World.Translation;
                    _player.Respawn(new Vector3(t.X, t.Y - _playerSettings.CapsuleHeightStanding * _playerSettings.CameraHeightRatio, t.Z), pose.UnityEuler.Y);
                    CreateWeapons();
                    for (int i = 0; i < 30; i++) _player.Update(1f / 60f, default(PlayerInput));
                    _player.Respawn(new Vector3(t.X, t.Y - _playerSettings.CapsuleHeightStanding * _playerSettings.CameraHeightRatio, t.Z), pose.UnityEuler.Y);
                    int steps = (int)Math.Round(_shotEnemySim * 60f);
                    for (int i = 0; i < steps; i++)
                    {
                        _player.Update(1f / 60f, default(PlayerInput));
                        _director.Update(1f / 60f);
                    }
                    foreach (Enemy en in _director.AllEnemies) Console.WriteLine("  enemy " + en.Data.Kind + " at " + en.Position + " yaw " + en.YawDegrees.ToString("0.0") + (en.Mobile != null ? " " + en.Mobile.AiState : ""));
                }
                RenderFrame(gameTime, pose.World, width / (float)height, pose.Fov);
                if (_shotHud && _hud != null && _shotJetpack >= 0f)
                {
                    _renderer.Overlay = _hud.BuildLayer(width, height);   // the flight of RunJetpackShot is the state to show
                }
                else if (_shotHud && _hud != null)
                {
                    _player.YawDegrees = pose.UnityEuler.Y;   // the compass reads the player's heading
                    {
                        // ... and position (root = camera - 1.44 standing eye height)
                        Vector3 t = pose.World.Translation;
                        _player.Respawn(new Vector3(t.X, t.Y - _playerSettings.CapsuleHeightStanding * _playerSettings.CameraHeightRatio, t.Z), pose.UnityEuler.Y);
                        CreateWeapons();   // rebinds the HUD to a fresh scene state
                    }
                    if (_shotHudTarget)
                    {
                        _weapons.IsEnemyHit = hit => true;   // stand-in until enemies exist: any hit counts
                        _weapons.Update(0f, default(PlayerInput));
                    }
                    _hud.SnapToSteadyState(_shotHudTime);
                    if (_shotHudKills > 0)
                    {
                        for (int k = 0; k < _shotHudKills && k < _director.AllEnemies.Count; k++)
                        {
                            _director.AllEnemies[k].Health.Kill();
                        }
                        _hud.Update(0.6f);   // notification faded in
                    }
                    if (_shotHudHealth >= 0f)
                    {
                        _director.PlayerHealth.TakeDamage(_director.PlayerHealth.MaxHealth * (1f - _shotHudHealth), null);
                        _hud.GameIsEnding = true;
                        _hud.Update(0.05f);
                    }
                    _renderer.Overlay = _hud.BuildLayer(width, height);
                }
                RenderFrame(gameTime, pose.World, width / (float)height, pose.Fov);
                if (_shotPause)
                {
                    EnsureMenus();
                    _pause.FramerateOn = true;
                    _pause.CountFrame(1f);
                    _pause.Open();
                    _pause.Update(0f, width, height, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
                    if (_shotPauseControls)
                    {
                        // click the Controls button
                        Point at = _pause.Controls[(int)PauseMenu.Row.Controls].Center;
                        _pause.Update(0.2f, width, height, new MouseState(at.X, at.Y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released), default(MouseState), default(KeyboardState), default(KeyboardState));
                        _pause.Update(0.2f, width, height, new MouseState(at.X, at.Y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released), new MouseState(at.X, at.Y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released), default(KeyboardState), default(KeyboardState));
                    }
                    else
                    {
                        _pause.Update(1f, width, height, default(MouseState), default(MouseState), new KeyboardState(Keys.Down), default(KeyboardState));   // select the slider
                        _pause.Update(1f, width, height, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
                    }
                    _pause.Draw(width, height);
                }
                string file = Path.Combine(_shotDir, "MainScene_" + pose.Name + ".png");
                if (rt != null)
                {
                    GraphicsDevice.SetRenderTarget(null);
                    using (FileStream fs = File.Create(file))
                    {
                        rt.SaveAsPng(fs, width, height);
                    }
                }
                else
                {
                    var data = new Color[GraphicsDevice.PresentationParameters.BackBufferWidth * GraphicsDevice.PresentationParameters.BackBufferHeight];
                    GraphicsDevice.GetBackBufferData(data);
                    using (var tex = new Texture2D(GraphicsDevice, GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight))
                    using (FileStream fs = File.Create(file))
                    {
                        tex.SetData(data);
                        tex.SaveAsPng(fs, tex.Width, tex.Height);
                    }
                }
                count++;
                Console.WriteLine("  wrote " + file);
            }
            Console.WriteLine("Rendered " + count + " poses in " + sw.ElapsedMilliseconds + " ms");
            rt?.Dispose();
        }
    }
}
