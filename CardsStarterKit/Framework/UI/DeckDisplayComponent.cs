//-----------------------------------------------------------------------------
// DeckDisplayComponent.cs
//
// Displays a visual representation of the card deck during gameplay
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Component that displays a visual representation of the deck at the dealer position.
    /// Shows a stack of card backs to indicate where cards are being dealt from.
    /// </summary>
    public class DeckDisplayComponent : DrawableGameComponent
    {
        private readonly CardsGame cardGame;
        private readonly SpriteBatch spriteBatch;
        private readonly Matrix globalTransformation;
        private readonly Vector2 position;
        private Texture2D cardBackTexture;

        /// <summary>
        /// Number of card back layers to draw for the stacked effect
        /// </summary>
        public int StackLayers { get; set; } = 3;

        /// <summary>
        /// Offset between each layer to create depth
        /// </summary>
        public Vector2 LayerOffset { get; set; } = new Vector2(1, 1);

        /// <summary>
        /// Rotation angle in radians (default -45 degrees / clockwise for casino look)
        /// </summary>
        public float Rotation { get; set; } = MathHelper.ToRadians(-45);

        /// <summary>
        /// Whether the deck should be visible
        /// </summary>
        public new bool Visible { get; set; } = true;

        /// <summary>
        /// Creates a new deck display component
        /// </summary>
        /// <param name="game">The game instance</param>
        /// <param name="cardGame">The card game instance</param>
        /// <param name="position">Position where the deck should be displayed</param>
        /// <param name="spriteBatch">Shared sprite batch for rendering</param>
        /// <param name="globalTransformation">Transformation matrix for scaling</param>
        public DeckDisplayComponent(
            Game game,
            CardsGame cardGame,
            Vector2 position,
            SpriteBatch spriteBatch,
            Matrix globalTransformation)
            : base(game)
        {
            this.cardGame = cardGame;
            this.position = position;
            this.spriteBatch = spriteBatch;
            this.globalTransformation = globalTransformation;
            
            DrawOrder = -5000; // Draw behind dealt cards but in front of table
        }

        /// <summary>
        /// Load the card back texture
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();
            
            // Get the card back texture from the card game's assets
            if (cardGame.cardsAssets.ContainsKey("CardBack_" + cardGame.Theme))
            {
                cardBackTexture = cardGame.cardsAssets["CardBack_" + cardGame.Theme];
            }
        }

        /// <summary>
        /// Draw the deck as a stack of card backs
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            if (!Visible || cardBackTexture == null)
                return;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);

            // Calculate origin point for rotation (center of the card)
            Vector2 origin = new Vector2(cardBackTexture.Width / 2f, cardBackTexture.Height / 2f);

            // Draw multiple layers to create a stacked deck effect
            for (int i = 0; i < StackLayers; i++)
            {
                Vector2 layerPosition = position + (LayerOffset * i);
                
                // Draw with slight transparency on lower layers for depth
                float alpha = 1.0f - (i * 0.1f);
                Color color = Color.White * alpha;
                
                // Draw with rotation around the center origin
                spriteBatch.Draw(cardBackTexture, layerPosition, null, color, Rotation, origin, 1.0f, SpriteEffects.None, 0f);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
