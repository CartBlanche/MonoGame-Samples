using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ThirdPersonShooter.Physics;
using System.Collections.Generic;

namespace ThirdPersonShooter
{
    /// <summary>Entry point of the MonoGame port of the Godot Third Person Shooter demo.</summary>
    public class ThirdPersonShooterGame : Game
    {
        readonly GraphicsDeviceManager _graphics;
        readonly string _shotPath;
        LevelScene _scene;
        LevelRenderer _renderer;
        Vector3 _camPos; float _yaw, _pitch;
        int _frame; bool _noPlayer;
        Vector3? _camOverride; Vector2 _camYawPitchDeg;
        MouseState _prevMouse;
        // play mode (default): fixed 60 Hz player + third-person camera; `--fly` / `--cam` keep the free camera
        bool _fly; string _simScript; Simulation _sim; InputMap _input; float _acc; bool _jumpLatched;
        enum GameState { Menu, Loading, Playing }
        GameState _state = GameState.Playing; bool _menuMode; string _screenArg, _uiScript;
        UiMessageBox _box; UiScreen _under; bool _prevStart; // _box: pause overlay while Playing; _under: screen drawn beneath a modal in the menu
        UiRenderer _ui; UiInputReader _uiIn; UiInput _uin; UiScreen _screen; MainMenuScreen _menu; MenuBackdrop _backdrop; Texture2D _logo;
        string _contentRoot; LevelColliders _colliders;
        ShadowMap _shadow; int _spotCasters; int _benchN; readonly List<double> _benchMs = new(); int _benchWarm; static readonly bool _spotEnvOff = Environment.GetEnvironmentVariable("TPS_SPOTSHADOWS") == "0", _keyMode = Environment.GetEnvironmentVariable("TPS_SPOTSHADOWS") == "0" /* default: level spot maps (SpotShadows.cs, 512 tiles, 4 taps). TPS_SPOTSHADOWS=0 = old key-light ortho map only */; static readonly bool _shadowsEnvOff = Environment.GetEnvironmentVariable("TPS_SHADOWS") == "0";
        PostProcess _post; CombatEffects _fx; BlobShadow _blob; GroundProbe _ground; PlayerRenderer _player, _robotR; DebrisRenderer _debrisR; Texture2D _crosshair; string _poseClip; float _poseTime;

        public ThirdPersonShooterGame(string[] args = null)
        {
            if (args != null)
                foreach (var a0 in args) { if (a0 == "--noplayer") _noPlayer = true; if (a0 == "--fly") _fly = true; }
            // Default launch boots into the main menu. Every verification flag skips it (they all expect the play scene);
            // `--screen menu|play-list|settings` forces a menu screen (also with --shot).
            AudioManager.Configure(args);
            bool skipMenu = args != null && Array.Exists(args, a => a == "--sim" || a == "--bench" || a == "--cam" || a == "--fly" || a == "--noplayer" || a == "--pose" || a == "--shot");
            if (args != null) for (int i = 0; i < args.Length - 1; i++) { if (args[i] == "--screen") _screenArg = args[i + 1]; if (args[i] == "--ui") _uiScript = args[i + 1]; }
            _menuMode = _screenArg != null || !skipMenu;
            if (args != null)
                for (int i = 0; i < args.Length - 1; i++)
                    {
                        if (args[i] == "--shot") _shotPath = args[i + 1];
                        if (args[i] == "--bench") _benchN = int.Parse(args[i + 1]);   // frame-cost benchmark: N frames of the default play view, no vsync, window-size render
                        if (args[i] == "--sim") _simScript = args[i + 1];
                        if (args[i] == "--pose")
                        {
                            var f = args[i + 1].Split(',');
                            _poseClip = f[0]; _poseTime = f.Length > 1 ? float.Parse(f[1], System.Globalization.CultureInfo.InvariantCulture) : 0f;
                        }
                        if (args[i] == "--cam")
                        {
                            var f = args[i + 1].Split(',');
                            if (f.Length == 5)
                            {
                                var ci = System.Globalization.CultureInfo.InvariantCulture;
                                _fly = true;
                                _camOverride = new Vector3(float.Parse(f[0], ci), float.Parse(f[1], ci), float.Parse(f[2], ci));
                                _camYawPitchDeg = new Vector2(float.Parse(f[3], ci), float.Parse(f[4], ci));
                            }
                        }
                    }
            // Lazy boot (menu first, level loaded behind the loading screen) only for a normal launch and `--screen loading`; every flag mode loads everything up front.
            bool loadingShot = _screenArg != null && _screenArg.StartsWith("loading");
            _lazy = _menuMode && !(args != null && Array.IndexOf(args, "--eager") >= 0) && (loadingShot || (_shotPath == null && _simScript == null && !_fly && !_noPlayer && _poseClip == null && _screenArg == null));
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1280,
                PreferredBackBufferHeight = 720,
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferMultiSampling = true,
            };
            // saved user settings (defaults in --shot runs so screenshots do not depend on the machine)
            if (_benchN > 0) { IsFixedTimeStep = false; DisplaySettings.Configure(_graphics, new GameSettings { VSync = false }); }   // bench: defaults (1280x720), vsync off, uncapped
            else if (_shotPath == null) { GameSettings.LoadCurrent(); DisplaySettings.Configure(_graphics, GameSettings.Current); }
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Third Person Shooter";
        }

