using System;
using Microsoft.Xna.Framework;

namespace CatapultGame
{
    /// <summary>
    /// The flight maths shared by the projectile, which flies for real, and the AI, which
    /// tries shots on paper before taking one.
    /// </summary>
    static class Ballistics
    {
        public const float Gravity = 500f;

        /// <summary>Shots can be aimed anywhere between these angles above the horizon.</summary>
        public static readonly float MinAngle = MathHelper.ToRadians(15f);
        public static readonly float MaxAngle = MathHelper.ToRadians(75f);
        public static readonly float DefaultAngle = MathHelper.PiOver4;

        /// <summary>
        /// Shots used to be fired at a fixed 45 degrees with the same speed on both axes, so the
        /// launch speed is the shot strength times root two. That keeps the old range at 45 degrees.
        /// </summary>
        const float SpeedScale = 1.41421356f;

        /// <summary>Launch speed on each axis for a shot strength and angle. Both are positive going forward and up.</summary>
        public static Vector2 LaunchVelocity(float shotVelocity, float angle)
        {
            float speed = shotVelocity * SpeedScale;
            return new Vector2(speed * (float)Math.Cos(angle), speed * (float)Math.Sin(angle));
        }

        /// <summary>Where a shot is after t seconds. direction is 1 to fire right, -1 to fire left.</summary>
        public static Vector2 PositionAt(Vector2 start, int direction, Vector2 velocity, float wind, float gravity, float t)
        {
            return new Vector2(
                start.X + direction * velocity.X * t + 4f * wind * t * t,
                start.Y - velocity.Y * t + 0.5f * gravity * t * t);
        }

        /// <summary>Plays a shot forward on paper and returns the x where it meets the ground.</summary>
        public static float LandingX(Vector2 start, int direction, float shotVelocity, float angle, float wind)
        {
            Vector2 velocity = LaunchVelocity(shotVelocity, angle);
            const float step = 1f / 60f;
            for (float t = step; t < 12f; t += step)
            {
                Vector2 p = PositionAt(start, direction, velocity, wind, Gravity, t);
                if (p.Y >= Terrain.SurfaceY(p.X))
                    return p.X;
            }
            return start.X;
        }
    }
}
