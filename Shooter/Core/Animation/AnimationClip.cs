using System;
using Microsoft.Xna.Framework;

namespace Shooter.Animation
{
    /// <summary>Keyframed local-space TRS samples for one bone, in the same space as the skeleton.</summary>
    public sealed class BoneTrack
    {
        public string BoneName;
        public float[] Times;
        public Vector3[] Translations;
        public Quaternion[] Rotations;
        public Vector3[] Scales;

        public int KeyCount
        {
            get { return Times.Length; }
        }

        /// <summary>Samples the track (linear for T/S, slerp for R). Time is clamped to the key range.</summary>
        public void Sample(float time, out Vector3 t, out Quaternion r, out Vector3 s)
        {
            int n = Times.Length;
            if (n == 1 || time <= Times[0])
            {
                t = Translations[0];
                r = Rotations[0];
                s = Scales[0];
                return;
            }

            if (time >= Times[n - 1])
            {
                t = Translations[n - 1];
                r = Rotations[n - 1];
                s = Scales[n - 1];
                return;
            }

            // binary search for the last key <= time
            int lo = 0;
            int hi = n - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) >> 1;
                if (Times[mid] <= time)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            float span = Times[hi] - Times[lo];
            float a = span > 1e-8f ? (time - Times[lo]) / span : 0f;
            t = Vector3.Lerp(Translations[lo], Translations[hi], a);
            r = Quaternion.Slerp(Rotations[lo], Rotations[hi], a);
            s = Vector3.Lerp(Scales[lo], Scales[hi], a);
        }
    }

    /// <summary>
    /// One animation clip (Unity "AnimationClip"): per-bone TRS tracks plus the import flags that matter at runtime.
    /// Bones are addressed by name so one clip can drive any skeleton that has those bone names.
    /// </summary>
    public sealed class AnimationClip
    {
        /// <summary>Unity clip name (e.g. "RunFoward").</summary>
        public string Name;

        /// <summary>Length in seconds (Unity: (lastFrame - firstFrame) / frameRate).</summary>
        public float Duration;

        public float FrameRate;

        /// <summary>Unity "Loop Time" import flag. The clip wraps when played from a looping state.</summary>
        public bool Loop;

        /// <summary>True when the root-most animated bone translates over the clip (none of the shipped clips do).</summary>
        public bool HasRootMotion;

        /// <summary>Root-most animated bone: translation(end) - translation(start), in model units.</summary>
        public Vector3 RootMotionDelta;

        public BoneTrack[] Tracks;

        public int FindTrack(string boneName)
        {
            for (int i = 0; i < Tracks.Length; i++)
            {
                if (string.Equals(Tracks[i].BoneName, boneName, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Maps a clip time (seconds) from a possibly-unbounded value into [0, Duration].</summary>
        public float WrapTime(float time, bool loop)
        {
            if (Duration <= 0f)
            {
                return 0f;
            }

            if (loop)
            {
                float t = time % Duration;
                return t < 0f ? t + Duration : t;
            }

            return MathHelper.Clamp(time, 0f, Duration);
        }

        public override string ToString()
        {
            return "AnimationClip(" + Name + ", " + Duration.ToString("0.###") + "s, loop=" + Loop + ")";
        }
    }
}
