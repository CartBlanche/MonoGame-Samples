using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Aim toggle state machine of player_input.gd:59-80 (aiming_timer / toggled_aim, AIM_HOLD_THRESHOLD 0.4 s) plus the camera animations "shoot"
    /// (aim) and "far" (release) of player.tscn: SpringArm3D:position tween over 0.5 s with Godot ease(t, 0.3) and the crosshair alpha over 0.2 s.
    /// Ticked at the fixed 60 Hz step with the held state of the aim button (RMB / left trigger).
    /// </summary>
    public sealed class AimController
    {
        bool _toggled, _prevHeld, _hasAnim;
        float _timer, _animT = PlayerConstants.AimTweenTime;
        bool _animToAim;
        public bool Aiming { get; private set; }
        /// <summary>SpringArm3D:position in the port's CameraRot space (feeds <see cref="ThirdPersonCamera.ArmOffset"/>).</summary>
        public Vector3 ArmOffset { get; private set; }
        public float CrosshairAlpha { get; private set; }

        /// <summary>Godot Math::ease for 0 &lt; c &lt; 1 (strong ease-out).</summary>
        public static float GodotEase(float x, float c)
        {
            x = MathHelper.Clamp(x, 0f, 1f);
            return c < 1f ? 1f - MathF.Pow(1f - x, 1f / c) : MathF.Pow(x, c);
        }

        public void Update(float dt, bool held)
        {
            bool released = _prevHeld && !held, pressed = !_prevHeld && held;
            bool current;
            if (released && _timer <= PlayerConstants.AimHoldThreshold) { current = true; _toggled = true; }
            else
            {
                current = _toggled || held;
                if (pressed) _toggled = false;
            }
            _timer = current ? _timer + dt : 0f;
            _prevHeld = held;
            if (Aiming != current) { Aiming = current; _animToAim = current; _animT = 0f; _hasAnim = true; } // camera_animation.play("shoot" / "far")
            if (!_hasAnim) return;
            _animT = Math.Min(_animT + dt, PlayerConstants.AimTweenTime);
            // Godot restarts the animation from key 0 (aim: 0 -> shoulder, far: shoulder -> 0), so an early release pops like Godot does
            Vector3 a = _animToAim ? Vector3.Zero : PlayerConstants.AimArmOffset, b = _animToAim ? PlayerConstants.AimArmOffset : Vector3.Zero;
            ArmOffset = Vector3.Lerp(a, b, GodotEase(_animT / PlayerConstants.AimTweenTime, PlayerConstants.AimTweenEase));
            float c = MathHelper.Clamp(_animT / PlayerConstants.CrosshairFadeTime, 0f, 1f);
            CrosshairAlpha = _animToAim ? c : 1f - c;
        }
    }
}
