//-----------------------------------------------------------------------------
// BlankTable.cs
//
// Minimal table renderer for the Blank starter.
// Avoids requiring a game-specific Images/UI/table asset while preserving
// CardsFramework.GameTable positioning behavior.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CardsFramework;

namespace Blank
{
    class BlankTable : GameTable
    {
        private Texture2D fillTexture;

        public BlankTable(Rectangle tableBounds, Vector2 dealerPosition, int places,
            System.Func<int, Vector2> placeOrder, Game game, SpriteBatch spriteBatch, Matrix globalTransformation)
            : base(tableBounds, dealerPosition, places, placeOrder, "", game, spriteBatch, globalTransformation)
        {
        }

        protected override void LoadContent()
        {
            fillTexture = Game.Content.Load<Texture2D>("Images/blank");
        }

        public override void Draw(GameTime gameTime)
        {
            SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, globalTransformation);
            SpriteBatch.Draw(fillTexture, TableBounds, new Color(26, 102, 52));
            SpriteBatch.End();
        }
    }
}
