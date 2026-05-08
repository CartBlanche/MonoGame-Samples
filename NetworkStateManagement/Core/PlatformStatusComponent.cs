using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.GamerServices;

namespace NetworkStateManagement
{
    /// <summary>
    /// Draws platform backend status at the bottom of the screen.
    /// Shows whether the player is logged into Steam, local, or offline.
    /// </summary>
    public class PlatformStatusComponent : DrawableGameComponent
    {
        private SpriteFont spriteFont;
        private string platformStatus;
        private Color statusColor;

        public PlatformStatusComponent(Game game) : base(game) { }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Load game font from content
            spriteFont = Game.Content.Load<SpriteFont>("gamefont");
            UpdateStatus();
        }

        public override void Update(GameTime gameTime)
        {
            UpdateStatus();
            base.Update(gameTime);
        }

        private void UpdateStatus()
        {
            if (SignedInGamer.Current == null)
            {
                platformStatus = "Logged into: Offline";
                statusColor = Color.Gray;
            }
            else if (SignedInGamer.Current.IsSignedInToLive)
            {
                platformStatus = $"Logged into: Steam ({SignedInGamer.Current.Gamertag})";
                statusColor = Color.LimeGreen;
            }
            else
            {
                platformStatus = "Logged into: Local";
                statusColor = Color.Yellow;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
        }

        public void DrawStatus(SpriteBatch spriteBatch, Vector2 position)
        {
            if (spriteFont != null)
            {
                spriteBatch.DrawString(spriteFont, platformStatus, position, statusColor);
            }
        }
    }
}
