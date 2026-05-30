//-----------------------------------------------------------------------------
// BlackjackCardGame.RuleEvents.cs
//
// Partial class containing Blackjack rule and event handlers.
//-----------------------------------------------------------------------------

using System;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        /// <summary>
        /// Helper method to hide insurance
        /// </summary>
        /// <param name="obj"></param>
        void HideInshurance(object obj)
        {
            showInsurance = false;
        }

        /// <summary>
        /// Shows the insurance button if the first player can afford insurance.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing
        /// the event data.</param>
        void InsuranceGameRule(object sender, EventArgs e)
        {
            BlackjackPlayer player = (BlackjackPlayer)players[0];
            if (player.Balance >= player.BetAmount / 2)
            {
                showInsurance = true;
            }
        }

        /// <summary>
        /// Shows the bust visual cue after the bust rule has been matched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing
        /// the event data.</param>
        void BustGameRule(object sender, EventArgs e)
        {
            showInsurance = false;
            BlackjackGameEventArgs args = (e as BlackjackGameEventArgs);
            BlackjackPlayer player = (BlackjackPlayer)args.Player;

            CueOverPlayerHand(player, "bust", args.Hand, null);

            switch (args.Hand)
            {
                case HandTypes.First:
                    player.Bust = true;

                    if (player.IsSplit && !player.SecondBlackJack)
                    {
                        player.CurrentHandType = HandTypes.Second;
                    }
                    else
                    {
                        turnFinishedByPlayer[players.IndexOf(player)] = true;
                    }

                    break;
                case HandTypes.Second:
                    player.SecondBust = true;
                    turnFinishedByPlayer[players.IndexOf(player)] = true;
                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }

            // Broadcast turn change after bust
            BroadcastCurrentTurnChanged();
        }

        /// <summary>
        /// Shows the blackjack visual cue after the blackjack rule has been matched.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing
        /// the event data.</param>
        void BlackJackGameRule(object sender, EventArgs e)
        {
            showInsurance = false;
            BlackjackGameEventArgs args = (e as BlackjackGameEventArgs);
            BlackjackPlayer player = (BlackjackPlayer)args.Player;

            CueOverPlayerHand(player, "blackjack", args.Hand, null);

            switch (args.Hand)
            {
                case HandTypes.First:
                    player.BlackJack = true;

                    if (player.IsSplit)
                    {
                        player.CurrentHandType = HandTypes.Second;
                    }
                    else
                    {
                        turnFinishedByPlayer[players.IndexOf(player)] = true;
                    }

                    break;
                case HandTypes.Second:
                    player.SecondBlackJack = true;
                    if (player.CurrentHandType == HandTypes.Second)
                    {
                        turnFinishedByPlayer[players.IndexOf(player)] = true;
                    }

                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }

            // Broadcast turn change after blackjack
            BroadcastCurrentTurnChanged();
        }
    }
}