        // ---- loading: boot loads only what the menu needs; Singleplayer loads the rest behind the loading screen (menu.tscn Loading + DoneTimer 0.5 s) ----
        const float DoneHold = 0.5f;                       // menu.tscn UI/Loading/DoneTimer wait_time
        bool _lazy, _playLoaded, _startupLogged; LoadRunner _loadRunner; LoadingScreen _loadScreen; int _loadFrames; float _loadHold; double _loadSecs; float _fakeProgress = -1f;
        System.Diagnostics.Stopwatch _loadWatch; double _loadContentSecs;
        Dictionary<string, Func<LoadStep>> _stepImpl;

        /// <summary>Name -> step factory for every asset group in <see cref="LoadTable"/> (the tables decide which steps run, and when).</summary>
        void BuildStepImpl()
        {
            string root = _contentRoot;
            _stepImpl = new Dictionary<string, Func<LoadStep>>
            {
                ["fonts-ui"] = () => LoadStep.Once("fonts-ui", 0, () => { InputCapture.Attach(this); UiTheme.LoadFonts(Content); _ui = new UiRenderer(GraphicsDevice); _uiIn = new UiInputReader(this); }),
                ["logo"] = () => LoadStep.Once("logo", 0, () => _logo = Content.Load<Texture2D>("Textures/Menu/tps_demo")),
                ["player"] = () => LoadStep.Once("player", 0, () => _player = new PlayerRenderer(GraphicsDevice, Content, root)),
                ["floor"] = () => LoadStep.Once("floor", 0, () => _backdrop = new MenuBackdrop(GraphicsDevice, _player, Content.Load<Texture2D>("Textures/Menu/floor"), Content.Load<Effect>("Effects/ShadowDepth"), Content.Load<Effect>("Effects/MenuFloor"))),
                ["post"] = () => LoadStep.Once("post", 0, () => { _post = new PostProcess(GraphicsDevice, Content.Load<Effect>("Effects/PostTonemap"), Content.Load<Effect>("Effects/PostBloom")); _post.BloomOn = GameSettings.Current.Bloom; }),
                ["audio-menu"] = () => LoadStep.Once("audio-menu", 0, () => { AudioManager.InitMenu(Content, playback: (_shotPath == null && _simScript == null) || Environment.GetEnvironmentVariable("TPS_AUDIO_PLAY") is { Length: > 0 }); AudioManager.ApplySettings(GameSettings.Current); }), // --sim/--shot stay silent and device-free (env TPS_AUDIO_PLAY=1 forces real playback of a --sim run: smoke test of the OpenAL path)
                // play: the level scene is the big one (models + ~90 DXT textures), loaded one model (with its textures) per tick
                ["scene"] = () => { _scene = new LevelScene(); return LoadStep.Incremental("scene", 0, SceneSlices(root)); },
                ["renderer"] = () => LoadStep.Once("renderer", 0, () => { _renderer = new LevelRenderer(GraphicsDevice, Content.Load<Effect>("Effects/PbrLit")); _renderer.Lighting.Load(root); var sdfx = Content.Load<Effect>("Effects/ShadowDepth"); _shadow = new ShadowMap(GraphicsDevice, sdfx); if (!_spotEnvOff) _renderer.Lighting.CreateSpotShadows(GraphicsDevice, sdfx); }),
                ["colliders"] = () => LoadStep.Once("colliders", 0, () => _colliders = LevelColliders.Load(root)),
                ["ground"] = () => LoadStep.Once("ground", 0, () => _ground = new GroundProbe(root)),
                ["robot"] = () => LoadStep.Once("robot", 0, () => _robotR = new PlayerRenderer(GraphicsDevice, Content, root, "red_robot")),
                ["debris"] = () => LoadStep.Once("debris", 0, () => _debrisR = new DebrisRenderer(Content, root)),
                ["blob"] = () => LoadStep.Once("blob", 0, () => _blob = new BlobShadow(GraphicsDevice)),
                ["fx"] = () => LoadStep.Once("fx", 0, () => _fx = CombatEffects.Load(GraphicsDevice, Content)),
                ["hud"] = () => LoadStep.Once("hud", 0, () => _crosshair = Content.Load<Texture2D>("Textures/Hud/crosshair")),   // player.tscn Crosshair TextureRect
                ["audio-level"] = () => LoadStep.Once("audio-level", 0, () => AudioManager.InitLevel(Content)),
            };
        }

        IEnumerable<float> SceneSlices(string root)
        {
            foreach (var f in _scene.LoadSteps(Content, root)) yield return Math.Min(f, 0.999f);
            OnSceneLoaded(); yield return 1f;
        }

