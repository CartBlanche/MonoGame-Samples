//-----------------------------------------------------------------------------
// BlankCardGame.Rendering.cs
//
// Partial class: all Draw / rendering methods.
// Mirrors BlackjackCardGame.Rendering.cs in its role.
//
// Called each frame by GameplayScreen.Draw() when the screen has focus.
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blank
{
    partial class BlankCardGame
    {
        /// <summary>
        /// Renders all game-owned visuals for this frame.
        /// The table (BlankTable) is drawn automatically as a Game.Component;
        /// add card and UI rendering here.
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                null, null, null, null, null,
                screenManager.GlobalTransformation);

            // TODO: draw player hands, card values, score labels, etc.
            //
            // When card images are loaded (base.LoadContent() in UIOrchestration),
            // AnimatedHandGameComponent / AnimatedCardsGameComponent handle
            // their own Draw() calls as Game.Components, so you typically only
            // draw HUD overlays and text here.
            //
            // Example:
            //   screenManager.SpriteBatch.DrawString(font, $"Score: {score}", pos, Color.White);

            screenManager.SpriteBatch.End();
        }
    }
}
