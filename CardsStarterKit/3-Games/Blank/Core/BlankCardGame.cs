//-----------------------------------------------------------------------------
// BlankCardGame.cs
//
// Core fields, constructor, and required CardsGame overrides.
// Concerns are separated into partial class files:
//   BlankCardGame.UIOrchestration.cs  — Initialize, UI setup
//   BlankCardGame.RoundFlow.cs        — Deal, StartPlaying, state transitions
//   BlankCardGame.Rendering.cs        — Draw
//-----------------------------------------------------------------------------

using System;
using CardsFramework;
using CardsFramework.Core;
using Microsoft.Xna.Framework;


namespace Blank
{
    /// <summary>
    /// Core card game class. Extend the partial files for each concern rather
    /// than adding code here.
    /// </summary>
    partial class BlankCardGame : CardsGame
    {
        ScreenManager screenManager;
        Rectangle tableBounds;

        /// <summary>
        /// Creates a new instance of the blank card game.
        /// </summary>
        /// <param name="tableBounds">The bounds of the playable area.</param>
        /// <param name="screenManager">The screen manager.</param>
        public BlankCardGame(Rectangle tableBounds, ScreenManager screenManager)
            : base(
                1,                                      // Number of decks
                0,                                      // Jokers per deck
                CardSuit.AllSuits,                      // Suits to use
                CardsFramework.CardValue.NonJokers,     // Card values to use
                BlankConstants.MinPlayers,              // Minimum players
                BlankConstants.MaxPlayers,              // Maximum players
                new BlankTable(
                    tableBounds,
                    new Vector2(tableBounds.Width * 0.5f, tableBounds.Height * 0.15f),
                    BlankConstants.MaxPlayers,
                    index => new Vector2((tableBounds.Width / (BlankConstants.MaxPlayers + 1f)) * (index + 1), tableBounds.Height * 0.62f),
                    screenManager.Game,
                    screenManager.SpriteBatch,
                    screenManager.GlobalTransformation),
                "Blue",                                 // Theme token — used as suffix for CardBack_Blue asset
                screenManager.Game)
        {
            this.screenManager = screenManager;
            this.tableBounds = tableBounds;
        }

        // ── Required CardsGame overrides ─────────────────────────────────────

        /// <summary>Adds a player to the game.</summary>
        public override void AddPlayer(Player player)
        {
            if (player is not BlankPlayer)
                throw new ArgumentException("Player must be a BlankPlayer instance.", nameof(player));

            if (players.Count >= MaximumPlayers)
                throw new InvalidOperationException("Maximum players reached.");

            players.Add(player);
        }

        /// <summary>Returns the player currently taking their turn.</summary>
        public override Player GetCurrentPlayer()
        {
            // TODO: replace with proper turn-tracking for your game.
            return players.Count > 0 ? players[0] : null;
        }

        // ── Frame pump ───────────────────────────────────────────────────────

        /// <summary>
        /// Per-frame game logic update.
        /// Drive your state machine here; see Blackjack Update() for reference.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // TODO: add game update logic — animations, input handling, rule checks.
        }
    }
}