        List<LoadStep> MakeSteps((string Name, float Weight)[] table)
        {
            var l = new List<LoadStep>();
            foreach (var (name, w) in table) { var st = _stepImpl[name](); l.Add(new LoadStep(name, w, st.Tick)); }
            return l;
        }

        /// <summary>Runs a whole plan synchronously (boot, and the flag modes that load everything up front), logging each step's time.</summary>
        void RunNow((string Name, float Weight)[] table, string tag)
        {
            var runner = new LoadRunner(MakeSteps(table)); var sw = System.Diagnostics.Stopwatch.StartNew(); double last = 0;
            while (!runner.Done) { string done = runner.Tick(); if (done != null) { Console.WriteLine($"[load] {tag} {done} {(sw.Elapsed.TotalMilliseconds - last):F0} ms"); last = sw.Elapsed.TotalMilliseconds; } }
        }

        /// <summary>Spawn-dependent state, once the level scene is in: model start pose, the free camera's start.</summary>
        void OnSceneLoaded()
        {
            _player.World = _scene.PlayerSpawn; // glb front +Z = spawn +Z = the camera's view direction (see Simulation)
            if (_poseClip != null) _player.Anim.SetPose(_poseClip, _poseTime); else { _player.Anim.Play("Idle"); _player.Anim.Evaluate(); }
            // free camera starts at the first player spawn, 2 m up and 3 m behind, looking along its forward
            Matrix sp = _scene.PlayerSpawn;
            Vector3 fwd = sp.Backward; fwd.Y = 0; fwd.Normalize();
            _camPos = sp.Translation + Vector3.Up * 2f - fwd * 3f;
            _yaw = MathF.Atan2(-fwd.X, -fwd.Z);
            _pitch = -0.15f;
            Console.WriteLine($"[spawn] pos={sp.Translation} fwd={sp.Forward}");
        }

        readonly RumbleService _rumble = new RumbleService(
            () => GamePad.GetCapabilities(PlayerIndex.One).IsConnected, () => GameSettings.Current.Rumble,
            (lo, hi) => { bool ok = GamePad.SetVibration(PlayerIndex.One, lo, hi); if (_rumbleTest) Console.WriteLine($"[rumble] t={DateTime.Now:HH:mm:ss.fff} SetVibration({lo:F2},{hi:F2}) -> {ok}"); });
        static readonly bool _rumbleTest = Environment.GetEnvironmentVariable("TPS_RUMBLETEST") == "1";
        static void LogRumbleSupport()
        {
            var c = GamePad.GetCapabilities(PlayerIndex.One);
            Console.WriteLine($"[rumble] pad connected={c.IsConnected} vibration supported={c.HasLeftVibrationMotor || c.HasRightVibrationMotor} (left={c.HasLeftVibrationMotor} right={c.HasRightVibrationMotor}) setting={GameSettings.Current.Rumble}" + (c.IsConnected ? "" : " - no pad, SetVibration untested on this machine"));
        }

        protected override void LoadContent()
        {
            _loadWatch = System.Diagnostics.Stopwatch.StartNew();
            LogRumbleSupport();
            string root = _contentRoot = Path.Combine(AppContext.BaseDirectory, Content.RootDirectory);
            BuildStepImpl();
            RunNow(LoadTable.Boot, "boot");
            if (!_lazy) { RunNow(LoadTable.Play, "play"); _playLoaded = true; }   // flag modes (and --eager) load everything before the first frame, as before the loading screen existed
            _loadScreen = new LoadingScreen(_logo);
            if (_menuMode) { if (_screenArg == "pause") { EnterMenu(null); StartPlay(); OpenPause(); if (_uiScript != null) RunUiScript(_uiScript); } else EnterMenu(_screenArg); } else StartPlay();
            if (_camOverride.HasValue) { _camPos = _camOverride.Value; _yaw = MathHelper.ToRadians(_camYawPitchDeg.X); _pitch = MathHelper.ToRadians(_camYawPitchDeg.Y); }
            _loadContentSecs = _loadWatch.Elapsed.TotalSeconds;
        }

        /// <summary>Singleplayer: everything already loaded -> straight into the game; otherwise show the loading screen and load the play steps one tick per frame.</summary>
        void StartPlay()
        {
            if (_fly) return;
            if (_playLoaded) { if (_lazy) Console.WriteLine("[loading] assets already loaded, skipping the loading screen"); StartPlayNow(); return; }
            _loadRunner = new LoadRunner(MakeSteps(LoadTable.Play)); _loadScreen.Progress = 0f; _loadFrames = 0; _loadHold = 0f; _loadSecs = 0; _fakeProgress = -1f;
            _state = GameState.Loading; _screen = _loadScreen; _under = null; InputCapture.Set(false); // mouse stays visible while loading; music keeps playing
            Console.WriteLine($"[loading] start: {_loadRunner.StepCount} steps");
        }

