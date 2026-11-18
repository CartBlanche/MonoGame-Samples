using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>Every Godot-sourced constant used by the player controller and camera (paths relative to tps-demo-master/).</summary>
    public static class PlayerConstants
    {
        // ---- player/player.gd ----
        public const float MotionInterpolateSpeed = 10f;   // player.gd:12 MOTION_INTERPOLATE_SPEED
        public const float RotationInterpolateSpeed = 10f; // player.gd:13 ROTATION_INTERPOLATE_SPEED
        public const float MinAirborneTime = 0.1f;         // player.gd:15 MIN_AIRBORNE_TIME
        public const float JumpSpeed = 5f;                 // player.gd:16 JUMP_SPEED
        public const float LandAirborneTime = 0.5f;        // player.gd:100-102 land() when airborne_time > 0.5
        public const float RespawnY = -40f;                // player.gd:175 respawn below y = -40
        public const float Gravity = 9.8f;                 // project.godot has no 3d default_gravity override -> Godot default 9.8 (player.gd:164 get_gravity())
        // ---- player/player.tscn ----
        public const float CapsuleRadius = 0.5f;           // player.tscn:239 CapsuleShape3D defaults (r 0.5, h 2.0)
        public const float CapsuleHeight = 2.0f;           // player.tscn:239; node offset y=1 at player.tscn:614 (feet at body origin)
        public const float SnapLength = 0.1f;              // Godot CharacterBody3D floor_snap_length default (player.tscn has no override)
        public const float SlopeLimitDeg = 45f;            // CharacterBody3D floor_max_angle default 45 deg
        public const float SkinWidth = 0.01f;              // approximation of safe_margin 0.001 (motor needs a little more)
        /// <summary>Godot uses a SeparationRayShape3D (player.tscn:617-618: length 1, from y=1.00357 down) rather than a step height; plan 03 sect.2 says use ~0.3-0.4.</summary>
        public const float StepOffset = 0.4f;
        public const float CameraPivotHeight = 1.6f;       // player.tscn:621-622 CameraBase transform y = 1.6
        public const float SpringLength = 2.4f;            // player.tscn:628 spring_length
        public const float SpringMargin = 0.01f;           // SpringArm3D default margin
        // SpringArm3D local basis player.tscn:627: rows (-1,0,0)(0,0.939693,0.34202)(0,0.34202,-0.939693) = 180 deg yaw + 20 deg pitch.
        public const float ArmPitchDeg = 20f;
        public const float Fov = 75f;                      // Camera3D default (player.tscn:630-632 only sets far)
        public static readonly float Near = float.TryParse(System.Environment.GetEnvironmentVariable("TPS_NEAR"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : 0.05f; // Camera3D default 0.05; TPS_NEAR overrides (depth-precision tests)
        public static readonly float Far = float.TryParse(System.Environment.GetEnvironmentVariable("TPS_FAR"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 300f;                     // player.tscn:632
        // ---- player/player_input.gd ----
        public const float StickLookSpeed = 3.0f;          // player_input.gd:4 CAMERA_CONTROLLER_ROTATION_SPEED (rad/s)
        public const float MouseLookSpeed = 0.001f;        // player_input.gd:5 CAMERA_MOUSE_ROTATION_SPEED (rad/pixel)
        public static readonly float PitchMin = MathHelper.ToRadians(-89.9f); // player_input.gd:7 CAMERA_X_ROT_MIN
        public static readonly float PitchMax = MathHelper.ToRadians(70f);    // player_input.gd:8 CAMERA_X_ROT_MAX
        public const float AimLookScaleMouse = 0.75f;      // player_input.gd:112 (used by part B)
        public const float AimLookScaleStick = 0.5f;       // player_input.gd:56-57 (used by part B)
        public const float FallFadeY = -17f;               // player_input.gd:101 fall fade start
        public const float StickDeadzone = 0.2f;           // plan 03 input map (stick deadzone 0.2)
        // ---- B2 shooting / aiming (tps-demo-master/player/...) ----
        public const float AimHoldThreshold = 0.4f;        // player_input.gd:13 AIM_HOLD_THRESHOLD (hold longer = hold-to-aim, shorter tap = toggle)
        public const float AimTweenTime = 0.5f;            // player.tscn:~276/~303 animations "shoot"/"far" length 0.5 (SpringArm3D:position track)
        public const float AimTweenEase = 0.3f;            // player.tscn:280,307 key-0 transition 0.3 -> Godot ease(t, 0.3) = 1 - (1-t)^(1/0.3)
        /// <summary>player.tscn:282,309 SpringArm3D:position (-0.6,-0.3,1.1) in Godot CameraRot space (view +Z). Port CameraRot space looks along -Z (180 deg about Y), so x and z flip: right shoulder = +X, forward = -Z.</summary>
        public static readonly Vector3 AimArmOffset = new Vector3(0.6f, -0.3f, -1.1f);
        public const float CrosshairFadeTime = 0.2f;       // player.tscn:~289-300 Crosshair self_modulate alpha 0<->1 over 0.2 s
        public const float FireCooldown = 0.4f;            // player.tscn:679 FireCooldown wait_time (restarted in shoot(), player.gd:199)
        public const float ShootTrauma = 0.35f;            // player.gd:201 add_camera_shake_trauma(0.35)
        public const float AimRayLength = 1000f;           // player_input.gd:90 intersect_ray length 1000
        public const float BulletSpeed = 20f;              // bullet.gd:4 BULLET_VELOCITY
        public const float BulletLifetime = 5f;            // bullet.gd:7 time_alive = 5.0
        public const float BulletRadius = 0.170413f;       // bullet.tscn:24 SphereShape3D radius
        public const float MuzzleForwardLocal = 0.4f;      // player.tscn:549 ShootFrom transform origin (0,0.4,0) under GunBone (BoneAttachment3D on hand.R)
        // camera_noise_shake_effect.gd:4-9
        public const float ShakeSpeed = 1f, ShakeDecayRate = 1.5f, ShakeMaxYaw = 0.05f, ShakeMaxPitch = 0.05f, ShakeMaxRoll = 0.1f, ShakeMaxTrauma = 1.2f;
        public const float ShakeTimeScale = 5000f;         // camera_noise_shake_effect.gd:46 time += delta * SPEED * 5000
        public const float ShakeNoiseFrequency = 0.01f;    // FastNoiseLite default frequency
        // ---- engine ----
        public const float PhysicsStep = 1f / 60f;         // Godot default physics_ticks_per_second 60 (physics interpolation on: project.godot:193)
        // TempRunSpeed (4.5 m/s guess) DELETED in B1: horizontal velocity is the root-motion delta of the animation tree (PlayerAnimator).
    }
}
