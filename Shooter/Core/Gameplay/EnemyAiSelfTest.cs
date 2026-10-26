using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Enemies;
using Shooter.Gameplay.Weapons;
using Shooter.Physics;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the HoverBot behaviour, milestone 4c-3b (run as part of --test-physics). Numbers come from the scene data.</summary>
    public static class EnemyAiSelfTest
    {
        private const float Dt = 1f / 60f;
        private static string F(float v) { return PhysicsSelfTest.F(v); }
        private static string V(Vector3 v) { return PhysicsSelfTest.V(v); }

        /// <summary>Records what the AI tells the model (animator parameters, eye colour).</summary>
        private class LogView : IEnemyView
        {
            public readonly List<string> Log = new List<string>();
            public float MoveSpeed, MaxMoveSpeed;
            public bool Alerted, AttackEye;
            public int Attacks, Damaged;
            public void SetFloat(string n, float v) { if (n == "MoveSpeed") { MoveSpeed = v; MaxMoveSpeed = Math.Max(MaxMoveSpeed, v); } }
            public void SetBool(string n, bool v) { if (n == "Alerted") { Alerted = v; Log.Add("Alerted=" + v); } }
            public void SetTrigger(string n) { if (n == "Attack") Attacks++; if (n == "OnDamaged") { Damaged++; } }
            public void SetEyeColor(bool attack) { AttackEye = attack; Log.Add("eye=" + (attack ? "attack" : "default")); }
            public Vector3 BodyEmission;
            public void SetBodyEmission(Vector3 e) { BodyEmission = e; }
            public void Update(float dt, Matrix modelWorld) { }
            public bool HasBone(string bone) { return false; }
            public bool TryGetBoneWorld(string bone, bool bindPose, out Matrix world) { world = Matrix.Identity; return false; }
            public void SetBoneWorldRotation(string bone, Quaternion worldRotation) { }
            public void SetLateUpdate(Action lateUpdate) { }
        }

        /// <summary>A HoverBot on a flat world with a stand-in player actor (position, aim point and capsule set by the test).</summary>
        private class Rig
        {
            public CollisionWorld World = new CollisionWorld();
            public GameEvents Events = new GameEvents();
            public ActorsManager Actors = new ActorsManager();
            public EnemyManager Manager;
            public ProjectileManager Projectiles;
            public Enemy Bot;
            public Actor Player = new Actor { Affiliation = 1, Name = "Player" };
            public Vector3 PlayerRoot;
            public LogView View = new LogView();
            public List<float> ShotTimes = new List<float>();
            public int PlayerHits;
            public float PlayerHitDamage;

            public Rig(LoadedScene loaded, PatrolPathDto path = null, bool withFloor = true)
            {
                if (withFloor)
                {
                    World.AddTriangle(new Vector3(-300, 0, -300), new Vector3(300, 0, -300), new Vector3(300, 0, 300));
                    World.AddTriangle(new Vector3(-300, 0, -300), new Vector3(300, 0, 300), new Vector3(-300, 0, 300));
                }
                World.Build();
                Manager = new EnemyManager(Events);
                Projectiles = new ProjectileManager();
                EnemyDto dto = loaded.Data.Enemies.Find(e => e.Kind == "HoverBot");
                ProjectileDto();
                var ctx = new EnemyContext { World = World, Projectiles = Projectiles, ProjectileSettings = ProjectileSettingsOf(loaded), PlayerActor = () => Player, View = View };
                Bot = new Enemy(dto, loaded.Data.Enemies.IndexOf(dto), Manager, Actors, ctx, path);
                Bot.Position = new Vector3(0f, 0.5f, 0f);
                Bot.YawDegrees = 0f;
                Bot.SetPathDestinationToClosestNode();
                PlayerRoot = new Vector3(100f, 0f, 100f);
                Player.GetPosition = () => PlayerRoot;
                Player.GetAimPoint = () => PlayerRoot + new Vector3(0f, 1.09f, 0f);
                Player.GetCapsule = () => new Capsule { A = PlayerRoot + new Vector3(0, 0.35f, 0), B = PlayerRoot + new Vector3(0, 1.45f, 0), Radius = 0.35f };
                Actors.Register(Player);
                Projectiles.PlayerHit += (i, s) => { PlayerHits++; PlayerHitDamage = i.Damage; };
                Bot.Attacked += () => ShotTimes.Add(Manager.Time);
            }

            private void ProjectileDto() { }

            private static ProjectileSettings ProjectileSettingsOf(LoadedScene loaded)
            {
                WeaponDto p = loaded.Data.EnemyProjectile;
                return ProjectileSettings.From(p.Components["ProjectileStandard"], new Vector3(p.TipInRoot[0], p.TipInRoot[1], p.TipInRoot[2]));
            }

            public void Step()
            {
                Manager.Update(Dt);
                Bot.Update(Dt);
                Projectiles.Update(Dt);
            }

            public void Run(float seconds, Action each = null)
            {
                int n = (int)Math.Round(seconds / Dt);
                for (int i = 0; i < n; i++) { Step(); if (each != null) each(); }
            }

            /// <summary>Player root so that the 3D distance from the detection source to the root is d, straight ahead (+X) of the source.</summary>
            public void PlaceAtDistance(float d)
            {
                PlayerRoot = Bot.DetectionSourcePosition + new Vector3(d, 0f, 0f);
            }

            public void AddWall(float x)
            {
                World.AddTriangle(new Vector3(x, -10, -100), new Vector3(x, -10, 100), new Vector3(x, 100, 100));
                World.AddTriangle(new Vector3(x, -10, -100), new Vector3(x, 100, 100), new Vector3(x, 100, -100));
                World.Build();
            }
        }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("HoverBot behaviour (4c-3b)");
            SceneFile scene = loaded.Data;
            EnemyDto dto = scene.Enemies.Find(e => e.Kind == "HoverBot");
            PatrolPathDto path = scene.PatrolPaths[dto.PatrolPath];

            // 1. data read from the scene
            {
                var r = new Rig(loaded);
                NavAgent a = r.Bot.Agent;
                DetectionModule d = r.Bot.Detection;
                WeaponSettings w = r.Bot.Weapon.Settings;
                ProjectileSettings ps = Rig_ProjectileSettings(loaded);
                bool ok = a.Speed == 3.5f && a.Acceleration == 8f && a.AngularSpeed == 120f && Math.Abs(a.StoppingDistance - 0.2f) < 1e-6f && a.Radius == 0.5f && a.Height == 2f &&
                          d.DetectionRange == 20f && d.AttackRange == 10f && d.KnownTargetTimeout == 12f && Math.Abs(r.Bot.Mobile.AttackStopDistanceRatio - 0.08f) < 1e-6f &&
                          r.Bot.OrientationSpeed == 10f && r.Bot.PathReachingRadius == 2f && Math.Abs(w.DelayBetweenShots - 0.8f) < 1e-6f && w.MaxAmmo == 100 && w.ShootType == WeaponShootType.Automatic &&
                          ps.Speed == 100f && ps.Damage == 10f && Math.Abs(ps.Radius - 0.1f) < 1e-6f && ps.TrajectoryCorrectionDistance == 5f;
                PhysicsSelfTest.Check("HoverBot agent / detection / weapon / projectile values read from the scene", ok,
                    "agent " + a.Speed + "/" + a.Acceleration + "/" + a.AngularSpeed + "/" + F(a.StoppingDistance) + ", detection " + d.DetectionRange + "/" + d.AttackRange + "/" + d.KnownTargetTimeout +
                    ", stop ratio " + F(r.Bot.Mobile.AttackStopDistanceRatio) + ", weapon delay " + w.DelayBetweenShots + " ammo " + w.MaxAmmo + ", projectile speed " + ps.Speed + " damage " + ps.Damage);
                Vector3 aim = r.Bot.AimPointPosition - r.Bot.Position, src = r.Bot.DetectionSourcePosition - r.Bot.Position;
                bool nodes = Math.Abs(aim.Y - 1.359f) < 1e-3f && Math.Abs(src.Y - 1.347f) < 1e-3f && Math.Abs(src.Z - -0.256f) < 1e-3f;
                PhysicsSelfTest.Check("aim point (HitBox) and detection source (WeaponRoot) resolved from the prefab, rotated with the bot", nodes, "aim " + V(aim) + ", source " + V(src));
                r.Bot.YawDegrees = 90f;   // Unity +X: the forward offset of the source follows
                Vector3 srcTurned = r.Bot.DetectionSourcePosition - r.Bot.Position;
                PhysicsSelfTest.Check("detection source follows the bot's yaw (forward offset 0.256 along Unity +X after a 90 degree turn)", Math.Abs(srcTurned.X - 0.256f) < 1e-3f && Math.Abs(srcTurned.Z) < 1e-3f, "source offset " + V(srcTurned));
            }

            // 2. detection range, line of sight, own and foreign hitboxes
            {
                var r = new Rig(loaded);
                r.PlaceAtDistance(19.9f); r.Step();
                bool near = r.Bot.Detection.KnownDetectedTarget != null && r.Bot.Detection.IsSeeingTarget;
                var r2 = new Rig(loaded);
                r2.PlaceAtDistance(20.1f); r2.Step();
                bool far = r2.Bot.Detection.KnownDetectedTarget == null;
                PhysicsSelfTest.Check("detection range 20: seen at 19.9 m, not at 20.1 m (3D distance from the source to the player's root)", near && far, "19.9 -> " + near + ", 20.1 -> " + !far);

                var r3 = new Rig(loaded);
                r3.PlaceAtDistance(10f);
                r3.AddWall(r3.Bot.DetectionSourcePosition.X + 5f);
                r3.Step();
                bool blocked = r3.Bot.Detection.KnownDetectedTarget == null;
                var r4 = new Rig(loaded);
                r4.PlaceAtDistance(10f);
                r4.AddWall(r4.Bot.DetectionSourcePosition.X + 11f);   // behind the player
                r4.Step();
                bool behind = r4.Bot.Detection.IsSeeingTarget;
                PhysicsSelfTest.Check("line of sight: a wall between blocks detection, a wall behind the player does not", blocked && behind, "wall between -> seen " + !blocked + ", wall behind -> seen " + behind);

                var r5 = new Rig(loaded);
                r5.PlaceAtDistance(10f);
                r5.World.CurrentOwner = 7;
                r5.World.AddDynamicSphere(7, r5.Bot.AimPointPosition, 0.48f);   // its own hitbox around the aim point
                r5.World.CurrentOwner = -1;
                r5.Bot.HitTags.Add(7);
                r5.Step();
                bool ownIgnored = r5.Bot.Detection.IsSeeingTarget;
                var r6 = new Rig(loaded);
                r6.PlaceAtDistance(10f);
                r6.World.AddDynamicSphere(8, r6.Bot.DetectionSourcePosition + new Vector3(4f, 0f, 0f), 0.5f);   // someone else's hitbox in the way
                r6.Step();
                bool otherBlocks = !r6.Bot.Detection.IsSeeingTarget;
                PhysicsSelfTest.Check("the bot's own hitbox does not block its view, another enemy's does", ownIgnored && otherBlocks, "own ignored " + ownIgnored + ", other blocks " + otherBlocks);
            }

            // 3. events, eye colour, animator parameters, state machine (Patrol -> Follow -> Attack -> Follow -> Patrol)
            {
                var r = new Rig(loaded);
                bool startPatrol = r.Bot.Mobile.AiState == EnemyMobile.AIState.Patrol && !r.View.AttackEye;
                r.PlaceAtDistance(15f);   // in sight, out of attack range
                r.Step();
                bool follow = r.Bot.Mobile.AiState == EnemyMobile.AIState.Follow && r.View.AttackEye && r.View.Alerted && r.Bot.IsDetectingTarget && !r.Bot.Detection.IsTargetInAttackRange;
                // attack range 10 is measured from the bot's root to the player's aim point: put the aim point at 9.9, then 10.1
                r.PlayerRoot = r.Bot.Position + new Vector3(9.9f, -1.09f, 0f);
                r.Step();
                bool attack = r.Bot.Mobile.AiState == EnemyMobile.AIState.Attack && r.Bot.Detection.IsTargetInAttackRange;
                var rb = new Rig(loaded);
                rb.PlaceAtDistance(15f); rb.Step();
                rb.PlayerRoot = rb.Bot.Position + new Vector3(10.1f, -1.09f, 0f);
                rb.Step();
                bool stillFollow = rb.Bot.Mobile.AiState == EnemyMobile.AIState.Follow && !rb.Bot.Detection.IsTargetInAttackRange;
                PhysicsSelfTest.Check("Patrol -> Follow on sight (eye turns attack colour, Alerted set); Attack within 10 m of the aim point, Follow beyond", startPatrol && follow && attack && stillFollow,
                    "start patrol " + startPatrol + ", follow " + follow + ", attack at 9.9 " + attack + ", follow at 10.1 " + stillFollow);

                // leave attack range: Attack -> Follow
                r.PlayerRoot = r.Bot.Position + new Vector3(10.5f, -1.09f, 0f);
                r.Step();
                bool backToFollow = r.Bot.Mobile.AiState == EnemyMobile.AIState.Follow;
                // hide behind a wall: known for 12 s, then lost (Patrol, eye back, Alerted off)
                r.AddWall(r.Bot.Position.X + 5f);
                float t0 = r.Manager.Time;
                r.Step();   // last frame it could see: IsSeeingTarget false now, timeout counts from the last time seen
                float lastSeen = r.Manager.Time - Dt;
                bool stillKnown = false, lostAfter = false, lostEarly = false;
                while (r.Manager.Time - lastSeen < 12.2f)
                {
                    r.Step();
                    float since = r.Manager.Time - lastSeen;
                    if (since > 11.5f && since < 11.9f && r.Bot.Detection.KnownDetectedTarget != null) stillKnown = true;
                    if (since < 11.99f && r.Bot.Detection.KnownDetectedTarget == null) lostEarly = true;
                    if (since > 12.1f && r.Bot.Detection.KnownDetectedTarget == null) lostAfter = true;
                }
                bool patrolAgain = r.Bot.Mobile.AiState == EnemyMobile.AIState.Patrol && !r.View.AttackEye && !r.View.Alerted && !r.Bot.IsDetectingTarget;
                PhysicsSelfTest.Check("target forgotten 12 s after it was last seen (Attack -> Follow -> Patrol, eye and Alerted reset)", backToFollow && stillKnown && lostAfter && !lostEarly && patrolAgain,
                    "back to follow " + backToFollow + ", known at 11.5-11.9 s " + stillKnown + ", lost early " + lostEarly + ", lost after 12.1 s " + lostAfter + ", patrol " + patrolAgain + ", log " + string.Join(",", r.View.Log));
            }

            // 4. damage makes the bot pursue the shooter, even unseen; the target is then the shooter's root, not the aim point
            {
                var r = new Rig(loaded);
                r.PlayerRoot = new Vector3(60f, 0f, 0f);   // far away, nothing to see
                r.Step();
                var dmg = new Damageable(r.Bot.Health, r.Bot);
                dmg.InflictDamage(15f, false, r.Player);
                r.Step();
                TargetRef known = r.Bot.Detection.KnownDetectedTarget;
                bool ok = known != null && !known.IsAimPoint && (known.Position - r.PlayerRoot).Length() < 1e-4f && r.Bot.Mobile.AiState == EnemyMobile.AIState.Follow && r.View.AttackEye && r.View.Damaged == 1 &&
                          Math.Abs(r.Bot.Health.CurrentHealth - 85f) < 1e-4f;
                PhysicsSelfTest.Check("a hit from far away makes the bot pursue (target = shooter's root, Follow, OnDamaged trigger)", ok, "known " + (known != null) + ", aim point " + (known != null && known.IsAimPoint) + ", state " + r.Bot.Mobile.AiState + ", damaged triggers " + r.View.Damaged);
            }

            // 5. movement: acceleration, top speed, braking, stopping distance, turning, wall sliding
            {
                var r = new Rig(loaded);
                var agent = r.Bot.Agent;
                agent.SetDestination(new Vector3(20f, 0.5f, 0f));
                float t = 0f, peak = 0f; bool timeSet = false; float tReach = 0f;
                while (t < 12f)
                {
                    agent.Update(Dt, r.World, -2);
                    t += Dt;
                    peak = Math.Max(peak, agent.Velocity.Length());
                    if (!timeSet && agent.Velocity.LengthSquared() < 1e-6f && t > 0.5f) { timeSet = true; tReach = t; }
                }
                float accelDist = 3.5f * 3.5f / (2f * 8f);
                float expectedT = 2f * (3.5f / 8f) + (20f - 0.2f - 2f * accelDist) / 3.5f;
                float stopAt = 20f - agent.Position.X;
                PhysicsSelfTest.Check("agent: top speed 3.5, stops within the 0.2 stopping distance, trip time follows accel 8 / braking", Math.Abs(peak - 3.5f) < 0.02f && stopAt > 0f && stopAt <= 0.25f && Math.Abs(tReach - expectedT) < 0.35f,
                    "peak " + F(peak) + ", stopped " + F(stopAt) + " m short after " + F(tReach) + " s (kinematic estimate " + F(expectedT) + " s)");

                var r2 = new Rig(loaded);
                var a2 = r2.Bot.Agent;
                a2.YawDegrees = 0f;
                a2.SetDestination(new Vector3(-30f, 0.5f, 0f));   // Unity yaw -90: a 90 degree turn at 120 deg/s
                float tt = 0f; float yawAtHalf = 0f;
                while (tt < 1.2f) { a2.Update(Dt, r2.World, -2); tt += Dt; if (Math.Abs(tt - 0.5f) < Dt / 2f) yawAtHalf = a2.YawDegrees; }
                bool turn = Math.Abs(a2.YawDegrees - -90f) < 0.5f && Math.Abs(Math.Abs(yawAtHalf) - 60f) < 3f;   // 120 deg/s x 0.5 s once it moves (a short accel delay)
                PhysicsSelfTest.Check("agent turns towards its velocity at 120 degrees per second", turn, "yaw after 0.5 s " + F(yawAtHalf) + ", after 1.2 s " + F(a2.YawDegrees));

                var r3 = new Rig(loaded);
                r3.AddWall(5f);
                var a3 = r3.Bot.Agent;
                a3.SetDestination(new Vector3(30f, 0.5f, 30f));
                for (int i = 0; i < 600; i++) a3.Update(Dt, r3.World, -2);
                bool slide = a3.Position.X < 5f - 0.5f + 0.01f && a3.Position.X > 4f && a3.Position.Z > 3f;
                PhysicsSelfTest.Check("agent slides along a wall instead of stopping (radius 0.5 keeps it off the wall)", slide, "after 10 s at " + V(a3.Position));
            }

            // 6. weapon: the bot orients its weapon, fires every 0.8 s, shots are projectiles of speed 100 and damage 10 that hit the player
            {
                var r = new Rig(loaded);
                r.PlayerRoot = r.Bot.Position + new Vector3(8f, -1.09f, 0f);   // in attack range, the aim point 8 m away
                r.Run(4.05f);
                bool seven = r.ShotTimes.Count >= 5 && r.ShotTimes.Count <= 6;
                float minGap = float.MaxValue, maxGap = 0f;
                for (int i = 1; i < r.ShotTimes.Count; i++) { float g = r.ShotTimes[i] - r.ShotTimes[i - 1]; minGap = Math.Min(minGap, g); maxGap = Math.Max(maxGap, g); }
                bool rate = r.ShotTimes.Count >= 2 && minGap > 0.8f && maxGap < 0.8f + 2.5f * Dt;
                bool ammo = r.Bot.Weapon.CurrentAmmo < 100f && r.Bot.Weapon.CurrentAmmo > 100f - r.ShotTimes.Count;
                bool triggers = r.View.Attacks == r.ShotTimes.Count;
                PhysicsSelfTest.Check("fire rate: one shot per 0.8 s while attacking, Attack animator trigger per shot, ammo drains from 100", seven && rate && ammo && triggers,
                    r.ShotTimes.Count + " shots in 4 s, gaps " + F(minGap) + ".." + F(maxGap) + " s, ammo " + F(r.Bot.Weapon.CurrentAmmo) + ", Attack triggers " + r.View.Attacks);
                bool hits = r.PlayerHits >= 3 && Math.Abs(r.PlayerHitDamage - 10f) < 1e-4f;
                PhysicsSelfTest.Check("the shots reach the player's capsule with damage 10 (applied to the player in 3d)", hits, r.PlayerHits + " hits, damage per hit " + F(r.PlayerHitDamage));

                // one shot in detail: where it starts and how fast it flies
                var r2 = new Rig(loaded);
                r2.Bot.Agent.Speed = 0f;   // keep the bot still so the muzzle can be predicted
                r2.PlayerRoot = r2.Bot.Position + new Vector3(8f, -1.09f, 0f);
                int guard = 0;
                while (r2.Projectiles.Live.Count == 0 && guard++ < 60) r2.Step();
                ProjectileStandard p = r2.Projectiles.Live.Count > 0 ? r2.Projectiles.Live[0] : null;
                bool spawned = p != null;
                if (spawned)
                {
                    Vector3 gun = r2.Bot.Weapon.GunRootWorld(r2.Bot);
                    Vector3 toAim = Vector3.Normalize(r2.Bot.Detection.KnownDetectedTarget.Position - gun);
                    float angle = MathHelper.ToDegrees((float)Math.Acos(MathHelper.Clamp(Vector3.Dot(p.Forward, toAim), -1f, 1f)));
                    Vector3 startPos = p.Position - p.Forward * (100f * Dt);   // the manager already moved it one frame in the step that fired it
                    Vector3 expectedMuzzle = r2.Bot.Weapon.PivotWorld(r2.Bot) + toAim * -0.026f;
                    r2.Step(); r2.Step();   // (a third frame would reach the player 8 m away: the tip is 2 m ahead of the root)
                    float travelled = (p.Position - startPos).Length() - 100f * Dt;   // two more frames
                    bool ok = angle < 0.5f && (startPos - expectedMuzzle).Length() < 0.01f && p.Alive && Math.Abs(travelled - 2f * 100f * Dt) < 0.01f;
                    PhysicsSelfTest.Check("projectile starts at the muzzle, flies at the aim point at 100 m/s", ok, "angle to the aim point " + F(angle) + " deg, start " + V(startPos) + " expected " + V(expectedMuzzle) + ", travelled " + F(travelled) + " m in 2 frames");
                }
                else
                {
                    PhysicsSelfTest.Check("projectile starts at the muzzle, flies at the aim point at 100 m/s", false, "no projectile spawned");
                }
            }

            // 7. real level: patrol over the baked path (timing, order, never stuck), animator, engagement
            RunLevel(loaded, path);
        }

        // (kept for the data check)
        private static ProjectileSettings Rig_ProjectileSettings(LoadedScene loaded)
        {
            WeaponDto p = loaded.Data.EnemyProjectile;
            return ProjectileSettings.From(p.Components["ProjectileStandard"], new Vector3(p.TipInRoot[0], p.TipInRoot[1], p.TipInRoot[2]));
        }

        private static void RunLevel(LoadedScene loaded, PatrolPathDto path)
        {
            SceneFile scene = loaded.Data;
            PlayerSettings ps = PlayerSettings.FromScene(scene).Clone();
            var player = new PlayerController(loaded.Collision, ps);
            var director = new EnemyDirector(loaded);
            director.SetPlayer(player, ps);
            director.Reset();
            Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
            var nodes = new List<Vector3>();
            foreach (float[] p in path.Points) nodes.Add(new Vector3(p[0], p[1], p[2]));

            // the player waits at the spawn, 35 m away: out of range
            player.Respawn(ps.StartPosition, ps.StartYawDegrees);
            for (int i = 0; i < 120; i++) player.Update(Dt, default(PlayerInput));

            // closest node at start
            int closest = 0;
            for (int i = 1; i < nodes.Count; i++) if (Vector3.Distance(bot.Position, nodes[i]) < Vector3.Distance(bot.Position, nodes[closest])) closest = i;
            PhysicsSelfTest.Check("patrol starts at the closest node of the baked path", bot.PatrolNodes != null && bot.PatrolNodes.Length == 3 && bot.PathDestinationNodeIndex == closest && bot.Mobile.AiState == EnemyMobile.AIState.Patrol,
                "path nodes " + (bot.PatrolNodes != null ? bot.PatrolNodes.Length : 0) + ", start " + V(bot.Position) + ", destination node " + bot.PathDestinationNodeIndex + " (closest " + closest + ", distance " + F(Vector3.Distance(bot.Position, nodes[closest])) + ")");

            float startToNode = Vector3.Distance(bot.Position, nodes[closest]);
            var order = new List<int> { bot.PathDestinationNodeIndex };
            var reachedAt = new List<float>();
            float time = 0f, stuckTime = 0f, maxSpeed = 0f;
            var states = new List<string>();
            string lastState = null;
            EnemyRig rig = loaded.EnemyViews != null ? loaded.EnemyViews[bot.Index] : null;
            while (time < 60f)
            {
                player.Update(Dt, default(PlayerInput));
                director.Update(Dt);
                time += Dt;
                maxSpeed = Math.Max(maxSpeed, bot.Agent.VelocityMagnitude);
                if (bot.Agent.IsStuck) stuckTime += Dt;
                if (bot.PathDestinationNodeIndex != order[order.Count - 1]) { order.Add(bot.PathDestinationNodeIndex); reachedAt.Add(time); }
                if (rig != null)
                {
                    string st = rig.Animator.GetCurrentStateId(0);
                    if (st != lastState) { states.Add(st + "@" + F(time)); lastState = st; }
                }
            }
            bool sequence = order.Count >= 7;
            for (int i = 1; i < order.Count && sequence; i++) sequence &= order[i] == (order[i - 1] + 1) % 3;
            float firstLeg = reachedAt.Count > 0 ? reachedAt[0] : -1f;
            float expectFirst = startToNode - bot.PathReachingRadius;
            PhysicsSelfTest.Check("patrol visits the nodes in order, never stuck, top speed 3.5 (60 s)", sequence && stuckTime < 0.05f && Math.Abs(maxSpeed - 3.5f) < 0.05f,
                "destinations " + string.Join(">", order) + ", first node reached at " + F(firstLeg) + " s (straight line " + F(expectFirst) + " m to the reaching radius), stuck " + F(stuckTime) + " s, top speed " + F(maxSpeed) + ", legs at " + string.Join(", ", reachedAt.ConvertAll(F)));
            if (rig != null)
            {
                bool anim = states.Exists(s => s.StartsWith("Locomotion/Patrolling"));
                PhysicsSelfTest.Check("animator: the idle bot starts patrolling animation once it moves (MoveSpeed > 0.05, Alerted false)", anim, "states " + string.Join(" > ", states));
            }
            else
            {
                PhysicsSelfTest.Check("animator: the idle bot starts patrolling animation once it moves", false, "no animator view (HoverBot.animator.json missing)");
            }

            // engagement on the real level: find a clear spot 15 m from the bot (in detection range, outside the 10 m attack range) and stand there
            Vector3 spot = default(Vector3); bool found = false;
            for (int i = 0; i < 24 && !found; i++)
            {
                float a = i * MathHelper.TwoPi / 24f;
                Vector3 c = bot.Position + new Vector3((float)Math.Cos(a) * 15f, 0f, (float)Math.Sin(a) * 15f);
                c.Y = 0.251f;
                Vector3 src = bot.DetectionSourcePosition, aim = c + new Vector3(0f, 1.09f, 0f);
                Vector3 d = Vector3.Normalize(aim - src);
                if (!loaded.Collision.Raycast(src, d, (aim - src).Length(), out CollisionWorld.RayHit hit, bot.HitTags) && loaded.Collision.OverlapsIgnoring(c + Vector3.Up * 0.35f, c + Vector3.Up * 1.45f, 0.35f, -2) == false)
                {
                    spot = c; found = true;
                }
            }
            if (!found)
            {
                PhysicsSelfTest.Check("real level: a player 15 m away is detected, followed, attacked; shots hit the player capsule", false, "no clear spot found around the bot");
            }
            else
            {
                director.Reset();
                bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
                player.Respawn(spot, 0f);
                for (int i = 0; i < 30; i++) player.Update(Dt, default(PlayerInput));
                var seen = new List<EnemyMobile.AIState>();
                int hits = 0;
                float t = 0f;
                director.EnemyProjectiles.PlayerHit += (imp, src) => hits++;
                while (t < 8f)
                {
                    player.Update(Dt, default(PlayerInput));
                    director.Update(Dt);
                    t += Dt;
                    if (seen.Count == 0 || seen[seen.Count - 1] != bot.Mobile.AiState) seen.Add(bot.Mobile.AiState);
                }
                float finalDist = Vector3.Distance(bot.Position, player.Position);
                bool states2 = seen.Count >= 2 && seen[0] == EnemyMobile.AIState.Follow && seen[1] == EnemyMobile.AIState.Attack;
                PhysicsSelfTest.Check("real level: a player 15 m away is detected, followed, attacked; shots hit the player capsule", states2 && hits >= 3 && bot.Weapon.ShotsFired >= 3,
                    "states " + string.Join(">", seen) + ", shots " + bot.Weapon.ShotsFired + ", player hits " + hits + ", end distance " + F(finalDist) + " m (AttackStopDistanceRatio 0.08 x 10 = 0.8 m is where it stops closing in), player spot " + V(spot));
            }
            loaded.Collision.SetOwnerActive(0, true);
            director.Reset();
        }
    }
}
