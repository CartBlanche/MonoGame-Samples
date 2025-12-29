//-----------------------------------------------------------------------------
// BlankPlayer.cs
//
// Minimal player implementation
//-----------------------------------------------------------------------------

using CardsFramework;

namespace Blank
{
    /// <summary>
    /// Minimal player implementation.
    /// Extend this class to add game-specific player properties and behavior.
    /// </summary>
    public class BlankPlayer : Player
    {
        /// <summary>
        /// Creates a new player instance.
        /// </summary>
        /// <param name="name">The player's name.</param>
        /// <param name="game">The associated game.</param>
        public BlankPlayer(string name, CardsGame game)
            : base(name, game)
        {
            // Add your player initialization here
        }

        // Add your game-specific player properties and methods here
        // Examples:
        // public int Score { get; set; }
        // public bool IsPlayerTurn { get; set; }
        // public void TakeTurn() { ... }
    }
}
