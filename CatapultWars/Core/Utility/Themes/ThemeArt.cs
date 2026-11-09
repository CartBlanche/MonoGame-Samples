using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// The textures of one theme, built once.
    /// </summary>
    public sealed class ThemeTextures
    {
        public Texture2D Sky, TitleShade, Body, Projectile, Prop, WindParticle;
        public Texture2D[] Flyers, Mascot;
    }

    /// <summary>
    /// Builds and caches theme textures per graphics device. Everything is built the first time a
    /// theme is asked for, which the screens do while loading, so nothing is generated mid-play.
    /// </summary>
    public sealed class ThemeArt
    {
        static ThemeArt instance;

        readonly GraphicsDevice device;
        readonly Dictionary<ThemeDefinition, ThemeTextures> cache = new Dictionary<ThemeDefinition, ThemeTextures>();

        public Texture2D Pixel { get; private set; }
        public Texture2D Glow { get; private set; }

        /// <summary>White premultiplied discs for particles: feathered puffs and crisp chunks.</summary>
        public Texture2D SoftDisc { get; private set; }
        public Texture2D HardDisc { get; private set; }

        public static ThemeArt Get(GraphicsDevice device)
        {
            if (instance == null || instance.device != device)
                instance = new ThemeArt(device);
            return instance;
        }

        ThemeArt(GraphicsDevice device)
        {
            this.device = device;
            Pixel = ShapeArt.Solid(device, 1, 1, Color.White);
            Glow = ShapeArt.Glow(device, 64);
            SoftDisc = ShapeArt.Disc(device, 32, soft: true);
            HardDisc = ShapeArt.Disc(device, 8, soft: false);
        }

        public ThemeTextures For(ThemeDefinition theme)
        {
            ThemeTextures t;
            if (!cache.TryGetValue(theme, out t))
            {
                t = Build(theme);
                cache[theme] = t;
            }
            return t;
        }

        readonly Dictionary<(ThemeDefinition, Texture2D), Texture2D> frosted =
            new Dictionary<(ThemeDefinition, Texture2D), Texture2D>();

        /// <summary>
        /// A copy of a daytime layer with its bright parts washed toward the theme's frost colour.
        /// Built once per source texture (the screens ask while loading) and then shared.
        /// </summary>
        public Texture2D Frost(ThemeDefinition theme, Texture2D source, float amount)
        {
            if (amount <= 0f)
                return source;

            Texture2D result;
            if (frosted.TryGetValue((theme, source), out result))
                return result;

            Color[] data = new Color[source.Width * source.Height];
            source.GetData(data);
            float sr = theme.FrostColor.R, sg = theme.FrostColor.G, sb = theme.FrostColor.B;

            for (int i = 0; i < data.Length; i++)
            {
                Color c = data[i];
                if (c.A == 0)
                    continue;

                // Work on the straight colour: the textures are premultiplied
                float a = c.A / 255f;
                float r = c.R / a, g = c.G / a, b = c.B / a;
                float lum = (.3f * r + .59f * g + .11f * b) / 255f;
                float t = MathHelper.Clamp((lum - .38f) / .28f, 0f, 1f);
                float k = amount * t * t * (3f - 2f * t);

                r += (sr - r) * k; g += (sg - g) * k; b += (sb - b) * k;
                data[i] = new Color((int)Math.Min(255f, r * a), (int)Math.Min(255f, g * a),
                    (int)Math.Min(255f, b * a), (int)c.A);
            }

            result = new Texture2D(device, source.Width, source.Height);
            result.SetData(data);
            frosted[(theme, source)] = result;
            return result;
        }

        ThemeTextures Build(ThemeDefinition d)
        {
            var t = new ThemeTextures();
            if (d.HasCustomSky)
                t.Sky = BuildSky(d, 4, 480);
            if (d.TitleTop.HasValue)
                t.TitleShade = BuildTitleShade(d, 4, 480);
            if (d.BodyArt != null)
                t.Body = d.BodyArt(device);
            if (d.ProjectileArt != null)
                t.Projectile = d.ProjectileArt(device);
            if (d.PropArt != null)
                t.Prop = d.PropArt == d.ProjectileArt && t.Projectile != null ? t.Projectile : d.PropArt(device);
            if (d.FlyerArt != null)
                t.Flyers = d.FlyerArt(device);
            if (d.MascotArt != null)
                t.Mascot = d.MascotArt(device);
            if (d.WindParticles != null && d.WindParticles.Art != null)
                t.WindParticle = d.WindParticles.Art(device);
            return t;
        }

        // Vertical gradient through the theme's colour stops
        Texture2D BuildSky(ThemeDefinition d, int w, int h)
        {
            Color[] stops = d.SkyStops;
            float[] at = d.SkyAt;

            Texture2D t = new Texture2D(device, w, h);
            Color[] data = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float p = Math.Min(y / d.SkyHeight, 1f);
                int s = 0;
                while (s < at.Length - 2 && p > at[s + 1])
                    s++;
                float k = MathHelper.Clamp((p - at[s]) / (at[s + 1] - at[s]), 0f, 1f);
                Color c = Color.Lerp(stops[s], stops[s + 1], k);
                for (int x = 0; x < w; x++)
                    data[y * w + x] = c;
            }
            t.SetData(data);
            return t;
        }

        // Soft wash over the title art
        Texture2D BuildTitleShade(ThemeDefinition d, int w, int h)
        {
            Texture2D t = new Texture2D(device, w, h);
            Color[] data = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float p = y / (float)(h - 1);
                float alpha = MathHelper.Lerp(d.TitleAlphaTop, d.TitleAlphaBottom, p);
                Color c = Color.Lerp(d.TitleTop.Value, d.TitleBottom, p) * alpha;
                for (int x = 0; x < w; x++)
                    data[y * w + x] = c;
            }
            t.SetData(data);
            return t;
        }
    }
}
