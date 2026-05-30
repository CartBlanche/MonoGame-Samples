//-----------------------------------------------------------------------------
// BlackjackCardGame.ActionFlow.cs
//
// Partial class containing turn action orchestration and helper flow.
//-----------------------------------------------------------------------------

using System;
using CardsFramework;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        /// <summary>
        /// Performs the "Stand" move for the current player.
        /// </summary>
        public void Stand()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            if (player == null)
                return;

            int playerIndex = players.IndexOf(player);

            BroadcastAsHost(() => BroadcastStandAction((byte)playerIndex));

            AdvanceAfterStandLikeAction(player, playerIndex, throwOnUnsupportedHandType: true);

            // Broadcast turn change to synchronize active player
            BroadcastCurrentTurnChanged();
        }

        /// <summary>
        /// Performs the "Split" move for the current player.
        /// This includes adding the animations which shows the first hand splitting
        /// into two.
        /// </summary>
        public void Split()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();

            int playerIndex = players.IndexOf(player);

            BroadcastAsHost(() => BroadcastSplitAction((byte)playerIndex));

            BlackjackSplitActionSetup splitSetup = ApplySplitSetup(player, playerIndex);

            TraditionalCard firstCard = dealer.DealCardToHand(player.Hand);
            AddDealAnimation(firstCard, animatedHands[playerIndex], true, DealDuration,
                splitSetup.FirstHandAnimation.EstimatedTimeForAnimationCompletion);

            BroadcastAsHost(() => BroadcastCardDealt(firstCard, (byte)playerIndex, false, HandTypes.First));

            TraditionalCard secondCard = dealer.DealCardToHand(player.SecondHand);
            AddDealAnimation(secondCard, animatedSecondHands[playerIndex], true, DealDuration,
                splitSetup.SecondHandAnimation.EstimatedTimeForAnimationCompletion + DealDuration);

            BroadcastAsHost(() => BroadcastCardDealt(secondCard, (byte)playerIndex, false, HandTypes.Second));
        }

        /// <summary>
        /// Performs the "Double" move for the current player.
        /// </summary>
        public void Double()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();

            int playerIndex = players.IndexOf(player);

            BroadcastAsHost(() => BroadcastDoubleAction((byte)playerIndex));

            BlackjackDoubleActionEngine.ApplyDoubleWager(
                player,
                playerIndex,
                betGameComponent,
                throwOnUnsupportedHandType: true);

            Hit();
            Stand();
        }

        /// <summary>
        /// Performs the "Hit" move for the current player.
        /// </summary>
        public void Hit()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            if (player == null)
                return;

            int playerIndex = players.IndexOf(player);

            // Broadcast to network in network games (host only deals cards)
            BroadcastAsHost(() => BroadcastHitAction((byte)playerIndex));

            DealHitCard(player, playerIndex);
            ApplyAutoStandOn21(player, Stand);
        }

        /// <summary>
        /// Performs the "Insurance" action for the current player.
        /// </summary>
        public void Insurance()
        {
            BlackjackPlayer player = (BlackjackPlayer)GetCurrentPlayer();
            if (player == null)
                return;

            int playerIndex = players.IndexOf(player);

            BroadcastAsHost(() => BroadcastInsuranceAction((byte)playerIndex));

            ApplyInsuranceWager(player, playerIndex);
            showInsurance = false;
        }

        /// <summary>
        /// Performs a "Hit" action for a specific player (used by host when receiving action from client).
        /// </summary>
        public void HitForPlayer(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            DealHitCard(player, playerIndex);
            ApplyAutoStandOn21(player, () => StandForPlayer(playerIndex));
        }

        /// <summary>
        /// Performs a "Stand" action for a specific player (used by host when receiving action from client).
        /// </summary>
        public void StandForPlayer(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            AdvanceAfterStandLikeAction(player, playerIndex, throwOnUnsupportedHandType: false);

            BroadcastAsHost(() => BroadcastStandAction(playerIndex));
        }

        /// <summary>
        /// Performs a "Double" action for a specific player (used by host when receiving action from client).
        /// </summary>
        public void DoubleForPlayer(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            BroadcastAsHost(() => BroadcastDoubleAction((byte)playerIndex));

            BlackjackDoubleActionEngine.ApplyDoubleWager(
                player,
                playerIndex,
                betGameComponent,
                throwOnUnsupportedHandType: true);

            // Hit then Stand
            HitForPlayer(playerIndex);
            StandForPlayer(playerIndex);
        }

        /// <summary>
        /// Performs a "Split" action for a specific player (used by host when receiving action from client).
        /// </summary>
        public void SplitForPlayer(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            BroadcastAsHost(() => BroadcastSplitAction((byte)playerIndex));

            ApplySplitSetup(player, playerIndex);
        }

        /// <summary>
        /// Performs an "Insurance" action for a specific player (used by host when receiving action from client).
        /// </summary>
        public void InsuranceForPlayer(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            ApplyInsuranceWager(player, playerIndex);

            BroadcastAsHost(() => BroadcastInsuranceAction((byte)playerIndex));
        }

        private void DealHitCard(BlackjackPlayer player, int playerIndex)
        {
            TraditionalCard card;
            switch (player.CurrentHandType)
            {
                case HandTypes.First:
                    card = dealer.DealCardToHand(player.Hand);
                    AddDealAnimation(card, animatedHands[playerIndex], true,
                        DealDuration, TimeSpan.Zero);

                    BroadcastAsHost(() => BroadcastCardDealt(card, (byte)playerIndex, false, HandTypes.First));

                    break;
                case HandTypes.Second:
                    card = dealer.DealCardToHand(player.SecondHand);
                    AddDealAnimation(card, animatedSecondHands[playerIndex], true,
                        DealDuration, TimeSpan.Zero);

                    BroadcastAsHost(() => BroadcastCardDealt(card, (byte)playerIndex, false, HandTypes.Second));

                    break;
                default:
                    throw new Exception("Player has an unsupported hand type.");
            }
        }

        private void ApplyAutoStandOn21(BlackjackPlayer player, Action standAction)
        {
            player.CalculateValues();
            int handValue = player.CurrentHandType == HandTypes.First ? player.FirstValue : player.SecondValue;
            if (GameSettings.Instance.AutoStandOn21 && handValue == 21)
            {
                standAction();
            }
        }

        private BlackjackSplitActionSetup ApplySplitSetup(BlackjackPlayer player, int playerIndex)
        {
            BlackjackSplitActionSetup splitSetup = BlackjackSplitActionEngine.PrepareSplit(
                player,
                playerIndex,
                (BlackjackAnimatedPlayerHandComponent)animatedHands[playerIndex],
                secondHandOffset,
                AnimationSpeedMultiplier,
                this,
                betGameComponent,
                screenManager);

            animatedSecondHands[playerIndex] = splitSetup.SecondHandComponent;
            return splitSetup;
        }

        private void AdvanceAfterStandLikeAction(
            BlackjackPlayer player,
            int playerIndex,
            bool throwOnUnsupportedHandType)
        {
            BlackjackTurnStateEngine.AdvanceAfterStandLikeAction(
                player,
                turnFinishedByPlayer,
                playerIndex,
                throwOnUnsupportedHandType);
        }

        private void ApplyInsuranceWager(BlackjackPlayer player, int playerIndex)
        {
            player.IsInsurance = true;
            player.Balance -= player.BetAmount / 2f;
            betGameComponent.AddChips(playerIndex, player.BetAmount / 2, true, false);
        }

        private bool TryGetPlayer(byte playerIndex, out BlackjackPlayer player)
        {
            if (playerIndex >= players.Count)
            {
                player = null;
                return false;
            }

            player = (BlackjackPlayer)players[playerIndex];
            return true;
        }

        private void BroadcastAsHost(Action broadcastAction)
        {
            if (IsNetworkGame && IsHost)
            {
                broadcastAction();
            }
        }
    }
}
