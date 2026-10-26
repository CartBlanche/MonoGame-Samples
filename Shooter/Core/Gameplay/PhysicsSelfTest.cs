using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Enemies;
using Shooter.Physics;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>
    /// Headless verification of the player controller (run with --test-physics). Scripted input steps against small
    /// synthetic worlds (exact expected numbers) and against the real baked level (settle, fuzz for tunnelling).
    /// Synthetic worlds use +X as "forward" (yaw 90), floor at y = 0, zero-thickness walls on purpose (worst case for tunnelling).
    /// </summary>
    public static class PhysicsSelfTest
    {
        internal static int _pass, _fail;

        internal static void Check(string name, bool ok, string detail)
        {
            if (ok) _pass++; else _fail++;
            Console.WriteLine("  [" + (ok ? "PASS" : "FAIL") + "] " + name + " - " + detail);
        }

        internal static string F(float v) { return v.ToString("0.0000"); }
        internal static string V(Vector3 v) { return "(" + v.X.ToString("0.000") + ", " + v.Y.ToString("0.000") + ", " + v.Z.ToString("0.000") + ")"; }

        private static PlayerInput Walk(bool sprint = false) { return new PlayerInput { Move = new Vector2(0, 1), SprintHeld = sprint }; }

        private static void Run(PlayerController pc, int frames, float dt, Func<int, PlayerInput> input, Action<int> each = null)
        {
            for (int i = 0; i < frames; i++)
            {
                pc.Update(dt, input(i));
                if (each != null) each(i);
            }
        }

        // ------------------------------------------------------------------ synthetic worlds

        private static CollisionWorld FlatWorld(float floorMaxX = 100f)
        {
            var w = new CollisionWorld();
            Quad(w, new Vector3(-100, 0, -100), new Vector3(floorMaxX, 0, -100), new Vector3(floorMaxX, 0, 100), new Vector3(-100, 0, 100));
            return w;
        }

        private static void Quad(CollisionWorld w, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            w.AddTriangle(a, b, c);
            w.AddTriangle(a, c, d);
        }

        private static PlayerController Spawn(CollisionWorld w, PlayerSettings s, Vector3 pos, float yaw = 90f)
        {
            w.Build();
            PlayerSettings s2 = s.Clone();
            s2.StartPosition = pos;
            s2.StartYawDegrees = yaw;
            return new PlayerController(w, s2);
        }

        public static int Run(SceneFile scene, CollisionWorld level, LoadedScene loaded = null)
        {
            _pass = _fail = 0;
            PlayerSettings s = PlayerSettings.FromScene(scene);
            Console.WriteLine("Physics self-test. Settings from scene: speed " + s.MaxSpeedOnGround + ", sprint x" + s.SprintSpeedModifier + ", crouch x" + s.MaxSpeedCrouchedRatio +
                              ", gravity " + s.GravityDownForce + ", jump " + s.JumpForce + ", height " + s.CapsuleHeightStanding + "/" + s.CapsuleHeightCrouching + ", radius " + s.Radius +
                              ", slope " + s.SlopeLimit + ", step " + s.StepOffset + ", skin " + s.SkinWidth + ", groundCheck " + s.GroundCheckDistance + ", kill " + s.KillHeight);
            const float dt = 1f / 60f;

            Console.WriteLine("Synthetic worlds");
            // 1. fall and rest
            {
                var pc = Spawn(FlatWorld(), s, new Vector3(0, 3, 0));
                Run(pc, 180, dt, i => default(PlayerInput));
                Check("land on the floor", pc.IsGrounded && Math.Abs(pc.Position.Y) < 0.01f && pc.CharacterVelocity.Length() < 0.05f,
                      "dropped from 3 m, rests at y=" + F(pc.Position.Y) + " grounded=" + pc.IsGrounded + " |v|=" + F(pc.CharacterVelocity.Length()));
                float cam = pc.CameraPosition.Y;
                Check("camera height", Math.Abs(cam - 1.44f) < 0.01f, "camera y above root " + F(cam - pc.Position.Y) + " (1.44 expected)");
            }
            // 2. walk into a wall, 3. sprint into a wall at coarse frame rates (tunnelling), 4. diagonal slide
            foreach (float step in new[] { 1f / 60f, 1f / 30f, 1f / 10f })
            {
                foreach (bool sprint in new[] { false, true })
                {
                    var w = FlatWorld();
                    Quad(w, new Vector3(5, 0, -100), new Vector3(5, 0, 100), new Vector3(5, 50, 100), new Vector3(5, 50, -100));   // zero-thickness wall at x = 5
                    var pc = Spawn(w, s, new Vector3(0, 0.001f, 0));
                    float maxX = -1e9f;
                    int frames = (int)(5f / step);
                    Run(pc, frames, step, i => Walk(sprint), i => maxX = Math.Max(maxX, pc.Position.X));
                    float expect = 5f - s.Radius - s.SkinWidth;
                    Check("wall stop dt=" + (1f / step).ToString("0") + "fps" + (sprint ? " sprint" : ""), maxX < 5f - s.Radius + 0.005f && Math.Abs(pc.Position.X - expect) < 0.02f,
                          "max x " + F(maxX) + ", rest x " + F(pc.Position.X) + " (expected " + F(expect) + "), |v|=" + F(pc.CharacterVelocity.Length()));
                }
            }
            {
                var w = FlatWorld();
                Quad(w, new Vector3(5, 0, -100), new Vector3(5, 0, 100), new Vector3(5, 50, 100), new Vector3(5, 50, -100));
                var pc = Spawn(w, s, new Vector3(0, 0.001f, 0), 45f);   // forward = (+x, -z): hits the wall at 45 degrees
                Run(pc, 240, dt, i => Walk());
                float expectX = 5f - s.Radius - s.SkinWidth;
                Check("wall slide at 45 degrees", Math.Abs(pc.Position.X - expectX) < 0.02f && pc.Position.Z < -15f,
                      "x=" + F(pc.Position.X) + " (wall at " + F(expectX) + "), slid to z=" + F(pc.Position.Z) + ", speed " + F(new Vector2(pc.CharacterVelocity.X, pc.CharacterVelocity.Z).Length()));
            }
            // 5. speeds
            {
                var pc = Spawn(FlatWorld(), s, new Vector3(0, 0.001f, 0));
                Run(pc, 120, dt, i => Walk());
                float walk = pc.CharacterVelocity.Length();
                pc = Spawn(FlatWorld(), s, new Vector3(0, 0.001f, 0));
                Run(pc, 120, dt, i => Walk(true));
                float sprint = pc.CharacterVelocity.Length();
                pc = Spawn(FlatWorld(), s, new Vector3(0, 0.001f, 0));
                Run(pc, 1, dt, i => new PlayerInput { CrouchDown = true });
                Run(pc, 120, dt, i => Walk());
                float crouch = pc.CharacterVelocity.Length();
                Check("walk speed", Math.Abs(walk - s.MaxSpeedOnGround) < 0.1f, F(walk) + " (expected " + s.MaxSpeedOnGround + ")");
                Check("sprint speed", Math.Abs(sprint - s.MaxSpeedOnGround * s.SprintSpeedModifier) < 0.1f, F(sprint) + " (expected " + s.MaxSpeedOnGround * s.SprintSpeedModifier + ")");
                Check("crouch speed", Math.Abs(crouch - s.MaxSpeedOnGround * s.MaxSpeedCrouchedRatio) < 0.1f && pc.IsCrouching, F(crouch) + " (expected " + s.MaxSpeedOnGround * s.MaxSpeedCrouchedRatio + "), height " + F(pc.Height) + ", camera " + F(pc.CameraLocalHeight));
            }
            // 6. step offset
            foreach (float h in new[] { 0.2f, 0.3f, 0.35f })
            {
                var wb = FlatWorld();
                wb.AddBox(Matrix.CreateTranslation(53, h * 0.5f, 0), Vector3.Zero, new Vector3(100, h, 20));   // slab x 3..103
                var pc = Spawn(wb, s, new Vector3(0, 0.001f, 0));
                Run(pc, 180, dt, i => Walk());
                bool climbs = h <= s.StepOffset + 1e-4f;
                bool ok = climbs ? (pc.Position.X > 6f && Math.Abs(pc.Position.Y - h) < 0.01f) : (pc.Position.X < 3f && pc.Position.Y < 0.01f);
                Check("step " + h.ToString("0.00") + " m", ok, (climbs ? "must climb" : "must block") + ": ended at " + V(pc.Position) + " grounded=" + pc.IsGrounded);
            }
            // 7. jump
            {
                var pc = Spawn(FlatWorld(), s, new Vector3(0, 0.001f, 0));
                Run(pc, 30, dt, i => default(PlayerInput));
                float maxY = 0f; int air = 0; bool landed = false;
                pc.Landed += (sp, dmg) => landed = true;
                Run(pc, 150, dt, i => i == 0 ? new PlayerInput { JumpDown = true } : default(PlayerInput), i => { maxY = Math.Max(maxY, pc.Position.Y); if (!pc.IsGrounded) air++; });
                float ideal = s.JumpForce * s.JumpForce / (2f * s.GravityDownForce);
                Check("jump height and landing", Math.Abs(maxY - ideal) < 0.1f && pc.IsGrounded && landed && Math.Abs(pc.Position.Y) < 0.01f,
                      "apex " + F(maxY) + " (v^2/2g = " + F(ideal) + "), airborne " + (air * dt).ToString("0.00") + " s (ideal " + (2f * s.JumpForce / s.GravityDownForce).ToString("0.00") + "), landed event " + landed + ", y=" + F(pc.Position.Y));
            }
            // 8. ramps (walkable 30 degrees, too steep 60 degrees)
            foreach (float deg in new[] { 30f, 60f })
            {
                var w = FlatWorld();
                float rise = 3f, run = rise / (float)Math.Tan(MathHelper.ToRadians(deg));
                float x0 = 2f, x1 = x0 + run;
                Quad(w, new Vector3(x0, 0, -10), new Vector3(x1, rise, -10), new Vector3(x1, rise, 10), new Vector3(x0, 0, 10));
                Quad(w, new Vector3(x1, rise, -10), new Vector3(60, rise, -10), new Vector3(60, rise, 10), new Vector3(x1, rise, 10));
                var pc = Spawn(w, s, new Vector3(0, 0.001f, 0));
                float maxY = 0f;
                Run(pc, 240, dt, i => Walk(), i => maxY = Math.Max(maxY, pc.Position.Y));
                bool walkable = deg <= s.SlopeLimit;
                bool ok = walkable ? (Math.Abs(pc.Position.Y - rise) < 0.02f && pc.IsGrounded) : (maxY < s.StepOffset + 0.05f && pc.Position.X < x0);
                Check("ramp " + deg + " deg", ok, (walkable ? "must climb to " + rise : "must not climb") + ": max y " + F(maxY) + ", end " + V(pc.Position));
            }
            // 9. crouch under a low ceiling
            {
                var w = FlatWorld();
                Quad(w, new Vector3(0, 1.2f, -10), new Vector3(0, 1.2f, 10), new Vector3(60, 1.2f, 10), new Vector3(60, 1.2f, -10));
                var pc = Spawn(w, s, new Vector3(-0.5f, 0.001f, 0));
                Run(pc, 1, dt, i => new PlayerInput { CrouchDown = true });
                Run(pc, 60, dt, i => Walk());                      // walk under the ceiling crouched
                bool under = pc.Position.X > 0.5f;
                Run(pc, 1, dt, i => new PlayerInput { CrouchDown = true });   // try to stand
                Run(pc, 60, dt, i => default(PlayerInput));
                bool still = pc.IsCrouching && pc.Height < 1.0f;
                Check("cannot stand under a 1.2 m ceiling", under && still, "x=" + F(pc.Position.X) + " crouching=" + pc.IsCrouching + " height " + F(pc.Height));
                Run(pc, 120, dt, i => Walk());
                Run(pc, 1, dt, i => new PlayerInput { CrouchDown = true });
                Run(pc, 60, dt, i => Walk(true));
                Check("sprint cannot force standing under the ceiling", pc.IsCrouching && pc.Height < 1.0f, "crouching=" + pc.IsCrouching + " height " + F(pc.Height));
            }
            // 10. kill height + landing speed
            {
                var pc = Spawn(FlatWorld(12f), s, new Vector3(0, 0.001f, 0));
                bool died = false; int deathFrame = -1;
                pc.Died += () => died = true;
                Run(pc, 600, dt, i => Walk(), i => { if (died && deathFrame < 0) deathFrame = i; });
                Check("kill height", died && pc.IsDead && pc.Position.Y < s.KillHeight + 1f, "walked off the edge, died=" + died + " at frame " + deathFrame + ", y=" + F(pc.Position.Y));
                var pc2 = Spawn(FlatWorld(), s, new Vector3(0, 10, 0));
                float landSpeed = 0f, landDmg = -1f;
                pc2.Landed += (sp, dmg) => { landSpeed = sp; landDmg = dmg; };
                Run(pc2, 200, dt, i => default(PlayerInput));
                float ideal = (float)Math.Sqrt(2f * s.GravityDownForce * 10f);
                Check("fall landing speed", Math.Abs(landSpeed - ideal) < 1f, "fell 10 m: speed " + F(landSpeed) + " (sqrt(2gh) = " + F(ideal) + "), damage " + F(landDmg) + (s.RecievesFallDamage ? "" : " (fall damage off)"));
            }
            // 10b. the controller drives a Health like PlayerCharacterController: fall damage through TakeDamage(null source), kill height through Kill()
            {
                var hs = s.Clone();
                hs.RecievesFallDamage = true;
                hs.MinSpeedForFallDamage = 20f; hs.MaxSpeedForFallDamage = 40f; hs.FallDamageAtMinSpeed = 10f; hs.FallDamageAtMaxSpeed = 30f;
                // a 10 m fall lands at 20 m/s (ratio 0: no damage), a 22.5 m fall at 30 m/s (ratio 0.5: 20 damage)
                var soft = Spawn(FlatWorld(), hs, new Vector3(0, 10, 0));
                var h1 = new Gameplay.Combat.Health(100f, 0.3f);
                soft.BindHealth(h1);
                Run(soft, 240, dt, i => default(PlayerInput));
                var hard = Spawn(FlatWorld(), hs, new Vector3(0, 22.5f, 0));
                var h2 = new Gameplay.Combat.Health(100f, 0.3f);
                hard.BindHealth(h2);
                float hardSpeed = 0f, hardDmg = -1f; int damagedEvents = 0; object src = new object();
                h2.OnDamaged += (d, o) => { damagedEvents++; src = o; };
                hard.Landed += (sp, dmg) => { hardSpeed = sp; hardDmg = dmg; };
                Run(hard, 400, dt, i => default(PlayerInput));
                float expected = MathHelper.Lerp(10f, 30f, (hardSpeed - 20f) / 20f);
                Check("fall damage reaches the player's Health (null source), none below the minimum speed",
                      h1.CurrentHealth == 100f && damagedEvents == 1 && src == null && Math.Abs(hardDmg - expected) < 1e-3f && Math.Abs((100f - h2.CurrentHealth) - hardDmg) < 1e-3f && hardDmg > 15f && hardDmg < 25f,
                      "10 m fall: health " + F(h1.CurrentHealth) + "; 22.5 m fall: speed " + F(hardSpeed) + ", damage " + F(hardDmg) + " (lerp " + F(expected) + "), health " + F(h2.CurrentHealth) + ", OnDamaged x" + damagedEvents);

                var kc = Spawn(FlatWorld(12f), hs, new Vector3(0, 0.001f, 0));
                var hk = new Gameplay.Combat.Health(100f, 0.3f);
                kc.BindHealth(hk);
                int died = 0, dieEvents = 0; float killDamage = 0f;
                kc.Died += () => died++;
                hk.OnDie += () => dieEvents++;
                hk.OnDamaged += (d, o) => killDamage = d;
                Run(kc, 600, dt, i => Walk());
                Check("kill height kills the player's Health (OnDamaged with MaxHealth, OnDie once) and the controller dies with it", hk.CurrentHealth == 0f && died == 1 && dieEvents == 1 && kc.IsDead && killDamage == 100f,
                      "health " + F(hk.CurrentHealth) + ", Died x" + died + ", OnDie x" + dieEvents + ", kill damage " + F(killDamage));
            }

            if (level != null)
            {
                RunLevelTests(scene, level, s);
            }
            else
            {
                Console.WriteLine("(no level collision world: level tests skipped)");
            }
            WeaponSelfTest.Run(scene, level);
            if (loaded != null)
            {
                EnemySelfTest.Run(loaded);
                EnemyAiSelfTest.Run(loaded);
                EnemyTurretSelfTest.Run(loaded);
                PlayerHealthSelfTest.Run(loaded);
                PickupSelfTest.Run(loaded);
                JetpackSelfTest.Run(loaded);
                EnemyFeedbackSelfTest.Run(loaded);
                GameFlowSelfTest.Run(loaded);
            }
            ParticleTrailSelfTest.Run();
            Console.WriteLine("Self-test (player + weapons): " + _pass + " passed, " + _fail + " failed");
            return _fail;
        }

        // ------------------------------------------------------------------ real level

        private static void RunLevelTests(SceneFile scene, CollisionWorld level, PlayerSettings s)
        {
            Console.WriteLine("Real level (" + level.TriangleCount + " triangles, bounds " + V(level.Bounds.Min) + " .. " + V(level.Bounds.Max) + ")");
            const float dt = 1f / 60f;

            var pc = new PlayerController(level, s);
            Vector3 afterRecovery = pc.Position;
            Console.WriteLine("  baked start " + V(s.StartPosition) + " -> after overlap recovery " + V(afterRecovery));
            Run(pc, 180, dt, i => default(PlayerInput));
            // Unity Play-mode reference: root settles to (Y 0.2509, Z -7.149 Unity) = (Y 0.2509, Z +7.149 here); X not captured.
            var unitySettled = new Vector3(s.StartPosition.X, 0.2509f, 7.149f);
            Check("spawn settles like Unity Play mode", pc.IsGrounded && Math.Abs(pc.Position.Y - 0.2509f) < 0.02f && Math.Abs(pc.Position.Z - 7.149f) < 0.05f,
                  "settled at " + V(pc.Position) + " vs Unity " + V(unitySettled) + " (X from Unity unknown, baked " + F(s.StartPosition.X) + "), grounded=" + pc.IsGrounded);
            Vector3 spawn = pc.Position;

            // camera convention: same Unity pose (root + 1.44, yaw, pitch) must give the same camera matrix as UnityConvert
            foreach (float[] pose in new[] { new float[] { -2f, 76.02191f }, new float[] { 8.2f, 169.5f } })
            {
                var cp = new PlayerController(level, s);
                cp.Respawn(new Vector3(-28.5f, 0.2509f, 7.149f), pose[1]);
                cp.Update(0f, new PlayerInput { LookY = pose[0] / s.RotationSpeed });
                Matrix expected = UnityConvert.CameraWorldFromUnityPose(new Vector3(cp.CameraPosition.X, cp.CameraPosition.Y, -cp.CameraPosition.Z), new Vector3(pose[0], pose[1], 0f));
                Matrix actual = cp.CameraWorld;
                float err = 0f;
                float[] a = { actual.M11, actual.M12, actual.M13, actual.M21, actual.M22, actual.M23, actual.M31, actual.M32, actual.M33, actual.M41, actual.M42, actual.M43 };
                float[] e = { expected.M11, expected.M12, expected.M13, expected.M21, expected.M22, expected.M23, expected.M31, expected.M32, expected.M33, expected.M41, expected.M42, expected.M43 };
                for (int i = 0; i < a.Length; i++) err = Math.Max(err, Math.Abs(a[i] - e[i]));
                Check("camera matrix pitch " + pose[0] + " yaw " + pose[1], err < 2e-4f && Math.Abs(cp.CameraPosition.Y - 1.6909f) < 0.002f,
                      "max element error vs UnityConvert pose " + err.ToString("0.000000") + ", camera y " + F(cp.CameraPosition.Y) + " (Unity 1.6909)");
            }
            if (Environment.GetEnvironmentVariable("SHOOTER_DUMP_SPAWN") != null)
            {
                var tv = new List<Vector3>();
                level.TrianglesNear(new BoundingBox(s.StartPosition - new Vector3(1.5f, 1f, 1.5f), s.StartPosition + new Vector3(1.5f, 3f, 1.5f)), tv);
                for (int i = 0; i < tv.Count; i += 3)
                {
                    Vector3 lo = Vector3.Min(tv[i], Vector3.Min(tv[i + 1], tv[i + 2])), hi = Vector3.Max(tv[i], Vector3.Max(tv[i + 1], tv[i + 2]));
                    if (hi.X > s.StartPosition.X - 1.5f && lo.X < s.StartPosition.X + 1.5f && hi.Z > s.StartPosition.Z - 1.5f && lo.Z < s.StartPosition.Z + 1.5f && hi.Y > 0f && lo.Y < 3f)
                        Console.WriteLine("    tri " + V(tv[i]) + " " + V(tv[i + 1]) + " " + V(tv[i + 2]));
                }
            }

            // 8 headings x sprint into whatever is there: must never leave the level or end up inside geometry.
            int badHeadings = 0;
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < 8; k++)
            {
                var p = new PlayerController(level, s);
                p.Respawn(s.StartPosition, k * 45f);
                float minY = 1e9f, worstDepth = 0f;
                Run(p, 60 * 10, dt, i => Walk(true), i =>
                {
                    minY = Math.Min(minY, p.Position.Y);
                    if (level.DeepestContact(p.Motor.BottomHemisphere(p.Position), p.Motor.TopHemisphere(p.Position, p.Height), p.Motor.Radius, out CollisionWorld.Contact c))
                    {
                        worstDepth = Math.Max(worstDepth, c.Depth);
                    }
                });
                bool inside = InsideBounds(level, p.Position);
                bool ok = inside && worstDepth < 0.02f && !p.IsDead;
                if (!ok) badHeadings++;
                sb.Append("    heading " + (k * 45) + ": end " + V(p.Position) + " minY " + F(minY) + " worst residual overlap " + F(worstDepth) + (ok ? "" : "  <-- BAD") + "\n");
            }
            Check("sprint 10 s in 8 headings stays inside the level", badHeadings == 0, badHeadings + " of 8 bad");
            Console.Write(sb.ToString());

            // fuzz: random input incl. jumps, crouch, big and small time steps
            foreach (float step in new[] { 1f / 60f, 1f / 30f, 1f / 15f })
            {
                int escapes = 0, deepest = 0, frames = 0, fellOff = 0; float worst = 0f; int deaths = 0;
                var timer = Stopwatch.StartNew();
                for (int seed = 1; seed <= 4; seed++)
                {
                    var rng = new Random(seed * 7919);
                    var p = new PlayerController(level, s);
                    float turn = 0f; bool sprint = false; Vector2 mv = new Vector2(0, 1);
                    Vector3 lastGrounded = p.Position;
                    int n = (int)(90f / step);
                    for (int i = 0; i < n; i++)
                    {
                        if (i % (int)Math.Max(1, 0.5f / step) == 0)
                        {
                            turn = (float)(rng.NextDouble() - 0.5) * 0.12f;
                            sprint = rng.Next(3) != 0;
                            mv = rng.Next(5) == 0 ? new Vector2((float)rng.NextDouble() - 0.5f, -0.3f) : new Vector2((float)(rng.NextDouble() - 0.5), 1f);
                        }
                        var inp = new PlayerInput { Move = mv, LookX = turn, SprintHeld = sprint, JumpDown = rng.Next(40) == 0, CrouchDown = rng.Next(150) == 0 };
                        p.Update(step, inp);
                        frames++;
                        if (p.IsGrounded) lastGrounded = p.Position;
                        if (p.Position.Y < level.Bounds.Min.Y - 1f && SolidUnder(level, p.Position) == 0) { fellOff++; break; }   // walked off an open edge into the void (the level has lower areas down to its bounds)
                        if ((p.IsGrounded && !InsideBounds(level, p.Position)) || p.Position.Y < level.Bounds.Min.Y - 1f) { escapes++; Console.WriteLine("    escape: seed " + seed + " frame " + i + " at " + V(p.Position) + " last grounded " + V(lastGrounded) + " floor triangles over the xz in y -9..1: " + SolidUnder(level, p.Position)); break; }
                        if (level.DeepestContact(p.Motor.BottomHemisphere(p.Position), p.Motor.TopHemisphere(p.Position, p.Height), p.Motor.Radius, out CollisionWorld.Contact c))
                        {
                            worst = Math.Max(worst, c.Depth);
                            if (c.Depth > 0.05f) deepest++;
                        }
                        if (p.IsDead) { deaths++; break; }
                    }
                }
                double perFrame = timer.Elapsed.TotalMilliseconds / Math.Max(1, frames);
                Check("random-input fuzz " + (1f / step).ToString("0") + " fps", escapes == 0 && deepest == 0 && deaths == 0,
                      frames + " frames in 4 runs: escaped/fell through " + escapes + " (edge falls into the void, not counted: " + fellOff + ")" + ", frames overlapping > 5 cm " + deepest + ", worst overlap " + F(worst) + ", deaths " + deaths + ", " + perFrame.ToString("0.000") + " ms/frame");
            }
        }

        private static int SolidUnder(CollisionWorld level, Vector3 p)
        {
            var tv = new List<Vector3>();
            level.TrianglesNear(new BoundingBox(new Vector3(p.X - 0.01f, -9f, p.Z - 0.01f), new Vector3(p.X + 0.01f, 1f, p.Z + 0.01f)), tv);
            int n = 0;
            for (int i = 0; i < tv.Count; i += 3)
            {
                Vector3 nn = Vector3.Cross(tv[i + 1] - tv[i], tv[i + 2] - tv[i]);
                if (nn.LengthSquared() < 1e-12f || Math.Abs(Vector3.Normalize(nn).Y) < 0.9f) continue;
                if (PointInTriXZ(p.X, p.Z, tv[i], tv[i + 1], tv[i + 2])) n++;
            }
            return n;
        }

        private static bool PointInTriXZ(float x, float z, Vector3 a, Vector3 b, Vector3 c)
        {
            float d1 = (x - b.X) * (a.Z - b.Z) - (a.X - b.X) * (z - b.Z);
            float d2 = (x - c.X) * (b.Z - c.Z) - (b.X - c.X) * (z - c.Z);
            float d3 = (x - a.X) * (c.Z - a.Z) - (c.X - a.X) * (z - a.Z);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }

        private static bool InsideBounds(CollisionWorld level, Vector3 p)
        {
            BoundingBox b = level.Bounds;
            return p.X > b.Min.X - 6f && p.X < b.Max.X + 6f && p.Z > b.Min.Z - 6f && p.Z < b.Max.Z + 6f && p.Y < b.Max.Y + 5f;
        }
    }
}
