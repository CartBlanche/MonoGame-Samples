using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CatapultGame
{
    /// <summary>
    /// A reusable description of one kind of particle: how it moves, grows, spins and fades, and
    /// what it looks like. Make one per effect when loading and spray it as often as you like;
    /// spraying never allocates. All rates are per second.
    /// </summary>
    public class ParticleSprayType
    {
        /// <summary>Downward acceleration in pixels per second squared (negative floats up).</summary>
        public float Gravity { get; set; }

        /// <summary>Drag: the share of velocity lost each second. 0 is none, 3 stops a particle in about a second.</summary>
        public float Drag { get; set; }

        /// <summary>How strongly the engine's wind pushes this particle, 0 (not at all) to 1 (fully).</summary>
        public float WindInfluence { get; set; }

        public float LifeTime { get; set; }
        public float LifeTimeRandom { get; set; }

        /// <summary>Size in pixels, and how fast it changes per second.</summary>
        public float Size { get; set; } = 8f;
        public float SizeRandom { get; set; }
        public float SizeInc { get; set; }
        public float SizeIncRandom { get; set; }

        /// <summary>Rotation in radians, and its speed per second.</summary>
        public float Angle { get; set; }
        public float AngleRandom { get; set; }
        public float AngleInc { get; set; }
        public float AngleIncRandom { get; set; }

        /// <summary>Colour, with a brightness variation (0.15 means each particle is 85% to 115%).</summary>
        public Color Tint { get; set; } = Color.White;
        public float TintVariation { get; set; }

        /// <summary>Peak opacity, 0 to 1.</summary>
        public float Opacity { get; set; } = 1f;

        /// <summary>
        /// The fade: fully visible until this share of the life has passed, then fading out to nothing
        /// along (1 - progress) raised to <see cref="FadePower"/>.
        /// </summary>
        public float FadeStart { get; set; } = .75f;
        public float FadePower { get; set; } = 1f;

        /// <summary>Glows: the sprite is added to what is behind it instead of covering it.</summary>
        public bool Additive { get; set; }

        /// <summary>
        /// The sprite. By default the whole texture is one picture. For a sheet of tiles set
        /// <see cref="TileWidth"/>, <see cref="TileHeight"/>, <see cref="TilesPerRow"/> and pick tiles
        /// with <see cref="FirstBlock"/> and <see cref="BlockCount"/>.
        /// </summary>
        public Texture2D Texture { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int TilesPerRow { get; set; } = 1;
        public int FirstBlock { get; set; }
        public int BlockCount { get; set; }
    }
}
