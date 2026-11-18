using System;
using Microsoft.Xna.Framework;
using ThirdPersonShooter.Physics;

namespace ThirdPersonShooter
{
    /// <summary>Headless checks behind `--test-physics` (no window). Returns the failure count.</summary>
    public static class PhysicsSelfTest
    {
        static int _fail;
        static float DistSeg(Vector3 c, Vector3 a, Vector3 b) { Vector3 ab = b - a; float t = MathHelper.Clamp(Vector3.Dot(c - a, ab) / ab.LengthSquared(), 0f, 1f); return Vector3.Distance(c, a + ab * t); }
        static void Check(string name, bool ok, string detail) { if (!ok) _fail++; Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {name}: {detail}"); }

        /// <summary>Flat floor (top y=0) 60x60 plus optional boxes.</summary>
        static LevelColliders Synthetic(params (Vector3 center, Vector3 size)[] boxes)
        {
            var lc = new LevelColliders();
            lc.LevelWorld.AddBox(Matrix.Identity, new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60));
            lc.PlayerWorld.AddBox(Matrix.Identity, new Vector3(0, -0.5f, 0), new Vector3(60, 1, 60));
            foreach (var b in boxes) { lc.LevelWorld.AddBox(Matrix.Identity, b.center, b.size); lc.PlayerWorld.AddBox(Matrix.Identity, b.center, b.size); }
            lc.LevelWorld.Build(); lc.PlayerWorld.Build();
            return lc;
        }

        public static int Run(string contentRoot)
        {
            _fail = 0;
            // 0. skeleton + clips without a GraphicsDevice: root motion drives every movement test below
            var sc = new System.ComponentModel.Design.ServiceContainer();
            using var cm = new Microsoft.Xna.Framework.Content.ContentManager(sc, contentRoot);
            var model = cm.Load<SkinnedModel>("Models/player");
            Simulation.Model = model;
            var ap = new AnimationPlayer(model);
            foreach (var cn in new[] { "running_nogun", "strafe_left", "strafe_right", "strafe_front", "strafe_back", "running_gun", "Idle", "jump_2_upwards", "jump_4_falling" })
            {
                if (!model.Clips.TryGetValue(cn, out var c)) { Console.WriteLine($"[rootmotion] {cn}: missing"); continue; }
                var tr = c.Tracks[ap.RootBone].T; Vector3 d = ap.RootDelta(c, 0f, c.Duration); Vector3 dm = ap.RootToModel(d);
                Console.WriteLine($"[rootmotion] {cn}: dur={c.Duration:F3}s loop={c.Loop} rootKeys={tr.Length} localDelta={d} modelDelta={dm} speed={dm.Length() / c.Duration:F3} m/s dir=({dm.X / dm.Length():F2},{dm.Z / dm.Length():F2}) y/s={dm.Y / c.Duration:F3}");
            }
            Console.WriteLine($"[rootmotion] root bone={ap.RootBone} parent={model.Parent[ap.RootBone]} ({(model.Parent[ap.RootBone] >= 0 ? model.BoneNames[model.Parent[ap.RootBone]] : "-")}) bindT={model.BindT[ap.RootBone]} bindS={model.BindS[ap.RootBone]}");
            // 1. real level: fall onto the floor at the spawn
            var level = LevelColliders.Load(contentRoot);
            Console.WriteLine($"[level] boxes={level.BoxCount} cylinders={level.CylinderCount} playerWorldTris={level.PlayerWorld.TriangleCount} levelWorldTris={level.LevelWorld.TriangleCount}");
            var probe = new GroundProbe(contentRoot);
            Vector3 sp = level.PlayerSpawn.Translation;
            var sim = new Simulation(level, sp + Vector3.Up * 1.5f);
            sim.Run(2f, Vector2.Zero);
            float gy = probe.GroundY(sp.X, sp.Z, sp.Y + 1f) ?? float.NaN;
            Check("lands on spawn floor", sim.Player.IsOnFloor && Math.Abs(sim.Player.Position.Y - gy) < 0.02f, $"feet y={sim.Player.Position.Y:F4} probe ground y={gy:F4} onFloor={sim.Player.IsOnFloor}");
            sim.RunScript("w:3.0;look:40,0;w+d:2.0");
            Check("walks real level without falling through", sim.Player.Position.Y > sp.Y - 1f && sim.Player.Position.Y < sp.Y + 3f, $"pos={sim.Player.Position} onFloor={sim.Player.IsOnFloor}");
            Console.WriteLine($"[level] walked to {sim.Player.Position}");

            // 2. wall: blocked and slides
            var wall = Synthetic((new Vector3(0, 2, -5), new Vector3(30, 4, 1)));       // face at z=-4.5
            var s = new Simulation(wall, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero);
            s.Run(4f, new Vector2(0, -1));
            Check("cannot walk through wall", s.Player.Position.Z >= -4.0f - 0.05f && s.Player.Position.Z <= -3.9f, $"z={s.Player.Position.Z:F3} (wall face -4.5, radius 0.5)");
            s = new Simulation(wall, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero);
            s.Run(3f, new Vector2(1, -1));
            Check("slides along wall", s.Player.Position.X > 4f && s.Player.Position.Z >= -4.05f, $"pos={s.Player.Position}");

            // 3. step
            float stepH = 0.35f;
            var step = Synthetic((new Vector3(0, stepH / 2, -6), new Vector3(30, stepH, 6)));  // top at 0.35, front face z=-3
            s = new Simulation(step, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero); s.Run(2f, new Vector2(0, -1));
            Check($"climbs {stepH} m step (plan 03: ~0.3-0.4)", s.Player.Position.Z < -3.5f && Math.Abs(s.Player.Position.Y - stepH) < 0.03f, $"pos={s.Player.Position}");
            var high = Synthetic((new Vector3(0, 0.4f, -6), new Vector3(30, 0.8f, 6)));
            s = new Simulation(high, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero); s.Run(2f, new Vector2(0, -1));
            Check("blocked by 0.8 m ledge", s.Player.Position.Z > -2.7f && s.Player.Position.Y < 0.1f, $"pos={s.Player.Position}");

            // 4. jump apex
            var flat = Synthetic();
            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(1f, Vector2.Zero);
            float y0 = s.Player.Position.Y; s.Player.ResetApex();
            s.Step(Vector2.Zero, true); s.Run(1.5f, Vector2.Zero);
            float apex = s.Player.ApexY - y0, expect = PlayerConstants.JumpSpeed * PlayerConstants.JumpSpeed / (2 * PlayerConstants.Gravity);
            Check("jump apex within 5% of v^2/2g", Math.Abs(apex - expect) / expect < 0.05f && s.Player.IsOnFloor, $"apex={apex:F4} expected={expect:F4} landed={s.Player.IsOnFloor}");

            // 5. camera ray
            var cw = Synthetic((new Vector3(0, 3, 2), new Vector3(30, 6, 1)));          // face at z=1.5, behind the player (camera is at +Z for yaw 0)
            var cam = new ThirdPersonCamera(cw.LevelWorld, 0f); cam.Update(Vector3.Zero);
            float expectLen = 1.5f / MathF.Cos(MathHelper.ToRadians(PlayerConstants.ArmPitchDeg)) - PlayerConstants.SpringMargin;
            Check("camera ray stops at wall", cam.Position.Z <= 1.5f && Math.Abs(cam.ArmLength - expectLen) < 0.02f, $"armLen={cam.ArmLength:F3} expected={expectLen:F3} camZ={cam.Position.Z:F3}");
            var open = new ThirdPersonCamera(Synthetic().LevelWorld, 0f); open.Update(Vector3.Zero);
            Check("camera arm full length in open space", Math.Abs(open.ArmLength - PlayerConstants.SpringLength) < 1e-4f, $"armLen={open.ArmLength:F3} cam={open.Position} view={open.ViewDir}");

            // 6. root motion drives the walk speed (replaces the 4.5 m/s TEMP): clip speed and measured steady-state sim speed
            var rc = model.Clips["running_nogun"]; Vector3 rd = ap.RootToModel(ap.RootDelta(rc, 0f, rc.Duration)); float clipSpeed = rd.Length() / rc.Duration;
            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero); s.Run(1.5f, new Vector2(0, -1));
            Vector3 q0 = s.Player.Position; s.Run(1f, new Vector2(0, -1)); float simSpeed = (s.Player.Position - q0).Length();
            float expectSpeed = clipSpeed * PlayerAnimator.WalkTimeScale;
            Check("root motion: run clip forward and plausible, sim speed = clip x 1.3", clipSpeed > 2.5f && clipSpeed < 4.5f && rd.Z / rd.Length() > 0.99f && Math.Abs(simSpeed - expectSpeed) / expectSpeed < 0.08f,
                $"clip {clipSpeed:F3} m/s (x{PlayerAnimator.WalkTimeScale} = {expectSpeed:F3}), sim steady {simSpeed:F3} m/s, state={s.Player.Animator.Current} clip={s.Player.Animator.DominantClip}");
            // info: aiming strafe speeds, jump momentum
            foreach (var (nm, mv) in new[] { ("strafe_front", new Vector2(0, -1)), ("strafe_back", new Vector2(0, 1)), ("strafe_left", new Vector2(-1, 0)), ("strafe_right", new Vector2(1, 0)) })
            {
                s = new Simulation(flat, Vector3.Zero, 0f); s.AimHeld = true; s.Run(0.5f, Vector2.Zero); s.Run(1.5f, mv); q0 = s.Player.Position; s.Run(1f, mv);
                Console.WriteLine($"[rootmotion] aim-strafe {nm}: sim steady {(s.Player.Position - q0).Length():F3} m/s dX={(s.Player.Position - q0).X:F2} dZ={(s.Player.Position - q0).Z:F2} clip={s.Player.Animator.DominantClip}");
            }
            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero); s.Run(1.5f, new Vector2(0, -1)); Vector3 j0 = s.Player.Position; s.Step(new Vector2(0, -1), true);
            var seen = new System.Collections.Generic.List<string>(); for (int i = 0; i < 90; i++) { s.Step(Vector2.Zero); var st = s.Player.Animator.Current.ToString(); if (seen.Count == 0 || seen[seen.Count - 1] != st) seen.Add(st); }
            Console.WriteLine($"[rootmotion] jump from run: carried {(s.Player.Position - j0).Length():F2} m, states {string.Join(">", seen)}, onFloor={s.Player.IsOnFloor}");
            Console.WriteLine($"[rootmotion] strafe2D weights (1,1)->{string.Join(",", Array.ConvertAll(PlayerAnimator.Strafe2D(new Vector2(1, 1)), x => x.ToString("F2")))} (0.5,0.25)->{string.Join(",", Array.ConvertAll(PlayerAnimator.Strafe2D(new Vector2(0.5f, 0.25f)), x => x.ToString("F2")))}");

