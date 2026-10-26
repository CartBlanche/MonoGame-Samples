using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Shooter.Scene;

namespace Shooter.Gameplay
{
    /// <summary>The Jetpack component's fields on the Player (Gameplay/Jetpack.cs defaults, overridden by the baked scene values).</summary>
    public sealed class JetpackSettings
    {
        public bool IsJetpackUnlockedAtStart;
        public float JetpackAcceleration = 7f;
        public float JetpackDownwardVelocityCancelingFactor = 1f;
        public float ConsumeDuration = 1.5f;
        public float RefillDurationGrounded = 2f;
        public float RefillDurationInTheAir = 5f;
        public float RefillDelay = 1f;
        public string SfxName;   // JetpackSfx (looping, 2D)

        public static JetpackSettings FromScene(SceneFile scene)
        {
            var s = new JetpackSettings();
            JsonElement j;
            if (scene.Player.Components != null && scene.Player.Components.TryGetValue("Jetpack", out j))
            {
                s.IsJetpackUnlockedAtStart = JsonFields.Bool(j, "IsJetpackUnlockedAtStart", s.IsJetpackUnlockedAtStart);
                s.JetpackAcceleration = JsonFields.Float(j, "JetpackAcceleration", s.JetpackAcceleration);
                s.JetpackDownwardVelocityCancelingFactor = JsonFields.Float(j, "JetpackDownwardVelocityCancelingFactor", s.JetpackDownwardVelocityCancelingFactor);
                s.ConsumeDuration = JsonFields.Float(j, "ConsumeDuration", s.ConsumeDuration);
                s.RefillDurationGrounded = JsonFields.Float(j, "RefillDurationGrounded", s.RefillDurationGrounded);
                s.RefillDurationInTheAir = JsonFields.Float(j, "RefillDurationInTheAir", s.RefillDurationInTheAir);
                s.RefillDelay = JsonFields.Float(j, "RefillDelay", s.RefillDelay);
                s.SfxName = JsonFields.AssetName(j, "JetpackSfx");
            }
            return s;
        }
    }

    /// <summary>
    /// Port of Unity's Jetpack (Assets/FPS/Scripts/Gameplay). It starts locked; a JetpackPickup unlocks it. After a jump, pressing jump again in the air
    /// arms it, and while jump is held (and fuel is left) it adds JetpackAcceleration plus gravity to the player's velocity (and cancels a downward
    /// velocity at once with a factor of 1), burning the fuel in ConsumeDuration seconds. The fuel refills after RefillDelay seconds without use,
    /// faster on the ground. Run <see cref="Update"/> after <see cref="PlayerController.Update"/> (it reads HasJumpedThisFrame and IsGrounded of that frame).
    /// The sound and the two trail emitters are driven by <see cref="Effects.JetpackEffects"/> from <see cref="IsInUse"/>.
    /// </summary>
    public sealed class Jetpack
    {
        private readonly PlayerController _player;
        private readonly JetpackSettings _s;
        private readonly float _gravityDownForce;
        private bool _canUseJetpack;
        private float _lastTimeOfUse;
        private float _time;

        /// <summary>1 is full, 0 is empty. Like Unity it is only clamped when the jetpack is not in use, so the frame the fuel runs out it can be slightly below 0 (FillBarColorChange flashes the bar on that).</summary>
        public float CurrentFillRatio { get; private set; }
        public bool IsJetpackUnlocked { get; private set; }
        /// <summary>The jetpack thrusts this frame (armed, unlocked, fuel left, jump held): the sound plays and the trails emit while true.</summary>
        public bool IsInUse { get; private set; }
        public JetpackSettings Settings { get { return _s; } }

        /// <summary>Jetpack.OnUnlockJetpack, raised by the first unlock only (NotificationHUDManager shows "Jetpack unlocked").</summary>
        public event Action Unlocked;

        public Jetpack(PlayerController player, JetpackSettings settings, float gravityDownForce)
        {
            _player = player;
            _s = settings;
            _gravityDownForce = gravityDownForce;
            IsJetpackUnlocked = settings.IsJetpackUnlockedAtStart;   // Start()
            CurrentFillRatio = 1f;
        }

        public void Update(float dt, PlayerInput input)
        {
            _time += dt;

            // jetpack can only be used if not grounded and jump has been pressed again once in-air
            if (_player.IsGrounded)
            {
                _canUseJetpack = false;
            }
            else if (!_player.HasJumpedThisFrame && input.JumpDown)
            {
                _canUseJetpack = true;
            }

            IsInUse = _canUseJetpack && IsJetpackUnlocked && CurrentFillRatio > 0f && input.JumpHeld;
            if (IsInUse)
            {
                // store the last time of use for refill delay
                _lastTimeOfUse = _time;

                float totalAcceleration = _s.JetpackAcceleration;

                // cancel out gravity
                totalAcceleration += _gravityDownForce;

                if (_player.CharacterVelocity.Y < 0f && dt > 0f)
                {
                    // compensate the downward velocity with bonus acceleration
                    totalAcceleration += (-_player.CharacterVelocity.Y / dt) * _s.JetpackDownwardVelocityCancelingFactor;
                }

                _player.CharacterVelocity += Vector3.Up * totalAcceleration * dt;

                // consume fuel
                CurrentFillRatio -= dt / _s.ConsumeDuration;
            }
            else
            {
                // refill the meter over time
                if (IsJetpackUnlocked && _time - _lastTimeOfUse >= _s.RefillDelay)
                {
                    float refillRate = 1f / (_player.IsGrounded ? _s.RefillDurationGrounded : _s.RefillDurationInTheAir);
                    CurrentFillRatio += dt * refillRate;
                }

                CurrentFillRatio = MathHelper.Clamp(CurrentFillRatio, 0f, 1f);
            }
        }

        /// <summary>JetpackPickup.OnPicked: false when it is already unlocked (the pickup stays).</summary>
        public bool TryUnlock()
        {
            if (IsJetpackUnlocked)
            {
                return false;
            }
            if (Unlocked != null) Unlocked();
            IsJetpackUnlocked = true;
            _lastTimeOfUse = _time;
            return true;
        }
    }
}
