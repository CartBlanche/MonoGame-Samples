using System;
using Microsoft.Xna.Framework;
using Shooter.Audio;
using Shooter.Gameplay.Combat;
using Shooter.Gameplay.Effects;
using Shooter.Gameplay.Pickups;
using Shooter.Physics;
using Shooter.Rendering.Particles;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of the jetpack and its pickup (run as part of --test-physics).</summary>
    public static class JetpackSelfTest
    {
        private const float Dt = 1f / 60f;
        private static string F(float v) { return PhysicsSelfTest.F(v); }

        private static Capsule At(Vector3 feet, float radius = 0.35f)
        {
            return new Capsule { A = feet + Vector3.Up * radius, B = feet + Vector3.Up * (1.8f - radius), Radius = radius };
        }

        private static Pickup Jetpack_(PickupManager pm)
        {
            foreach (Pickup p in pm.All) if (p.Kind == "Jetpack") return p;
            return null;
        }

        private sealed class Rig
        {
            public PlayerController Player;
            public Jetpack Jetpack;

            public Rig(PlayerSettings settings, JetpackSettings jetpackSettings, bool unlocked)
            {
                var world = new CollisionWorld();
                world.AddTriangle(new Vector3(-100, 0, -100), new Vector3(100, 0, -100), new Vector3(100, 0, 100));
                world.AddTriangle(new Vector3(-100, 0, -100), new Vector3(100, 0, 100), new Vector3(-100, 0, 100));
                world.Build();
                PlayerSettings s = settings.Clone();
                s.StartPosition = Vector3.Zero;
                Player = new PlayerController(world, s);
                Jetpack = new Jetpack(Player, jetpackSettings, s.GravityDownForce);
                if (unlocked) Jetpack.TryUnlock();
                for (int i = 0; i < 30; i++) Step(default(PlayerInput));   // settle on the ground
            }

            /// <summary>One play-loop frame: the controller, then the jetpack.</summary>
            public void Step(PlayerInput input)
            {
                Player.Update(Dt, input);
                Jetpack.Update(Dt, input);
            }

            public static PlayerInput Jump { get { return new PlayerInput { JumpDown = true, JumpHeld = true }; } }
            public static PlayerInput Hold { get { return new PlayerInput { JumpHeld = true }; } }
        }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("Jetpack");
            SceneFile data = loaded.Data;
            PlayerSettings ps = PlayerSettings.FromScene(data);
            JetpackSettings js = JetpackSettings.FromScene(data);

            // 1. values from the scene
            {
                bool ok = !js.IsJetpackUnlockedAtStart && js.JetpackAcceleration == 8f && js.JetpackDownwardVelocityCancelingFactor == 1f && js.ConsumeDuration == 1.5f &&
                          js.RefillDurationGrounded == 2f && js.RefillDurationInTheAir == 5f && js.RefillDelay == 1f && js.SfxName == "Audio/SFX/Player/Jetpack_Use";
                PhysicsSelfTest.Check("jetpack values from the Player: locked at start, acceleration 8, canceling factor 1, consume 1.5 s, refill 2 s grounded / 5 s in the air after 1 s, sound Jetpack_Use", ok,
                    "start " + js.IsJetpackUnlockedAtStart + ", accel " + F(js.JetpackAcceleration) + ", cancel " + F(js.JetpackDownwardVelocityCancelingFactor) + ", consume " + F(js.ConsumeDuration) +
                    ", refill " + F(js.RefillDurationGrounded) + " / " + F(js.RefillDurationInTheAir) + " after " + F(js.RefillDelay) + ", sfx " + js.SfxName);
            }

            // 2. the pickup we placed: a Jetpack pickup in the scene, over the floor, away from the others
            {
                var pm = new PickupManager();
                pm.AddFromScene(data);
                Pickup jp = Jetpack_(pm);
                bool values = jp != null && jp.VerticalBobFrequency == 1f && jp.BobbingAmount == 0.3f && jp.RotatingSpeed == 360f && jp.SfxName == "Audio/SFX/Pickups/Pickup_Jetpack" &&
                              string.IsNullOrEmpty(jp.VfxPrefab) && Vector3.Distance(jp.TriggerCenter, new Vector3(0f, 0.5f, 0f)) < 1e-4f && Vector3.Distance(jp.TriggerHalfSize, new Vector3(0.5f, 0.5f, 0.5f)) < 1e-4f;
                float floor = float.NaN;
                if (jp != null && loaded.Collision.Raycast(jp.Start + Vector3.Up * 3f, Vector3.Down, 10f, out CollisionWorld.RayHit hit))
                {
                    floor = hit.Point.Y;
                }
                bool onFloor = jp != null && Math.Abs(jp.Start.Y - floor) < 0.02f;   // the health pickups' roots sit on their floor too
                float nearest = float.MaxValue;
                foreach (Pickup p in pm.All) if (p != jp) nearest = Math.Min(nearest, Vector3.Distance(p.Start, jp.Start));
                int at = -1;
                for (int i = 0; i < pm.All.Count; i++) if (pm.All[i] == jp) at = i;
                bool mesh = at >= 0 && loaded.PickupItemIndex[at] >= 0;
                PhysicsSelfTest.Check("the jetpack pickup: kind Jetpack, bob 0.3 at 1 Hz, spin 360, 1 m trigger centred 0.5 m up, Pickup_Jetpack sound, no VFX, root on the floor, mesh matched, 20 m or more from the health pickups",
                    values && onFloor && mesh && nearest >= 20f, "values " + values + (jp != null ? ", at " + jp.Start + ", floor " + F(floor) : "") + ", mesh " + mesh + ", nearest other " + F(nearest));
            }

            // 3. pickup: unlocks once (raising Unlocked), then it is gone; an unlocked jetpack leaves a second pickup alone
            {
                var pm = new PickupManager();
                pm.AddFromScene(data);
                Pickup jp = Jetpack_(pm);
                var rig = new Rig(ps, js, false);
                int unlocked = 0, picked = 0;
                rig.Jetpack.Unlocked += () => unlocked++;
                pm.Picked += p => { if (p.Kind == "Jetpack") picked++; };
                Capsule cap = At(jp.Start);
                pm.GetPlayerCapsule = () => cap;
                pm.GetPlayerHealth = () => new Health(100f, 0.3f);
                pm.GetPlayerJetpack = () => rig.Jetpack;
                bool lockedBefore = !rig.Jetpack.IsJetpackUnlocked;
                pm.Update(Dt);
                bool taken = lockedBefore && rig.Jetpack.IsJetpackUnlocked && unlocked == 1 && picked == 1 && jp.Destroyed && Jetpack_(pm) == null;
                Pickup second = pm.SpawnLike(jp, jp.Start, "second");
                pm.Update(Dt);
                bool stays = !second.Destroyed && picked == 1 && unlocked == 1 && !rig.Jetpack.TryUnlock();
                PhysicsSelfTest.Check("jetpack pickup: unlocks the Jetpack (Unlocked once) and is destroyed; with the jetpack already unlocked a second pickup stays", taken && stays,
                    "taken " + taken + ", stays " + stays + ", Unlocked x" + unlocked + ", Picked x" + picked);
            }

            // 4. locked: jump and hold does what a plain jump does and the fuel stays full
            {
                float Peak(bool withJetpack)
                {
                    var rig = new Rig(ps, js, false);
                    float peak = 0f;
                    rig.Step(Rig.Jump);
                    for (int i = 0; i < 90; i++)
                    {
                        rig.Step(withJetpack && i == 20 ? Rig.Jump : Rig.Hold);
                        peak = Math.Max(peak, rig.Player.Position.Y);
                    }
                    return peak;
                }
                var probe = new Rig(ps, js, false);
                probe.Step(Rig.Jump);
                for (int i = 0; i < 40; i++) probe.Step(i == 20 ? Rig.Jump : Rig.Hold);
                float plain = Peak(false), pressed = Peak(true);
                PhysicsSelfTest.Check("a locked jetpack does nothing: pressing jump again in the air gives the plain jump height, the fuel stays full and it never counts as in use",
                    Math.Abs(plain - pressed) < 1e-4f && probe.Jetpack.CurrentFillRatio == 1f && !probe.Jetpack.IsInUse, "plain jump " + F(plain) + ", with a second press " + F(pressed) + ", fuel " + F(probe.Jetpack.CurrentFillRatio));
            }

            // 5. armed only by a second press in the air: holding the first jump gives no thrust
            {
                var rig = new Rig(ps, js, true);
                rig.Step(Rig.Jump);
                bool grounded = rig.Player.IsGrounded;
                int inUse = 0;
                for (int i = 0; i < 15; i++) { rig.Step(Rig.Hold); if (rig.Jetpack.IsInUse) inUse++; }
                float fuelBefore = rig.Jetpack.CurrentFillRatio;
                rig.Step(Rig.Jump);
                bool armed = rig.Jetpack.IsInUse && rig.Jetpack.CurrentFillRatio < 1f;
                PhysicsSelfTest.Check("the jetpack needs a second jump press in the air: the first jump held gives no thrust (fuel full), the second press thrusts at once", !grounded && inUse == 0 && fuelBefore == 1f && armed,
                    "airborne " + !grounded + ", frames in use before the second press " + inUse + ", fuel " + F(fuelBefore) + ", in use after " + rig.Jetpack.IsInUse);
            }

            // 6. thrust: net +8 m/s^2 (acceleration + gravity - gravity), a falling player is brought to (acceleration + gravity) * dt at once
            {
                var rig = new Rig(ps, js, true);
                rig.Step(Rig.Jump);
                for (int i = 0; i < 5; i++) rig.Step(Rig.Hold);
                rig.Step(Rig.Jump);   // armed
                float v0 = rig.Player.CharacterVelocity.Y;
                for (int i = 0; i < 30; i++) rig.Step(Rig.Hold);
                float gain = rig.Player.CharacterVelocity.Y - v0;
                bool net = Math.Abs(gain - 8f * 30f * Dt) < 0.05f;
                // falling: arm while sinking
                var fall = new Rig(ps, js, true);
                fall.Step(Rig.Jump);
                for (int i = 0; i < 40; i++) fall.Step(default(PlayerInput));   // 0.67 s after the jump: still airborne, sinking at 4.3 m/s
                float vy = fall.Player.CharacterVelocity.Y;
                fall.Step(Rig.Jump);
                float after = fall.Player.CharacterVelocity.Y;
                bool cancel = vy < -2f && Math.Abs(after - (js.JetpackAcceleration + ps.GravityDownForce) * Dt) < 1e-3f;
                PhysicsSelfTest.Check("thrust: +8 m/s^2 net of gravity while held; armed while falling the downward speed is cancelled and the velocity becomes (8 + gravity) * dt", net && cancel,
                    "gain over 0.5 s " + F(gain) + " (expected " + F(8f * 30f * Dt) + "), falling " + F(vy) + " -> " + F(after) + " (expected " + F((js.JetpackAcceleration + ps.GravityDownForce) * Dt) + ")");
            }

            // 7. fuel: empties in ConsumeDuration (1.5 s) and the thrust stops
            {
                var rig = new Rig(ps, js, true);
                rig.Step(Rig.Jump);
                for (int i = 0; i < 5; i++) rig.Step(Rig.Hold);
                rig.Step(Rig.Jump);
                int thrust = 1;
                for (int i = 0; i < 110; i++) { rig.Step(Rig.Hold); if (rig.Jetpack.IsInUse) thrust++; }   // 20 frames past the empty tank, inside the refill delay
                PhysicsSelfTest.Check("fuel lasts ConsumeDuration: about 90 frames of thrust at 60 fps, then the jetpack stops while jump stays held and the fuel is clamped to 0", thrust >= 89 && thrust <= 91 && !rig.Jetpack.IsInUse && rig.Jetpack.CurrentFillRatio == 0f,
                    "frames of thrust " + thrust + ", in use " + rig.Jetpack.IsInUse + ", fuel " + F(rig.Jetpack.CurrentFillRatio));
            }

            // 8. refill: nothing during RefillDelay, then 1/5 per second in the air and 1/2 per second on the ground
            {
                var rig = new Rig(ps, js, true);
                rig.Step(Rig.Jump);
                for (int i = 0; i < 5; i++) rig.Step(Rig.Hold);
                rig.Step(Rig.Jump);
                for (int i = 0; i < 30; i++) rig.Step(Rig.Hold);   // 0.5 s of thrust: fuel 2/3
                float used = rig.Jetpack.CurrentFillRatio;
                for (int i = 0; i < 45; i++) rig.Step(default(PlayerInput));   // 0.75 s released, still inside the 1 s delay
                bool delayed = !rig.Player.IsGrounded && Math.Abs(rig.Jetpack.CurrentFillRatio - used) < 1e-5f;
                for (int i = 0; i < 45; i++) rig.Step(default(PlayerInput));   // 0.25 s past the delay: air rate
                float inAir = rig.Jetpack.CurrentFillRatio - used;
                bool airOk = !rig.Player.IsGrounded && Math.Abs(inAir - 0.2f * 0.5f) < 0.011f;   // the delay ends 60 frames after the last use, so 30 of the 45 frames refill at 0.2 per second
                // on the ground the fuel refills in 2 s: from empty
                var ground = new Rig(ps, js, true);
                ground.Step(Rig.Jump);
                for (int i = 0; i < 5; i++) ground.Step(Rig.Hold);
                ground.Step(Rig.Jump);
                while (ground.Jetpack.CurrentFillRatio > 0f) ground.Step(Rig.Hold);
                int guard = 0;
                while (!ground.Player.IsGrounded && guard++ < 1200) ground.Step(default(PlayerInput));
                float atLanding = ground.Jetpack.CurrentFillRatio;
                int frames = 0;
                while (ground.Jetpack.CurrentFillRatio < 1f && frames < 600) { ground.Step(default(PlayerInput)); frames++; }
                PhysicsSelfTest.Check("refill: nothing for RefillDelay after the last use, then slowly in the air; on the ground a full refill takes 2 s once the delay is over", delayed && airOk && ground.Player.IsGrounded && frames >= 75 && frames <= 83,
                    "delay held " + delayed + ", air refill " + F(inAir) + " in the 0.5 s after the delay (expected 0.1), landed with " + F(atLanding) + " and refilled in " + frames + " frames (grounded)");
            }

            // 9. effects: the two trail emitters follow the thrust, the looping sound too
            {
                var manager = new ParticleManager(null, null);
                var vfx = new VfxLibrary(manager, name => null);
                var sfx = new SfxPlayer(null);
                var rig = new Rig(ps, js, true);
                var fx = new JetpackEffects(rig.Jetpack, rig.Player, vfx, manager, sfx);
                bool two = fx.Trails.Count == 2 && fx.Trails[0].Name == "VFX_JetpackTrail_left" && fx.Trails[1].Name == "VFX_JetpackTrail_right";
                bool offAtStart = fx.Trails.Count == 2 && !fx.Trails[0].IsEmitting && !fx.Trails[1].IsEmitting && fx.Sound != null && !fx.Sound.IsPlaying;
                rig.Step(Rig.Jump); fx.Update(); manager.Update(Dt);
                bool offOnJump = !fx.Trails[0].IsEmitting && !fx.Sound.IsPlaying;
                for (int i = 0; i < 5; i++) { rig.Step(Rig.Hold); fx.Update(); manager.Update(Dt); }
                rig.Step(Rig.Jump); fx.Update();
                for (int i = 0; i < 20; i++) { rig.Step(Rig.Hold); fx.Update(); manager.Update(Dt); }
                bool on = fx.Trails[0].IsEmitting && fx.Trails[1].IsEmitting && fx.Sound.IsPlaying && fx.Sound.PlayCalls == 1 && fx.Sound.Loop && fx.Trails[0].ParticleCount > 0;
                Vector3 emitterAt = fx.Trails[0].World.Translation;
                bool follows = Vector3.Distance(emitterAt, rig.Player.Position + new Vector3(0f, 0.625f, 0f)) < 0.6f;
                rig.Step(default(PlayerInput)); fx.Update();
                bool off = !fx.Trails[0].IsEmitting && !fx.Trails[1].IsEmitting && !fx.Sound.IsPlaying;
                int alive = fx.Trails[0].ParticleCount;
                for (int i = 0; i < 5; i++) { rig.Step(default(PlayerInput)); fx.Update(); manager.Update(Dt); }
                bool carryOn = alive > 0;   // the particles already in the air live on after the emission stops
                fx.Dispose();
                PhysicsSelfTest.Check("jetpack effects: VFX_JetpackTrail_left / _right emit and the looping sound plays only while thrusting (not on the first jump), particles in flight live on, the emitters sit on the player",
                    two && offAtStart && offOnJump && on && follows && off && carryOn, "two " + two + ", off at start " + offAtStart + ", off on the jump " + offOnJump + ", on " + on + ", follows " + follows + " (" + emitterAt + "), off after " + off + ", particles carry on " + carryOn);
            }
        }
    }
}
