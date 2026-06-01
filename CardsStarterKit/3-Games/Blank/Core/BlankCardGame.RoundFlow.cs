//-----------------------------------------------------------------------------
// BlankCardGame.RoundFlow.cs
//
// Partial class: round lifecycle — dealing, starting, and state transitions.
// Mirrors BlackjackCardGame.RoundFlow.cs in its role.
//-----------------------------------------------------------------------------

using CardsFramework;
using Microsoft.Xna.Framework;

namespace Blank
{
    partial class BlankCardGame
    {
        /// <summary>
        /// Deals cards to all players at the start of a round.
        /// Called by StartPlaying(); override for your game's deal rules.
        /// </summary>
        public override void Deal()
        {
            // Return all cards from each player's hand to the dealer deck.
            foreach (BlankPlayer player in players)
            {
                while (player.Hand.Count > 0)
                    player.Hand[0].MoveToHand(null);
            }

            // Deal 5 cards per player.
            // TODO: replace 5 with your game's deal count, or implement staged
            // dealing with animation callbacks (see Blackjack AddDealAnimation).
            foreach (BlankPlayer player in players)
            {
                for (int i = 0; i < 5 && dealer.Count > 0; i++)
                    dealer.DealCardToHand(player.Hand);
            }
        }

        /// <summary>
        /// Starts a new round — shuffle, deal, set initial state.
        /// Called by GameplayScreen after Initialize().
        /// </summary>
        public override void StartPlaying()
        {
            // TODO: shuffle, set state to Playing, reset any per-round counters.
            Deal();
        }

        /// <summary>
        /// Checks rule conditions for the current game state.
        /// Invoke at the appropriate point in Update() when you have rules to check.
        /// </summary>
        public override void CheckRules()
        {
            // TODO: iterate rules and invoke rule-specific logic.
            // Example: foreach (GameRule rule in rules) rule.Check(this);
        }
    }
}
