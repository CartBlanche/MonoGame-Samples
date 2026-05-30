//-----------------------------------------------------------------------------
// BlackjackCardGame.RoundFlow.cs
//
// Partial class containing round lifecycle and result presentation flow.
//-----------------------------------------------------------------------------

using System;
using CardsFramework;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        /// <summary>
        /// Ends the current round.
        /// </summary>
        private void EndRound()
        {
            RevealDealerFirstCard();
            DealerAI();
            ShowResults();
            State = BlackjackGameState.RoundEnd;
        }

        /// <summary>
        /// Causes the dealer's hand to be displayed.
        /// </summary>
        private void ShowDealerHand()
        {
            dealerHandComponent =
                new BlackjackAnimatedDealerHandComponent(-1, dealerPlayer.Hand, this, screenManager.SpriteBatch,
                    screenManager.GlobalTransformation);
            Game.Components.Add(dealerHandComponent);
        }

        /// <summary>
        /// Reveal's the dealer's hidden card.
        /// </summary>
        private void RevealDealerFirstCard()
        {
            // Iterate over all dealer cards expect for the last
            AnimatedCardsGameComponent cardComponent = dealerHandComponent.GetCardGameComponent(1);
            cardComponent.AddAnimation(new FlipGameComponentAnimation()
            {
                Duration = TimeSpan.FromSeconds(0.5 * AnimationSpeedMultiplier),
                StartDelay = TimeSpan.Zero
            });
        }

        /// <summary>
        /// Present visual indication as to how the players fared in the current round.
        /// </summary>
        private void ShowResults()
        {
            // Calculate the dealer's hand value
            int dealerValue = dealerPlayer.FirstValue;

            if (dealerPlayer.FirstValueConsiderAce)
            {
                dealerValue += 10;
            }

            // Show each player's result
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                ShowResultForPlayer((BlackjackPlayer)players[playerIndex], dealerValue, HandTypes.First);
                if (((BlackjackPlayer)players[playerIndex]).IsSplit)
                {
                    ShowResultForPlayer((BlackjackPlayer)players[playerIndex], dealerValue, HandTypes.Second);
                }
            }
        }

        /// <summary>
        /// Display's a player's status after the turn has ended.
        /// </summary>
        /// <param name="player">The player for which to display the status.</param>
        /// <param name="dealerValue">The dealer's hand value.</param>
        /// <param name="currentHandType">The player's hand to take into 
        /// account.</param>
        private void ShowResultForPlayer(BlackjackPlayer player, int dealerValue,
            HandTypes currentHandType)
        {
            // Calculate the player's hand value and check his state (blackjack/bust)
            bool blackjack, bust;
            int playerValue;
            switch (currentHandType)
            {
                case HandTypes.First:
                    blackjack = player.BlackJack;
                    bust = player.Bust;

                    playerValue = player.FirstValue;

                    if (player.FirstValueConsiderAce)
                    {
                        playerValue += 10;
                    }

                    break;
                case HandTypes.Second:
                    blackjack = player.SecondBlackJack;
                    bust = player.SecondBust;

                    playerValue = player.SecondValue;

                    if (player.SecondValueConsiderAce)
                    {
                        playerValue += 10;
                    }

                    break;
                default:
                    throw new Exception(
                        "Player has an unsupported hand type.");
            }

            // The bust or blackjack state are animated independently of this method,
            // so only trigger different outcome indications
            if (player.MadeBet &&
                (!blackjack || (dealerPlayer.BlackJack && blackjack)) && !bust)
            {
                string assetName = GetResultAsset(player, dealerValue, playerValue);

                CueOverPlayerHand(player, assetName, currentHandType, dealerHandComponent);
            }
        }

        /// <summary>
        /// Return the asset name according to the result.
        /// </summary>
        /// <param name="player">The player for which to return the asset name.</param>
        /// <param name="dealerValue">The dealer's hand value.</param>
        /// <param name="playerValue">The player's hand value.</param>
        /// <returns>The asset name</returns>
        private string GetResultAsset(BlackjackPlayer player, int dealerValue, int playerValue)
        {
            string assetName;
            if (dealerPlayer.Bust)
            {
                assetName = "win";
            }
            else if (dealerPlayer.BlackJack)
            {
                if (player.BlackJack)
                {
                    assetName = "push";
                }
                else
                {
                    assetName = "lose";
                }
            }
            else if (playerValue < dealerValue)
            {
                assetName = "lose";
            }
            else if (playerValue > dealerValue)
            {
                assetName = "win";
            }
            else
            {
                assetName = "push";
            }

            return assetName;
        }

        /// <summary>
        /// Have the dealer play. The dealer hits until reaching 17+ and then 
        /// stands.
        /// </summary>
        private void DealerAI()
        {
            // The dealer may have not need to draw additional cards after his first
            // two. Check if this is the case and if so end the dealer's play.
            dealerPlayer.CalculateValues();
            int dealerValue = dealerPlayer.FirstValue;

            if (dealerPlayer.FirstValueConsiderAce)
            {
                dealerValue += 10;
            }

            if (dealerValue > 21)
            {
                dealerPlayer.Bust = true;
                CueOverPlayerHand(dealerPlayer, "bust", HandTypes.First, dealerHandComponent);
            }
            else if (dealerValue == 21)
            {
                dealerPlayer.BlackJack = true;
                CueOverPlayerHand(dealerPlayer, "blackjack", HandTypes.First, dealerHandComponent);
            }

            if (dealerPlayer.BlackJack || dealerPlayer.Bust)
            {
                return;
            }

            // Draw cards until 17 is reached, or the dealer gets a blackjack or busts
            int cardsDealed = 0;
            while (dealerValue <= 17)
            {
                TraditionalCard card = dealer.DealCardToHand(dealerPlayer.Hand);
                AddDealAnimation(card, dealerHandComponent, true, DealDuration,
                    TimeSpan.FromMilliseconds(1000 * AnimationSpeedMultiplier * (cardsDealed + 1)));
                cardsDealed++;
                dealerPlayer.CalculateValues();
                dealerValue = dealerPlayer.FirstValue;

                if (dealerPlayer.FirstValueConsiderAce)
                {
                    dealerValue += 10;
                }

                if (dealerValue > 21)
                {
                    dealerPlayer.Bust = true;
                    CueOverPlayerHand(dealerPlayer, "bust", HandTypes.First, dealerHandComponent);
                }
            }
        }

        /// <summary>
        /// Displays the hands currently in play.
        /// </summary>
        private void DisplayPlayingHands()
        {

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                AnimatedHandGameComponent animatedHandGameComponent =
                    new BlackjackAnimatedPlayerHandComponent(playerIndex, players[playerIndex].Hand, this,
                        screenManager.SpriteBatch, screenManager.GlobalTransformation);
                Game.Components.Add(animatedHandGameComponent);
                animatedHands[playerIndex] = animatedHandGameComponent;
            }

            ShowDealerHand();
        }

        /// <summary>
        /// Reinitializes the dealer with the correct number of decks based on player count.
        /// Formula: numberOfDecks = totalPlayers + 1 (minimum of 2)
        /// </summary>
        private void ReinitializeDealerWithDynamicDeckCount()
        {
            // Calculate required deck count: total players + 1
            int totalPlayers = players.Count;
            int requiredDecks = Math.Max(2, totalPlayers + 1);

            // Reinitialize the dealer with the new deck count
            dealer = new CardPacket(requiredDecks, 0, CardSuit.AllSuits, CardsFramework.CardValue.NonJokers);
        }
    }
}
