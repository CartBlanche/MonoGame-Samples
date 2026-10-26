using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Shooter.Gameplay.Weapons;
using Shooter.Physics;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the weapon port (run as part of --test-physics): timings, ammo, recoil, aim, bob, projectiles.</summary>
    public static class WeaponSelfTest
    {
        private const float Dt = 1f / 60f;

        private class Rig
        {
            public PlayerController Player;
            public PlayerWeaponsManager Weapons;
            public ProjectileManager Projectiles = new ProjectileManager();
            public readonly List<ProjectileImpact> Impacts = new List<ProjectileImpact>();
            public float Time;

            public Rig(SceneFile scene, CollisionWorld world, Vector3 pos, float yaw)
            {
                PlayerSettings s = PlayerSettings.FromScene(scene).Clone();
                s.StartPosition = pos;
                s.StartYawDegrees = yaw;
                world.Build();
                Player = new PlayerController(world, s);
                Weapons = new PlayerWeaponsManager(scene, Player, s, world, Projectiles);
                Projectiles.Impact += i => Impacts.Add(i);
            }

            // same order as ShooterGame.UpdatePlay
            public void Step(PlayerInput input, float dt = Dt)
            {
                Player.Update(dt, input);
                Weapons.Update(dt, input);
                Projectiles.Update(dt);
                Weapons.LateUpdate(dt);
                Time += dt;
            }

            public void Run(float seconds, Func<float, PlayerInput> input, Action each = null)
            {
                int n = (int)Math.Round(seconds / Dt);
                for (int i = 0; i < n; i++)
                {
                    Step(input(i * Dt));
                    if (each != null) each();
                }
            }
        }

        private static CollisionWorld Flat(float wallX = float.NaN)
        {
            var w = new CollisionWorld();
            w.AddTriangle(new Vector3(-200, 0, -200), new Vector3(200, 0, -200), new Vector3(200, 0, 200));
            w.AddTriangle(new Vector3(-200, 0, -200), new Vector3(200, 0, 200), new Vector3(-200, 0, 200));
            if (!float.IsNaN(wallX))
            {
                w.AddTriangle(new Vector3(wallX, 0, -100), new Vector3(wallX, 0, 100), new Vector3(wallX, 50, 100));
                w.AddTriangle(new Vector3(wallX, 0, -100), new Vector3(wallX, 50, 100), new Vector3(wallX, 50, -100));
            }
            return w;
        }

        private static PlayerInput Idle(float t) { return default(PlayerInput); }
        private static PlayerInput Fire(float t) { return new PlayerInput { FireHeld = true, FireDown = t < 0.001f }; }

        public static void Run(SceneFile scene, CollisionWorld level)
        {
            Console.WriteLine("Weapons");
            var v = PlayerSettingsAccess(scene);

            // 1. settings from the scene
            {
                WeaponSettings ws = WeaponSettings.From(scene.Weapon.Components["WeaponController"]);
                bool ok = ws.ShootType == WeaponShootType.Automatic && Math.Abs(ws.DelayBetweenShots - 0.1f) < 1e-5f && ws.MaxAmmo == 16 && Math.Abs(ws.AmmoReloadRate - 16f) < 1e-4f &&
                          Math.Abs(ws.AmmoReloadDelay - 1f) < 1e-4f && Math.Abs(ws.AimZoomRatio - 0.5f) < 1e-5f && Math.Abs(ws.AimOffsetUnity.Y - 0.075f) < 1e-5f;
                PhysicsSelfTest.Check("Blaster settings read from the scene", ok, ws.WeaponName + ": " + ws.ShootType + ", delay " + ws.DelayBetweenShots + ", ammo " + ws.MaxAmmo + ", reload " + ws.AmmoReloadRate + "/s after " + ws.AmmoReloadDelay + " s, zoom " + ws.AimZoomRatio + ", aim offset y " + ws.AimOffsetUnity.Y);
                ProjectileSettings ps = ProjectileSettings.From(scene.Projectile.Components["ProjectileStandard"], Vector3.Zero);
                PhysicsSelfTest.Check("projectile settings read from the scene", ps.Speed == 300f && ps.Damage == 15f && ps.TrajectoryCorrectionDistance == 5f && ps.MaxLifeTime == 5f, "speed " + ps.Speed + ", damage " + ps.Damage + ", correction distance " + ps.TrajectoryCorrectionDistance + ", life " + ps.MaxLifeTime + ", tip " + PhysicsSelfTest.V(new Vector3(scene.Projectile.TipInRoot[0], scene.Projectile.TipInRoot[1], scene.Projectile.TipInRoot[2])));
            }

            // 1b. HUD data from the weapon (crosshair sprite / size / colour, icon) and IsPointingAtEnemy
            {
                WeaponSettings ws = WeaponSettings.From(scene.Weapon.Components["WeaponController"]);
                CrosshairData d = ws.CrosshairDefault, t = ws.CrosshairTargetInSight;
                bool dataOk = d.Sprite == "Textures/UI/Crosshair_Hexa" && d.Size == 55 && Math.Abs(d.Color.W - 0.38431373f) < 1e-5f && d.Color.X == 1f && d.Color.Y == 1f &&
                              t.Sprite == "Textures/UI/Crosshair_Hexa" && t.Size == 40 && t.Color.X == 1f && t.Color.Y == 0f && t.Color.Z == 0f && t.Color.W == 1f &&
                              ws.WeaponIcon == "Textures/UI/HUD_Weapon03";
                PhysicsSelfTest.Check("crosshair and icon data read from the WeaponController", dataOk, "default " + d.Sprite + " " + d.Size + " " + d.Color + ", target " + t.Size + " " + t.Color + ", icon " + ws.WeaponIcon);

                var r = new Rig(scene, Flat(wallX: 10f), new Vector3(0, 0.001f, 0), 90f);
                r.Run(0.1f, Idle);
                bool offByDefault = !r.Weapons.IsPointingAtEnemy;
                r.Weapons.IsEnemyHit = hit => Math.Abs(hit.Distance - 10f) < 0.35f;   // stand-in enemy: the wall 10 m ahead (eye height 1.44, radius 0.35 start)
                r.Run(0.1f, Idle);
                bool onWall = r.Weapons.IsPointingAtEnemy;
                r.Weapons.IsEnemyHit = hit => false;
                r.Run(0.1f, Idle);
                PhysicsSelfTest.Check("IsPointingAtEnemy follows the centre ray and the enemy test", offByDefault && onWall && !r.Weapons.IsPointingAtEnemy, "stub default " + !offByDefault + ", enemy hit " + onWall + ", cleared " + !r.Weapons.IsPointingAtEnemy);
                WeaponController wc = r.Weapons.ActiveWeapon;
                PhysicsSelfTest.Check("ammo needed to shoot is 1/MaxAmmo (FillBarColorChange empty value)", Math.Abs(wc.AmmoNeededToShoot - 1f / 16f) < 1e-6f, "ratio " + wc.AmmoNeededToShoot);
            }

            // 1c. effect and sound data from the scene (WeaponController, projectile, AudioSource, OverheatBehavior)
            {
                WeaponSettings ws = WeaponSettings.From(scene.Weapon.Components["WeaponController"]);
                bool weaponOk = ws.ShootSfx == "Audio/SFX/Weapons/Blaster_Shot" && ws.ChangeWeaponSfx == "Audio/SFX/Pickups/Pickup_Weapon_Small" && ws.MuzzleFlashPrefab == "VFX_MuzzleFlashGreen" && !ws.UnparentMuzzleFlash;
                PhysicsSelfTest.Check("weapon sounds and muzzle flash named by the WeaponController", weaponOk, ws.ShootSfx + ", " + ws.ChangeWeaponSfx + ", " + ws.MuzzleFlashPrefab + ", unparent " + ws.UnparentMuzzleFlash);
                ProjectileSettings ps = ProjectileSettings.From(scene.Projectile.Components["ProjectileStandard"], Vector3.Zero);
                PhysicsSelfTest.Check("impact VFX from the projectile", ps.ImpactVfx == "VFX_LazerSparksGreen" && ps.ImpactVfxLifetime == 5f && Math.Abs(ps.ImpactVfxSpawnOffset - 0.1f) < 1e-6f && ps.ImpactSfx == null, ps.ImpactVfx + ", lifetime " + ps.ImpactVfxLifetime + ", offset " + ps.ImpactVfxSpawnOffset + ", sfx " + (ps.ImpactSfx ?? "none"));
                float vol = JsonFields.Float(scene.Weapon.Components["AudioSource"], "m_Volume", -1f);
                string cool = JsonFields.AssetName(scene.Weapon.Components["OverheatBehavior"], "CoolingCellsSound");
                var curve = Shooter.Rendering.Particles.AnimCurve.FromJson(scene.Weapon.Components["OverheatBehavior"].GetProperty("AmmoToVolumeRatioCurve"));
                bool curveOk = Math.Abs(curve.Evaluate(0f)) < 1e-4f && Math.Abs(curve.Evaluate(0.4f) - 0.6253f) < 0.001f && Math.Abs(curve.Evaluate(0.8f) - 1f) < 1e-3f && Math.Abs(curve.Evaluate(1f) - 1f) < 1e-3f;
                PhysicsSelfTest.Check("weapon AudioSource volume and cooling sound curve", Math.Abs(vol - 0.3f) < 1e-6f && cool == "Audio/SFX/Weapons/Weapon_Cooling" && curveOk, "volume " + vol + ", cooling " + cool + ", curve(0, 0.4, 0.8, 1) = " + curve.Evaluate(0f) + ", " + curve.Evaluate(0.4f) + ", " + curve.Evaluate(0.8f) + ", " + curve.Evaluate(1f));
            }

            // 1d. overheat gradient and fuel cell data
            {
                var oh = scene.Weapon.Components["OverheatBehavior"];
                var grad = Shooter.Rendering.Particles.Gradient.FromJson(oh.GetProperty("OverheatGradient"));
                Vector3 full = Shooter.Gameplay.Effects.OverheatVisuals.EmissionFor(grad, 1f);
                Vector3 empty = Shooter.Gameplay.Effects.OverheatVisuals.EmissionFor(grad, 0f);
                Vector3 half = Shooter.Gameplay.Effects.OverheatVisuals.EmissionFor(grad, 0.5f);
                bool gradOk = (full - new Vector3(0.31408098f, 3.482202f, 0f)).Length() < 1e-3f && (empty - new Vector3(5.2164755f, 1.5567491f, 0f)).Length() < 1e-3f && (half - (full + empty) * 0.5f).Length() < 1e-3f;
                PhysicsSelfTest.Check("overheat gradient: green when full, orange when empty", gradOk, "full " + PhysicsSelfTest.V(full) + ", half " + PhysicsSelfTest.V(half) + ", empty " + PhysicsSelfTest.V(empty) + ", steam max " + JsonFields.Float(oh, "SteamVfxEmissionRateMax", -1f) + "/s");
                var cells = scene.Weapon.FuelCellPieces;
                var fc = scene.Weapon.Components["WeaponFuelCellHandler"];
                bool cellsOk = cells.Count == 4 && cells[0] == "Primary_Weapon_Container_04" && cells[3] == "Primary_Weapon_Container_01" && !JsonFields.Bool(fc, "SimultaneousFuelCellsUsage", true) &&
                               Math.Abs(JsonFields.Vector3(fc, "FuelCellUsedPosition", Vector3.Zero).Y - 0.1f) < 1e-6f && JsonFields.Vector3(fc, "FuelCellUnusedPosition", Vector3.One) == Vector3.Zero;
                PhysicsSelfTest.Check("fuel cells resolved to pieces with used / unused positions", cellsOk, string.Join(", ", cells) + "; used y " + JsonFields.Vector3(fc, "FuelCellUsedPosition", Vector3.Zero).Y);
            }

            // 2. raising the weapon at the start (Down -> Default over WeaponSwitchDelay)
            {
                var r = new Rig(scene, Flat(), new Vector3(0, 0.001f, 0), 90f);
                Vector3 down = r.Weapons.SocketLocal;
                bool downOk = (down - new Vector3(0.24f, -1.296f, -0.231f)).Length() < 1e-3f && r.Weapons.SwitchState == PlayerWeaponsManager.WeaponSwitchState.PutUpNew && r.Weapons.WeaponVisible;
                float delay = r.Weapons.WeaponSwitchDelay;
                r.Run(delay * 0.5f, Idle);
                Vector3 mid = r.Weapons.SocketLocal;
                r.Run(delay * 0.5f + 0.3f, Idle);   // put-up ends at 'delay'; the aim lerp then closes the last millimetres
                Vector3 up = r.Weapons.SocketLocal;
                bool midOk = mid.Y > -1.296f + 0.1f && mid.Y < -0.347f - 0.1f;
                bool upOk = midOk && r.Weapons.SwitchState == PlayerWeaponsManager.WeaponSwitchState.Up && (up - new Vector3(0.264f, -0.347f, -0.465f)).Length() < 5e-3f;
                PhysicsSelfTest.Check("weapon is raised from Down to Default in WeaponSwitchDelay", downOk && upOk, "t=0 " + PhysicsSelfTest.V(down) + " (Down 0.24,-1.296,-0.231), half way y " + PhysicsSelfTest.F(mid.Y) + " (between -1.296 and -0.347), settled " + PhysicsSelfTest.V(up) + " state " + r.Weapons.SwitchState);
            }

            // 3. fire rate, ammo, reload delay
            {
                var r = new Rig(scene, Flat(), new Vector3(0, 0.001f, 0), 90f);
                r.Run(1.1f, Idle);
                int first = 0;
                var times = new List<float>();
                int prev = r.Weapons.ShotsFired;
                float t0 = r.Time;
                r.Run(1f, Fire, () => { if (r.Weapons.ShotsFired != prev) { times.Add(r.Time - t0); prev = r.Weapons.ShotsFired; } });
                first = r.Weapons.ShotsFired;
                float ammoAfter1s = r.Weapons.ActiveWeapon.CurrentAmmo;
                float lastShot = 0f; int count = r.Weapons.ShotsFired;
                r.Run(1.5f, Fire, () => { if (r.Weapons.ShotsFired != count) { count = r.Weapons.ShotsFired; lastShot = r.Time; } });   // t = 2.5 s of continuous fire: the magazine is empty, each shot blocks the refill
                int at2_5 = r.Weapons.ShotsFired;
                float ammoMin = r.Weapons.ActiveWeapon.CurrentAmmo;
                float gap = times.Count > 2 ? (times[times.Count - 1] - times[0]) / (times.Count - 1) : 0f;
                PhysicsSelfTest.Check("automatic fire: 0.1 s gate and ammo", first >= 8 && first <= 9 && Math.Abs(gap - 0.1167f) < 0.01f && Math.Abs(ammoAfter1s - (16 - first)) < 0.01f && at2_5 == 16 && ammoMin < 1f,
                      first + " shots in the first second (gap " + PhysicsSelfTest.F(gap) + " s = 7 frames), ammo " + PhysicsSelfTest.F(ammoAfter1s) + " after it; " + at2_5 + " shots by 2.5 s with ammo " + PhysicsSelfTest.F(ammoMin));
                // let go: nothing for 1 s after the last shot, then 16 ammo/s
                r.Run(0.9f - (r.Time - lastShot), Idle);
                float notYet = r.Weapons.ActiveWeapon.CurrentAmmo;
                r.Run(0.6f, Idle);   // 0.5 s after the delay ended
                float half = r.Weapons.ActiveWeapon.CurrentAmmo;
                r.Run(1.3f, Idle);
                float refilled = r.Weapons.ActiveWeapon.CurrentAmmo;
                PhysicsSelfTest.Check("ammo refills 16/s after the 1 s delay", notYet < 0.01f && half > 6f && half < 10f && refilled >= 15.9f, "ammo 0.9 s after the last shot " + PhysicsSelfTest.F(notYet) + ", ~0.5 s into the refill " + PhysicsSelfTest.F(half) + ", later " + PhysicsSelfTest.F(refilled));
            }

            // 4. recoil
            {
                var r = new Rig(scene, Flat(), new Vector3(0, 0.001f, 0), 90f);
                r.Run(1.1f, Idle);
                float restZ = r.Weapons.SocketLocal.Z;
                float peak = 0f;
                r.Run(1f, Fire, () => peak = Math.Max(peak, r.Weapons.SocketLocal.Z - restZ));
                r.Run(1.5f, Idle);
                float after = r.Weapons.SocketLocal.Z - restZ;
                float limit = r.Weapons.MaxRecoilDistance;
                PhysicsSelfTest.Check("recoil kicks back up to MaxRecoilDistance and recovers", peak > limit * 0.7f && peak <= limit + 1e-4f && Math.Abs(after) < 0.005f, "peak " + PhysicsSelfTest.F(peak) + " m back (limit " + limit + " from the scene), 1.5 s after the last shot " + PhysicsSelfTest.F(after));
            }

            // 5. aiming
            {
                var r = new Rig(scene, Flat(), new Vector3(0, 0.001f, 0), 90f);
                r.Run(1.1f, Idle);
                r.Run(1f, t => new PlayerInput { AimHeld = true });
                Vector3 aim = r.Weapons.SocketLocal;
                float fovAim = r.Weapons.Fov;
                float mult = r.Player.Position == Vector3.Zero ? 0 : 1;
                // look 1 px worth at 0.001 sensitivity while aiming turns 0.4x as fast
                float yaw0 = r.Player.YawDegrees;
                r.Step(new PlayerInput { AimHeld = true, LookX = 0.01f });
                float turned = r.Player.YawDegrees - yaw0;
                r.Run(1f, Idle);
                bool ok = Math.Abs(fovAim - 30f) < 0.5f && (aim - new Vector3(0f, -0.347f + 0.075f, -0.465f)).Length() < 0.005f && Math.Abs(turned - 0.01f * 200f * 0.4f) < 1e-3f && Math.Abs(r.Weapons.Fov - 60f) < 0.5f;
                PhysicsSelfTest.Check("aim: socket to Aiming + AimOffset, FOV 60 -> 30, look x0.4, then back", ok, "socket " + PhysicsSelfTest.V(aim) + ", fov " + PhysicsSelfTest.F(fovAim) + ", turned " + PhysicsSelfTest.F(turned) + " deg for a 2 deg input, fov after release " + PhysicsSelfTest.F(r.Weapons.Fov) + (mult > 0 ? "" : ""));
            }

            // 6. bob
            {
                var results = new List<string>();
                bool ok = true;
                foreach (var mode in new[] { "idle", "walk", "sprint" })
                {
                    var r = new Rig(scene, Flat(), new Vector3(0, 0.001f, 0), 90f);
                    r.Run(1.1f, Idle);
                    float maxX = 0f, maxY = 0f, baseX = r.Weapons.SocketLocal.X, baseY = r.Weapons.SocketLocal.Y;
                    r.Run(3f, t => new PlayerInput { Move = mode == "idle" ? Vector2.Zero : new Vector2(0, 1), SprintHeld = mode == "sprint" }, () =>
                    {
                        maxX = Math.Max(maxX, Math.Abs(r.Weapons.SocketLocal.X - baseX));
                        maxY = Math.Max(maxY, Math.Abs(r.Weapons.SocketLocal.Y - baseY));
                    });
                    float expected = mode == "idle" ? 0f : mode == "walk" ? 0.05f * (13f / 19.5f) : 0.05f;
                    ok &= Math.Abs(maxX - expected) < 0.004f && Math.Abs(maxY - expected) < 0.004f;
                    results.Add(mode + " x " + PhysicsSelfTest.F(maxX) + " y " + PhysicsSelfTest.F(maxY) + " (expect " + PhysicsSelfTest.F(expected) + ")");
                }
                PhysicsSelfTest.Check("weapon bob amplitude follows speed (0.05 x speed / 19.5)", ok, string.Join("; ", results));
            }

            // 7. projectile hits a wall where the camera points (trajectory correction), 8. camera-to-muzzle wall check
            {
                var r = new Rig(scene, Flat(20f), new Vector3(0, 0.001f, 0), 90f);
                r.Run(1.1f, Idle);
                Vector3 camPos = r.Player.CameraPosition;
                r.Step(new PlayerInput { FireHeld = true, FireDown = true });
                int ticks = 0;
                while (r.Impacts.Count == 0 && ticks++ < 120) r.Step(Idle(0));
                ProjectileImpact hit = r.Impacts.Count > 0 ? r.Impacts[0] : default(ProjectileImpact);
                bool ok = r.Impacts.Count == 1 && Math.Abs(hit.Point.X - 20f) < 0.05f && Math.Abs(hit.Point.Y - camPos.Y) < 0.03f && Math.Abs(hit.Point.Z - camPos.Z) < 0.03f && hit.Normal.X < -0.99f && hit.Damage == 15f;
                PhysicsSelfTest.Check("blaster shot lands on the screen centre 20 m away", ok, r.Impacts.Count + " impact(s) after " + ticks + " frames at " + PhysicsSelfTest.V(hit.Point) + " vs camera line (20, " + PhysicsSelfTest.F(camPos.Y) + ", " + PhysicsSelfTest.F(camPos.Z) + "), normal " + PhysicsSelfTest.V(hit.Normal) + ", damage " + hit.Damage);

                var r2 = new Rig(scene, Flat(5f), new Vector3(0, 0.001f, 0), 90f);
                r2.Run(1.1f, Idle);
                r2.Run(1f, t => new PlayerInput { Move = new Vector2(0, 1) });   // walk up to the wall (stops at x = 4.649)
                r2.Step(new PlayerInput { FireHeld = true, FireDown = true });
                bool cam = r2.Impacts.Count == 1 && r2.Impacts[0].FromCameraRay && Math.Abs(r2.Impacts[0].Point.X - 5f) < 0.01f && r2.Projectiles.Live.Count == 0;
                PhysicsSelfTest.Check("shooting a wall closer than the muzzle hits it at once", cam, "player x " + PhysicsSelfTest.F(r2.Player.Position.X) + ", muzzle x " + PhysicsSelfTest.F(r2.Weapons.MuzzleWorld(r2.Player.CameraWorld).X) + " (beyond the wall), impacts " + r2.Impacts.Count + ", from camera ray " + (r2.Impacts.Count > 0 && r2.Impacts[0].FromCameraRay));
            }

            // 9. weapon switching state machine (a second slot is added only for this test)
            {
                var r = new Rig(scene, Flat(), new Vector3(0, 0.001f, 0), 90f);
                r.Run(1.1f, Idle);
                r.Step(new PlayerInput { SwitchWeapon = 1 });   // one weapon only: nothing happens
                bool noOp = r.Weapons.SwitchState == PlayerWeaponsManager.WeaponSwitchState.Up && r.Weapons.ActiveWeaponIndex == 0;
                r.Weapons.AddWeapon(new WeaponController(WeaponSettings.From(scene.Weapon.Components["WeaponController"])));
                r.Step(new PlayerInput { SwitchWeapon = 1 });
                bool down = r.Weapons.SwitchState == PlayerWeaponsManager.WeaponSwitchState.PutDownPrevious;
                float delay = r.Weapons.WeaponSwitchDelay;
                r.Run(delay + 0.05f, Idle);
                bool up = r.Weapons.SwitchState == PlayerWeaponsManager.WeaponSwitchState.PutUpNew && r.Weapons.ActiveWeaponIndex == 1;
                r.Run(delay + 0.05f, Idle);
                bool done = r.Weapons.SwitchState == PlayerWeaponsManager.WeaponSwitchState.Up;
                PhysicsSelfTest.Check("weapon switch: down, up (WeaponSwitchDelay each), ignored with a single weapon", noOp && down && up && done, "single weapon no-op " + noOp + ", put down " + down + ", put up " + up + ", settled " + done);
            }

            // 10. real level: every shot stops where an independent camera ray hits
            if (level != null)
            {
                var r = new Rig(scene, level, PlayerSettings.FromScene(scene).StartPosition, 0f);
                int shots = 0, agree = 0, worstIdx = -1; float worst = 0f;
                for (int k = 0; k < 24; k++)
                {
                    float yaw = k * 15f;
                    float pitch = (k % 4 - 1) * 10f;   // -10, 0, 10, 20 degrees (down positive)
                    r.Player.Respawn(PlayerSettings.FromScene(scene).StartPosition, yaw);
                    r.Weapons.GetType();   // keep the rig's weapon state; raised once at the start
                    r.Player.Update(0f, new PlayerInput { LookY = pitch / 200f });
                    if (k == 0) r.Run(1.1f, Idle);
                    Matrix cam = r.Player.CameraWorld;
                    Vector3 fwd = Vector3.TransformNormal(Vector3.Forward, cam);
                    r.Impacts.Clear();
                    r.Projectiles.Clear();
                    bool expectHit = level.Raycast(cam.Translation, fwd, 1000f, out CollisionWorld.RayHit expected);
                    r.Step(new PlayerInput { FireHeld = true, FireDown = true });
                    for (int i = 0; i < 240 && r.Impacts.Count == 0; i++) r.Step(Idle(0));
                    shots++;
                    // wait for the weapon rate gate between iterations
                    r.Run(0.2f, Idle);
                    if (expectHit && expected.Distance > 16f && r.Impacts.Count == 1)
                    {
                        float err = (r.Impacts[0].Point - expected.Point).Length();
                        if (err < 0.1f) agree++;
                        if (err > worst) { worst = err; worstIdx = k; }
                    }
                    else if (!expectHit && r.Impacts.Count == 0) agree++;   // sky: no impact expected
                    else if (expectHit && expected.Distance <= 16f) agree++;  // too close for the correction to settle: not judged
                }
                PhysicsSelfTest.Check("level: shots hit what the camera ray hits", agree == shots, agree + " of " + shots + " shots agree (impact within 10 cm of an independent camera ray, or sky / <16 m skipped (the projectile tip is 10 m ahead of its root and the correction needs 5 m)), worst error " + PhysicsSelfTest.F(worst) + " m at shot " + worstIdx);
            }
        }

        private static object PlayerSettingsAccess(SceneFile scene) { return null; }
    }
}
