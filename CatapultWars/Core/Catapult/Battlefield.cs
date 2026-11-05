using System;
using Microsoft.Xna.Framework;

namespace CatapultGame
{
    /// <summary>The overall profile of the hill between the catapults.</summary>
    enum HillShape
    {
        Round,
        Pointy,
        Plateau,
        Twin,
    }

    /// <summary>
    /// Where everything stands for one game: the hill, the two catapults and their crews. A layout
    /// comes from a single number, so in a LAN game the host picks it and the other machine builds
    /// exactly the same battlefield from it. The catapults always stand the same distance from the hill.
    /// </summary>
    static class Battlefield
    {
        /// <summary>The number the current layout was built from.</summary>
        public static int Seed { get; private set; }

        public static float HillCentre { get; private set; }
        public static float HillHalfWidth { get; private set; }
        public static float HillHeight { get; private set; }
        public static HillShape Shape { get; private set; }
        public static bool TwinTallerOnRight { get; private set; }
        public static float WobblePhase { get; private set; }

        /// <summary>Top-left of each catapult's sprite. The ground line is the same for both.</summary>
        public static Vector2 LeftCatapult { get; private set; }
        public static Vector2 RightCatapult { get; private set; }

        /// <summary>Where each crew member's feet touch the ground.</summary>
        public static Vector2 LeftMan { get; private set; }
        public static Vector2 RightMan { get; private set; }

        /// <summary>Stretches of ground that are never dug out: the catapults and their crews stand on them.</summary>
        public static Vector2 LeftPad { get; private set; }
        public static Vector2 RightPad { get; private set; }

        // The idle catapult sprite is 75 wide; its middle is what sits a set distance from the hill
        const float CatapultWidth = 75f;
        const float FireFrameWidth = 122f; // the red catapult's throwing arm swings out this far
        const float RowY = 332f;
        const float ManY = 394f;
        const float ManGap = 5f;  // how close the crew stand to the back of their catapult

        static Battlefield()
        {
            // The layout the game always used, until a new game picks another
            Build(0, 407.5f, 230f, 125f, 80f, HillShape.Round, false, 0f);
        }

        public static int NewSeed()
        {
            return new Random().Next(1, int.MaxValue);
        }

        /// <summary>Builds the layout for a seed. The same seed always gives the same battlefield.</summary>
        public static void Generate(int seed)
        {
            var rng = new Random(seed);

            float halfWidth = rng.Next(85, 151);
            float height = rng.Next(55, 96);
            var shape = (HillShape)rng.Next(4);
            float centre = rng.Next(350, 451);
            bool twinRight = rng.Next(2) == 1;
            float phase = (float)(rng.NextDouble() * Math.PI * 2);

            // Far enough from the hill to clear its foot, close enough that the crews stay on screen
            float minDistance = Math.Max(215f, halfWidth + 80f);
            float maxDistance = Math.Min(275f, Math.Min(centre - 80f, 700f - centre));
            if (maxDistance < minDistance)
                minDistance = maxDistance;
            float distance = minDistance + rng.Next((int)(maxDistance - minDistance) + 1);

            Build(seed, centre, distance, halfWidth, height, shape, twinRight, phase);
        }

        static void Build(int seed, float centre, float distance, float halfWidth, float height,
            HillShape shape, bool twinRight, float phase)
        {
            Seed = seed;
            HillCentre = centre;
            HillHalfWidth = halfWidth;
            HillHeight = height;
            Shape = shape;
            TwinTallerOnRight = twinRight;
            WobblePhase = phase;

            float left = centre - distance - CatapultWidth / 2f;
            float right = centre + distance - CatapultWidth / 2f;
            LeftCatapult = new Vector2(left, RowY);
            RightCatapult = new Vector2(right, RowY);
            LeftMan = new Vector2(left - ManGap, ManY);
            RightMan = new Vector2(right + CatapultWidth + ManGap, ManY);

            LeftPad = new Vector2(LeftMan.X - 22f, left + CatapultWidth + 6f);
            RightPad = new Vector2(right - 6f, RightMan.X + 22f);
        }

        /// <summary>How high the hill stands above the flat ground at x. Zero beyond its foot.</summary>
        public static float HillAt(float x)
        {
            float u = (x - HillCentre) / HillHalfWidth;
            if (u <= -1f || u >= 1f)
                return 0f;

            float profile;
            switch (Shape)
            {
                case HillShape.Pointy:
                    profile = (float)Math.Pow(Bump(u), 1.7);
                    break;
                case HillShape.Plateau:
                    profile = (float)Math.Pow(Bump(u), 0.6);
                    break;
                case HillShape.Twin:
                    float a = Bump((u + 0.5f) / 0.55f), b = Bump((u - 0.5f) / 0.55f);
                    profile = TwinTallerOnRight ? Math.Max(0.8f * a, b) : Math.Max(a, 0.8f * b);
                    break;
                default:
                    profile = Bump(u);
                    break;
            }

            // A little unevenness so no two hills are quite the same, fading out at the foot
            float wobble = 0.035f * (float)Math.Sin(u * 7f + WobblePhase) * Bump(u);
            return HillHeight * MathHelper.Clamp(profile + wobble, 0f, 1f);
        }

        // 1 at the middle, easing to 0 at u = +-1, and 0 outside
        static float Bump(float u)
        {
            if (u <= -1f || u >= 1f)
                return 0f;
            return 0.5f * (1f + (float)Math.Cos(Math.PI * u));
        }
    }
}
