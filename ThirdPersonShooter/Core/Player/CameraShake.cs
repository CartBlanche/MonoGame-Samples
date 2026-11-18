using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// camera_noise_shake_effect.gd: trauma (max 1.2, decays 1.5/s), shake = trauma^2, time += dt * 5000, rotation offset = (pitch 0.05, yaw 0.05, roll 0.1) * shake * noise.
    /// Godot uses OpenSimplex 1D (FastNoiseLite, frequency 0.01); this uses 1D gradient noise with the same sampling rate. The offset is view-only:
    /// it never touches the physics, the aim ray or the camera object (apply with <see cref="Apply"/> to the final view matrix).
    /// </summary>
    public sealed class CameraShake
    {
        public float Trauma { get; private set; }
        float _time;
        readonly int _seed;
        public Vector3 Offset { get; private set; } // (pitch, yaw, roll) radians

        public CameraShake(int seed = 12345) { _seed = seed; }

        /// <summary>Raised for every AddTrauma (rumble hook, port-only enhancement).</summary>
        public event Action<float> TraumaAdded;

        public void AddTrauma(float amount) { Trauma = Math.Min(Trauma + amount, PlayerConstants.ShakeMaxTrauma); TraumaAdded?.Invoke(amount); }

        /// <summary>One fixed tick: decay then apply (script order decay_trauma, apply_shake).</summary>
        public void Update(float dt)
        {
            if (Trauma <= 0f) { Offset = Vector3.Zero; return; }
            Trauma = Math.Max(Trauma - PlayerConstants.ShakeDecayRate * dt, 0f);
            _time += dt * PlayerConstants.ShakeSpeed * PlayerConstants.ShakeTimeScale;
            float shake = Trauma * Trauma;
            Offset = new Vector3(PlayerConstants.ShakeMaxPitch * shake * Noise(_seed + 1, _time), PlayerConstants.ShakeMaxYaw * shake * Noise(_seed, _time), PlayerConstants.ShakeMaxRoll * shake * Noise(_seed + 2, _time));
        }

        /// <summary>View matrix with the shake rotation applied in camera space.</summary>
        public Matrix Apply(Matrix view)
        {
            if (Offset == Vector3.Zero) return view;
            Matrix world = Matrix.Invert(view);
            return Matrix.Invert(Matrix.CreateFromYawPitchRoll(Offset.Y, Offset.X, Offset.Z) * world);
        }

        static float Grad(int seed, int i)
        {
            uint h = (uint)(i * 374761393 + seed * 668265263); h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x7FFFFF - 1f; // -1..1
        }

        static float Noise(int seed, float t)
        {
            float x = t * PlayerConstants.ShakeNoiseFrequency; int i = (int)MathF.Floor(x); float f = x - i;
            float u = f * f * f * (f * (f * 6f - 15f) + 10f);
            return MathHelper.Clamp(MathHelper.Lerp(Grad(seed, i) * f, Grad(seed, i + 1) * (f - 1f), u) * 2f, -1f, 1f);
        }
    }
}
