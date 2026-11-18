using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace ThirdPersonShooter
{
    /// <summary>
    /// The level's one animation: level.tscn AnimationPlayer "mawaru" on the FlyingForklifts node (autoplay, 30 s, loop_mode 1, one value track ".:rotation_degrees",
    /// linear, keys 0 s = (0,0,0), 30 s = (0,-360,0)); read from level.scene.json `dynamicAnimations[].valueTracks` so the numbers stay in the data.
    /// Time is the 60 Hz sim clock (frozen while paused). Also holds the flying_forklift.gd random-model rule (fixed seed here, Godot calls randomize()).
    /// </summary>
    public sealed class LevelAnimation
    {
        public const int ForkliftSeed = 1117;   // port choice: flying_forklift.gd uses randomize(); we pick reproducibly
        public float Length = 30f; public int LoopMode = 1; float[] _times = { 0f, 30f }; Vector3[] _values = { Vector3.Zero, new Vector3(0, -360, 0) };

        public static LevelAnimation Load(string contentRoot)
        {
            var a = new LevelAnimation();
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(contentRoot, "Data", "level.scene.json")));
            foreach (var an in doc.RootElement.GetProperty("dynamicAnimations").EnumerateArray())
            {
                if (an.GetProperty("name").GetString() != "mawaru") continue;
                a.Length = an.GetProperty("length").GetSingle(); a.LoopMode = an.GetProperty("loopMode").GetInt32();
                var keys = an.GetProperty("valueTracks")[0].GetProperty("keys");
                var ts = keys.GetProperty("times").GetProperty("values"); var vs = keys.GetProperty("values");
                a._times = new float[ts.GetArrayLength()]; a._values = new Vector3[a._times.Length];
                for (int i = 0; i < a._times.Length; i++)
                {
                    a._times[i] = ts[i].GetSingle(); var v = vs[i].GetProperty("values");
                    a._values[i] = new Vector3(v[0].GetSingle(), v[1].GetSingle(), v[2].GetSingle());
                }
            }
            return a;
        }

        /// <summary>rotation_degrees of the FlyingForklifts node at animation time t (loop_mode 1 = wrap at the length).</summary>
        public Vector3 RotationDegrees(float t)
        {
            t = LoopMode == 1 ? t - MathF.Floor(t / Length) * Length : Math.Clamp(t, 0f, Length);
            int n = _times.Length; if (t <= _times[0]) return _values[0];
            for (int i = 1; i < n; i++)
                if (t <= _times[i]) return Vector3.Lerp(_values[i - 1], _values[i], (t - _times[i - 1]) / (_times[i] - _times[i - 1]));
            return _values[n - 1];
        }

        /// <summary>Node transform of FlyingForklifts at time t; a forklift's world = (its flattened world) * this (parent applied last in the row-vector convention).</summary>
        public Matrix ParentTransform(float t)
        {
            var d = RotationDegrees(t);
            return Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(d.Y), MathHelper.ToRadians(d.X), MathHelper.ToRadians(d.Z)); // Godot Euler order YXZ
        }

        /// <summary>flying_forklift.gd _ready: which = floor(randf() * child_count) of the glb root's children (Red, Yellow, Blue); only that child is visible.</summary>
        public static int PickVariant(int forkliftIndex, int count) => Math.Min(count - 1, (int)MathF.Floor((float)new Random(ForkliftSeed * 31 + forkliftIndex).NextDouble() * count));
    }
}
