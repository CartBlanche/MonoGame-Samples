//-----------------------------------------------------------------------------
// BlankCardGame.cs
//
// Minimal card game implementation showing how to extend CardsGame
//-----------------------------------------------------------------------------

using System;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace Blank
{
    /// <summary>
    /// Minimal card game implementation.
    /// This shows the basic structure - extend this for your own game.
    /// </summary>
    class BlankCardGame : CardsGame
    {
        ScreenManager screenManager;
        Rectangle tableBounds;

        /// <summary>
        /// Creates a new instance of the blank card game.
        /// </summary>
        /// <param name="tableBounds">The table bounds for the game area.</param>
        /// <param name="screenManager">The screen manager.</param>
        public BlankCardGame(Rectangle tableBounds, ScreenManager screenManager)
            : base(
                1,                                      // Number of decks
                0,                                      // Cards to deal initially
                CardSuit.AllSuits,                      // Which suits to use
                CardsFramework.CardValue.NonJokers,     // Which card values to use (enum type)
                BlankConstants.MinPlayers,              // Minimum players
                BlankConstants.MaxPlayers,              // Maximum players
                null,                                   // Card table component (optional)
                "CardBack",                             // Theme name
                screenManager.Game)                     // Game instance
        {
            this.screenManager = screenManager;
            this.tableBounds = tableBounds;
        }

        /// <summary>
        /// Add a player to the game.
        /// Required override from CardsGame.
        /// </summary>
        public override void AddPlayer(Player player)
        {
            players.Add(player);
        }

        /// <summary>
        /// Get the current player.
        /// Required override from CardsGame.
        /// </summary>
        public override Player GetCurrentPlayer()
        {
            // Return the first player or null
            return players.Count > 0 ? players[0] : null;
        }

        /// <summary>
        /// Deal cards.
        /// Required override from CardsGame.
        /// </summary>
        public override void Deal()
        {
            // Example: Deal 5 cards to each player
            foreach (BlankPlayer player in players)
            {
                // Remove all cards from player's hand
                while (player.Hand.Count > 0)
                {
                    player.Hand[0].MoveToHand(null);
                }

                // Deal 5 cards
                for (int i = 0; i < 5 && dealer.Count > 0; i++)
                {
                    dealer.DealCardToHand(player.Hand);
                }
            }
        }

        /// <summary>
        /// Start playing the game.
        /// Required override from CardsGame.
        /// </summary>
        public override void StartPlaying()
        {
            // Initialize game state and deal cards
            Deal();
        }

        /// <summary>
        /// Update game logic.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Add your game update logic here
            // Example: Update animations, check for win conditions, etc.
        }

        /// <summary>
        /// Draw the game.
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            // Add your game rendering logic here
            // Example: Draw cards, UI elements, etc.

            // Note: To draw cards, you'll need to load card textures and render them
            // using SpriteBatch. See Blackjack implementation for advanced examples.
        }
    }
}