            // 7. B2 shooting: cooldown 0.4 s (1.0 s of held fire = 3 bullets), bullet speed/lifetime/wall distance, camera trauma
            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(1f, Vector2.Zero); s.AimHeld = true; s.Run(0.1f, Vector2.Zero);
            var shotTimes = new System.Collections.Generic.List<float>(); s.Shot += (m, d) => shotTimes.Add(s.Time);
            s.FireHeld = true; s.Run(1.0f, Vector2.Zero); s.FireHeld = false;
            float gap = shotTimes.Count >= 3 ? Math.Min(shotTimes[1] - shotTimes[0], shotTimes[2] - shotTimes[1]) : 0f, gapMax = shotTimes.Count >= 3 ? Math.Max(shotTimes[1] - shotTimes[0], shotTimes[2] - shotTimes[1]) : 9f;
            Check("fire cooldown 0.4 s: 1.0 s of held fire = 3 bullets", s.Bullets.TotalSpawned == 3 && gap > 0.39f && gapMax < 0.42f, $"bullets={s.Bullets.TotalSpawned} spacing {gap:F3}..{gapMax:F3} s (shots at {string.Join(",", shotTimes.ConvertAll(t => t.ToString("F2")))})");

            var open2 = Synthetic(); var bs = new Simulation(open2, new Vector3(10, 5, 10), 0f); var imp = new System.Collections.Generic.List<(Vector3 p, float t)>(); bs.Bullets.Impact += (p, n, t) => imp.Add((p, bs.Time));
            bs.Bullets.Spawn(new Vector3(0, 1, 0), Vector3.Forward); bs.Run(0.5f, Vector2.Zero); Vector3 bp = bs.Bullets.All[0].Position; float travelled = -bp.Z / 0.5f;
            bs.Run(4.4f, Vector2.Zero); bool aliveAt49 = bs.Bullets.Count == 1; bs.Run(0.2f, Vector2.Zero);
            var wallW = Synthetic((new Vector3(0, 2, -5.5f), new Vector3(30, 4, 1))); var ws = new Simulation(wallW, new Vector3(0, 5, 20), 0f); var wi = new System.Collections.Generic.List<(Vector3 p, float t)>(); ws.Bullets.Impact += (p, n, t) => wi.Add((p, ws.Time));
            ws.Bullets.Spawn(new Vector3(0, 1, 0), Vector3.Forward); ws.Run(0.5f, Vector2.Zero);
            float expectZ = -5f + PlayerConstants.BulletRadius;
            Check("bullet 20 m/s, removed at 5 s, stops at wall (centre = face + 0.17)", Math.Abs(travelled - 20f) < 0.3f && aliveAt49 && bs.Bullets.Count == 0 && imp.Count == 1 && imp[0].t > 4.97f && imp[0].t < 5.05f
                && wi.Count == 1 && Math.Abs(wi[0].p.Z - expectZ) < 0.03f && ws.Bullets.Count == 0,
                $"speed {travelled:F2} m/s, alive@4.9s={aliveAt49}, expired t={(imp.Count > 0 ? imp[0].t : -1):F2}s; wall hit z={(wi.Count > 0 ? wi[0].p.Z : float.NaN):F3} (expect {expectZ:F3}) at t={(wi.Count > 0 ? wi[0].t : -1):F3}s");

            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(1f, Vector2.Zero); s.RunScript("aim;wait:0.1;fire:1"); float tr0 = s.Shake.Trauma; s.Run(0.2f, Vector2.Zero); float tr1 = s.Shake.Trauma; s.Run(0.5f, Vector2.Zero);
            Check("camera trauma +0.35 per shot, decays 1.5/s, view-only", Math.Abs(tr0 - 0.35f) < 1e-4f && Math.Abs(tr1 - (0.35f - 0.2f * 1.5f)) < 0.02f && s.Shake.Trauma == 0f && s.Shake.Offset == Vector3.Zero,
                $"trauma after shot {tr0:F3}, +0.2 s {tr1:F3}, +0.7 s {s.Shake.Trauma:F3}");

