using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static CatapultGame.ShapeArt;

namespace CatapultGame
{
    /// <summary>The colours of one reindeer. A null nose is a plain dark one; a null eye is the usual dark dot.</summary>
    sealed class ReindeerLook
    {
        public Color Fur, Dark, Belly, Horn, Ink;
        public Color? RedNose;
        public Color? Eye;
    }

    /// <summary>
    /// The reindeer standing on the far hill in every theme. It faces left. Frame 0 has its head up,
    /// frame 1 bends down to graze. Each theme gives it a look of its own.
    /// </summary>
    static class ReindeerArt
    {
        /// <summary>Plain brown, with a plain dark nose: the classic look.</summary>
        public static readonly ReindeerLook Natural = new ReindeerLook
        {
            Fur = new Color(150, 102, 62), Dark = new Color(112, 76, 46), Belly = new Color(214, 178, 138),
            Horn = new Color(222, 196, 150), Ink = new Color(40, 26, 18),
        };

        /// <summary>The Christmas one, with a glowing red nose.</summary>
        public static readonly ReindeerLook RedNosed = new ReindeerLook
        {
            Fur = new Color(150, 102, 62), Dark = new Color(112, 76, 46), Belly = new Color(214, 178, 138),
            Horn = new Color(222, 196, 150), Ink = new Color(40, 26, 18), RedNose = new Color(226, 34, 34),
        };

        /// <summary>Dusky fur and charred antlers, for the ones that are on fire, and ember eyes.</summary>
        public static readonly ReindeerLook Haunted = new ReindeerLook
        {
            Fur = new Color(104, 78, 84), Dark = new Color(70, 52, 60), Belly = new Color(150, 122, 130),
            Horn = new Color(58, 40, 40), Ink = new Color(24, 16, 20), Eye = new Color(255, 150, 40),
        };

        public static Texture2D[] Build(GraphicsDevice gd, ReindeerLook look)
        {
            return new[] { Frame(gd, look, false), Frame(gd, look, true) };
        }

        static Texture2D Frame(GraphicsDevice gd, ReindeerLook look, bool grazing)
        {
            Color fur = look.Fur, dark = look.Dark, belly = look.Belly, horn = look.Horn;
            Color eye = look.Eye ?? look.Ink;

            var layers = new System.Collections.Generic.List<(Shape, Color)>();

            // Legs, then the body over them
            layers.Add((Capsule(new Vector2(22, 32), new Vector2(21, 46), 1.5f), dark));
            layers.Add((Capsule(new Vector2(26.5f, 33), new Vector2(26.5f, 46), 1.5f), dark));
            layers.Add((Capsule(new Vector2(35, 33), new Vector2(36, 46), 1.5f), dark));
            layers.Add((Capsule(new Vector2(39, 32), new Vector2(40, 46), 1.5f), dark));
            layers.Add((Ellipse(30.5f, 28, 13.5f, 7.5f), fur));
            layers.Add((Ellipse(30.5f, 31.5f, 9.5f, 3.6f), belly));
            layers.Add((Ellipse(44, 24, 2.6f, 2.2f), Color.White));

            Vector2 head;
            if (!grazing)
            {
                head = new Vector2(9, 13.5f);
                layers.Add((Capsule(new Vector2(20, 25), new Vector2(12, 15), 3.4f), fur));
                layers.Add((Ellipse(head.X, head.Y, 5.4f, 3.5f), fur));
                layers.Add((Ellipse(13.5f, 9.6f, 1.6f, 2.4f), dark));
                layers.Add((Ellipse(8.6f, 12.2f, .9f, .9f), eye));
                layers.Add((Capsule(new Vector2(11.5f, 10), new Vector2(15, 2), .9f), horn));
                layers.Add((Capsule(new Vector2(13.4f, 6), new Vector2(9.5f, 3), .8f), horn));
                layers.Add((Capsule(new Vector2(14.6f, 3.4f), new Vector2(18.5f, 1), .8f), horn));
            }
            else
            {
                head = new Vector2(8.5f, 34);
                layers.Add((Capsule(new Vector2(20, 25), new Vector2(12, 31), 3.4f), fur));
                layers.Add((Ellipse(head.X, head.Y, 5.4f, 3.5f), fur));
                layers.Add((Ellipse(13f, 30.6f, 1.6f, 2.4f), dark));
                layers.Add((Ellipse(8.2f, 32.6f, .9f, .9f), eye));
                layers.Add((Capsule(new Vector2(12.5f, 29), new Vector2(17, 21.5f), .9f), horn));
                layers.Add((Capsule(new Vector2(14.5f, 25.5f), new Vector2(10.5f, 22.5f), .8f), horn));
            }

            // The nose: red with a little shine at Christmas, otherwise a plain dark one
            Vector2 tip = head + new Vector2(-5f, 1.3f);
            if (look.RedNose.HasValue)
            {
                layers.Add((Ellipse(tip.X, tip.Y, 2.3f, 2.3f), look.RedNose.Value));
                layers.Add((Ellipse(tip.X - .7f, tip.Y - .8f, .7f, .7f), new Color(255, 170, 160)));
            }
            else
            {
                layers.Add((Ellipse(tip.X, tip.Y, 2.1f, 2f), look.Ink));
            }
            return Render(gd, 56, 50, layers.ToArray());
        }

        // A thick line with round ends
        static Shape Capsule(Vector2 a, Vector2 b, float radius)
        {
            Vector2 ab = b - a;
            float len2 = System.Math.Max(ab.LengthSquared(), 0.0001f);
            return (x, y) =>
            {
                float t = MathHelper.Clamp(Vector2.Dot(new Vector2(x, y) - a, ab) / len2, 0f, 1f);
                return Vector2.DistanceSquared(new Vector2(x, y), a + ab * t) <= radius * radius;
            };
        }
    }
}
