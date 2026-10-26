using System;
using Microsoft.Xna.Framework;
using Shooter.Audio;
using Shooter.Gameplay.Effects;
using Shooter.Gameplay.Enemies;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the enemy feedback, milestone 4c-3d-3 (run as part of --test-physics): names, hit flash, death point, sounds.</summary>
    public static class EnemyFeedbackSelfTest
    {
        private const float Dt = 1f / 60f;
        private static string F(float v) { return PhysicsSelfTest.F(v); }
        private static string V(Vector3 v) { return PhysicsSelfTest.V(v); }

        private static int Count(SfxPlayer sfx, string name) { return sfx.PlayCounts.TryGetValue(name, out int n) ? n : 0; }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("Enemy feedback (4c-3d-3)");
            SceneFile data = loaded.Data;
            PlayerSettings ps = PlayerSettings.FromScene(data).Clone();
            var player = new PlayerController(loaded.Collision, ps);
            var director = new EnemyDirector(loaded);
            director.SetPlayer(player, ps);
            Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
            Enemy turret = director.AllEnemies.Find(e => e.Data.Kind == "Turret");

            // 1. names and numbers from the scene / the mixer
            {
                System.Text.Json.JsonElement bec = bot.Data.Components["EnemyController"], tec = turret.Data.Components["EnemyController"];
                string impact = JsonFields.AssetFileName(data.EnemyProjectile.Components["ProjectileStandard"], "ImpactVfx");
                string impactT = JsonFields.AssetFileName(data.TurretProjectile.Components["ProjectileStandard"], "ImpactVfx");
                string detectB = JsonFields.AssetName(bot.Data.Components["EnemyMobile"], "OnDetectSfx"), detectT = JsonFields.AssetName(turret.Data.Components["EnemyTurret"], "OnDetectSfx");
                var pc = data.Player.Components["PlayerCharacterController"];
                bool ok = JsonFields.AssetFileName(bec, "DeathVfx") == "VFX_HoverBot_Explosion" && JsonFields.AssetFileName(tec, "DeathVfx") == "VFX_Explosion" &&
                          JsonFields.AssetName(bec, "DamageTick") == "Audio/SFX/Player/Damage_Tick2" && JsonFields.AssetName(tec, "DamageTick") == "Audio/SFX/Player/Damage_Tick2" &&
                          detectB == "Audio/SFX/Enemies/Hoverbot_Alert" && detectT == "Audio/SFX/Enemies/Turret_Alert" &&
                          bot.Weapon.Settings.ShootSfx == "Audio/SFX/Enemies/Hoverbot_Attack" && turret.Weapon.Settings.ShootSfx == "Audio/SFX/Enemies/Turret_Attack" &&
                          bot.Weapon.Settings.MuzzleFlashPrefab == null && turret.Weapon.Settings.MuzzleFlashPrefab == null &&
                          impact == "VFX_LazerSparksRed" && impactT == "VFX_LazerSparksRed" &&
                          bot.FlashOnHitDuration == 0.2f && turret.FlashOnHitDuration == 0.125f &&
                          JsonFields.AssetName(pc, "LandSfx") == "Audio/SFX/Player/Land" && JsonFields.AssetName(pc, "FallDamageSfx") == "Audio/SFX/Player/Land_Damage" &&
                          Math.Abs(EnemyEffects.DbToLinear(EnemyEffects.DamageTickDb) - 0.4698f) < 1e-3f && Math.Abs(EnemyEffects.DbToLinear(EnemyEffects.EnemyDetectionDb) - 5.69f) < 0.01f;
                PhysicsSelfTest.Check("feedback data: death VFX, DamageTick, alert and attack sounds, impact VFX, flash durations, landing sounds and the mixer gains (-6.56 / +15.1 dB)", ok,
                    "death " + JsonFields.AssetFileName(bec, "DeathVfx") + " / " + JsonFields.AssetFileName(tec, "DeathVfx") + ", tick " + JsonFields.AssetName(bec, "DamageTick") + ", alert " + detectB + " / " + detectT +
                    ", attack " + bot.Weapon.Settings.ShootSfx + " / " + turret.Weapon.Settings.ShootSfx + ", impact " + impact + ", flash " + F(bot.FlashOnHitDuration) + " / " + F(turret.FlashOnHitDuration));
            }

            // 2. spatial sound: logarithmic rolloff, spatial blend, pan, mixer gain cap
            {
                var sfx = new SfxPlayer(null);
                Matrix listener = Matrix.CreateTranslation(0f, 1.7f, 0f);   // looks down -z, right = +x
                float g10 = SfxPlayer.RolloffGain(10f, 1f, 500f), gNear = SfxPlayer.RolloffGain(0.4f, 1f, 500f), gFar = SfxPlayer.RolloffGain(1000f, 1f, 20f);
                sfx.PlayOneShot3D("a", 0.6f, 0.5f, new Vector3(10f, 1.7f, 0f), listener, 1f, 20f);          // enemy weapon: volume 0.6, blend 0.5, 10 m to the right
                float vol = sfx.LastVolume, pan = sfx.LastPan;
                sfx.PlayOneShot3D("b", 1f, 1f, new Vector3(-3f, 1.7f, 0f), listener, 1f, 500f, EnemyEffects.DbToLinear(EnemyEffects.EnemyDetectionDb));   // alert, 3 m left: capped
                float capVol = sfx.LastVolume, capPan = sfx.LastPan;
                sfx.PlayOneShot3D("c", 1f, 1f, new Vector3(0f, 1.7f, -400f), listener, 1f, 500f, EnemyEffects.DbToLinear(EnemyEffects.EnemyDetectionDb));
                float farVol = sfx.LastVolume, farPan = sfx.LastPan;
                float expectedWeapon = 0.6f * MathHelper.Lerp(1f, 0.1f, 0.5f);
                PhysicsSelfTest.Check("spatial one-shots: rolloff min / d (no change inside 1 m, flat beyond the max distance), blend 0.5 at 10 m, pan to the source's side, mixer gain capped at volume 1",
                    Math.Abs(g10 - 0.1f) < 1e-5f && gNear == 1f && Math.Abs(gFar - 0.05f) < 1e-5f && Math.Abs(vol - expectedWeapon) < 1e-4f && pan > 0.49f && pan < 0.51f &&
                    capVol == 1f && capPan < -0.99f && Math.Abs(farVol - 5.69f / 400f) < 1e-3f && Math.Abs(farPan) < 1e-3f,
                    "gain 10 m " + F(g10) + ", 0.4 m " + F(gNear) + ", 1000 m (max 20) " + F(gFar) + "; weapon " + F(vol) + " (expected " + F(expectedWeapon) + ") pan " + F(pan) + "; alert 3 m " + F(capVol) + " pan " + F(capPan) + "; 400 m " + F(farVol));
            }

            // 3. hit flash: OnHitBodyGradient over FlashOnHitDuration after player damage
            {
                var rig = (EnemyRig)loaded.EnemyViews[bot.Index];
                for (int i = 0; i < 30; i++) director.Update(Dt);
                bool quiet = rig.BodyEmission == Vector3.Zero;
                bot.Health.TakeDamage(10f, director.PlayerActor);
                director.Update(Dt);                           // t = 1 frame
                float early = rig.BodyEmission.X;
                for (int i = 0; i < 2; i++) director.Update(Dt);   // 3 frames = 0.05 s > the first white key (0.229 x 0.2 s)
                float white = rig.BodyEmission.X;
                for (int i = 0; i < 4; i++) director.Update(Dt);   // 0.117 s: still white (key 2 at 0.803)
                float held = rig.BodyEmission.X;
                for (int i = 0; i < 10; i++) director.Update(Dt);  // 0.283 s: over
                float over = rig.BodyEmission.X;
                // damage from an enemy does not flash
                bot.Health.TakeDamage(5f, turret);
                director.Update(Dt);
                float enemyHit = rig.BodyEmission.X;
                PhysicsSelfTest.Check("hit flash: black before damage, rising on the first frames, white while held, black again after FlashOnHitDuration, none for enemy fire",
                    quiet && early > 0f && early < 0.9f && Math.Abs(white - 1f) < 0.02f && Math.Abs(held - 1f) < 0.02f && over == 0f && enemyHit == 0f,
                    "before " + quiet + ", 1 frame " + F(early) + ", 3 frames " + F(white) + ", 7 frames " + F(held) + ", 17 frames " + F(over) + ", after enemy fire " + F(enemyHit));
                director.Reset();
            }

            // 4. death point, death sound, damage tick once per frame
            {
                bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
                turret = director.AllEnemies.Find(e => e.Data.Kind == "Turret");
                var sfx = new SfxPlayer(null);
                var fx = new EnemyEffects(director, null, sfx, () => Matrix.CreateTranslation(0f, 1.7f, 0f));
                Vector3 botPos = default(Vector3), turretPos = default(Vector3); int dyingBot = 0, dyingTurret = 0;
                bot.Dying += (e, p) => { botPos = p; dyingBot++; };
                turret.Dying += (e, p) => { turretPos = p; dyingTurret++; };
                Vector3 expectedBot = bot.AimPointPosition;
                ((IEnemyView)loaded.EnemyViews[turret.Index]).TryGetBoneWorld("Turret_Pivot_C_Jnt", false, out Matrix pivot);

                bot.Health.TakeDamage(1f, director.PlayerActor);
                bot.Health.TakeDamage(1f, director.PlayerActor);   // same frame: one tick
                int ticks1 = Count(sfx, "Audio/SFX/Player/Damage_Tick2");
                float tickVol = sfx.LastVolume;
                fx.Update(Dt);
                bot.Health.TakeDamage(1f, director.PlayerActor);   // next frame: another
                int ticks2 = Count(sfx, "Audio/SFX/Player/Damage_Tick2");
                PhysicsSelfTest.Check("damage tick: one per frame however many hits, volume -6.56 dB, 2D", ticks1 == 1 && ticks2 == 2 && Math.Abs(tickVol - 0.4698f) < 1e-3f && Math.Abs(sfx.LastPan) < 1e-6f,
                    "ticks " + ticks1 + " then " + ticks2 + ", volume " + F(tickVol));

                bot.Health.Kill();
                turret.Health.Kill();
                bool botOk = dyingBot == 1 && Vector3.Distance(botPos, expectedBot) < 1e-4f;
                bool turretOk = dyingTurret == 1 && Vector3.Distance(turretPos, pivot.Translation) < 1e-3f && Vector3.Distance(turretPos, turret.Position) > 0.5f;
                PhysicsSelfTest.Check("death: the HoverBot's DeathVfxSpawnPoint is its HitBox (aim point), the Turret's is the pivot bone, each announced once, with the death sounds",
                    botOk && turretOk && Count(sfx, "Audio/SFX/Enemies/Hoverbot_Death") == 1 && Count(sfx, "Audio/SFX/Enemies/Turret_Death") == 1,
                    "bot " + V(botPos) + " (aim point " + V(expectedBot) + "), turret " + V(turretPos) + " (pivot bone " + V(pivot.Translation) + ", root " + V(turret.Position) + "), sounds " +
                    Count(sfx, "Audio/SFX/Enemies/Hoverbot_Death") + "/" + Count(sfx, "Audio/SFX/Enemies/Turret_Death"));
                fx.Dispose();
                director.Reset();
            }

            // 5. real level: alert sound once on detection, an attack sound per shot
            {
                bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
                var sfx = new SfxPlayer(null);
                var fx = new EnemyEffects(director, null, sfx, () => player.CameraWorld);
                Vector3 spot = PlayerHealthSelfTest.FindSpotNear(loaded, bot.DetectionSourcePosition, 15f);
                player.Respawn(spot, 0f);
                for (int i = 0; i < 30; i++) player.Update(Dt, default(PlayerInput));
                for (int i = 0; i < 4 * 60; i++) { player.Update(Dt, default(PlayerInput)); director.Update(Dt); fx.Update(Dt); }
                int alerts = Count(sfx, "Audio/SFX/Enemies/Hoverbot_Alert"), attacks = Count(sfx, "Audio/SFX/Enemies/Hoverbot_Attack");
                PhysicsSelfTest.Check("real level: the HoverBot's alert sound plays once when it detects the player, its attack sound once per shot (at volume 0.6 x rolloff)", alerts == 1 && attacks == bot.Weapon.ShotsFired && attacks >= 3,
                    "alerts " + alerts + ", attacks " + attacks + " (shots " + bot.Weapon.ShotsFired + ")");
                fx.Dispose();
                director.Reset();
                player.Respawn(ps.StartPosition, ps.StartYawDegrees);
            }
        }
    }
}
