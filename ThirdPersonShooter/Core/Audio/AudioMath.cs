using System;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Pure audio maths (no device): the bus model and the Godot AudioStreamPlayer3D attenuation, so `--test-audio` can check them headless.
    /// Bus model: Master bus of default_bus_layout.tres sits at -10 dB (<see cref="MasterBusDb"/>); the settings sliders (0..1, linear) multiply on top:
    /// effective = lin(-10 dB) x MasterVolume x (MusicVolume | SfxVolume). Music and every SFX go to Master (no per-type bus in Godot).
    /// 3D model (own implementation, not OpenAL Apply3D): Godot ATTENUATION_INVERSE_DISTANCE, att_db = 20 log10(unit_size / d) + volume_db, clamped to
    /// max_db = 3 (Godot defaults max_db 3.0, max_distance 0 = unlimited). Written from memory of audio_stream_player_3d.cpp (no Godot source in-tree): unverified.
    /// </summary>
    public static class AudioMath
    {
        public const float MasterBusDb = -10f;   // default_bus_layout.tres bus/0/volume_db
        public const float MaxDb = 3f;           // AudioStreamPlayer3D.max_db default
        const float Eps = 0.00001f;              // CMP_EPSILON

        public static float DbToLinear(float db) => MathF.Pow(10f, db / 20f);
        public static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;

        /// <summary>Linear gain of Master bus x one slider bus (music or sfx), both 0..1.</summary>
        public static float BusGain(float master, float bus) => DbToLinear(MasterBusDb) * Clamp01(master) * Clamp01(bus);

        /// <summary>Godot inverse-distance attenuation + node volume_db, clamped to max_db (dB, before the Master bus).</summary>
        public static float Attenuation3DDb(float distance, float unitSize, float volumeDb)
        {
            float att = 20f * MathF.Log10(1f / (distance / unitSize + Eps)) + volumeDb;
            return MathF.Min(att, MaxDb);
        }

        /// <summary>Stereo pan -1 (left) .. 1 (right) from the listener's right vector; 0 when the emitter sits on the listener.</summary>
        public static float Pan(Vector3 emitter, Vector3 listener, Vector3 right)
        {
            Vector3 d = emitter - listener; if (d.LengthSquared() < 1e-6f) return 0f;
            d.Normalize(); right.Normalize(); return MathHelper.Clamp(Vector3.Dot(d, right), -1f, 1f);
        }

        /// <summary>True when the periodic key time (key + n x period) lies in (prev, now]; times are un-wrapped playback seconds.</summary>
        public static bool Crossed(float prev, float now, float key, float period)
        {
            if (period <= 0f) return prev < key && now >= key;
            for (int n = (int)MathF.Floor(prev / period); n <= (int)MathF.Floor(now / period); n++) { float k = n * period + key; if (k > prev && k <= now) return true; }
            return false;
        }
    }
}
