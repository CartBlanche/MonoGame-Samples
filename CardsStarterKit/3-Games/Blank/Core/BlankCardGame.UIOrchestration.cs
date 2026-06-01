//-----------------------------------------------------------------------------
// BlankCardGame.UIOrchestration.cs
//
// Partial class: UI component setup, button creation, and UI event wiring.
// Mirrors BlackjackCardGame.UIOrchestration.cs in its role.
//
// Called by GameplayScreen.LoadContent() between player registration and
// StartPlaying(), so that UI components are ready before the first frame.
//-----------------------------------------------------------------------------

using CardsFramework.Core;
using Microsoft.Xna.Framework;

namespace Blank
{
    partial class BlankCardGame
    {
        /// <summary>
        /// Loads card assets and prepares UI components.
        /// Call this after AddPlayer() and before StartPlaying().
        /// </summary>
        public void Initialize()
        {
            // Uncomment when card images are added to Content.mgcb.
            // base.LoadContent() loads all 52 card face textures and the card-back
            // (CardBack_Blue.png) from Content/Images/Cards/ — required for
            // AnimatedCardsGameComponent and AnimatedHandGameComponent to render.
            //
            // base.LoadContent();

            // TODO: Create and wire UI components — buttons, score labels, etc.
            //
            // Example (see Blackjack UIOrchestration for full implementation):
            //
            //   var dealButton = new Button(ScreenManager.ButtonBackground, ScreenManager.ButtonPressed)
            //   {
            //       Text    = "Deal",
            //       Bounds  = new Rectangle(x, y, width, height),
            //       Font    = screenManager.Font,
            //   };
            //   dealButton.Click += DealButton_Click;
            //   Game.Components.Add(dealButton);
        }
    }
}
