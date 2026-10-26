using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Enemies;
using Shooter.Gameplay.Weapons;
using Shooter.Physics;
using Shooter.Rendering;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the Turret behaviour, milestone 4c-3c (run as part of --test-physics). Numbers come from the scene data.</summary>
    public static class EnemyTurretSelfTest
    {
        private const float Dt = 1f / 60f;
        private static string F(float v) { return PhysicsSelfTest.F(v); }
        private static string V(Vector3 v) { return PhysicsSelfTest.V(v); }

        /// <summary>The real animated rig of the Turret, with a log of what the AI did to it.</summary>
        private class RecordingView : IEnemyView
        {
            private readonly EnemyRig _inner;
            public int Triggers, PivotOverrides;
            public bool IsActive;
            public readonly List<string> States = new List<string>();
            public string LastState;
            public RecordingView(EnemyRig inner) { _inner = inner; }
            public void SetFloat(string n, float v) { _inner.SetFloat(n, v); }
            public void SetBool(string n, bool v) { if (n == "IsActive") IsActive = v; _inner.SetBool(n, v); }
            public void SetTrigger(string n) { if (n == "OnDamaged") Triggers++; _inner.SetTrigger(n); }
            public void SetEyeColor(bool attack) { _inner.SetEyeColor(attack); }
            public void SetBodyEmission(Vector3 e) { _inner.SetBodyEmission(e); }
            public void Update(float dt, Matrix modelWorld)
            {
                _inner.Update(dt, modelWorld);
                string st = _inner.Animator.GetCurrentStateId(0);
                if (st != LastState) { States.Add(st); LastState = st; }
            }
            public bool HasBone(string b) { return _inner.HasBone(b); }
            public bool TryGetBoneWorld(string b, bool bind, out Matrix w) { return _inner.TryGetBoneWorld(b, bind, out w); }
            public void SetBoneWorldRotation(string b, Quaternion q) { PivotOverrides++; _inner.SetBoneWorldRotation(b, q); }
            public void SetLateUpdate(Action a) { _inner.SetLateUpdate(a); }
        }

        private class Rig
        {
            public CollisionWorld World = new CollisionWorld();
            public GameEvents Events = new GameEvents();
            public ActorsManager Actors = new ActorsManager();
            public EnemyManager Manager;
            public ProjectileManager Projectiles = new ProjectileManager();
            public Enemy Turret;
            public RecordingView View;
            public Actor Player = new Actor { Affiliation = 1, Name = "Player" };
            public Vector3 PlayerRoot = new Vector3(0f, -100f, 0f);
            public List<float> ShotTimes = new List<float>();
            public int PlayerHits;
            public float PlayerHitDamage;
            public float DetectedAt = -1f, LostAt = -1f;

            public Rig(LoadedScene loaded)
            {
                World.AddTriangle(new Vector3(-300, -10, -300), new Vector3(300, -10, -300), new Vector3(300, -10, 300));
                World.AddTriangle(new Vector3(-300, -10, -300), new Vector3(300, -10, 300), new Vector3(-300, -10, 300));
                World.Build();
                Manager = new EnemyManager(Events);
                EnemyDto dto = loaded.Data.Enemies.Find(e => e.Kind == "Turret");
                int index = loaded.Data.Enemies.IndexOf(dto);
                EnemyRig real = loaded.EnemyViews[index];
                // back to the idle pose before every test: no override, idle parameters, a few seconds of animation
                real.SetLateUpdate(null);
                real.ResetState();
                Matrix m = SceneLoader.ToMatrix(dto.M);
                for (int i = 0; i < 300; i++) real.Update(Dt, m);
                View = new RecordingView(real);
                WeaponDto p = loaded.Data.TurretProjectile;
                var ctx = new EnemyContext { World = World, Projectiles = Projectiles, ProjectileSettings = ProjectileSettings.From(p.Components["ProjectileStandard"], new Vector3(p.TipInRoot[0], p.TipInRoot[1], p.TipInRoot[2])), PlayerActor = () => Player, View = View };
                Turret = new Enemy(dto, index, Manager, Actors, ctx, null);
                Player.GetPosition = () => PlayerRoot;
                Player.GetAimPoint = () => PlayerRoot + new Vector3(0f, 1.09f, 0f);
                Player.GetCapsule = () => new Capsule { A = PlayerRoot + new Vector3(0, 0.35f, 0), B = PlayerRoot + new Vector3(0, 1.45f, 0), Radius = 0.35f };
                Actors.Register(Player);
                Projectiles.PlayerHit += (i, s) => { PlayerHits++; PlayerHitDamage = i.Damage; };
                Turret.Attacked += () => ShotTimes.Add(Manager.Time);
                Turret.DetectedTarget += () => { if (DetectedAt < 0f) DetectedAt = Manager.Time; };
                Turret.LostTarget += () => LostAt = Manager.Time;
            }

            public void Step()
            {
                Manager.Update(Dt);
                Turret.Update(Dt);
                Projectiles.Update(Dt);
            }

            public void Run(float seconds, Action each = null)
            {
                int n = (int)Math.Round(seconds / Dt);
                for (int i = 0; i < n; i++) { Step(); if (each != null) each(); }
            }

            public Vector3 Source { get { return Turret.DetectionSourcePosition; } }

            /// <summary>Player root so that the 3D distance from the detection source to the root is d along +X (root at the source's height).</summary>
            public void PlaceAtDistance(float d)
            {
                PlayerRoot = Source + new Vector3(d, 0f, 0f);
            }

            /// <summary>The Unity-space forward of a node (local +Z) converted to MonoGame.</summary>
            public Vector3 NodeForward(string node)
            {
                Matrix w = Turret.NodeWorld(node);
                Vector3 backward = Vector3.Normalize(new Vector3(w.M31, w.M32, w.M33));   // MonoGame local +Z axis in world space
                return -backward;   // Unity forward (+Z) is MonoGame -Z
            }
        }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("Turret behaviour (4c-3c)");
            SceneFile scene = loaded.Data;
            if (loaded.EnemyViews == null || loaded.EnemyViews.Length == 0 || loaded.EnemyViews[scene.Enemies.FindIndex(e => e.Kind == "Turret")] == null)
            {
                PhysicsSelfTest.Check("Turret animated view available", false, "no animator view for the Turret (Turret.animator.json missing?)");
                return;
            }

            // 1. data
            {
                var r = new Rig(loaded);
                DetectionModule d = r.Turret.Detection;
                EnemyTurret t = r.Turret.Turret;
                WeaponSettings w = r.Turret.Weapon.Settings;
                WeaponDto pd = scene.TurretProjectile;
                ProjectileSettings ps = ProjectileSettings.From(pd.Components["ProjectileStandard"], new Vector3(pd.TipInRoot[0], pd.TipInRoot[1], pd.TipInRoot[2]));
                bool ok = d.DetectionRange == 30f && d.AttackRange == 35f && d.KnownTargetTimeout == 4f && t.AimRotationSharpness == 5f && t.LookAtRotationSharpness == 2.5f &&
                          Math.Abs(t.DetectionFireDelay - 1.1f) < 1e-6f && t.AimingTransitionBlendTime == 1f && Math.Abs(w.DelayBetweenShots - 0.06f) < 1e-6f && w.MaxAmmo == 15 &&
                          Math.Abs(w.AmmoReloadDelay - 0.6f) < 1e-6f && float.IsPositiveInfinity(w.AmmoReloadRate) && w.ShootType == WeaponShootType.Automatic && ps.Damage == 3f && ps.Speed == 100f;
                PhysicsSelfTest.Check("Turret detection / aiming / weapon / projectile values read from the scene", ok,
                    "detection " + d.DetectionRange + "/" + d.AttackRange + "/" + d.KnownTargetTimeout + ", sharpness " + t.AimRotationSharpness + "/" + t.LookAtRotationSharpness + ", fire delay " + F(t.DetectionFireDelay) + ", blend " + t.AimingTransitionBlendTime +
                    ", weapon " + F(w.DelayBetweenShots) + " s, ammo " + w.MaxAmmo + ", reload rate " + w.AmmoReloadRate + " after " + F(w.AmmoReloadDelay) + " s, projectile damage " + ps.Damage + " speed " + ps.Speed);
                bool bones = r.View.HasBone("Turret_Pivot_C_Jnt") && r.View.HasBone("Turret_Head_C_Jnt") && r.View.HasBone("IrisBlade_C_Jnt");
                // the head bone's pose comes from the skeleton: Hitbox Top is a child of it. The baker's flat hierarchy put it 0.88 above the root (0.22 x 4); the skeleton says otherwise.
                r.View.TryGetBoneWorld("Turret_Head_C_Jnt", false, out Matrix head);
                r.View.TryGetBoneWorld("Turret_Head_C_Jnt", true, out Matrix headBind);
                RenderItem item = loaded.EnemyItems[r.Turret.Index].Value;
                Vector3[] corners = item.Mesh.Bounds.GetCorners();
                var pts = new Vector3[8];
                for (int i = 0; i < 8; i++) pts[i] = Vector3.Transform(corners[i], item.World);
                BoundingBox drawn = BoundingBox.CreateFromPoints(pts);
                Vector3 baked = SceneLoader.ToMatrix(scene.Enemies[r.Turret.Index].M).Translation + new Vector3(0f, 0.88f, 0f);
                PhysicsSelfTest.Check("skeleton bones found; the head bone (idle pose) lies inside the drawn mesh bounds", bones && drawn.Contains(head.Translation) == ContainmentType.Contains,
                    "head bone idle " + V(head.Translation) + ", bind pose " + V(headBind.Translation) + ", baked flat-hierarchy guess " + V(baked) + ", mesh bounds " + V(drawn.Min) + " .. " + V(drawn.Max) + ", weapon root idle " + V(r.Turret.NodeWorld("WeaponRoot").Translation));
                float scale = r.Turret.NodeWorld("Weapon_MachineGun").Right.Length();
                Vector3 pivot = r.Turret.Weapon.PivotWorld(r.Turret), muzzle = r.Turret.Weapon.MuzzleWorld(r.Turret);
                r.Turret.Weapon.Forward = Vector3.Normalize(new Vector3(0.3f, 0.2f, -0.9f));
                float along = Vector3.Dot(r.Turret.Weapon.MuzzleWorld(r.Turret) - pivot, r.Turret.Weapon.Forward);
                PhysicsSelfTest.Check("muzzle hangs 0.026 x world scale 4 behind the weapon pivot along its forward", Math.Abs(scale - 4f) < 1e-3f && Math.Abs(along - -0.104f) < 1e-3f, "weapon world scale " + F(scale) + ", muzzle offset along forward " + F(along));
            }

            // 2. detection range from the aim point (the WeaponRoot), idle until then
            {
                var r = new Rig(loaded);
                r.PlaceAtDistance(29.9f); r.Step();
                bool near = r.Turret.Detection.IsSeeingTarget && r.Turret.Turret.AiState == EnemyTurret.AIState.Attack && r.View.IsActive;
                var r2 = new Rig(loaded);
                r2.PlaceAtDistance(30.1f); r2.Step();
                bool far = !r2.Turret.Detection.IsSeeingTarget && r2.Turret.Turret.AiState == EnemyTurret.AIState.Idle && !r2.View.IsActive;
                PhysicsSelfTest.Check("detection range 30: attack mode (IsActive set) at 29.9 m, idle at 30.1 m", near && far, "29.9 -> " + near + ", 30.1 idle -> " + far);
            }

            // 3. fire delay 1.1 s after detection, then 15-round bursts every 0.06 s with a 0.6 s pause; each hit deals 3
            {
                var r = new Rig(loaded);
                r.PlaceAtDistance(20f);
                r.Run(6f);
                int first = r.ShotTimes.Count > 0 ? 0 : -1;
                float delay = first >= 0 ? r.ShotTimes[0] - r.DetectedAt : -1f;
                bool delayOk = first >= 0 && delay > 1.1f && delay < 1.1f + 2.5f * Dt;
                // split into bursts at gaps > 0.2 s
                var bursts = new List<List<float>>();
                for (int i = 0; i < r.ShotTimes.Count; i++)
                {
                    if (i == 0 || r.ShotTimes[i] - r.ShotTimes[i - 1] > 0.2f) bursts.Add(new List<float>());
                    bursts[bursts.Count - 1].Add(r.ShotTimes[i]);
                }
                bool burstOk = bursts.Count >= 3, gapOk = true, pauseOk = true;
                float minGap = 9f, maxGap = 0f, minPause = 9f, maxPause = 0f;
                for (int b = 0; b < bursts.Count; b++)
                {
                    bool complete = b < bursts.Count - 1;
                    if (complete) burstOk &= bursts[b].Count == 15;
                    for (int i = 1; i < bursts[b].Count; i++) { float g = bursts[b][i] - bursts[b][i - 1]; minGap = Math.Min(minGap, g); maxGap = Math.Max(maxGap, g); }
                    if (b > 0) { float p = bursts[b][0] - bursts[b - 1][bursts[b - 1].Count - 1]; minPause = Math.Min(minPause, p); maxPause = Math.Max(maxPause, p); }
                }
                gapOk = minGap > 0.06f && maxGap < 0.06f + 1.5f * Dt;
                pauseOk = bursts.Count < 2 || (minPause >= 0.6f && maxPause < 0.6f + 4f * Dt);
                PhysicsSelfTest.Check("fire delay 1.1 s after detection; bursts of 15 shots 0.06 s apart, 0.6 s pause between bursts", delayOk && burstOk && gapOk && pauseOk,
                    "first shot " + F(delay) + " s after detection, " + bursts.Count + " bursts of " + string.Join("/", bursts.ConvertAll(b => b.Count.ToString())) + ", gaps " + F(minGap) + ".." + F(maxGap) + " s, pauses " + F(minPause) + ".." + F(maxPause) + " s");
                bool hits = r.PlayerHits >= 20 && Math.Abs(r.PlayerHitDamage - 3f) < 1e-5f;
                PhysicsSelfTest.Check("the Turret's shots reach the player's capsule for damage 3", hits, r.PlayerHits + " hits of " + r.ShotTimes.Count + " shots, damage per hit " + F(r.PlayerHitDamage));
            }

            // 4. the swivel: the pivot turns the barrel at the target (the offset between the muzzle's forward and the pivot is kept), fast once shooting
            {
                var r = new Rig(loaded);
                r.PlayerRoot = r.Source + new Vector3(16f, 3f, 12f);   // off to the side and above
                float t0 = 0f;
                var err = new List<KeyValuePair<float, float>>();
                r.Run(4f, () =>
                {
                    Vector3 aim = r.Turret.NodeWorld("WeaponRoot").Translation;
                    Vector3 toTarget = Vector3.Normalize(r.Player.GetAimPoint() - aim);
                    float angle = MathHelper.ToDegrees((float)Math.Acos(MathHelper.Clamp(Vector3.Dot(r.NodeForward("GunMuzzle"), toTarget), -1f, 1f)));
                    err.Add(new KeyValuePair<float, float>(r.Manager.Time, angle));
                });
                float after1s = err.Find(e => e.Key >= 1.0f).Value, after35 = err[err.Count - 1].Value;
                float worstLate = 0f;
                foreach (var e in err) if (e.Key > 3f) worstLate = Math.Max(worstLate, e.Value);
                PhysicsSelfTest.Check("barrel aims at the target: error shrinks while turning (sharpness 2.5, then 5 after the fire delay) and is within 1.5 degrees after 3 s", worstLate < 1.5f && after35 < 1.5f && err[0].Value > after35,
                    "error at start " + F(err[0].Value) + " deg, after 1 s " + F(after1s) + " deg, after 4 s " + F(after35) + " deg (worst after 3 s " + F(worstLate) + ")");
                PhysicsSelfTest.Check("the pivot is overridden every frame while attacking", r.View.PivotOverrides >= 4 * 60 - 5, r.View.PivotOverrides + " overrides in 4 s");
            }

            // 5. losing the target: 4 s timeout, back to idle (IsActive false), then a 1 s blend from the aim to the animation, then no more overrides
            {
                var r = new Rig(loaded);
                r.PlaceAtDistance(20f);
                r.Run(2.5f);
                bool attacking = r.Turret.Turret.AiState == EnemyTurret.AIState.Attack;
                r.PlayerRoot = new Vector3(0f, -100f, 0f);   // out of range at once: known for 4 s, then lost
                float lastSeen = r.Manager.Time;             // the last frame that saw the player
                var rotAt = new List<KeyValuePair<float, Quaternion>>();
                var overrideAt = new List<float>();
                int seen = r.View.PivotOverrides;
                bool stillAttacking = false;
                r.Run(6f, () =>
                {
                    r.View.TryGetBoneWorld("Turret_Pivot_C_Jnt", false, out Matrix pm);
                    rotAt.Add(new KeyValuePair<float, Quaternion>(r.Manager.Time, Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(Norm(pm)))));
                    if (r.View.PivotOverrides != seen) { overrideAt.Add(r.Manager.Time); seen = r.View.PivotOverrides; }
                    if (r.Manager.Time - lastSeen > 3.85f && r.Manager.Time - lastSeen < 3.9f) stillAttacking = r.Turret.Turret.AiState == EnemyTurret.AIState.Attack && r.View.IsActive;
                });
                bool idle = r.Turret.Turret.AiState == EnemyTurret.AIState.Idle && !r.View.IsActive && r.LostAt > 0f && r.LostAt - lastSeen > 3.9f && r.LostAt - lastSeen < 4.2f;
                int during = overrideAt.FindAll(x => x > r.LostAt && x <= r.LostAt + 1.0f + Dt).Count;
                int late = overrideAt.FindAll(x => x > r.LostAt + 1.0f + 2f * Dt).Count;
                float stepAtLoss = 0f;
                for (int k = 1; k < rotAt.Count; k++)
                {
                    if (Math.Abs(rotAt[k].Key - r.LostAt) > 2.5f * Dt) continue;
                    float dot = Math.Abs(Quaternion.Dot(rotAt[k - 1].Value, rotAt[k].Value));
                    stepAtLoss = Math.Max(stepAtLoss, MathHelper.ToDegrees(2f * (float)Math.Acos(MathHelper.Clamp(dot, 0f, 1f))));
                }
                PhysicsSelfTest.Check("target lost 4 s after the last sighting: idle, IsActive false, a 1 s blend (an override every frame), then the animation alone", attacking && stillAttacking && idle && during >= 57 && late == 0 && stepAtLoss < 4f,
                    "attacking " + attacking + ", still attacking at 3.9 s " + stillAttacking + ", idle after " + F(r.LostAt - lastSeen) + " s " + idle + ", overrides in the 1 s after the loss " + during + ", overrides after the blend " + late + ", pivot step across the loss " + F(stepAtLoss) + " deg per frame");
            }

            // 6. animator: Idle -> AttackIn -> AttackLoop when active, AttackOut -> Idle when not; a hit plays the hurt animation
            {
                var r = new Rig(loaded);
                r.PlaceAtDistance(20f);
                r.Run(3f);
                r.PlayerRoot = new Vector3(0f, -100f, 0f);
                r.Run(8f);
                string seq = string.Join(" > ", r.View.States);
                bool states = r.View.States.Contains("AttackIn") && r.View.States.Contains("AttackLoop") && r.View.States.Contains("AttackOut") &&
                              r.View.States.IndexOf("AttackIn") < r.View.States.IndexOf("AttackLoop") && r.View.States.IndexOf("AttackLoop") < r.View.States.IndexOf("AttackOut") && r.View.LastState == "Idle";
                var dmg = new Damageable(r.Turret.Health, r.Turret);
                dmg.InflictDamage(15f, false, r.Player);
                bool trig = r.View.Triggers == 1 && Math.Abs(r.Turret.Health.CurrentHealth - 385f) < 1e-3f;
                PhysicsSelfTest.Check("animator follows IsActive (Idle > AttackIn > AttackLoop > AttackOut > Idle) and a hit sets the OnDamaged trigger", states && trig, "states " + seq + ", triggers " + r.View.Triggers);
            }

            // 7. real level: the head hitbox follows the bone, and a player on the level is engaged
            RunLevel(loaded);
        }

        private static Matrix Norm(Matrix m)
        {
            Vector3 x = Vector3.Normalize(new Vector3(m.M11, m.M12, m.M13)), y = Vector3.Normalize(new Vector3(m.M21, m.M22, m.M23)), z = Vector3.Normalize(new Vector3(m.M31, m.M32, m.M33));
            return new Matrix(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1);
        }

        private static void RunLevel(LoadedScene loaded)
        {
            SceneFile scene = loaded.Data;
            PlayerSettings ps = PlayerSettings.FromScene(scene).Clone();
            var player = new PlayerController(loaded.Collision, ps);
            var director = new EnemyDirector(loaded);
            director.SetPlayer(player, ps);
            director.Reset();
            Enemy turret = director.AllEnemies.Find(e => e.Kind() == "Turret");
            if (turret == null) return;
            EnemyRig view = loaded.EnemyViews[turret.Index];

            // the hitbox of the head (Hitbox Top) hangs off Turret_Head_C_Jnt: a ray at the head's current centre must hit it, before and after a swivel
            HitboxInfo top = loaded.Hitboxes.Find(h => h.EnemyIndex == turret.Index && h.Name == "Hitbox Top");
            bool hasBone = turret.HitboxBones.Exists(b => b.Key == top.Tag && b.Value == "Turret_Head_C_Jnt");
            player.Respawn(ps.StartPosition, ps.StartYawDegrees);
            for (int i = 0; i < 60; i++) { player.Update(Dt, default(PlayerInput)); director.Update(Dt); }
            Vector3 headIdle; { view.TryGetBoneWorld("Turret_Head_C_Jnt", false, out Matrix h); headIdle = h.Translation; }
            bool idleHit = RayAtHead(loaded, view, turret, top.Tag, out float d0);

            // stand 14 m away at floor level (a clear spot) so the turret swivels towards the player
            Vector3 spot = default(Vector3); bool found = false;
            Vector3 src = turret.DetectionSourcePosition;
            for (int i = 0; i < 48 && !found; i++)
            {
                float a = i * MathHelper.TwoPi / 48f;
                Vector3 c = new Vector3(src.X + (float)Math.Cos(a) * 14f, src.Y + 3f, src.Z + (float)Math.Sin(a) * 14f);
                if (!loaded.Collision.Raycast(c, Vector3.Down, 12f, out CollisionWorld.RayHit floor)) continue;
                c.Y = floor.Point.Y + 0.01f;
                Vector3 aim = c + new Vector3(0f, 1.09f, 0f);
                Vector3 dir = Vector3.Normalize(aim - src);
                bool clear = !loaded.Collision.Raycast(src, dir, (aim - src).Length(), out CollisionWorld.RayHit blk, turret.HitTags);
                if (clear && !loaded.Collision.OverlapsIgnoring(c + Vector3.Up * 0.35f, c + Vector3.Up * 1.45f, 0.35f, -2) && Math.Abs(floor.Normal.Y) > 0.9f) { spot = c; found = true; }
            }
            if (!found)
            {
                PhysicsSelfTest.Check("real level: a player 14 m from the Turret is detected and shot at", false, "no clear spot found around the Turret");
                director.Reset();
                return;
            }
            director.Reset();
            turret = director.AllEnemies.Find(e => e.Kind() == "Turret");
            player.Respawn(spot, 0f);
            for (int i = 0; i < 30; i++) player.Update(Dt, default(PlayerInput));
            int hits = 0;
            director.EnemyProjectiles.PlayerHit += (imp, s) => hits++;
            bool sawAttack = false;
            float t = 0f;
            while (t < 6f)
            {
                player.Update(Dt, default(PlayerInput));
                director.Update(Dt);
                t += Dt;
                sawAttack |= turret.Turret.AiState == EnemyTurret.AIState.Attack;
            }
            view.TryGetBoneWorld("Turret_Head_C_Jnt", false, out Matrix h2);
            float headMoved = Vector3.Distance(headIdle, h2.Translation);
            bool swivelHit = RayAtHead(loaded, view, turret, top.Tag, out float d1);
            PhysicsSelfTest.Check("real level: the head hitbox follows its bone (a ray at the head centre hits it, idle and after the swivel)", hasBone && idleHit && swivelHit,
                "follows bone " + hasBone + ", idle ray " + idleHit + " (" + F(d0) + " m), after the swivel " + swivelHit + " (" + F(d1) + " m), head moved " + F(headMoved) + " m");
            PhysicsSelfTest.Check("real level: a player 14 m away is detected, the Turret shoots and its shots hit the player capsule", sawAttack && turret.Weapon.ShotsFired >= 15 && hits >= 10,
                "attack " + sawAttack + ", shots " + turret.Weapon.ShotsFired + ", player hits " + hits + ", spot " + V(spot));
            director.Reset();
        }

        private static bool RayAtHead(LoadedScene loaded, EnemyRig view, Enemy turret, int topTag, out float distance)
        {
            view.TryGetBoneWorld("Turret_Head_C_Jnt", false, out Matrix head);
            // the box centre (-0.072 local y of Hitbox Top, times the scale) in world space: cast a ray at it from a point 6 m above
            ColliderDto c = loaded.Data.Colliders.Find(x => x.Owner == "Hitbox Top");
            Vector3 centre = Vector3.Transform(new Vector3(c.Center[0], c.Center[1], c.Center[2]), head);
            Vector3 origin = centre + new Vector3(0f, 6f, 0f) + new Vector3(0.7f, 0f, 0.4f);
            Vector3 dir = Vector3.Normalize(centre - origin);
            bool hit = loaded.Collision.Raycast(origin, dir, 20f, out CollisionWorld.RayHit h);
            distance = hit ? h.Distance : -1f;
            return hit && h.Owner == topTag;
        }

        private static string Kind(this Enemy e) { return e.Data.Kind; }
    }
}
