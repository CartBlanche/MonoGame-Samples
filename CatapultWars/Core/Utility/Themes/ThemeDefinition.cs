using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// Everything that makes one look different from another, as data. A new season is a new
    /// definition (palette and art factories) registered in <see cref="Themes"/>; the game code
    /// never branches on which season it is. Optional parts left null are simply not drawn,
    /// which is how the classic look is described.
    /// </summary>
    public sealed class ThemeDefinition
    {
        public Season Id { get; init; }

        /// <summary>The name used by <c>--theme=</c>.</summary>
        public string Name { get; init; }

        // Inclusive month/day window; it may run over the new year. Month 0 means never by date.
        public int StartMonth { get; init; }
        public int StartDay { get; init; }
        public int EndMonth { get; init; }
        public int EndDay { get; init; }

        /// <summary>False for the classic look, which draws only the original art.</summary>
        public bool IsStyled { get; init; }

        // Sky: gradient stops from the top of the screen down to SkyHeight. Null keeps the daytime sky.
        public Color[] SkyStops { get; init; }
        public float[] SkyAt { get; init; }
        public float SkyHeight { get; init; } = 390f;
        public bool HasCustomSky { get { return SkyStops != null; } }

        // Multiply tints for the daytime layers. The foreground tint also colours the earth of the hill.
        public Color CloudTint { get; init; } = Color.White;
        public Color MountainTint { get; init; } = Color.White;
        public Color ForegroundTint { get; init; } = Color.White;

        /// <summary>When set, craters show this colour instead of the hillside behind the ground.</summary>
        public Color? CraterColor { get; init; }

        // Soft wash over the title art (null top means none).
        public Color? TitleTop { get; init; }
        public Color TitleBottom { get; init; }
        public float TitleAlphaTop { get; init; }
        public float TitleAlphaBottom { get; init; }

        // Twinkling stars
        public int StarCount { get; init; }
        public Color StarColor { get; init; } = Color.White;

        // Sun or moon: art, where it sits in the game and on the title, and its glow
        public Func<GraphicsDevice, Texture2D> BodyArt { get; init; }
        public Vector2 BodyOrigin { get; init; }
        public float BodyArtRadius { get; init; }
        public Vector2 BodyGamePosition { get; init; }
        public float BodyGameRadius { get; init; }
        public Vector2 BodyTitlePosition { get; init; }
        public float BodyTitleRadius { get; init; }
        public Color BodyGlow { get; init; }
        public float BodyGlowAlpha { get; init; }

        // Projectile (also the HUD ammo icon). Null keeps the rock from the content.
        public Func<GraphicsDevice, Texture2D> ProjectileArt { get; init; }

        // Impact dust palette
        public Color ImpactPuff { get; init; } = new Color(165, 165, 165);
        public Color ImpactChunk { get; init; } = new Color(105, 105, 105);

        // Lit castle windows and a prop (lanterns) resting on the ground
        public bool LitWindows { get; init; }
        public Color WindowColor { get; init; }
        public Color WindowGlow { get; init; }
        public Func<GraphicsDevice, Texture2D> PropArt { get; init; }
        public Point PropSize { get; init; }
        public Color PropGlow { get; init; }
        public float PropGlowSize { get; init; }

        // Ambient flyers (bats): animation frames, origin and how many
        public Func<GraphicsDevice, Texture2D[]> FlyerArt { get; init; }
        public Vector2 FlyerOrigin { get; init; }
        public int FlyerCount { get; init; }

        /// <summary>Sprite for particles blown by the wind. Not used yet.</summary>
        public Func<GraphicsDevice, Texture2D> WindParticleArt { get; init; }
    }
}
