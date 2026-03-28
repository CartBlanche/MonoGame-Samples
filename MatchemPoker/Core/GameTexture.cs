using Microsoft.Xna.Framework.Graphics;

namespace MatchemPoker
{
    /// <summary>
    /// Enumeration for textureID
    /// </summary>
    public enum TextureID
    {
        TexLogo = 1,
        TexPieces = 2,
        TexPiecesSelected = 3,
        TexFontScore = 4,
        TexGradient = 5,
        TexMeter = 6,
        TexMeterBase = 7,
        TexParticle = 8,
        TexFont = 9,
        TexBackground = 10,
        TexExtra1 = 11,
        TexEndOfList
    }

    /// <summary>
    /// A container class for a single texture used in a game.
    /// </summary>
    public class GameTexture
    {
        public TextureID Id { get; private set; }
        public string Name { get; private set; }
        public int TilesX { get; private set; }
        public int TilesY { get; private set; }
        public Texture2D XnaTexture { get; private set; }

        public GameTexture(TextureID id, Texture2D xnaTexture, string name, int tilesX, int tilesY)
        {
            Id = id;
            Name = name;
            TilesX = tilesX;
            TilesY = tilesY;
            XnaTexture = xnaTexture;
        }
    }
}