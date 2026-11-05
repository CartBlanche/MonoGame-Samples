using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// The little crew member who stands beside each catapult. He stands about, cheers when his side
    /// scores or wins, and shakes both fists at the other side when it scores or he loses.
    /// </summary>
    class CatapultMan
    {
        public enum Mood { Idle, Cheer, Fist }

        readonly Texture2D[][] frames;   // [mood][frame]
        Vector2 feet;
        readonly SpriteEffects effects;

        Mood mood = Mood.Idle;
        float moodTimeLeft;
        bool permanent;
        float clock;

        /// <param name="feet">Where his feet touch the ground.</param>
        /// <param name="blueTeam">True for the blue crew, false for the red one.</param>
        /// <param name="facingRight">Which way he looks: towards the enemy.</param>
        public CatapultMan(GraphicsDevice device, Vector2 feet, bool blueTeam, bool facingRight)
        {
            frames = CatapultMenArt.Get(device).Frames(blueTeam);
            this.feet = feet;
            effects = facingRight ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        }

        /// <summary>Walks him to a new spot.</summary>
        public void MoveTo(Vector2 newFeet) { feet = newFeet; }

        /// <summary>Cheer for a few seconds, then go back to standing about.</summary>
        public void Cheer(float seconds) { Set(Mood.Cheer, seconds); }

        /// <summary>Shake both fists for a few seconds, then go back to standing about.</summary>
        public void ShakeFists(float seconds) { Set(Mood.Fist, seconds); }

        /// <summary>Cheer until told otherwise. For the winner of the match.</summary>
        public void CheerForever() { Set(Mood.Cheer, 0); permanent = true; }

        /// <summary>Shake fists until told otherwise. For the loser of the match.</summary>
        public void ShakeFistsForever() { Set(Mood.Fist, 0); permanent = true; }

        void Set(Mood m, float seconds)
        {
            if (permanent)
                return;
            mood = m;
            moodTimeLeft = seconds;
            clock = 0;
        }

        public void Update(float elapsed)
        {
            clock += elapsed;
            if (!permanent && mood != Mood.Idle)
            {
                moodTimeLeft -= elapsed;
                if (moodTimeLeft <= 0)
                    mood = Mood.Idle;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            float rate = mood == Mood.Idle ? 1.5f : mood == Mood.Cheer ? 5f : 9f;
            Texture2D[] set = frames[(int)mood];
            Texture2D frame = set[(int)(clock * rate) % set.Length];
            spriteBatch.Draw(frame, feet, null, Color.White, 0f, CatapultMenArt.Feet, 1f, effects, 0f);
        }
    }

    /// <summary>
    /// The crew's textures, drawn in code in the same flat style as the rest of the game's art.
    /// Every figure faces right; the right-hand crew is drawn mirrored.
    /// </summary>
    class CatapultMenArt
    {
        public const int Width = 44;
        public const int Height = 48;

        /// <summary>The point in each texture that sits on the ground.</summary>
        public static readonly Vector2 Feet = new Vector2(22, 46);

        static readonly Color Skin = new Color(238, 196, 146);
        static readonly Color Cap = new Color(150, 98, 52);
        static readonly Color Ink = new Color(52, 34, 22);
        static readonly Color Trousers = new Color(78, 58, 40);

        // [team][mood][frame]
        readonly Texture2D[][][] teams = new Texture2D[2][][];

        static CatapultMenArt instance;
        static GraphicsDevice instanceDevice;

        public static CatapultMenArt Get(GraphicsDevice device)
        {
            if (instance == null || instanceDevice != device)
            {
                instance = new CatapultMenArt(device);
                instanceDevice = device;
            }
            return instance;
        }

        public Texture2D[][] Frames(bool blueTeam)
        {
            return teams[blueTeam ? 0 : 1];
        }

        CatapultMenArt(GraphicsDevice gd)
        {
            teams[0] = Build(gd, new Color(52, 100, 206), new Color(30, 62, 140));
            teams[1] = Build(gd, new Color(208, 62, 52), new Color(140, 36, 30));
        }

        // Arm angles are measured from hanging straight down: 90 points forward, 180 straight up.
        static Texture2D[][] Build(GraphicsDevice gd, Color tunic, Color belt)
        {
            return new[]
            {
                // Idle: standing about, drawing breath
                new[]
                {
                    Figure(gd, tunic, belt, 0f, -24f, 24f, Face.Smile),
                    Figure(gd, tunic, belt, 1f, -18f, 18f, Face.Smile),
                },
                // Cheer: both arms up, bouncing
                new[]
                {
                    Figure(gd, tunic, belt, 3f, -150f, 150f, Face.Cheer),
                    Figure(gd, tunic, belt, 0f, -172f, 172f, Face.Cheer),
                },
                // Fist: both fists raised and shaking, scowling
                new[]
                {
                    Figure(gd, tunic, belt, 0f, -112f, 142f, Face.Angry),
                    Figure(gd, tunic, belt, 1f, -140f, 112f, Face.Angry),
                },
            };
        }

        enum Face { Smile, Cheer, Angry }

        static Texture2D Figure(GraphicsDevice gd, Color tunic, Color belt, float lift,
            float backArm, float frontArm, Face face)
        {
            float cx = 22f, up = -lift;
            var layers = new System.Collections.Generic.List<(HalloweenArt.Shape, Color)>();

            // Legs and boots
            layers.Add((Capsule(new Vector2(cx - 3, 34 + up), new Vector2(cx - 4, 44), 2.7f), Trousers));
            layers.Add((Capsule(new Vector2(cx + 3, 34 + up), new Vector2(cx + 4, 44), 2.7f), Trousers));
            layers.Add((HalloweenArt.Ellipse(cx - 4, 45, 3.6f, 1.8f), Ink));
            layers.Add((HalloweenArt.Ellipse(cx + 5, 45, 3.6f, 1.8f), Ink));

            // Arms sit behind the body so raised ones read as coming from the shoulders
            AddArm(layers, new Vector2(cx - 7, 24 + up), backArm, tunic, face == Face.Angry);
            AddArm(layers, new Vector2(cx + 7, 24 + up), frontArm, tunic, face == Face.Angry);

            // Tunic and belt
            layers.Add((HalloweenArt.Ellipse(cx, 28 + up, 8.5f, 9f), tunic));
            layers.Add((HalloweenArt.Rect(cx - 8, 32 + up, cx + 8, 34 + up), belt));

            // Head, with a leather cap
            layers.Add((HalloweenArt.Ellipse(cx, 14 + up, 7f, 7f), Skin));
            var cap = HalloweenArt.Ellipse(cx, 11.5f + up, 7.8f, 6.2f);
            float capBottom = 12f + up;
            layers.Add(((x, y) => y <= capBottom && cap(x, y), Cap));

            // Face
            Color ink = Ink;
            layers.Add((HalloweenArt.Ellipse(cx - 2.5f, 14.5f + up, 0.95f, 1.1f), ink));
            layers.Add((HalloweenArt.Ellipse(cx + 3f, 14.5f + up, 0.95f, 1.1f), ink));
            switch (face)
            {
                case Face.Smile:
                    layers.Add((Capsule(new Vector2(cx - 1.5f, 18f + up), new Vector2(cx + 3f, 18f + up), 0.6f), ink));
                    break;
                case Face.Cheer:
                {
                    // Open, laughing mouth
                    var mouth = HalloweenArt.Ellipse(cx + 0.5f, 17.2f + up, 3f, 2.6f);
                    float top = 17.2f + up;
                    layers.Add(((x, y) => y >= top && mouth(x, y), new Color(150, 40, 40)));
                    break;
                }
                case Face.Angry:
                    // Slanted brows and a shouting mouth
                    layers.Add((Capsule(new Vector2(cx - 4.5f, 11.8f + up), new Vector2(cx - 1f, 13.2f + up), 0.7f), ink));
                    layers.Add((Capsule(new Vector2(cx + 1.5f, 13.2f + up), new Vector2(cx + 5f, 11.4f + up), 0.7f), ink));
                    layers.Add((HalloweenArt.Ellipse(cx + 0.5f, 18.3f + up, 2.4f, 1.8f), new Color(150, 40, 40)));
                    break;
            }

            var list = new (HalloweenArt.Shape, Color)[layers.Count];
            layers.CopyTo(list);
            return HalloweenArt.Render(gd, Width, Height, list);
        }

        static void AddArm(System.Collections.Generic.List<(HalloweenArt.Shape, Color)> layers,
            Vector2 shoulder, float degrees, Color sleeve, bool fist)
        {
            float a = MathHelper.ToRadians(degrees);
            Vector2 hand = shoulder + new Vector2((float)Math.Sin(a), (float)Math.Cos(a)) * 11f;
            layers.Add((Capsule(shoulder, hand, 2.5f), sleeve));
            layers.Add((HalloweenArt.Ellipse(hand.X, hand.Y, fist ? 3.4f : 2.6f, fist ? 3.4f : 2.6f), Skin));
        }

        /// <summary>A thick line with round ends.</summary>
        static HalloweenArt.Shape Capsule(Vector2 a, Vector2 b, float radius)
        {
            Vector2 ab = b - a;
            float len2 = Math.Max(ab.LengthSquared(), 0.0001f);
            return (x, y) =>
            {
                float t = MathHelper.Clamp(Vector2.Dot(new Vector2(x, y) - a, ab) / len2, 0f, 1f);
                return Vector2.DistanceSquared(new Vector2(x, y), a + ab * t) <= radius * radius;
            };
        }
    }
}
