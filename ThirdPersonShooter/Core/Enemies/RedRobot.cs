using System;
using Microsoft.Xna.Framework;
using ThirdPersonShooter.Physics;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Port of enemies/red_robot/red_robot.gd (CharacterBody3D, layer 3 / mask 3) at the fixed 60 Hz tick. Server-only (single player): IDLE / APPROACH / AIM / SHOOTING,
    /// root-motion movement ("MASTER" bone), 20 m detection sphere, sight rays, 4 s shoot timeline (shoot_check at 2.25 s, resume_approach at 3.0 s),
    /// 5 health, hit one-shots, explode on death. Not ported: debris parts/sparks/sounds, respawn (level.gd), laser smoke/ember particles.
    /// </summary>
    public sealed class RedRobot : IDamageable
    {
        public enum State { Idle = 0, Approach = 1, Aim = 2, Shooting = 3 }   // red_robot.gd:6-11

        // ---- red_robot.gd ----
        public static readonly float PlayerAimTolerance = MathHelper.ToRadians(15f); // :14 deg_to_rad(15.0), the tolerance is compared against radians as-is
        public const float ShootWait = 6f;          // :16 SHOOT_WAIT
        public const float AimTime = 1f;            // :17 AIM_TIME
        public const float AimPrepareTime = 0.5f;   // :19 AIM_PREPARE_TIME
        public const float BlendAimSpeed = 0.05f;   // :20 BLEND_AIM_SPEED
        public const int MaxHealth = 5;             // :26 health
        public const float LaserMaxDist = 1000f;    // :104 max_dist
        public const float LaserTraumaDelay = 0.1f; // :134 create_timer(0.1)
        public const float LaserTrauma = 13f;       // :135 add_camera_shake_trauma(13.0)
        // ---- red_robot.tscn ----
        public const float SphereRadius = 1.11815f;     // :10209-10210 SphereShape3D 53
        public const float SphereCenterY = 1.11655f;    // :10804 CollisionShape3D origin
        public const float SphereCenterZ = -0.0212681f; // :10804
        public const float DetectionRadius = 20f;       // :10212-10213 SphereShape3D 54 (PlayerDetectionArea)
        public const float Gravity = 9.8f;              // project default (same as the player)
        public const float ShootFireTime = 2.25f, ShootResumeTime = 3.0f, ShootLength = 4.0f;      // :10215+ "shoot" animation (length 4.0, method calls at 2.25 / 3.0)
        public const float ShootTreeOffFrom = 2.6f, ShootTreeOffTo = 2.9f;                         // shoot animation track 1: AnimationTree:active false..true
        /// <summary>RayMesh local transform inside RayFrom (tscn:10671): +90 deg about X, origin (0,1.25,0). XNA row-vector form (rows = images of the axes).</summary>
        static readonly Matrix RayMeshLocal = new Matrix(1, 0, 0, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 1.25f, 0, 1);

        readonly CollisionWorld _world, _rayWorld;   // _world = layers the body collides with (mask 3); _rayWorld = every static layer (sight / laser rays use mask 0xFFFFFFFF, incl. "Player Only" props)
        readonly CharacterMotor _motor;
        readonly int _cannonBone;
        readonly Random _rng = new Random(20251117);
        Vector3 _pos, _prevPos, _vel; float _yaw, _prevYaw;

        public readonly RobotAnimator Anim;
        public State Mode { get; private set; } = State.Idle;
        public int Health { get; private set; } = MaxHealth;
        public bool Dead { get; private set; }
        public bool HasTarget { get; private set; }     // red_robot.gd `player != null`
        public bool OnFloor { get; private set; }
        public Vector3 Position => _pos;
        public float Yaw => _yaw;
        /// <summary>Fixed-tick count since spawn.</summary>
        public int Ticks { get; private set; }
        public float ShootCountdown { get; private set; } = ShootWait;
        public float AimCountdown { get; private set; } = AimTime;
        public float AimPreparing { get; private set; } = AimPrepareTime;
        public float ShootClock { get; private set; }  // seconds into the shoot clip while it plays
        public bool ShootPlaying { get; private set; }
        public int ShotsFired { get; private set; }
        public bool TestShoot;                         // red_robot.gd test_shoot (debug: fire on the next tick)
        public int OwnerId = 1;
        /// <summary>Every robot of the simulation (CharacterBody3D layer 3 / mask 3: robots collide with each other); null = none.</summary>
        public System.Collections.Generic.IReadOnlyList<RedRobot> Peers;
        /// <summary>Explosion debris (RigidBody3D layer 3 / mask 3: robots cannot walk through it); null = none.</summary>
        public DebrisSystem Debris;

        /// <summary>Hitscan laser fired (shoot()): muzzle, end point, whether the player was the first thing hit.</summary>
        public event Action<Vector3, Vector3, bool> LaserFired;
        /// <summary>`exploded` signal: death position.</summary>
        public event Action<Vector3> Exploded;
        public event Action<int> Damaged; // health after the hit
        public event Action<State, State> StateChanged;
        /// <summary>Sound hooks (event, world position of the robot's SoundEffects node, gain scale): walk keys, cannon charge/shoot keys, hit, explosion.</summary>
        public event Action<AudioEvent, Vector3, float> Audio;

        public RedRobot(CollisionWorld levelWorld, SkinnedModel model, Vector3 position, float yaw, CollisionWorld rayWorld = null)
        {
            _world = levelWorld; _rayWorld = rayWorld ?? levelWorld; _pos = _prevPos = position; _yaw = _prevYaw = yaw;
            // sphere r 1.11815 as a capsule of height 2r (floor snap 0.1, slope 45, no step offset: CharacterBody3D sphere)
            _motor = new CharacterMotor(levelWorld, SphereRadius * 2f, SphereRadius, 45f, 0f, 0.01f) { Position = position };
            Anim = new RobotAnimator(model);
            Anim.WalkKey += w => Audio?.Invoke(AudioEvent.RobotWalk, _pos, w);
            _cannonBone = model.BoneIndex("CannonAnimRecoil");
        }

        // ------------------------------------------------------------------ geometry helpers
        public Matrix ModelWorld(float alpha) =>
            Matrix.CreateRotationY(MathHelper.Lerp(_prevYaw, UnwrapNear(_prevYaw, _yaw), alpha)) * Matrix.CreateTranslation(Vector3.Lerp(_prevPos, _pos, alpha));
        static float UnwrapNear(float a, float b) { while (b - a > MathHelper.Pi) b -= MathHelper.TwoPi; while (b - a < -MathHelper.Pi) b += MathHelper.TwoPi; return b; }
        Matrix Heading => Matrix.CreateRotationY(_yaw) * Matrix.CreateTranslation(_pos);
        /// <summary>Global transform of the RayFrom BoneAttachment3D (= the CannonAnimRecoil bone pose) at the current tick pose.</summary>
        public Matrix RayFromWorld => Anim.Pose.BoneModel(_cannonBone) * Heading;
        Matrix RayMeshWorld => RayMeshLocal * RayFromWorld;
        /// <summary>Centre of the body sphere.</summary>
        public Vector3 SphereCenter => _pos + Vector3.Up * SphereCenterY + new Vector3(MathF.Sin(_yaw), 0, MathF.Cos(_yaw)) * SphereCenterZ;

        /// <summary>red_robot.gd `target * global_transform` then atan2(x, z): angle of the target in the robot's frame (+Z front, +X left).</summary>
        float AngleTo(Vector3 target)
        {
            Vector3 d = target - _pos; float s = MathF.Sin(_yaw), c = MathF.Cos(_yaw);
            float lx = d.X * c - d.Z * s, lz = d.X * s + d.Z * c;   // dot with the images of X (cos,0,-sin) and Z (sin,0,cos)
            return MathF.Atan2(lx, lz);
        }

        static float DistToPlayer(Vector3 origin, Vector3 playerFeet)
        {
            // Area3D overlap of the sphere r 20 with the player capsule (r 0.5, axis y 0.5..1.5 above the feet)
            Vector3 a = playerFeet + Vector3.Up * 0.5f, b = playerFeet + Vector3.Up * 1.5f;
            float y = MathHelper.Clamp(origin.Y, a.Y, b.Y);
            return Vector3.Distance(origin, new Vector3(a.X, y, a.Z)) - 0.5f;
        }

        public bool PlayerVisible(Vector3 playerFeet)
        {
            Vector3 from = RayFromWorld.Translation, to = playerFeet + Vector3.Up;   // red_robot.gd:93-94 ray to player origin + UP
            Vector3 d = to - from; float len = d.Length(); if (len < 1e-4f) return true;
            return !_rayWorld.Raycast(from, d / len, len, out _);                          // first hit == player <=> nothing in the level before the (inside-the-capsule) end point
        }

        // ------------------------------------------------------------------ fixed tick
        public void Tick(float dt, Vector3 playerFeet)
        {
            _prevPos = _pos; _prevYaw = _yaw; Ticks++;
            if (Dead) return;

            // Area3D body_entered / body_exited (red_robot.gd:~265-275)
            bool inside = DistToPlayer(_pos, playerFeet) <= DetectionRadius;
            if (inside && !HasTarget) { HasTarget = true; SetMode(State.Approach); }
            else if (!inside && HasTarget) { HasTarget = false; SetMode(State.Idle); }

            Anim.Update(dt); // AnimationTree (physics callback) evaluates with the parameters of the previous tick, before _physics_process

            if (TestShoot) { Shoot(playerFeet); TestShoot = false; }

            Vector3 target = Vector3.Zero;
            if (!HasTarget)
            {
                Animate(dt, Vector3.Zero);
                _vel = new Vector3(0, -Gravity * dt, 0);   // :82 set_velocity(get_gravity() * delta)
                MoveAndSlide(dt, playerFeet);
                AdvanceShootClip(dt);
                return;
            }
            target = playerFeet;

            if (Mode == State.Approach)
            {
                if (AimPreparing > 0f) { AimPreparing -= dt; if (AimPreparing < 0f) AimPreparing = 0f; }
                float angle = AngleTo(target);
                if (angle > -PlayerAimTolerance && angle < PlayerAimTolerance)
                {
                    ShootCountdown -= dt;
                    if (ShootCountdown < 0f)
                    {
                        if (PlayerVisible(playerFeet)) { SetMode(State.Aim); AimCountdown = AimTime; AimPreparing = 0f; }
                        else ShootCountdown = ShootWait;
                    }
                }
            }
            else if (Mode == State.Aim || Mode == State.Shooting)
            {
                if (AimPreparing < AimPrepareTime) { AimPreparing += dt; if (AimPreparing > AimPrepareTime) AimPreparing = AimPrepareTime; }
                AimCountdown -= dt;
                if (AimCountdown < 0f && Mode == State.Aim)
                {
                    if (PlayerVisible(playerFeet)) { SetMode(State.Shooting); ShootCountdown = ShootWait; ShootPlaying = true; ShootClock = 0f; Audio?.Invoke(AudioEvent.RobotCharge, _pos, 1f); }
                    else ResumeApproach();
                }
            }

            Animate(dt, target);
            // orientation *= Transform3D(rootRot, rootPos); h_velocity = orientation.origin / delta (translation uses the heading before this tick's rotation)
            Vector3 h = Vector3.Transform(Anim.RootMotion, Matrix.CreateRotationY(_yaw)) / dt;
            _vel.X = h.X; _vel.Z = h.Z; _vel.Y -= Gravity * dt;
            MoveAndSlide(dt, playerFeet);
            _yaw += Anim.RootYaw;
            _yaw = MathF.IEEERemainder(_yaw, MathHelper.TwoPi);
            AdvanceShootClip(dt);
        }

        /// <summary>Debug (--sim item rshoot:clock): put the robot into the Shooting state with the shoot clip at <paramref name="clock"/> seconds, so a frame can show the beam without waiting ~9 s.</summary>
        public void DebugForceShoot(float clock) { HasTarget = true; SetMode(State.Shooting); ShootPlaying = true; ShootClock = clock; }
        void SetMode(State s) { if (s == Mode) return; var old = Mode; Mode = s; StateChanged?.Invoke(old, s); }

        void ResumeApproach() { SetMode(State.Approach); AimPreparing = AimPrepareTime; ShootCountdown = ShootWait; }   // :48-51

        /// <summary>red_robot.gd animate(): sets the AnimationTree parameters.</summary>
        void Animate(float dt, Vector3 target)
        {
            if (Mode == State.Approach)
            {
                float angle = AngleTo(target);
                if (angle > PlayerAimTolerance) Anim.Request = RobotAnimator.Loco.TurnLeft;
                else if (angle < -PlayerAimTolerance) Anim.Request = RobotAnimator.Loco.TurnRight;
                else if (target == Vector3.Zero) Anim.Request = RobotAnimator.Loco.Idle;
                else Anim.Request = RobotAnimator.Loco.Walk;
            }
            else Anim.Request = RobotAnimator.Loco.Idle;

            if (target != Vector3.Zero)
            {
                Anim.AimAmount = MathHelper.Clamp(AimPreparing / AimPrepareTime, 0f, 1f);
                Vector3 local = Vector3.Transform(target + Vector3.Up, Matrix.Invert(RayMeshWorld));   // (target + UP) * ray_mesh.global_transform
                float h = MathHelper.ToDegrees(MathF.Atan2(local.X, -local.Z)), v = MathHelper.ToDegrees(MathF.Atan2(local.Y, -local.Z));
                var bp = Anim.AimPos;
                bp.X = MathHelper.Clamp(bp.X + BlendAimSpeed * dt * -h, -1f, 1f);
                bp.Y = MathHelper.Clamp(bp.Y + BlendAimSpeed * dt * v, -1f, 1f);
                Anim.AimPos = bp;
            }
        }

        /// <summary>The "shoot" AnimationPlayer (processed after the physics body: test_shoot set at 2.25 s fires on the next tick, as in Godot).</summary>
        void AdvanceShootClip(float dt)
        {
            if (!ShootPlaying) return;
            float t0 = ShootClock; ShootClock += dt;
            if (t0 < AudioTable.RobotCannonShootKey && ShootClock >= AudioTable.RobotCannonShootKey) Audio?.Invoke(AudioEvent.RobotShoot, _pos, 1f);   // shoot anim audio track 2, key 2.05 s
            if (t0 < ShootFireTime && ShootClock >= ShootFireTime) TestShoot = true;                // shoot_check()
            if (t0 < ShootResumeTime && ShootClock >= ShootResumeTime) ResumeApproach();            // resume_approach()
            Anim.Frozen = ShootClock >= ShootTreeOffFrom && ShootClock < ShootTreeOffTo;            // AnimationTree:active track
            if (ShootClock >= ShootLength) { ShootPlaying = false; Anim.Frozen = false; }
        }

        // ------------------------------------------------------------------ movement
        void MoveAndSlide(float dt, Vector3 playerFeet)
        {
            bool wasOnFloor = OnFloor; Vector3 motion = _vel * dt;
            if (wasOnFloor && _vel.Y <= 0f) motion.Y = 0f;
            Vector3 before = _motor.Position;
            _motor.Move(motion, preventClimbing: wasOnFloor);
            float movedY = _motor.Position.Y - before.Y;
            if (_vel.Y > 0f && movedY < motion.Y * 0.5f) _vel.Y = 0f;
            bool floor = false;
            if (_vel.Y <= 0f)
            {
                float snap = wasOnFloor ? PlayerConstants.SnapLength : 0.05f;
                if (_motor.CapsuleCast(Vector3.Down, snap + PlayerConstants.SkinWidth, out float d, out Vector3 n) && _motor.IsWalkable(n))
                {
                    floor = true;
                    if (wasOnFloor && d > PlayerConstants.SkinWidth) _motor.Position += Vector3.Down * (d - PlayerConstants.SkinWidth * 0.5f);
                }
            }
            if (floor) _vel.Y = 0f;
            OnFloor = floor;
            // robot (layer 3) collides with the player body (layer 2): keep the spheres apart horizontally (the player capsule is not in the static world)
            Vector3 p = _motor.Position; Vector3 dd = new Vector3(p.X - playerFeet.X, 0, p.Z - playerFeet.Z); float dist = dd.Length(), min = SphereRadius + 0.5f;
            if (dist < min && Math.Abs(p.Y + SphereCenterY - (playerFeet.Y + 1f)) < SphereRadius + 1f) { if (dist < 1e-4f) dd = Vector3.UnitZ; else dd /= dist; _motor.Position = new Vector3(playerFeet.X, p.Y, playerFeet.Z) + dd * min; }
            // robot (layer 3) vs the other robots (layer 3) and the death debris (layer 3): both act as obstacles at their current pose (a CharacterBody3D move_and_slide stops at them)
            if (Peers != null) foreach (var o in Peers) if (o != this && !o.Dead) SeparateFromSphere(o.SphereCenter, SphereRadius);
            if (Debris != null) Debris.AppendSpheres(SeparateFromSphere);
            _pos = _motor.Position;
            if (_pos.Y < -60f) { Dead = true; }
        }

        /// <summary>Pushes the body sphere horizontally out of another sphere (same floor-bound resolution as the player push above).</summary>
        void SeparateFromSphere(Vector3 center, float r)
        {
            Vector3 p = _motor.Position, mine = p + Vector3.Up * SphereCenterY + new Vector3(MathF.Sin(_yaw), 0, MathF.Cos(_yaw)) * SphereCenterZ;
            float min = SphereRadius + r, dy = mine.Y - center.Y; if (Math.Abs(dy) >= min) return;
            float need = MathF.Sqrt(min * min - dy * dy);
            Vector3 dd = new Vector3(mine.X - center.X, 0, mine.Z - center.Z); float dist = dd.Length();
            if (dist >= need) return;
            if (dist < 1e-4f) dd = Vector3.UnitZ; else dd /= dist;
            _motor.Position = p + dd * (need - dist);
        }

        // ------------------------------------------------------------------ damage
        public void Hit(Vector3 point)
        {
            if (Dead) return;
            Anim.RequestHit(_rng.Next(3) + 1);   // :43 "parameters/hit" + str(randi() % 3 + 1) + "/request" = 1
            Audio?.Invoke(AudioEvent.RobotHit, _pos, 1f);    // red_robot.gd:84 hit_sound.play() (also on the lethal hit)
            Health--;
            Damaged?.Invoke(Health);
            if (Health == 0)
            {
                Dead = true; ShootPlaying = false; Anim.Frozen = false;
                Audio?.Invoke(AudioEvent.RobotExplosion, _pos, 1f);   // red_robot.gd:100
                Exploded?.Invoke(_pos + Vector3.Up * 1.1f);
            }
        }

        /// <summary>ProjectileManager.TargetSweep: bullet sphere vs the robot's body sphere (radii add), nothing while dead or for the robot's own shots.</summary>
        public bool Sweep(Vector3 from, Vector3 dir, float length, float radius, int owner, out float distance)
        {
            distance = 0f;
            if (Dead || owner == OwnerId) return false;
            Vector3 oc = from - SphereCenter; float R = SphereRadius + radius;
            float b = Vector3.Dot(oc, dir), c = oc.LengthSquared() - R * R;
            if (c <= 0f) { distance = 0f; return true; }                 // already overlapping
            float disc = b * b - c; if (disc < 0f) return false;
            float t = -b - MathF.Sqrt(disc);
            if (t < 0f || t > length) return false;
            distance = t; return true;
        }

        // ------------------------------------------------------------------ laser
        /// <summary>red_robot.gd shoot(): hitscan from the cannon along RayFrom basis.y, 1000 m, first hit among level and player.</summary>
        void Shoot(Vector3 playerFeet)
        {
            ComputeBeam(playerFeet, out Vector3 origin, out Vector3 end, out bool hitPlayer);
            ShotsFired++;
            LaserFired?.Invoke(origin, end, hitPlayer);
        }

        public void ComputeBeam(Vector3 playerFeet, out Vector3 origin, out Vector3 end, out bool hitPlayer)
        {
            Matrix g = RayFromWorld; origin = g.Translation; Vector3 dir = Vector3.Normalize(g.Up);   // basis.y
            float len = LaserMaxDist;
            if (_rayWorld.Raycast(origin, dir, LaserMaxDist, out var hit)) len = hit.Distance;
            hitPlayer = false;
            if (RayCapsule(origin, dir, len, playerFeet + Vector3.Up * 0.5f, playerFeet + Vector3.Up * 1.5f, 0.5f, out float tp)) { len = tp; hitPlayer = true; }
            end = origin + dir * len;
        }

        /// <summary>Ray vs capsule (segment a-b, radius r) using the closest approach between the ray and the axis (adequate for a 1 m upright capsule).</summary>
        static bool RayCapsule(Vector3 o, Vector3 d, float maxLen, Vector3 a, Vector3 b, float r, out float t)
        {
            t = 0f; Vector3 e = b - a, w = o - a; float A = Vector3.Dot(d, d), B = Vector3.Dot(d, e), C = Vector3.Dot(e, e), D = Vector3.Dot(d, w), E = Vector3.Dot(e, w);
            float den = A * C - B * B; float s = den > 1e-8f ? (B * E - C * D) / den : 0f;           // ray parameter
            float u = MathHelper.Clamp((E + B * s) / C, 0f, 1f); s = (B * u - D) / A; if (s < 0f) s = 0f;
            Vector3 pr = o + d * s, pc = a + e * u; float dist = Vector3.Distance(pr, pc);
            if (dist > r) return false;
            t = Math.Max(0f, s - MathF.Sqrt(r * r - dist * dist));
            return t <= maxLen + 1e-3f;
        }

        /// <summary>Beam visibility 0..1 over the shoot clip (1 - the "cutoff_value" track: hidden until 1.65 s, partly cut 1.7-2.1, open 2.2-2.3, closing to 2.6).</summary>
        public float BeamVisibility()
        {
            if (!ShootPlaying) return 0f;
            float[] ts = { 0, 1.65f, 1.7f, 2.1f, 2.2f, 2.3f, 2.4f, 2.6f }, cs = { 1, 1, 0.55f, 0.55f, 0, 0, 0.55f, 1 };
            float t = ShootClock; for (int i = 1; i < ts.Length; i++) if (t <= ts[i]) return 1f - MathHelper.Lerp(cs[i - 1], cs[i], (t - ts[i - 1]) / (ts[i] - ts[i - 1]));
            return 0f;
        }
    }
}
