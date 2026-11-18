using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using ThirdPersonShooter.Physics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Headless fixed-step driver: player + camera with synthetic input. Script format (items separated by ';'):
    ///   w:2.0  a:1  s:1  d:1  w+d:1.5   hold keys for N seconds     look:30,-5   turn camera instantly (yaw right deg, pitch down deg)
    ///   space  jump (optionally space:0.5 = press then advance 0.5 s)         wait:1.0   advance with no input   aim / noaim   hold / release the aim button   fire[:n]   hold fire until n bullets spawned   robot[:k|ahead,D|x,y,z,yaw]   place the debug red robot   robots   spawn all level robots (respawning)
    /// </summary>
    public sealed class Simulation
    {
        public readonly PlayerController Player;
        public readonly ThirdPersonCamera Camera;
        public readonly LevelColliders Level;
        public float Time;
        /// <summary>Held buttons (aim = RMB / LT, fire = LMB / RT); the toggle/hold logic lives in <see cref="Aim"/>.</summary>
        public bool AimHeld, FireHeld;
        public readonly AimController Aim = new AimController();
        public readonly CameraShake Shake = new CameraShake();
        public readonly ProjectileManager Bullets;
        /// <summary>Explosion debris (part.gd RigidBody3D parts), stepped with the sim tick.</summary>
        public readonly DebrisSystem Debris;
        float _cooldown;
        /// <summary>Raised when a shot is fired: (muzzle position, bullet direction).</summary>
        public event Action<Vector3, Vector3> Shot;
        /// <summary>Skeleton + clips used to build each player's animator (root motion drives movement). Set once by the game / self-test.</summary>
        public static SkinnedModel Model;
        /// <summary>Red robot skeleton + clips (Models/red_robot), set once by the game / self-test.</summary>
        public static SkinnedModel RobotModel;
        /// <summary>Every robot of this simulation: empty by default (self-tests, `--sim` runs), the debug robot after <see cref="SpawnRobot"/> / the `robot` script item, one per level marker after <see cref="SpawnLevelRobots"/>.</summary>
        public readonly System.Collections.Generic.List<RedRobot> Robots = new System.Collections.Generic.List<RedRobot>();
        /// <summary>The first robot (the debug robot of `--sim robot...`), null when there is none.</summary>
        public RedRobot Robot => Robots.Count > 0 ? Robots[0] : null;
        /// <summary>level.gd:104 _respawn_robot: create_timer(15.0) after the `exploded` signal, then spawn_robot at the same marker.</summary>
        public const float RobotRespawnWait = 15f;
        readonly System.Collections.Generic.List<int> _marker = new System.Collections.Generic.List<int>();       // per robot: spawn marker index, -1 = debug robot (never respawns)
        readonly System.Collections.Generic.List<float> _respawnAt = new System.Collections.Generic.List<float>(); // per robot: sim time to respawn at, < 0 = none pending
        int _emitters;
        /// <summary>Raised on the tick the robot's laser fires: (muzzle, end, hitPlayer).</summary>
        public event Action<Vector3, Vector3, bool> RobotLaser;
        public event Action<Vector3> RobotExploded;
        readonly System.Collections.Generic.List<float> _pendingTrauma = new System.Collections.Generic.List<float>();
        public int LaserHitsOnPlayer;
        public static bool LogRobot;

        public Simulation(LevelColliders level, Vector3? spawn = null, float? yaw = null)
        {
            Level = level;
            Matrix sp = level.PlayerSpawn;
            // Godot's SpringArm3D carries a 180 deg turn (player.tscn), so the camera looks along the spawn's +Z (Backward), not -Z.
            Vector3 fwd = sp.Backward; fwd.Y = 0; fwd.Normalize();
            float y = yaw ?? MathF.Atan2(-fwd.X, -fwd.Z);
            Player = new PlayerController(level.PlayerWorld, spawn ?? sp.Translation, y, Model != null ? new PlayerAnimator(Model) : null);
            Camera = new ThirdPersonCamera(level.LevelWorld, y, 0f);
            Bullets = new ProjectileManager(level.LevelWorld);
            Debris = new DebrisSystem(level.LevelWorld);
            // audio hooks (null-safe static service): player.gd jump()/land(), foot touch-downs, bullet explode
            Player.OnJump += () => AudioManager.Play(AudioEvent.PlayerJump);
            Player.OnLand += () => AudioManager.Play(AudioEvent.PlayerLand);
            if (Player.Animator != null) Player.Animator.Footstep += () => AudioManager.Play(AudioEvent.PlayerStep);
            Bullets.Impact += (p, n, t) => AudioManager.Play(AudioEvent.BulletExplode, p);
        }

        /// <summary>One fresh RedRobot on the floor below (x, z) (level.gd spawn_robot: the marker transform; the robot is a CharacterBody3D that settles on the floor), events wired. Not yet in <see cref="Robots"/>.</summary>
        RedRobot MakeRobot(Vector3 pos, float yaw)
        {
            if (Level.LevelWorld.Raycast(pos + Vector3.Up * 2f, Vector3.Down, 10f, out var h)) pos = h.Point + Vector3.Up * 0.02f;
            var r = new RedRobot(Level.LevelWorld, RobotModel, pos, yaw, Level.PlayerWorld); int emitter = ++_emitters;   // emitter = the robot's own Godot audio nodes
            r.Audio += (e, p, sc) => AudioManager.Play(e, p, sc, emitter);
            r.LaserFired += (o, e, hp) => { RobotLaser?.Invoke(o, e, hp); if (hp) _pendingTrauma.Add(Time + RedRobot.LaserTraumaDelay); };
            r.Exploded += p => { Debris.Explode(r.Position, r.Yaw, unchecked((int)(Math.Round(Time * 60f) * 7919 + r.Position.X * 131 + r.Position.Z * 337))); int i = Robots.IndexOf(r); if (i >= 0 && _marker[i] >= 0) _respawnAt[i] = Time + RobotRespawnWait; RobotExploded?.Invoke(p); };   // level.gd:99 exploded -> _respawn_robot
            if (LogRobot) { r.StateChanged += (a, b) => Console.WriteLine($"[robot] t={Time:F2} state {a} -> {b}"); r.Damaged += h2 => Console.WriteLine($"[robot] t={Time:F2} hit, health {h2}"); }
            Bullets.SweepTargets = SweepRobots; r.Peers = Robots; r.Debris = Debris;
            return r;
        }

        /// <summary>Nearest live robot along the bullet's sweep (the level hit, if closer, is resolved by the caller).</summary>
        bool SweepRobots(Vector3 from, Vector3 dir, float len, float r, int owner, out float d, out IDamageable t)
        {
            d = float.MaxValue; t = null;
            foreach (var rb in Robots) if (rb.Sweep(from, dir, len, r, owner, out float rd) && rd < d) { d = rd; t = rb; }
            return t != null;
        }

        /// <summary>Debug placement: the one robot of a `--sim robot...` item or a self-test (replaces any robots, no respawn).</summary>
        public RedRobot SpawnRobot(Vector3 pos, float yaw)
        {
            Robots.Clear(); _marker.Clear(); _respawnAt.Clear();
            var r = MakeRobot(pos, yaw); Robots.Add(r); _marker.Add(-1); _respawnAt.Add(-1f);
            return r;
        }

        /// <summary>Debug / self-test: one more robot beside the existing ones (no respawn).</summary>
        public RedRobot AddRobot(Vector3 pos, float yaw)
        {
            var r = MakeRobot(pos, yaw); Robots.Add(r); _marker.Add(-1); _respawnAt.Add(-1f);
            return r;
        }

        /// <summary>level.gd:28-30: a robot at EVERY robot marker, each respawned <see cref="RobotRespawnWait"/> s after it explodes.</summary>
        public void SpawnLevelRobots()
        {
            Robots.Clear(); _marker.Clear(); _respawnAt.Clear();
            for (int k = 0; k < Level.RobotSpawns.Length; k++) { Robots.Add(MakeMarkerRobot(k)); _marker.Add(k); _respawnAt.Add(-1f); }
        }

        RedRobot MakeMarkerRobot(int k) { Matrix m = Level.RobotSpawns[k]; return MakeRobot(m.Translation, MathF.Atan2(m.Backward.X, m.Backward.Z)); }

        /// <summary>Robot spawn marker k (RobotSpawnpoints/Marker3D{k+1}): position + yaw of its +Z front.</summary>
        public RedRobot SpawnRobotAtMarker(int k)
        {
            Matrix m = Level.RobotSpawns[k]; var r = SpawnRobot(m.Translation, MathF.Atan2(m.Backward.X, m.Backward.Z)); return r;
        }

        /// <summary>Raised after each fixed step (dt); used by the rumble --sim hook.</summary>
        public event Action<float> Stepped;

        public void Step(Vector2 motion, bool jump = false)
        {
            const float dt = PlayerConstants.PhysicsStep;
            if (AudioManager.Playing || AudioManager.Log) { Camera.Update(Player.Position); AudioManager.LogTime = Time; AudioManager.SetListener(Camera.Position, Vector3.TransformNormal(Vector3.Right, Camera.CameraRotation)); } // listener = the camera
            Aim.Update(dt, AimHeld);                       // player_input.gd aim toggle + camera "shoot"/"far" tween
            Player.Aiming = Aim.Aiming; Camera.ArmOffset = Aim.ArmOffset;
            Player.Tick(dt, motion, Camera.Yaw, jump, Camera.Pitch);
            Shake.Update(dt);
            _cooldown = Math.Max(0f, _cooldown - dt);
            if (FireHeld && Player.Aiming && !Player.IsOnAir && _cooldown <= 1e-4f) Fire();   // player.gd:133 (only in the grounded aim branch)
            foreach (var rb in Robots) rb.Tick(dt, Player.Position);
            Bullets.Update(dt);
            // debris (layer 3) vs the player (layers 2+3) and live robots (layer 3): kinematic obstacles for the parts, sphere obstacles for the player's capsule queries
            Debris.Obstacles.Clear(); Debris.Obstacles.Add((Player.Position + Vector3.Up * 0.5f, Player.Position + Vector3.Up * 1.5f, 0.5f));
            foreach (var rb in Robots) if (!rb.Dead) Debris.Obstacles.Add((rb.SphereCenter, rb.SphereCenter, RedRobot.SphereRadius));
            Debris.Update(dt);
            Level.PlayerWorld.ClearTransientSpheres(); if (Debris.Count > 0) Debris.AppendSpheres(Level.PlayerWorld.AddTransientSphere);
            Time += dt;
            for (int i = 0; i < Robots.Count; i++) if (_respawnAt[i] >= 0f && Time >= _respawnAt[i]) { Robots[i] = MakeMarkerRobot(_marker[i]); _respawnAt[i] = -1f; } // fresh RedRobot at the marker: full health, Idle
            for (int i = _pendingTrauma.Count - 1; i >= 0; i--) if (Time >= _pendingTrauma[i]) { Shake.AddTrauma(RedRobot.LaserTrauma); LaserHitsOnPlayer++; _pendingTrauma.RemoveAt(i); } // red_robot.gd:134-135
            Stepped?.Invoke(dt);
        }

        /// <summary>player.gd:133-143 + player_input.gd:84-95: aim ray from the camera through the screen centre (1000 m vs the level), bullet from ShootFrom to that point.</summary>
        void Fire()
        {
            Camera.Update(Player.Position);
            Vector3 from = Camera.Position, d = Camera.ViewDir;
            Vector3 target = Level.LevelWorld.Raycast(from, d, PlayerConstants.AimRayLength, out var hit) ? hit.Point : from + d * PlayerConstants.AimRayLength;
            Vector3 muzzle = Player.MuzzlePosition(), dir = Vector3.Normalize(target - muzzle);
            Bullets.Spawn(muzzle, dir, 0);
            _cooldown = PlayerConstants.FireCooldown;                       // player.gd:199 fire_cooldown.start()
            Shake.AddTrauma(PlayerConstants.ShootTrauma);                   // player.gd:201
            AudioManager.Play(AudioEvent.PlayerShoot);                      // player.gd:200
            Shot?.Invoke(muzzle, dir);
        }

        public void Run(float seconds, Vector2 motion)
        {
            int n = (int)Math.Round(seconds / PlayerConstants.PhysicsStep);
            for (int i = 0; i < n; i++) Step(motion);
        }

        public void RunScript(string script)
        {
            var ci = CultureInfo.InvariantCulture;
            foreach (var raw in script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                string name = raw, arg = null; int c = raw.IndexOf(':');
                if (c >= 0) { name = raw.Substring(0, c); arg = raw.Substring(c + 1); }
                name = name.ToLowerInvariant();
                if (name == "look")
                {
                    var f = arg.Split(',');
                    Camera.Rotate(new Vector2(MathHelper.ToRadians(float.Parse(f[0], ci)), f.Length > 1 ? MathHelper.ToRadians(float.Parse(f[1], ci)) : 0f));
                }
                else if (name == "tp")                           // tp:x,y,z,yawDeg = teleport the player and set the camera yaw (XNA RotationY of the view direction, degrees)
                {
                    var f = arg.Split(','); Player.Teleport(new Vector3(float.Parse(f[0], ci), float.Parse(f[1], ci), float.Parse(f[2], ci)));
                    if (f.Length > 3) Camera.Yaw = MathHelper.ToRadians(float.Parse(f[3], ci));
                }
                else if (name == "robot")                        // robot | robot:k (spawn marker 1-4) | robot:ahead,D (D m in front of the camera, facing the player) | robot:x,y,z,yawDeg
                {
                    if (arg == null) SpawnRobotAtMarker(0);
                    else
                    {
                        var f = arg.Split(',');
                        if (f[0] == "ahead")
                        {
                            float d = float.Parse(f[1], ci); Vector3 fwd = new Vector3(-MathF.Sin(Camera.Yaw), 0, -MathF.Cos(Camera.Yaw)); Vector3 p = Player.Position + fwd * d;
                            SpawnRobot(p, MathF.Atan2(-fwd.X, -fwd.Z) + (f.Length > 2 ? MathHelper.ToRadians(float.Parse(f[2], ci)) : 0f));   // optional 3rd value: extra yaw in degrees (debug: turn the robot sideways)
                        }
                        else if (f.Length == 1) SpawnRobotAtMarker(int.Parse(f[0], ci) - 1);
                        else SpawnRobot(new Vector3(float.Parse(f[0], ci), float.Parse(f[1], ci), float.Parse(f[2], ci)), MathHelper.ToRadians(float.Parse(f[3], ci)));
                    }
                }
                else if (name == "addrobot")                     // addrobot:ahead,D,lateral (another robot D m ahead of the camera, shifted lateral m to the right, facing the player) - debug, keeps the existing robots
                {
                    var f = arg.Split(','); float d = float.Parse(f[1], ci), lat = f.Length > 2 ? float.Parse(f[2], ci) : 0f; Vector3 fwd = new Vector3(-MathF.Sin(Camera.Yaw), 0, -MathF.Cos(Camera.Yaw));
                    AddRobot(Player.Position + fwd * d + new Vector3(-fwd.Z, 0, fwd.X) * -lat, MathF.Atan2(-fwd.X, -fwd.Z));
                }
                else if (name == "rshoot") { if (Robots.Count > 0) Robots[Robots.Count - 1].DebugForceShoot(float.Parse(arg, ci)); }   // rshoot:clock = last robot mid shoot clip (debug)
                else if (name == "robots") SpawnLevelRobots();   // a robot at every level marker (as the real game), respawning 15 s after each explodes
                else if (name == "aim") AimHeld = true;          // hold the aim button (stays held; use noaim to release)
                else if (name == "noaim") AimHeld = false;
                else if (name == "fire")                         // fire[:n] = hold the fire button until n bullets were spawned (default 1); aim first
                {
                    int n = arg == null ? 1 : int.Parse(arg, ci), goal = Bullets.TotalSpawned + n, guard = 0;
                    FireHeld = true;
                    while (Bullets.TotalSpawned < goal && guard++ < 1200) Step(Vector2.Zero);
                    FireHeld = false;
                }
                else if (name == "space") { Step(Vector2.Zero, true); if (arg != null) Run(float.Parse(arg, ci), Vector2.Zero); }
                else if (name == "wait") Run(float.Parse(arg, ci), Vector2.Zero);
                else
                {
                    Vector2 m = Vector2.Zero;
                    foreach (var k in name.Split('+'))
                        switch (k) { case "w": m.Y -= 1; break; case "s": m.Y += 1; break; case "a": m.X -= 1; break; case "d": m.X += 1; break;
                            default: throw new ArgumentException("bad sim item: " + raw); }
                    Run(float.Parse(arg, ci), m);
                }
            }
        }
    }
}
