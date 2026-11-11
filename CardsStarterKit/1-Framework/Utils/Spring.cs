//-----------------------------------------------------------------------------
// Spring.cs
//
// A small damped spring used for "juice": hover lean, drag swing-back, landing
// squash, ripples. Unlike a time-based tween it keeps velocity, so it can
// overshoot and settle, and it can be kicked with an impulse at any time.
//-----------------------------------------------------------------------------

using System;

namespace CardsFramework
{
    /// <summary>
    /// A damped spring that moves <see cref="Value"/> toward <see cref="Target"/>.
    /// Damping below 2*sqrt(Stiffness) is underdamped (overshoots and swings back).
    /// </summary>
    public struct Spring
    {
        // Integrate in fixed small steps so large frame times can't blow the spring up.
        private const float MaxStep = 1f / 120f;
        private const int MaxSteps = 8;

        public float Value;
        public float Velocity;
        public float Target;
        public float Stiffness;
        public float Damping;

        public Spring(float stiffness, float damping, float value = 0f)
        {
            Stiffness = stiffness;
            Damping = damping;
            Value = value;
            Target = value;
            Velocity = 0f;
        }

        /// <summary>True when the spring has effectively stopped on its target.</summary>
        public bool IsAtRest(float epsilon = 0.0005f) =>
            Math.Abs(Value - Target) < epsilon && Math.Abs(Velocity) < epsilon;

        /// <summary>Advances the spring by <paramref name="dt"/> seconds.</summary>
        public void Update(float dt)
        {
            if (dt <= 0f) return;

            int steps = Math.Min(MaxSteps, (int)Math.Ceiling(dt / MaxStep));
            float h = Math.Min(dt, MaxSteps * MaxStep) / steps;
            for (int i = 0; i < steps; i++)
            {
                // Semi-implicit Euler: velocity first, then position.
                Velocity += (-Stiffness * (Value - Target) - Damping * Velocity) * h;
                Value += Velocity * h;
            }
        }

        /// <summary>Adds an instant velocity change (a "kick").</summary>
        public void Impulse(float velocity) => Velocity += velocity;

        /// <summary>Jumps to a value and stops.</summary>
        public void Snap(float value)
        {
            Value = value;
            Target = value;
            Velocity = 0f;
        }
    }

    /// <summary>Common easing curves. All take t in 0..1 and return roughly 0..1.</summary>
    public static class Easing
    {
        public static float OutCubic(float t)
        {
            t = Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float InOutCubic(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 3) / 2f;
        }

        /// <summary>Overshoots past 1 then settles back.</summary>
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Clamp01(t);
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private static float Clamp01(float t) => t < 0f ? 0f : (t > 1f ? 1f : t);
    }
}