        void UpdateLoading(float dt)
        {
            if (_loadRunner == null) return;                                  // --screen loading: frozen at the fake progress
            if (_loadFrames++ == 0) return;                                   // draw the empty bar once before the first (blocking) step
            if (!_loadRunner.Done)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew(); string name = _loadRunner.CurrentName;
                string finished = _loadRunner.Tick(); _loadSecs += sw.Elapsed.TotalSeconds;
                _loadScreen.Progress = _loadRunner.Progress;
                if (finished != null) Console.WriteLine($"[loading] {finished} done, progress {_loadRunner.Progress * 100f:F0}% ({_loadRunner.StepIndex}/{_loadRunner.StepCount}) frame {_loadFrames} t={_loadSecs:F2}s");
                return;
            }
            _loadHold += dt;                                                  // DoneTimer: hold the full bar, then switch
            if (_loadHold >= DoneHold)
            {
                _playLoaded = true; _loadRunner = null; _loadScreen.Progress = 1f;
                Console.WriteLine($"[loading] complete: load work {_loadSecs:F2}s over {_loadFrames} frames + {DoneHold:F1}s hold, switching to Playing");
                StartPlayNow();
            }
        }

        void StartPlayNow()
        {
            AudioManager.PlayMusic(MusicTrack.Level);                                                   // level.tscn Music autoplay
            {
                _fx = CombatEffects.Load(GraphicsDevice, Content); // fresh effect list: the sim clock restarts at 0
                string root = _contentRoot; _colliders ??= LevelColliders.Load(root); var lc = _colliders;
                Simulation.Model = _player.Model; Simulation.RobotModel = _robotR.Model;
                _sim = new Simulation(lc);
                _sim.Shake.TraumaAdded += _rumble.OnTrauma;
                if (_poseClip == null) _player.Anim = _sim.Player.Animator.Pose; // animator drives the rendered pose
                _input = new InputMap();
                _fx.GroundY = (x, z, my) => _ground?.GroundY(x, z, my);
                _sim.Shot += (m, d) => _fx.Muzzle(m, d, _sim.Time);                       // muzzle flash at ShootFrom (player.gd shoot())
                _sim.Bullets.Impact += (p, n, t) => { _fx.Hit(p, n, _sim.Time); if (_simScript != null) Console.WriteLine($"[impact] t={_sim.Time:F2} p={p} dist={(p - _sim.Player.MuzzlePosition()).Length():F2} m"); };            // bullet explode()
                _sim.Debris.SparkBurst += p => _fx.DetachSparks(p, _sim.Time); _sim.Debris.Puff += p => _fx.DebrisPuff(p, _sim.Time);   // part.gd destroy() puff, DetachSpark1/2
                _sim.RobotLaser += (o, e, hp) => _fx.LaserImpact(e, _sim.Time);                             // red_robot.gd shoot(): blast_scene (impact_effect.tscn) at col.position
                _sim.RobotExploded += p => _fx.Explosion(p, _sim.Time);                                // red_robot.gd hit(): death explosion
                if (_simScript == null) _sim.SpawnLevelRobots();                                            // play mode: a robot at every RobotSpawnpoints marker (level.gd:28-30), respawned 15 s after exploding
                if (_simScript != null)
                {
                    Simulation.LogRobot = true;
                    _sim.RobotLaser += (o, e, hp) => Console.WriteLine($"[robot] laser fired t={_sim.Time:F2} from={o} to={e} len={(e - o).Length():F1} hitPlayer={hp}");
                    if (_rumbleTest) _sim.Stepped += dt => { _rumble.Update(dt, true); System.Threading.Thread.Sleep((int)(dt * 1000)); }; // TPS_RUMBLETEST=1 --sim: realtime steps so a connected pad actually pulses and every SetVibration call is logged
                    _sim.RunScript(_simScript);
                    if (_rumbleTest) _rumble.Zero();
                    foreach (var dp in _sim.Debris.All) if (dp.Alive) Console.WriteLine($"[debris] type={dp.Type} pos={dp.Pos} v={dp.V.Length():F2} sleeping={dp.Sleeping} fade={dp.Fade:F2} touching={dp.Touching} elapsed={dp.Elapsed:F2}/{dp.Wait:F2}");
                    if (_sim.Robot != null) Console.WriteLine($"[robot] state={_sim.Robot.Mode} health={_sim.Robot.Health} dead={_sim.Robot.Dead} pos={_sim.Robot.Position} yaw={MathHelper.ToDegrees(_sim.Robot.Yaw):F1} onFloor={_sim.Robot.OnFloor} clip={_sim.Robot.Anim.DominantClip} shots={_sim.Robot.ShotsFired} laserHitsOnPlayer={_sim.LaserHitsOnPlayer}");
                    Console.WriteLine($"[sim] t={_sim.Time:F2}s fade={_sim.Player.FadeAlpha:F3} pos={_sim.Player.Position} yaw={MathHelper.ToDegrees(_sim.Player.FacingYaw):F1} camYaw={MathHelper.ToDegrees(_sim.Camera.Yaw):F1} onFloor={_sim.Player.IsOnFloor}");
                }
                else if (_shotPath == null) InputCapture.Set(true);
            }
            _acc = 0f; _jumpLatched = false; _state = GameState.Playing;
        }

        // ---- menu flow: Menu (main -> play list) -> Singleplayer = Playing; Settings returns to whoever opened it (parameter) ----
        void EnterMenu(string screenArg)
        {
            _state = GameState.Menu; InputCapture.Set(false);
            AudioManager.PlayMusic(MusicTrack.Menu);                                                    // menu.tscn Music autoplay
            _menu = new MainMenuScreen(_logo, singleplayer: StartPlay, settings: () => OpenSettings(() => _screen = _menu), quit: OpenQuit);
            _screen = _menu;
            if (screenArg == "play-list") _menu.Show(MainMenuScreen.Page.PlayList);
            else if (screenArg == "settings") OpenSettings(() => _screen = _menu);
            else if (screenArg == "quit") OpenQuit();
            else if (screenArg != null && screenArg.StartsWith("loading"))
            {
                float p = 0.6f; int c = screenArg.IndexOf(':'); if (c >= 0) float.TryParse(screenArg.Substring(c + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out p);
                _fakeProgress = Math.Clamp(p, 0f, 1f); _loadScreen.Progress = _fakeProgress; _loadRunner = null; _state = GameState.Loading; _screen = _loadScreen;
            }
            if (_uiScript != null && _screenArg != "pause") RunUiScript(_uiScript);
        }

        void OpenSettings(Action returnTo)
        {
            var editor = new SettingsEditor(GameSettings.Current, _shotPath != null ? Path.Combine(Path.GetTempPath(), "tps-shot-settings.json") : null);
            _screen = new SettingsScreen(_logo, editor, DisplaySettings.Desktop(), applied => { if (applied) ApplySettings(); returnTo(); });
        }

        void ApplySettings()
        {
            if (_shotPath == null) DisplaySettings.Apply(_graphics, GameSettings.Current);
            _post.BloomOn = GameSettings.Current.Bloom;
            AudioManager.ApplySettings(GameSettings.Current);
        }

        /// <summary>Main-menu QUIT: "Quit?" box over the live menu; the default (focused) button is Quit!, Esc/B = No.</summary>
        void OpenQuit()
        {
            _under = _menu;
            _screen = new UiMessageBox("Quit?", "No", "Quit!", MessageBoxResult.Right, r => { _screen = _menu; _under = null; if (r == MessageBoxResult.Right) Exit(); });
        }

        /// <summary>Esc / pad Start / B while playing: "Leave?" box over the frozen frame (the sim is not stepped while it is open), mouse released.</summary>
        void OpenPause()
        {
            InputCapture.Set(false);
            _box = new UiMessageBox("Leave?", "Stay", "Leave!", MessageBoxResult.Left, r => { _box = null; if (r == MessageBoxResult.Left) ResumePlay(); else ReturnToMenu(); });
        }

        void ResumePlay()
        {
            if (_simScript != null || _shotPath != null) return;
            InputCapture.Set(true);       // swallows the first mouse delta
            _input.Read(0f);              // prime key/pad edges so the confirming Space/A does not become a jump; also recentres the cursor
            _jumpLatched = false;
        }

        void ReturnToMenu()
        {
            _box = null; _under = null;
            InputCapture.Set(false); _menu.Reset(); _screen = _menu; _state = GameState.Menu;
            AudioManager.PlayMusic(MusicTrack.Menu);
        }

        /// <summary>`--ui "down,down,enter"`: feed keys to the current screen once (for screenshots).</summary>
        void RunUiScript(string script)
        {
            foreach (var k in script.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var i = new UiInput { Up = k == "up", Down = k == "down", Left = k == "left", Right = k == "right", Confirm = k == "enter", Back = k == "esc" };
                (_box ?? _screen).Update(i, 0.016f);
            }
        }

        Matrix Rot => Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0);

        protected override void Update(GameTime gameTime)
        {
            float udt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);
            var bb = GraphicsDevice.PresentationParameters;
            _uin = _uiIn.Poll(udt, UiRenderer.ScaleFor(bb.BackBufferWidth, bb.BackBufferHeight)); // polled every frame so edges (Esc!) are consumed across state changes
            AudioManager.Update(udt);
            _rumble.Update(udt, IsActive && _state == GameState.Playing && _box == null && _sim != null);
            if (_shotPath != null) return;
            var gp = GamePad.GetState(PlayerIndex.One); bool startEdge = IsActive && gp.Buttons.Start == ButtonState.Pressed && !_prevStart; _prevStart = gp.Buttons.Start == ButtonState.Pressed;
            if (_state == GameState.Loading) { _backdrop.Update(udt); UpdateLoading(udt); base.Update(gameTime); return; }
            if (_state == GameState.Menu)
            {
                _backdrop.Update(udt);
                if (_screen is UiMessageBox qb) qb.Layout(bb.BackBufferWidth, bb.BackBufferHeight);
                _screen.Update(_uin, udt); base.Update(gameTime); return;
            }
            if (_box != null) { _box.Layout(bb.BackBufferWidth, bb.BackBufferHeight); _box.Update(_uin, udt); base.Update(gameTime); return; } // paused: nothing below is stepped (sim, aim, shake, effects, animation)
            var ks = Keyboard.GetState(); var ms = Mouse.GetState();
            if (_uin.Back || startEdge) { if (_menuMode && !_fly) { OpenPause(); base.Update(gameTime); return; } else if (_uin.Back) Exit(); }
            if (_state == GameState.Menu) return;
            if (!_fly)
            {
                float fdt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);
                var pin = _input.Read(fdt, _sim.Aim.Aiming);
                _sim.AimHeld = pin.Aim; _sim.FireHeld = pin.Shoot;
                _sim.Camera.Rotate(pin.Look);
                _jumpLatched |= pin.Jump;
                _acc += fdt;
                while (_acc >= PlayerConstants.PhysicsStep)
                {
                    _sim.Step(pin.Motion, _jumpLatched);
                    _jumpLatched = false; _acc -= PlayerConstants.PhysicsStep;
                }
                base.Update(gameTime);
                return;
            }
            if (IsActive && ms.RightButton == ButtonState.Pressed)
            {
                _yaw -= (ms.X - _prevMouse.X) * 0.003f;
                _pitch = MathHelper.Clamp(_pitch - (ms.Y - _prevMouse.Y) * 0.003f, -1.5f, 1.5f);
            }
            _prevMouse = ms;
            if (_poseClip == null) _player.Anim.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float speed = (ks.IsKeyDown(Keys.LeftShift) ? 40f : 8f) * dt;
            var r = Rot; Vector3 mv = Vector3.Zero;
            if (ks.IsKeyDown(Keys.W)) mv += r.Forward;
            if (ks.IsKeyDown(Keys.S)) mv += r.Backward;
            if (ks.IsKeyDown(Keys.D)) mv += r.Right;
            if (ks.IsKeyDown(Keys.A)) mv += r.Left;
            if (ks.IsKeyDown(Keys.E)) mv += Vector3.Up;
            if (ks.IsKeyDown(Keys.Q)) mv += Vector3.Down;
            _camPos += mv * speed;
            base.Update(gameTime);
        }

        protected override void OnExiting(object sender, ExitingEventArgs args) { _rumble?.Zero(); AudioManager.Shutdown(); base.OnExiting(sender, args); }

        protected override void Draw(GameTime gameTime)
        {
            var gd = GraphicsDevice;
            long benchT0 = _benchN > 0 ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            if (_state != GameState.Playing) { DrawMenu(gameTime); return; }
            int W = _shotPath != null ? 1280 : gd.PresentationParameters.BackBufferWidth, H = _shotPath != null ? 720 : gd.PresentationParameters.BackBufferHeight;
            float aspect = W / (float)H;
            Matrix view, proj;
            if (_fly || _sim == null)
            {
                view = Matrix.CreateLookAt(_camPos, _camPos + Rot.Forward, Vector3.Up);
                proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(PlayerConstants.Fov), aspect, PlayerConstants.Near, PlayerConstants.Far);
            }
            else
            {
                float alpha = _shotPath != null ? 1f : _acc / PlayerConstants.PhysicsStep; // render interpolation between fixed ticks
                _sim.Camera.Update(_sim.Player.RenderPosition(alpha));
                _camPos = _sim.Camera.Position;
                view = _sim.Shake.Apply(_sim.Camera.View); proj = _sim.Camera.Projection(aspect);
                _player.World = _sim.Player.ModelWorld(alpha);
            }
            if (_shotPath != null && ++_frame < 3) { gd.Clear(Color.Black); return; }
            RenderTarget2D rt = null;
            if (_shotPath != null) rt = new RenderTarget2D(gd, W, H, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            // real shadows (Settings > SHADOWS; TPS_SHADOWS=0 forces off): one depth pass for the key light around the player, before the HDR pass; off = blob shadows
            bool shOn = _shadow != null && _shadow.Enabled && GameSettings.Current.Shadows && !_shadowsEnvOff;
            _renderer.Shadow = null;
            if (shOn)
            {
                Vector3 cf0 = new Vector3(-view.M13, 0f, -view.M33); cf0 = cf0.LengthSquared() > 1e-6f ? Vector3.Normalize(cf0) : Vector3.UnitZ;
                Vector3 sc = (_sim != null && !_fly && !_noPlayer) ? _player.World.Translation + Vector3.Up + cf0 * 8f : _camPos + new Vector3(-view.M13, -view.M23, -view.M33) * 16f;
                void Casters(ShadowMap sm)
                {
                    sm.SetTechnique(true);
                    if (!_noPlayer && _player != null) _player.DrawShadow(gd, sm);
                    if (_sim != null && !_fly)
                        foreach (var rb in _sim.Robots) { if (rb.Dead) continue; _robotR.Anim = rb.Anim.Pose; _robotR.World = rb.ModelWorld(_shotPath != null ? 1f : _acc / PlayerConstants.PhysicsStep); if (sm.Contains(new BoundingSphere(_robotR.World.Translation + Vector3.Up, 3f))) _robotR.DrawShadow(gd, sm); }
                }
                if (_keyMode)
                {
                    _shadow.SetupFrame(_renderer.Lighting.KeyDir, sc);
                    _shadow.Begin(gd, false);
                    _renderer.DrawShadow(gd, _scene, _shadow);
                    Casters(_shadow);
                    _shadow.End(gd);
                    _renderer.Shadow = _shadow;
                }
                var spots = _renderer.Lighting.Spots;
                if (spots != null && !_keyMode)
                {
                    float sdt = _shotPath != null ? -1f : (float)gameTime.ElapsedGameTime.TotalSeconds;
                    spots.Update((_sim != null && !_fly && !_noPlayer) ? _player.World.Translation : _camPos, sdt);
                    spots.Enabled = spots.ActiveCount > 0;
                    if (spots.Enabled)
                    {
                        var am = spots.Atlas; am.Begin(gd, false); am.Casters = 0;
                        foreach (var e in spots.Active)
                        {
                            am.BeginTile(gd, e.Tile, SpotShadows.Tile, e.View, e.Proj, SpotShadows.Far, e.Frustum);
                            am.SetTechnique(false); _renderer.DrawShadow(gd, _scene, am);
                            Casters(am);
                        }
                        am.End(gd); _spotCasters = am.Casters;
                    }
                }
            }
            if (!shOn || _keyMode) { if (_renderer.Lighting.Spots != null) _renderer.Lighting.Spots.Enabled = false; }
            _post.Begin(W, H);
            _renderer.Lighting.HdrScale = _post.HdrScale;
            gd.Clear(new Color(0.02f, 0.015f, 0.01f));
            float lvlT = _sim != null ? _sim.Time + (_shotPath != null ? 0f : _acc) : (float.TryParse(Environment.GetEnvironmentVariable("TPS_LEVEL_TIME"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lt) ? lt : 0f); _scene.UpdateDynamics(lvlT); _fx?.SetForkliftLights(_renderer.Lighting, _scene, lvlT); // mawaru on the 60 Hz sim clock (frozen while paused); free camera / --noplayer shots: t=0 unless --sim ran
            if (_sim != null && !_fly && _fx != null) _fx.CollectLights(_renderer.Lighting, _sim.Time + (_shotPath != null ? 0f : _acc), _sim.Bullets.All, _shotPath != null ? 1f : _acc / PlayerConstants.PhysicsStep);   // bullet OmniLight3D into the nearest-lights set
            _renderer.Draw(gd, _scene, view, proj, _camPos);
            if (!_noPlayer) { _player.Draw(gd, _renderer, view, proj, _camPos);
            var pp = _player.World.Translation; float gy = _ground.GroundY(pp.X, pp.Z, pp.Y + 0.5f) ?? pp.Y; if (pp.Y - gy < 0.25f) gy = pp.Y; /* grounded: visual floor sits ~0.1 m above the collider top, so snap to the feet */ if (_frame == 3) Console.WriteLine($"[blob] player y={pp.Y:F3} ground y={gy:F3}"); if (_renderer.Shadow == null) _blob.Draw(gd, view, proj, new Vector3(pp.X, gy, pp.Z)); }
            if (_sim != null && !_fly)
                foreach (var rb in _sim.Robots)
                {
                    if (rb.Dead) continue;
                    float ra = _shotPath != null ? 1f : _acc / PlayerConstants.PhysicsStep;
                    _robotR.Anim = rb.Anim.Pose; _robotR.World = rb.ModelWorld(ra);
                    _robotR.Draw(gd, _renderer, view, proj, _camPos);
                    var rp = rb.Position; if (_renderer.Shadow == null) _blob.Draw(gd, view, proj, new Vector3(rp.X, rp.Y, rp.Z));
                    if (rb.ShootPlaying) { rb.ComputeBeam(_sim.Player.Position, out var bo, out var be, out _); _fx.Beam(bo, be, rb.ShootClock); }          // RayMesh laser during the shoot clip
                }
            if (_sim != null && !_fly && float.TryParse(Environment.GetEnvironmentVariable("TPS_BEAMTEST"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var bclock))   // debug: a synthetic laser across the view at the given shoot-clip time (no robot needed)
            { Vector3 cf = Vector3.Normalize(new Vector3(-view.M13, -view.M23, -view.M33)), cr = new Vector3(view.M11, view.M21, view.M31); _fx.Beam(_camPos + cf * 9f + cr * 5f - Vector3.Up * 0.3f, _camPos + cf * 9f - cr * 7f - Vector3.Up * 0.3f, bclock); }
            if (_sim != null && !_fly && _debrisR != null) _debrisR.Draw(gd, _renderer, _sim.Debris, _shotPath != null ? 1f : _acc / PlayerConstants.PhysicsStep, view, proj, _camPos, _fx);   // explosion parts (rigid bodies) + fade glow
            if (_sim != null && !_fly)
            {
                float a2 = _shotPath != null ? 1f : _acc / PlayerConstants.PhysicsStep;
                _fx.Draw(gd, view, proj, _sim.Time + (_shotPath != null ? 0f : _acc), _sim.Bullets.All, a2, _post.HdrScale); // additive glows inside the HDR pass: bloom picks them up
            }
            _fx?.DrawForklifts(gd, _scene, view, proj, _camPos, lvlT, _post.HdrScale);   // forklift exhaust (additive, HDR pass)
            _post.Resolve(rt);
            if (_sim != null && !_fly)   // HUD after the HDR resolve (not tonemapped/bloomed): fall-fade ColorRect, then the crosshair; the pause box draws on top
            {
                float fade = _sim.Player.FadeAlpha, ch = _box != null ? 0f : _sim.Aim.CrosshairAlpha;
                if (fade > 0.001f || ch > 0.001f)
                {
                    gd.SetRenderTarget(rt); gd.DepthStencilState = DepthStencilState.None; gd.BlendState = BlendState.AlphaBlend;
                    float us = UiRenderer.ScaleFor(W, H); float vw = W / us, vh = H / us;
                    _ui.Begin(W, H);
                    if (fade > 0.001f) _ui.Rect(0, 0, vw, vh, Color.Black * fade);                                                  // player_input.gd:101-104 ColorRect (black) modulate.a
                    if (ch > 0.001f && _crosshair != null) _ui.Image(_crosshair, vw * 0.5f - 8f, vh * 0.5f - 8f, 16f, 16f, Color.White * ch); // player.tscn Crosshair: 16x16 centred TextureRect, self_modulate alpha
                    _ui.End();
                }
            }
            if (_box != null) // pause overlay over the frozen frame
            {
                gd.SetRenderTarget(rt); gd.DepthStencilState = DepthStencilState.None; gd.BlendState = BlendState.AlphaBlend;
                _box.Layout(W, H); _ui.Begin(W, H); _box.Draw(_ui); _ui.End();
            }
            if (_shotPath != null)
            {
                gd.SetRenderTarget(null);
                Console.WriteLine($"[shot] drawCalls={_renderer.DrawCalls} triangles={_renderer.Triangles} instances={_scene.Items.Count}");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_shotPath)));
                using (var fs = File.Create(_shotPath)) rt.SaveAsPng(fs, W, H);
                Exit();
            }
            else if ((++_frame % 120) == 0 && _benchN == 0) Console.WriteLine($"[draw] calls={_renderer.DrawCalls} tris={_renderer.Triangles}");
            if (_benchN > 0)
            {
                var px = new Color[1]; gd.GetBackBufferData(new Rectangle(0, 0, 1, 1), px, 0, 1);   // glReadPixels = full pipeline sync, so the time below is CPU + GPU for this frame
                double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - benchT0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                if (++_benchWarm > 40) _benchMs.Add(ms);
                if (_benchMs.Count >= _benchN)
                {
                    _benchMs.Sort(); double avg = 0; foreach (var m in _benchMs) avg += m; avg /= _benchMs.Count;
                    Console.WriteLine($"[bench] shadows={(_renderer.Shadow != null || _renderer.AnyShadow ? "ON" : "OFF")} spotMaps={(_renderer.Lighting.Spots?.ActiveCount ?? 0)} spotCasterDraws={_spotCasters} frames={_benchMs.Count} avg={avg:F2} ms p50={_benchMs[_benchMs.Count / 2]:F2} p95={_benchMs[(int)(_benchMs.Count * 0.95)]:F2} drawCalls={_renderer.DrawCalls} shadowCasterDraws={(_shadow?.Casters ?? 0)} size={W}x{H}");
                    Exit();
                }
            }
            base.Draw(gameTime);
        }

        void DrawMenu(GameTime gameTime)
        {
            var gd = GraphicsDevice;
            if (_shotPath != null && ++_frame < 3) { gd.Clear(Color.Black); return; }
            int W = _shotPath != null ? 1280 : gd.PresentationParameters.BackBufferWidth, H = _shotPath != null ? 720 : gd.PresentationParameters.BackBufferHeight;
            RenderTarget2D rt = null;
            if (_shotPath != null) rt = new RenderTarget2D(gd, W, H, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _backdrop.Draw(gd, _post, _player, W, H, rt);
            gd.SetRenderTarget(rt);
            gd.DepthStencilState = DepthStencilState.None; gd.BlendState = BlendState.AlphaBlend;
            _ui.Begin(W, H);
            if (_screen is UiMessageBox mb) mb.Layout(W, H);
            _loadScreen.Layout(W, H);
            _under?.Draw(_ui); _screen.Draw(_ui); _ui.End();
            if (!_startupLogged && _shotPath == null)
            {
                _startupLogged = true;
                double sinceStart = (DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalSeconds;
                Console.WriteLine($"[startup] first menu frame at {sinceStart:F2} s (LoadContent {_loadContentSecs:F2} s, lazy={_lazy})");
            }
            if (_shotPath != null)
            {
                gd.SetRenderTarget(null);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_shotPath)));
                using (var fs = File.Create(_shotPath)) rt.SaveAsPng(fs, W, H);
                Console.WriteLine($"[shot] menu screen={_screenArg ?? "menu"} -> {_shotPath}");
                Exit();
            }
            base.Draw(gameTime);
        }
    }
}
