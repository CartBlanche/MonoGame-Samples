using System;
using System.Collections.Generic;

namespace ThirdPersonShooter
{
    /// <summary>
    /// Foot-contact phases derived from the clips themselves (the glb carries no footstep markers and the Godot demo never plays its Step node):
    /// for each foot bone the model-space height is sampled over the loop; the contact is the moment the foot drops into the lowest 25% of its range
    /// (touch-down, first sample below the threshold before the global minimum). Result = fractions of the clip duration, sorted.
    /// </summary>
    public static class FootstepTable
    {
        static readonly Dictionary<(SkinnedModel, string), float[]> Cache = new();
        public static readonly string[] FootClips = { "running_nogun", "strafe_left", "strafe_right", "strafe_front", "strafe_back" };

        public static float[] For(SkinnedModel m, string clip)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue((m, clip), out var c)) return c;
                return Cache[(m, clip)] = Array.Exists(FootClips, x => x == clip) && m.Clips.TryGetValue(clip, out var sc) ? Compute(m, sc) : Array.Empty<float>();
            }
        }

        public static float[] Compute(SkinnedModel m, SkinnedClip clip)
        {
            int n = Math.Max(8, (int)MathF.Round(clip.Duration * 60f));
            var res = new List<float>();
            var ap = new AnimationPlayer(m);
            foreach (var foot in new[] { "foot.L", "foot.R" })
            {
                int b = m.BoneIndex(foot); if (b < 0) continue;
                var y = new float[n]; float lo = float.MaxValue, hi = float.MinValue; int imin = 0;
                for (int i = 0; i < n; i++)
                {
                    float t = clip.Duration * i / n; ap.BeginFrame(); ap.Add(clip, t, t, 1f); ap.Finish();
                    y[i] = ap.BoneModel(b).Translation.Y; if (y[i] < lo) { lo = y[i]; imin = i; } if (y[i] > hi) hi = y[i];
                }
                float thr = lo + 0.25f * (hi - lo); int k = imin;
                for (int s = 0; s < n && y[((k - 1) % n + n) % n] <= thr; s++) k = ((k - 1) % n + n) % n; // walk back to the first sample inside the low band
                res.Add((float)k / n);
            }
            res.Sort(); return res.ToArray();
        }
    }
}