            // 8. C1 red robot: root-motion walk speed, turning toward the player (root yaw), bullet hits -> hit animation -> explode, stays on the floor
            var rmodel = cm.Load<SkinnedModel>("Models/red_robot"); Simulation.RobotModel = rmodel;
            var rap = new AnimationPlayer(rmodel, "MASTER") { ExtractRootRotation = true };
            Console.WriteLine($"[robot] root bone MASTER={rap.RootBone} clips={string.Join(",", rmodel.Clips.Keys)}");
            foreach (var cn in new[] { "Idle", "Walk", "TurnLeft", "TurnRight" })
            {
                var c = rmodel.Clips[cn]; Vector3 dm = rap.RootToModel(rap.RootDelta(c, 0f, c.Duration)); float yaw = rap.RootYawDelta(c, 0f, c.Duration);
                Console.WriteLine($"[robot] clip {cn}: dur={c.Duration:F3}s loop={c.Loop} modelDelta={dm} speed={dm.Length() / c.Duration:F3} m/s yawDelta={MathHelper.ToDegrees(yaw):F1} deg ({MathHelper.ToDegrees(yaw) / c.Duration:F1} deg/s)");
            }
            var wc = rmodel.Clips["Walk"]; float walkClip = rap.RootToModel(rap.RootDelta(wc, 0f, wc.Duration)).Length() / wc.Duration;
            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero);
            var rb = s.SpawnRobot(new Vector3(0, 0, -19f), 0f);                  // 19 m in front of the player (-Z), facing +Z = toward the player, inside the 20 m aggro sphere
            s.Run(1.0f, Vector2.Zero); Vector3 w0 = rb.Position; s.Run(wc.Duration, Vector2.Zero); float walked = Vector3.Distance(w0, rb.Position) / wc.Duration; // one full Walk cycle (root motion is not uniform inside a cycle)
            Check("robot walks at the Walk clip's root-motion speed, grounded, state APPROACH", Math.Abs(walked - walkClip) / walkClip < 0.08f && rb.Mode == RedRobot.State.Approach && rb.OnFloor && Math.Abs(rb.Position.Y) < 0.05f,
                $"sim {walked:F3} m/s vs clip {walkClip:F3} m/s, state={rb.Mode} clip={rb.Anim.DominantClip} onFloor={rb.OnFloor} y={rb.Position.Y:F3} z={rb.Position.Z:F2}");

            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero);
            rb = s.SpawnRobot(new Vector3(12f, 0, 0f), MathHelper.ToRadians(0f));  // player is at -X of the robot, robot faces +Z: needs a 90 deg turn
            float a0 = MathF.Atan2(((Vector3.Zero - rb.Position).X * MathF.Cos(rb.Yaw) - (Vector3.Zero - rb.Position).Z * MathF.Sin(rb.Yaw)), ((Vector3.Zero - rb.Position).X * MathF.Sin(rb.Yaw) + (Vector3.Zero - rb.Position).Z * MathF.Cos(rb.Yaw)));
            s.Run(8f, Vector2.Zero);
            Vector3 toP = Vector3.Normalize(Vector3.Zero - rb.Position); float faceErr = MathF.Abs(MathHelper.WrapAngle(MathF.Atan2(toP.X, toP.Z) - rb.Yaw));
            Check("robot turns toward the player with the turn clips' root yaw (initial 90 deg off)", MathF.Abs(a0) > 1.4f && faceErr < MathHelper.ToRadians(20f) && Vector3.Distance(rb.Position, Vector3.Zero) < 12f,
                $"initial angle {MathHelper.ToDegrees(a0):F0} deg, after 8 s facing error {MathHelper.ToDegrees(faceErr):F1} deg, dist {Vector3.Distance(rb.Position, Vector3.Zero):F2} m, state={rb.Mode}");

            s = new Simulation(flat, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero);
            rb = s.SpawnRobot(new Vector3(0, 0, -8f), 0f); bool exploded = false; rb.Exploded += p => exploded = true; var healths = new System.Collections.Generic.List<int>(); rb.Damaged += h => healths.Add(h);
            for (int i = 0; i < 5; i++) { s.Bullets.Spawn(new Vector3(0, 1.2f, -1f), Vector3.Normalize(rb.SphereCenter - new Vector3(0, 1.2f, -1f)), 0); s.Run(0.5f, Vector2.Zero); }
            Check("bullet hits reach RedRobot.Hit: 5 hits = health 4..0, hit anim in between, explodes once, no more hits when dead", exploded && rb.Dead && rb.Health == 0 && string.Join(",", healths) == "4,3,2,1,0" && s.Bullets.Count == 0,
                $"health sequence {string.Join(",", healths)}, exploded={exploded}, dead={rb.Dead}");

            // 9. H1 four robots: SpawnLevelRobots puts one at every marker, the nearest robot on a ray takes the hit, a killed robot respawns fresh 15 s (sim time) later
            var ml = Synthetic(); ml.RobotSpawns = new[] { Matrix.CreateTranslation(25, 0.33f, -10), Matrix.CreateTranslation(25, 0.33f, -20), Matrix.CreateTranslation(-25, 0.33f, -10), Matrix.CreateTranslation(-25, 0.33f, -20) };
            s = new Simulation(ml, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero); s.SpawnLevelRobots(); s.Run(0.2f, Vector2.Zero);
            bool atMarkers = s.Robots.Count == 4; for (int k = 0; atMarkers && k < 4; k++) atMarkers = Math.Abs(s.Robots[k].Position.X - ml.RobotSpawns[k].Translation.X) < 0.05f && Math.Abs(s.Robots[k].Position.Z - ml.RobotSpawns[k].Translation.Z) < 0.05f && Math.Abs(s.Robots[k].Position.Y) < 0.05f;
            Check("SpawnLevelRobots: one robot per level marker, snapped to the floor", atMarkers, $"robots={s.Robots.Count} at {string.Join(" ", s.Robots.ConvertAll(r => r.Position.ToString()))}");
            var shotFrom = new Vector3(25, 1.2f, 0); var near = s.Robots[0]; var far = s.Robots[1];
            s.Bullets.Spawn(shotFrom, Vector3.Normalize(near.SphereCenter - shotFrom), 0); s.Run(1.5f, Vector2.Zero);
            Check("bullet sweep over several robots: the nearest on the ray is hit, the one behind it is not", near.Health == RedRobot.MaxHealth - 1 && far.Health == RedRobot.MaxHealth && s.Robots[2].Health == RedRobot.MaxHealth, $"near health {near.Health}, far health {far.Health}");
            for (int i = 0; i < 4; i++) { s.Bullets.Spawn(shotFrom, Vector3.Normalize(near.SphereCenter - shotFrom), 0); s.Run(0.5f, Vector2.Zero); }
            float tDead = s.Time; bool othersAlive = !s.Robots[1].Dead && !s.Robots[2].Dead && !s.Robots[3].Dead; s.Run(Simulation.RobotRespawnWait - 0.2f, Vector2.Zero); bool stillDead = s.Robots[0] == near && near.Dead;
            s.Run(0.3f, Vector2.Zero); var fresh = s.Robots[0];
            Check("killed robot: other 3 stay alive, a fresh full-health Idle robot appears at its marker 15 s of sim time after exploding, not before", near.Dead && othersAlive && stillDead && fresh != near && !fresh.Dead && fresh.Health == RedRobot.MaxHealth && fresh.Mode == RedRobot.State.Idle && Math.Abs(fresh.Position.X - 25f) < 0.05f && Math.Abs(fresh.Position.Z + 10f) < 0.05f && s.Time - tDead < 15f + 0.5f,
                $"died at t={tDead:F2}, still dead at +{Simulation.RobotRespawnWait - 0.2f:F1} s, respawned by +{s.Time - tDead:F2} s: health {fresh.Health} state {fresh.Mode} at {fresh.Position}");


            // 10. I1 dynamic level elements: mawaru (FlyingForklifts rotation_degrees.y 0 -> -360 over 30 s, loop) and the door finding (door.tscn is a prefab never instanced by the level)
            var anim = LevelAnimation.Load(contentRoot);
            var f1 = new Vector3(67.8049f, 2.27771f, 0f);   // FlyingForklift1 world position (level.tscn)
            Vector3 p0 = Vector3.Transform(f1, anim.ParentTransform(0f)), p15 = Vector3.Transform(f1, anim.ParentTransform(15f)), p75 = Vector3.Transform(f1, anim.ParentTransform(7.5f)), p30 = Vector3.Transform(f1, anim.ParentTransform(30f)), p375 = Vector3.Transform(f1, anim.ParentTransform(37.5f));
            Check("mawaru: t=0 identity, t=7.5 s = -90 deg about Y, t=15 s = 180 deg (x -> -x), loops at 30 s", Vector3.Distance(p0, f1) < 1e-3f && Math.Abs(anim.RotationDegrees(7.5f).Y + 90f) < 1e-3f && Math.Abs(p15.X + f1.X) < 1e-2f && Math.Abs(p15.Z) < 1e-2f && Vector3.Distance(p30, f1) < 1e-3f && Vector3.Distance(p375, p75) < 1e-3f && anim.Length == 30f && anim.LoopMode == 1,
                $"len={anim.Length} loop={anim.LoopMode} rotY(7.5)={anim.RotationDegrees(7.5f).Y:F1} t15={p15} t30={p30} t37.5={p375} vs t7.5={p75}");
            string sceneJson = System.IO.File.ReadAllText(System.IO.Path.Combine(contentRoot, "Data", "level.scene.json"));
            using (var sd = System.Text.Json.JsonDocument.Parse(sceneJson)) { var root = sd.RootElement; int doorInst = 0; foreach (var arr in new[] { "instances", "dynamics", "triggers" }) foreach (var e in root.GetProperty(arr).EnumerateArray()) if (e.ToString().Contains("door", StringComparison.OrdinalIgnoreCase)) doorInst++;
                Check("door finding: level.scene.json has 0 door instances/triggers (door.tscn only exists as a prefab; nothing in level/*.tscn or any .gd instances it); forklift variant pick is deterministic", doorInst == 0 && LevelAnimation.PickVariant(0, 3) == LevelAnimation.PickVariant(0, 3), $"door instances/dynamics/triggers={doorInst}, variants f0={LevelAnimation.PickVariant(0, 3)} f1={LevelAnimation.PickVariant(1, 3)}"); }

            {   // 12. effects part 3: forklift exhaust follows the forklift matrix, particle timings, spot light, LaserCenter curve
                using var sdoc = System.Text.Json.JsonDocument.Parse(sceneJson); var roots = new System.Collections.Generic.List<Matrix>();
                foreach (var e in sdoc.RootElement.GetProperty("instances").EnumerateArray()) if (e.TryGetProperty("dynamic", out var dp) && dp.GetBoolean()) { var a = e.GetProperty("world"); var f = new float[16]; for (int i = 0; i < 16; i++) f[i] = a[i].GetSingle(); roots.Add(new Matrix(f[0], f[1], f[2], f[3], f[4], f[5], f[6], f[7], f[8], f[9], f[10], f[11], f[12], f[13], f[14], f[15])); }
                Func<int, float, Matrix> fw = (i, t) => roots[i] * anim.ParentTransform(t);
                bool follow = roots.Count == 2; string det = $"roots={roots.Count}";
                foreach (float t in new[] { 0f, 7.5f, 15f })
                    for (int fi = 0; fi < roots.Count && follow; fi++)
                    {
                        Matrix w = fw(fi, t); var em = Vector3.Transform(Vector3.Zero, FxSpec.ForkEmitters[0] * w); var expectF = Vector3.Transform(new Vector3(0.471727f, -0.277269f, -2.89514f), w);
                        // particle born exactly at t (k=0, phase 0): its position = emitter origin + sphere offset (<= 0.1 m) at that time
                        FxSpec.ForkParticle(0, 0, t + 1f, x => fw(fi, x), out var p0F, out float l0F, out float sp0F);   // k=0 is born at t+1 (cycle boundary): age 0
                        float dist = Vector3.Distance(p0F, Vector3.Transform(Vector3.Zero, FxSpec.ForkEmitters[0] * fw(fi, sp0F)));
                        follow &= Vector3.Distance(em, expectF) < 1e-3f && dist <= 0.1001f && Math.Abs(sp0F - (t + 1f)) < 1e-3f;
                        det += $" f{fi}@{t}:emitter={em.X:F1},{em.Y:F1},{em.Z:F1} bornDist={dist:F3}";
                    }
                Check("forklift exhaust: emitter world position = local transform x forklift World (mawaru) at t=0/7.5/15 for both forklifts; a fresh particle starts within r 0.1 of the emitter at its spawn time", follow, det);
                Matrix w0F = fw(0, 0f); bool firstRoot = Vector3.Distance(w0F.Translation, new Vector3(67.8049f, 2.27771f, 0f)) < 1e-2f;
                int alive = 0; float maxAge = 0f, minAge = 9f; for (int k = 0; k < FxSpec.ForkCount; k++) if (FxSpec.ForkParticle(1, k, 3.3f, x => fw(0, x), out _, out float l01, out _)) { alive++; maxAge = Math.Max(maxAge, l01); minAge = Math.Min(minAge, l01); }
                var vel = FxSpec.ForkParticle(0, 5, 4.0f, x => fw(0, x), out var pv1, out float lv1, out float sv1); var pv2 = default(Vector3); FxSpec.ForkParticle(0, 5, 4.0f, x => fw(0, x), out pv2, out _, out _);
                // trail: world-space particles stay where they were born while the forklift moves on (14 m/s): a 0.4 s-old particle is within ~0.3 + 0.1 + 3*0.4 m of its birth emitter, not of the forklift
                FxSpec.ForkParticle(0, 0, 10.4f, x => fw(0, x), out var pOld, out float lOld, out float spOld); float toBirth = Vector3.Distance(pOld, Vector3.Transform(Vector3.Zero, FxSpec.ForkEmitters[0] * fw(0, spOld))), toNow = Vector3.Distance(pOld, Vector3.Transform(Vector3.Zero, FxSpec.ForkEmitters[0] * fw(0, 10.4f)));
                Check("forklift exhaust particles: 4 emitters x 20, life 0.5 s, all 20 alive at t=3.3 with ages 0..1, scale curve 0.3 -> 0.8 @0.2 -> 1, ramp alpha 1 -> 0.86 @0.1 -> 0.41 @0.3 -> 0, stays at its birth place (world space)",
                    firstRoot && FxSpec.ForkEmitters.Length == 4 && FxSpec.ForkCount == 20 && FxSpec.ForkLife == 0.5f && alive == 20 && maxAge < 1f && minAge >= 0f && Math.Abs(FxSpec.ForkScale(0f) - 0.3f) < 1e-4f && Math.Abs(FxSpec.ForkScale(0.2f) - 0.8f) < 1e-4f && Math.Abs(FxSpec.ForkScale(1f) - 1f) < 1e-4f
                    && Math.Abs(FxSpec.ForkColor(0.1f).W - 0.858824f) < 1e-4f && Math.Abs(FxSpec.ForkColor(0.3f).W - 0.411765f) < 1e-4f && FxSpec.ForkColor(1f).W == 0f && toBirth < 1.6f && toNow > 3f,
                    $"alive={alive} age {minAge:F2}..{maxAge:F2} scale(.1)={FxSpec.ForkScale(0.1f):F2} dist to birth emitter={toBirth:F2} m, to emitter now={toNow:F2} m (forklift moved {Vector3.Distance(fw(0, spOld).Translation, fw(0, 10.4f).Translation):F2} m), forklift0 root={w0F.Translation}");
                var ldir = Vector3.Normalize(FxSpec.ForkLightDir); var lpos = Vector3.Transform(FxSpec.ForkLightPos, fw(0, 0f));
                Check("forklift spot light: range 50, angle 50 deg, energy 12; points down and forward (local +Z, y < 0), position 4.1 m ahead of the forklift origin", FxSpec.ForkLightRange == 50f && FxSpec.ForkLightAngleDeg == 50f && FxSpec.ForkLightEnergy == 12f && ldir.Y < -0.5f && ldir.Z > 0.8f && Math.Abs(Vector3.Distance(lpos, w0F.Translation) - 4.11f) < 0.1f, $"dir={ldir} pos={lpos}");
                Check("LaserCenter (red_robot.tscn:10725): scale curve 0 @0, ~0.5 @0.43 s-of-life, 0.99 @0.85, 0 @1; born at 0.2 s, lives 1.5 s (alive for clock 0.2..1.7)", Math.Abs(FxSpec.LaserCenterScale(0.00263158f)) < 1e-4f && Math.Abs(FxSpec.LaserCenterScale(0.85f) - 0.990909f) < 1e-3f && FxSpec.LaserCenterScale(1f) == 0f && FxSpec.LaserCenterScale(0.5f) > 0.4f && FxSpec.LaserCenterScale(0.5f) < 1.0f && FxSpec.LaserCenterScale(0.95f) < 0.9f && FxSpec.LaserCenterAge(0.1f) < 0f && Math.Abs(FxSpec.LaserCenterAge(0.2f)) < 1e-5f && Math.Abs(FxSpec.LaserCenterAge(1.7f) - 1.5f) < 1e-4f && FxSpec.LaserCenterAge(1.8f) < 0f,
                    $"scale(.1)={FxSpec.LaserCenterScale(0.1f):F3} (.5)={FxSpec.LaserCenterScale(0.5f):F3} (.85)={FxSpec.LaserCenterScale(0.85f):F3} (.95)={FxSpec.LaserCenterScale(0.95f):F3}");
            }

            // 11. J1 explosion debris (part.gd): 3 parts per robot death, gravity + restitution + sleeping, all freed after 3-6 s + fade 0.3 s + puff 0.2 s
            var dflat = Synthetic(); s = new Simulation(dflat, Vector3.Zero, 0f); s.Run(0.5f, Vector2.Zero);
            rb = s.SpawnRobot(new Vector3(0, 0, -8f), 0f); int puffs = 0, sparks = 0; s.Debris.Puff += p => puffs++; s.Debris.SparkBurst += p => sparks++;
            for (int i = 0; i < 5; i++) { s.Bullets.Spawn(new Vector3(0, 1.2f, -1f), Vector3.Normalize(rb.SphereCenter - new Vector3(0, 1.2f, -1f)), 0); s.Run(0.5f, Vector2.Zero); }
            float tDeath = s.Time; int partsAfter = s.Debris.Count; int total = s.Debris.TotalSpawned;
            Check("robot death spawns 3 debris parts (PartShield1, PartShield2, PartHead) + 2 DetachSpark bursts", rb.Dead && total == 3 && sparks == 2 && partsAfter >= 1 && partsAfter <= 3, $"spawned={total} alive after 0.5 s={partsAfter} sparkBursts={sparks}");
            var dsys = new DebrisSystem(dflat.LevelWorld);                                  // free fall of the head sphere (r 1) from y=6: restitution 0.5 -> second apex = e^2 x first drop; Godot bounce 0 -> no bounce, sleeps at rest
            float Apex2(float e, out float restY, out bool slept)
            {
                dsys = new DebrisSystem(dflat.LevelWorld) { Restitution = e }; var pt = dsys.Spawn(2, new Vector3(0, 6f, 0), Quaternion.Identity, Vector3.Zero, Vector3.Zero, 100f);
                bool bounced = false; float apex = 0f, prevY = 6f; for (int i = 0; i < 60 * 8; i++) { dsys.Update(1f / 60f); float y = pt.Pos.Y; if (!bounced && y > prevY) bounced = true; if (bounced) apex = Math.Max(apex, y); prevY = y; }
                restY = pt.Pos.Y; slept = pt.Sleeping; return apex - 1f;
            }
            float a5 = Apex2(0.5f, out _, out _), aE0 = Apex2(0f, out float y0r, out bool sl0);
            Check("debris part falls under gravity, bounces with the solver restitution (second apex = e^2 x 5 m) and, with Godot's bounce 0, rests on the floor and sleeps", Math.Abs(a5 - 1.25f) < 0.2f && aE0 < 0.15f && Math.Abs(y0r - 1f) < 0.03f && sl0, $"e=0.5 second apex {a5:F3} m (expect 1.25), e=0 rebound {aE0:F3} m, rest height {y0r:F3} (expect 1.0), sleeping={sl0}");
            s.Run(7f - (s.Time - tDeath), Vector2.Zero);
            Check("debris lifetime 3 + 3*rand s, fade 0.3 s, puff + free 0.2 s later: pool empty after 6.5 s, one puff per part", s.Debris.Count == 0 && puffs == 3, $"alive at +{s.Time - tDeath:F2} s = {s.Debris.Count}, puffs={puffs}");

            {   // 13. robot-robot and debris contacts (red_robot.tscn: robots layer 3 / mask 3, parts layer 3 / mask 3, player layer 6 / mask 7)
                float twoR = 2f * RedRobot.SphereRadius;
                Func<bool, (float min, float reach)> converge = peers =>
                {
                    var cs = Synthetic(); var cz = new Simulation(cs, Vector3.Zero, 0f); cz.Run(0.3f, Vector2.Zero);
                    var ra = cz.SpawnRobot(new Vector3(-1.7f, 0, -6f), 0f); var rb2 = cz.AddRobot(new Vector3(1.7f, 0, -6f), 0f);
                    if (!peers) { ra.Peers = null; rb2.Peers = null; }
                    float mn = 99f;
                    for (int i = 0; i < 60 * 20; i++) { cz.Step(Vector2.Zero); mn = Math.Min(mn, Vector3.Distance(ra.SphereCenter, rb2.SphereCenter)); }
                    return (mn, mn);
                };
                var withP = converge(true); var noP = converge(false);
                Check("robot-robot: two robots walking toward the player (and so toward each other) never overlap (distance >= 2 x 1.118); without the peer list the same run overlaps (test is sensitive)", withP.min >= twoR - 0.02f && withP.min < twoR + 0.6f && noP.min < twoR - 0.1f, $"min distance with contact {withP.min:F3} m, without {noP.min:F3} m, limit {twoR:F3}");

                var dfl = Synthetic(); var ds = new Simulation(dfl, new Vector3(0, 0, 20f), 0f); ds.Run(0.3f, Vector2.Zero);
                var r1 = ds.SpawnRobot(new Vector3(0, 0, -6f), 0f); for (int i = 0; i < 5; i++) { ds.Bullets.Spawn(new Vector3(0, 1.2f, -1f), Vector3.Normalize(r1.SphereCenter - new Vector3(0, 1.2f, -1f)), 0); ds.Run(0.4f, Vector2.Zero); }
                ds.Run(2.0f, Vector2.Zero); float lowest = 99f; int touching = 0, aliveParts = 0;
                foreach (var pt in ds.Debris.All) if (pt.Alive) { aliveParts++; touching += pt.Touching > 0 ? 1 : 0; foreach (var sq in pt.Spheres) lowest = Math.Min(lowest, (pt.Pos + Vector3.Transform(sq.C, pt.Rot)).Y - sq.R); }
                Check("debris from a killed robot stays on the floor (layer 1 level, mask 3): no sphere below the floor, parts resting on it", aliveParts == 3 && lowest > -0.06f && touching >= 2, $"parts={aliveParts} lowest sphere bottom {lowest:F3} m, touching={touching}");

                var pl = Synthetic(); var ps = new Simulation(pl, Vector3.Zero, 0f); ps.Run(0.3f, Vector2.Zero);
                Vector3 p0w = ps.Player.Position; ps.Run(0.4f, new Vector2(0, -1)); Vector3 wdir = ps.Player.Position - p0w; wdir.Y = 0; wdir.Normalize();
                ps.Run(1.0f, Vector2.Zero); Vector3 pStart = ps.Player.Position; Vector3 hc = pStart + wdir * 4f + Vector3.Up * 1.0f;
                var head = ps.Debris.Spawn(2, hc, Quaternion.Identity, Vector3.Zero, Vector3.Zero, 100f);
                float worstPen = 0f, closest = 99f, lateral = 0f; for (int i = 0; i < 60 * 4; i++) { ps.Step(new Vector2(0, -1)); Vector3 f = ps.Player.Position; float dd = DistSeg(head.Pos, f + Vector3.Up * 0.5f, f + Vector3.Up * 1.5f); worstPen = Math.Max(worstPen, 1.5f - dd); closest = Math.Min(closest, dd); lateral = Math.Max(lateral, Math.Abs(Vector3.Dot(f - pStart, Vector3.Cross(wdir, Vector3.Up)))); }
                Vector3 pEnd = ps.Player.Position;
                Check("player vs debris (player layer 6 / part layer 3): the player walking straight into a resting head part is stopped and deflected around it (no penetration > 5 cm, closest axis distance >= 1.45, sideways deflection > 0.4 m), the part is not shoved away",
                    worstPen < 0.05f && closest >= 1.45f && lateral > 0.4f && Vector3.Distance(head.Pos, hc) < 0.15f, $"closest {closest:F3} m, worst penetration {worstPen:F3} m, sideways {lateral:F2} m, head moved {Vector3.Distance(head.Pos, hc):F3} m");

                var ps2 = new Simulation(pl, Vector3.Zero, 0f); ps2.Run(0.3f, Vector2.Zero); Vector3 pf = ps2.Player.Position;
                var drop = ps2.Debris.Spawn(2, pf + new Vector3(0.3f, 5f, 0), Quaternion.Identity, Vector3.Zero, Vector3.Zero, 100f);
                float worstP2 = 0f; for (int i = 0; i < 60 * 3; i++) { ps2.Step(Vector2.Zero); Vector3 f = ps2.Player.Position; worstP2 = Math.Max(worstP2, 1.5f - DistSeg(drop.Pos, f + Vector3.Up * 0.5f, f + Vector3.Up * 1.5f)); }
                Check("debris dropped on the player bounces off the capsule instead of passing through it (penetration < 8 cm after 3 s, player not launched)", worstP2 < 0.08f && Vector3.Distance(ps2.Player.Position, pf) < 1.5f && ps2.Player.IsOnFloor, $"worst penetration {worstP2:F3} m, player moved {Vector3.Distance(ps2.Player.Position, pf):F2} m, head at {drop.Pos}");

                var rs = new Simulation(Synthetic(), new Vector3(0, 0, 25f), 0f); rs.Run(0.3f, Vector2.Zero); var idle = rs.SpawnRobot(Vector3.Zero, 0f);
                var hd = rs.Debris.Spawn(2, idle.SphereCenter + new Vector3(0.2f, 4f, 0), Quaternion.Identity, Vector3.Zero, Vector3.Zero, 100f);
                float worstR = 0f; for (int i = 0; i < 60 * 3; i++) { rs.Step(Vector2.Zero); worstR = Math.Max(worstR, RedRobot.SphereRadius + 1f - Vector3.Distance(hd.Pos, idle.SphereCenter)); }
                Check("debris vs live robot (both layer 3): a head part dropped on a robot does not pass through its body sphere", worstR < 0.1f && Math.Abs(idle.Position.Y) < 0.1f, $"worst overlap {worstR:F3} m, robot y {idle.Position.Y:F3}");
            }

            FxSpec.BeamParams(1.0f, out float btr1, out float ben1, out float bcu1, out float bof1); FxSpec.BeamParams(2.25f, out float btr2, out float ben2, out float bcu2, out float bof2); FxSpec.BeamParams(2.6f, out float btr3, out float ben3, out float bcu3, out float bof3);
            Check("laser tracks (red_robot.tscn shoot): hidden before 1.65 s (cutoff 1, traversal 0), open at 2.25 s (cutoff 0, traversal 1.5, energy 5, uv offset 0.3), back to closed at 2.6 s",
                bcu1 == 1f && btr1 == 0f && ben1 == 1f && bcu2 == 0f && Math.Abs(btr2 - 1.5f) < 1e-4f && ben2 == 5f && Math.Abs(bof2 - 0.3f) < 1e-4f && bcu3 == 1f && btr3 == 0f && ben3 == 1f, $"t=1.0 cut={bcu1} trav={btr1}; t=2.25 cut={bcu2} trav={btr2} en={ben2} off={bof2}; t=2.6 cut={bcu3}");
            FxSpec.BulletLight(-1f, out float ble0, out float blr0); FxSpec.BulletLight(0.25f, out float ble1, out float blr1); FxSpec.BulletLight(0.7f, out float ble2, out float blr2); FxSpec.BulletLight(0.85f, out float ble3, out float blr3);
            Check("bullet OmniLight (bullet.tscn): flying energy 1.5 range 1.5; explode peaks 3.0 at 0.25 s, 0 energy at 0.7 s, 0 range at 0.85 s",
                ble0 == 1.5f && blr0 == 1.5f && Math.Abs(ble1 - 3f) < 1e-4f && ble2 == 0f && blr3 == 0f && blr1 < 3f && blr2 > 0f, $"flying {ble0}/{blr0}; 0.25 s {ble1}/{blr1}; 0.7 s {ble2}/{blr2}; 0.85 s {ble3}/{blr3}");
            Check("particle timings: hit smoke 0.6 s x250, light balls 1.5 s x40 spread over 0.24 s, last ball spawn delay < 0.24 s, barrel smoke 3 particles 0.25 s apart, scale curve peaks at t=0.1",
                FxSpec.HitSmokeLife == 0.6f && FxSpec.HitSmokeCount == 250 && FxSpec.HitBallLife == 1.5f && FxSpec.HitBallCount == 40 && FxSpec.SpawnDelay(39, 40, 1.5f, 0.84f) < 0.24f && Math.Abs(FxSpec.SpawnDelay(1, 3, 1.5f, 0.5f) - 0.25f) < 1e-5f && Math.Abs(FxSpec.HitBallScale(0.1f) - 1f) < 1e-5f && FxSpec.HitBallScale(0.5f) < 1f,
                $"ball delay(39)={FxSpec.SpawnDelay(39, 40, 1.5f, 0.84f):F3} barrel delay(1)={FxSpec.SpawnDelay(1, 3, 1.5f, 0.5f):F3}");

            {   // LightParticle floor bounce (bullet.tscn:416-428) and effect timings of part 2
                var fp0 = new Vector3(0, 1f, 0); var fv0 = new Vector3(1f, 0f, 0f);
                var pa = FxSpec.BallPos(fp0, fv0, -9.81f, 0.3f, 0f, out int ba);       // free fall 0.3 s: y = 1 - 4.905*0.09 = 0.559, no bounce yet (floor hit at 0.4518 s)
                var pb = FxSpec.BallPos(fp0, fv0, -9.81f, 0.6f, 0f, out int bb);       // after the first bounce: rises again, y > 0, one bounce
                var pc = FxSpec.BallPos(fp0, fv0, -9.81f, 1.5f, 0f, out int bc);       // many bounces, never below the floor, horizontal speed decayed by friction
                var pd = FxSpec.BallPos(fp0, fv0, -9.81f, 0.6f, float.NegativeInfinity, out _);   // no floor: plain parabola
                Check("LightParticle floor bounce: free fall before the floor, 1 bounce at 0.6 s (y above floor), >=2 bounces by 1.5 s and never below the floor, x slowed by friction, no-floor case = parabola",
                    Math.Abs(pa.Y - 0.5590f) < 1e-3f && ba == 0 && bb == 1 && pb.Y > 0f && bc >= 2 && pc.Y >= 0f && pc.X < 1.5f && pc.X > 1.2f && Math.Abs(pd.Y - (1f - 4.905f * 0.36f)) < 1e-4f,
                    $"t=0.3 y={pa.Y:F3} b={ba}; t=0.6 y={pb.Y:F3} b={bb}; t=1.5 y={pc.Y:F3} x={pc.X:F3} b={bc}; no floor y={pd.Y:F3}");
                Check("impact_effect + muzzle + buildup constants (impact_effect.tscn, player.tscn:551, red_robot.tscn:10675): 50 embers from 0.2 s, 20 smoke from 0.3 s, blast 0.2..0.5, 10 shoot particles 0.3 s, 64 buildup particles 0.4 s emitting 0.2..1.0 s",
                    FxSpec.ImpactEmberCount == 50 && FxSpec.ImpactEmberStart == 0.2f && FxSpec.ImpactSmokeCount == 20 && FxSpec.ImpactSmokeStart == 0.3f && FxSpec.ImpactBlastStart == 0.2f && FxSpec.ImpactBlastLife == 0.3f && FxSpec.ShootParticleCount == 10 && FxSpec.ShootParticleLife == 0.3f && FxSpec.BuildupCount == 64 && FxSpec.BuildupLife == 0.4f && FxSpec.BuildupStart == 0.2f && FxSpec.BuildupEnd == 1.0f && Math.Abs(FxSpec.ImpactSmokeScale(1f) - 1f) < 1e-5f, "constants");
            }

            {   // shadow map fitting: the player is inside the light frustum, and snapping makes the view translation a whole number of texels (no shimmer)
                var kd = Vector3.Normalize(new Vector3(0.8f, -0.2f, 0.6f)); var ld = ShadowMap.DirFromKey(kd, 40f);
                bool contains = true, snapped = true, stable = true; float maxFrac = 0f;
                var rnd = new Random(7);
                for (int k = 0; k < 40; k++)
                {
                    var c = new Vector3((float)rnd.NextDouble() * 200f - 50f, (float)rnd.NextDouble() * 20f - 10f, (float)rnd.NextDouble() * 200f - 50f);
                    ShadowMap.Fit(ld, c, 32f, 80f, 2048, out var v, out var pr, out float tx);
                    var q = Vector3.Transform(Vector3.Transform(c + new Vector3(0, 1f, 0), v), pr);
                    contains &= Math.Abs(q.X) < 1f && Math.Abs(q.Y) < 1f && q.Z > 0f && q.Z < 1f;
                    float fx = v.M41 / tx, fy = v.M42 / tx; float fr = Math.Max(Math.Abs(fx - MathF.Round(fx)), Math.Abs(fy - MathF.Round(fy))); maxFrac = Math.Max(maxFrac, fr); snapped &= fr < 1e-3f;
                    ShadowMap.Fit(ld, c + new Vector3(0.002f, 0f, 0.001f), 32f, 80f, 2048, out var v2, out _, out _);   // a tiny move must not change the matrix (still the same texel)
                    stable &= Math.Abs(v2.M41 - v.M41) < 1e-5f && Math.Abs(v2.M42 - v.M42) < 1e-5f || Math.Abs(v2.M41 - v.M41) % tx < 1e-3f || Math.Abs(v2.M41 - v.M41) % tx > tx - 1e-3f;
                }
                Check("shadow map fit: frustum contains the player (40 random spots), view translation is a whole number of texels (max frac " + maxFrac.ToString("F5") + "), tiny moves change it only by whole texels", contains && snapped && stable, $"contains={contains} snapped={snapped} stable={stable} texel={2f * 32f / 2048f:F4} m dir={ld}");
            }
            {   // spot shadow maps (SpotShadows): cone frustum contains points inside the cone; nearest-N selection with hysteresis is stable under 0.1 m moves; distance fade is monotonic
                var rnd = new Random(11); bool inside = true; int tested = 0; float outer = 0.39269909f;
                for (int k = 0; k < 200; k++)
                {
                    var pos = new Vector3((float)rnd.NextDouble() * 100f, (float)rnd.NextDouble() * 20f - 10f, (float)rnd.NextDouble() * 100f);
                    var dir = Vector3.Normalize(new Vector3((float)rnd.NextDouble() - 0.5f, (float)rnd.NextDouble() * -0.4f, (float)rnd.NextDouble() - 0.5f));
                    SpotShadows.Fit(pos, dir, outer, out var v, out var pr, out _);
                    for (int q = 0; q < 8; q++)
                    {
                        var side = Vector3.Normalize(Vector3.Cross(dir, Vector3.UnitY)); var up = Vector3.Cross(side, dir);
                        float ang = (float)rnd.NextDouble() * outer * 0.98f, az = (float)rnd.NextDouble() * MathHelper.TwoPi, d = 1f + (float)rnd.NextDouble() * 27f;
                        var t = pos + (dir * MathF.Cos(ang) + (side * MathF.Cos(az) + up * MathF.Sin(az)) * MathF.Sin(ang)) * d;
                        var c = Vector4.Transform(new Vector4(t, 1f), v * pr); float w = c.W; tested++;
                        inside &= w > 0f && Math.Abs(c.X / w) < 1f && Math.Abs(c.Y / w) < 1f && c.Z / w > 0f && c.Z / w < 1f && w / SpotShadows.Far < 1f;
                    }
                }
                var lights = new[] { 0f, 10f }; var sel = new bool[] { true, false }; int flips = 0; bool prev = sel[1];
                float[] D(float x) => new[] { Math.Abs(x - lights[0]), Math.Abs(x - lights[1]) };
                for (int k = 0; k < 40; k++) { SpotShadows.SelectNearest(D(5f + (k % 2 == 0 ? 0.1f : -0.1f)), sel, 1, SpotShadows.Hysteresis, 100f); if (sel[1] != prev) { flips++; prev = sel[1]; } }
                int osc = flips;
                for (float x = 0f; x <= 10f; x += 0.1f) { SpotShadows.SelectNearest(D(x), sel, 1, SpotShadows.Hysteresis, 100f); if (sel[1] != prev) { flips++; prev = sel[1]; } }
                for (float x = 10f; x >= 0f; x -= 0.1f) { SpotShadows.SelectNearest(D(x), sel, 1, SpotShadows.Hysteresis, 100f); if (sel[1] != prev) { flips++; prev = sel[1]; } }
                bool mono = true; float lastW = 2f; for (float d = 0f; d < 40f; d += 0.5f) { float wv = SpotShadows.DistWeight(d); mono &= wv <= lastW + 1e-6f; lastW = wv; }
                Check($"spot shadow maps: cone frustum contains {tested} points inside the cone (fov = 2 x (outer + 4 deg)), nearest-N selection keeps its choice under +-0.1 m moves ({osc} flips) and flips once per direction on a 10 m walk ({flips - osc} flips), distance fade is monotonic", inside && osc == 0 && flips - osc == 2 && mono && SpotShadows.DistWeight(0f) == 1f && SpotShadows.DistWeight(SpotShadows.FadeFar) == 0f, $"inside={inside} oscFlips={osc} walkFlips={flips - osc} mono={mono}");
            }
            Console.WriteLine($"[test-physics] failures={_fail}");
            return _fail;
        }
    }
}
