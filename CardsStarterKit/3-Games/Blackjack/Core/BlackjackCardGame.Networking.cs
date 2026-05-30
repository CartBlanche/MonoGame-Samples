//-----------------------------------------------------------------------------
// BlackjackCardGame.Networking.cs
//
// Partial class containing networking and network-driven action handlers.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using CardsFramework;
using Microsoft.Xna.Framework;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        private void ExecuteInputAction(Networking.BlackjackAction action)
        {
            if (IsNetworkGame && !IsHost)
            {
                SendPlayerAction(action);
            }
            else
            {
                ExecuteLocalAction(action);
            }

            showInsurance = false;
        }

        private void BroadcastCurrentTurnChanged()
        {
            if (!IsNetworkGame || !IsHost)
                return;

            Player nextPlayer = GetCurrentPlayer();
            byte nextPlayerIndex = nextPlayer != null ? (byte)players.IndexOf(nextPlayer) : (byte)255;
            BroadcastTurnChanged(nextPlayerIndex);
        }

        private void ExecuteLocalAction(Networking.BlackjackAction action)
        {
            switch (action)
            {
                case Networking.BlackjackAction.Hit:
                    Hit();
                    break;
                case Networking.BlackjackAction.Stand:
                    Stand();
                    break;
                case Networking.BlackjackAction.Double:
                    this.Double();
                    break;
                case Networking.BlackjackAction.Split:
                    Split();
                    break;
                case Networking.BlackjackAction.Insurance:
                    Insurance();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported blackjack action.");
            }
        }

        public void ExecutePlayerAction(Networking.BlackjackAction action, byte playerIndex)
        {
            switch (action)
            {
                case Networking.BlackjackAction.Hit:
                    HitForPlayer(playerIndex);
                    break;
                case Networking.BlackjackAction.Stand:
                    StandForPlayer(playerIndex);
                    break;
                case Networking.BlackjackAction.Double:
                    DoubleForPlayer(playerIndex);
                    break;
                case Networking.BlackjackAction.Split:
                    SplitForPlayer(playerIndex);
                    break;
                case Networking.BlackjackAction.Insurance:
                    InsuranceForPlayer(playerIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported blackjack action.");
            }
        }

        /// <summary>
        /// Broadcasts the shuffle seed to all players in the network session.
        /// </summary>
        private void BroadcastShuffleSeed(int seed)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.ShuffleSeedPacket { Seed = seed };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.ShuffleSeed);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Handles receiving a shuffle seed from the host.
        /// </summary>
        public void ReceiveShuffleSeed(int seed)
        {
            currentShuffleSeed = seed;
            dealer.Shuffle(seed);
        }

        /// <summary>
        /// Broadcasts the complete player list (including NPC players) to all clients.
        /// Should be called by the host after all players (human + NPC) have been added.
        /// </summary>
        public void BroadcastPlayerList()
        {
            if (NetworkSession == null || !IsHost)
                return;

            var playerInfoList = new List<Networking.PlayerInfo>();
            foreach (var player in players)
            {
                playerInfoList.Add(new Networking.PlayerInfo
                {
                    Name = player.Name,
                    IsNPC = player is BlackjackNPCPlayer
                });
            }

            var packet = new Networking.PlayerListSyncPacket { Players = playerInfoList };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.PlayerListSync);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a card dealt event to all players.
        /// </summary>
        private void BroadcastCardDealt(TraditionalCard card, byte playerIndex, bool faceDown, HandTypes handType)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.CardDealtPacket
            {
                Card = card,
                PlayerIndex = playerIndex,
                FaceDown = faceDown,
                HandType = handType
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.CardDealt);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a bet placed event to all players.
        /// </summary>
        public void BroadcastBetPlaced(byte playerIndex, int betAmount)
        {
            if (NetworkSession == null || !IsHost)
                return;

            // Validate player index in case player list changed after disconnect
            if (playerIndex >= players.Count)
            {
                Debug.WriteLine($"[BetPlaced] Player index {playerIndex} out of range (current count: {players.Count}). Ignoring bet broadcast.");
                return;
            }

            var packet = new Networking.BetPlacedPacket
            {
                PlayerIndex = playerIndex,
                BetAmount = betAmount
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.BetPlaced);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Sends a bet placed event from client to host.
        /// </summary>
        public void SendBetPlaced(byte playerIndex, int betAmount)
        {
            if (NetworkSession == null || NetworkSession.LocalGamers.Count == 0)
                return;

            // Validate player index in case player list changed after disconnect
            if (playerIndex >= players.Count)
            {
                Debug.WriteLine($"[BetPlaced] Player index {playerIndex} out of range (current count: {players.Count}). Ignoring bet send.");
                return;
            }

            var packet = new Networking.BetPlacedPacket
            {
                PlayerIndex = playerIndex,
                BetAmount = betAmount
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.BetPlaced);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a chip added event to all players.
        /// </summary>
        public void BroadcastChipAdded(byte playerIndex, int chipValue)
        {
            if (NetworkSession == null || !IsHost)
                return;

            // Validate player index in case player list changed after disconnect
            if (playerIndex >= players.Count)
            {
                Debug.WriteLine($"[ChipAdded] Player index {playerIndex} out of range (current count: {players.Count}). Ignoring chip broadcast.");
                return;
            }

            var packet = new Networking.ChipAddedPacket
            {
                PlayerIndex = playerIndex,
                ChipValue = chipValue
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.ChipAdded);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Sends a chip added event from client to host.
        /// </summary>
        public void SendChipAdded(byte playerIndex, int chipValue)
        {
            if (NetworkSession == null || NetworkSession.LocalGamers.Count == 0)
                return;

            // Validate player index in case player list changed after disconnect
            if (playerIndex >= players.Count)
            {
                Debug.WriteLine($"[ChipAdded] Player index {playerIndex} out of range (current count: {players.Count}). Ignoring chip send.");
                return;
            }

            var packet = new Networking.ChipAddedPacket
            {
                PlayerIndex = playerIndex,
                ChipValue = chipValue
            };

            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.ChipAdded);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Broadcasts a turn change event.
        /// </summary>
        private void BroadcastTurnChanged(byte currentPlayerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.TurnChangedPacket { CurrentPlayerIndex = currentPlayerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.TurnChanged);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        // Phase 5: Gameplay Action Broadcasting
        private void BroadcastHitAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.HitActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.HitAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastStandAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.StandActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.StandAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastDoubleAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.DoubleActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.DoubleAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastSplitAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.SplitActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.SplitAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        private void BroadcastInsuranceAction(byte playerIndex)
        {
            if (NetworkSession == null || !IsHost)
                return;

            var packet = new Networking.InsuranceActionPacket { PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.InsuranceAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Sends a player action to the host.
        /// </summary>
        public void SendPlayerAction(Networking.BlackjackAction action)
        {
            if (NetworkSession == null || NetworkSession.LocalGamers.Count == 0)
                return;

            byte playerIndex = (byte)LocalPlayerIndex;
            var packet = new Networking.PlayerActionPacket { Action = action, PlayerIndex = playerIndex };
            var writer = new Microsoft.Xna.Framework.Net.PacketWriter();
            writer.Write((byte)Networking.PacketType.PlayerAction);
            packet.Serialize(writer);

            NetworkSession.LocalGamers[0].SendData(writer, Microsoft.Xna.Framework.Net.SendDataOptions.Reliable);
        }

        /// <summary>
        /// Handles a card dealt packet received from the host (client-side).
        /// Recreates the dealing action that happened on the host.
        /// </summary>
        // Track the sequence of cards dealt for proper animation timing on clients
        private int cardSequenceCounter = 0;

        private TimeSpan lastDealTime = TimeSpan.FromSeconds(-10);
        private TimeSpan currentGameTime = TimeSpan.Zero;

        public void HandleReceivedCardDealt(TraditionalCard card, byte playerIndex, bool faceDown, HandTypes handType)
        {
            // This method is called on clients when they receive a CardDealt packet from the host
            // The deck is already synchronized via the shuffle seed, so cards are dealt in the same order

            // Reset sequence counter at the start of a new deal sequence
            if ((currentGameTime - lastDealTime).TotalSeconds > 5)
            {
                lastDealTime = currentGameTime;
                cardSequenceCounter = 0;
            }

            // Calculate staggered delay based on card sequence
            // This matches the timing logic in Deal() method
            TimeSpan startDelay = TimeSpan.FromSeconds(DealDuration.TotalSeconds * cardSequenceCounter);
            cardSequenceCounter++;

            if (playerIndex == 255)
            {
                // Dealer card
                var dealtCard = dealer.DealCardToHand(dealerPlayer.Hand);

                if (dealerHandComponent != null)
                {
                    AddDealAnimation(dealtCard, dealerHandComponent, faceDown, DealDuration, startDelay);
                }
                else
                {
                    Debug.WriteLine("[CardDealt] Warning: dealerHandComponent is null, skipping animation");
                }
            }
            else if (playerIndex < players.Count)
            {
                // Player card
                var player = (BlackjackPlayer)players[playerIndex];
                Hand targetHand = (handType == HandTypes.First) ? player.Hand : player.SecondHand;

                var dealtCard = dealer.DealCardToHand(targetHand);

                // Only add animation if the animated hand component exists for this player
                if (animatedHands != null && playerIndex < animatedHands.Length && animatedHands[playerIndex] != null)
                {
                    AddDealAnimation(dealtCard, animatedHands[playerIndex], !faceDown, DealDuration, startDelay);
                }
                else
                {
                    Debug.WriteLine($"[CardDealt] Warning: animatedHands[{playerIndex}] is null, skipping animation");
                }
            }
        }

        /// <summary>
        /// Handles a bet placed packet received from the host (client-side).
        /// Updates the player's bet state to match the host.
        /// </summary>
        public void HandleReceivedBetPlaced(byte playerIndex, int betAmount)
        {
            // This method is called on clients when they receive a BetPlaced packet from the host
            if (playerIndex < players.Count)
            {
                var player = (BlackjackPlayer)players[playerIndex];

                if (betAmount == 0)
                {
                    // Player passed: clear any previously mirrored chip deductions.
                    player.ClearBet();
                    ShowPlayerPass(playerIndex);
                }
                else
                {
                    // Reconcile to host-authoritative final bet amount.
                    // ChipAdded packets may have already adjusted local balance/bet state.
                    if (Math.Abs(player.BetAmount - betAmount) > float.Epsilon)
                    {
                        player.ClearBet();
                        player.Bet(betAmount);
                    }
                }

                player.IsDoneBetting = true;
            }
        }

        /// <summary>
        /// Handles a chip added packet received over the network.
        /// Adds the chip with animation on the receiving machine.
        /// </summary>
        public void HandleReceivedChipAdded(byte playerIndex, int chipValue)
        {
            // Validate player index in case it arrived after a player disconnect
            if (playerIndex >= players.Count)
            {
                Debug.WriteLine($"[ChipAdded] Received chip for invalid player index {playerIndex} (current count: {players.Count}). Ignoring.");
                return;
            }

            // When a client places a chip, the deduction already happened locally.
            // The host echoes the packet back to all clients including the originator,
            // so skip re-applying it here to prevent a double-deduction.
            if (IsNetworkGame && !IsHost && playerIndex == LocalPlayerIndex)
            {
                Debug.WriteLine($"[ChipAdded] Skipping echo for local player {playerIndex} (already applied locally).");
                return;
            }

            if (betGameComponent != null)
            {
                betGameComponent.AddChip(playerIndex, chipValue, false, sendToNetwork: false);
            }
        }

        // Phase 5: Gameplay Action Handlers
        /// <summary>
        /// Handles a Hit action received from the network.
        /// Executes the Hit move for the specified player.
        /// NOTE: Does NOT deal cards - cards are dealt via CardDealt packets.
        /// </summary>
        public void HandleReceivedHitAction(byte playerIndex)
        {
            // This handler is for game state synchronization only
            // The actual card dealing is handled by CardDealt packets
            // which are sent separately by the host

            // No action needed here - the CardDealt packet will handle the card
        }

        /// <summary>
        /// Handles a Stand action received from the network.
        /// Executes the Stand move for the specified player.
        /// </summary>
        public void HandleReceivedStandAction(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            AdvanceAfterStandLikeAction(player, playerIndex, throwOnUnsupportedHandType: true);
        }

        /// <summary>
        /// Handles a Double action received from the network.
        /// Executes the Double move for the specified player.
        /// NOTE: Does NOT deal cards - cards are dealt via CardDealt packets.
        /// </summary>
        public void HandleReceivedDoubleAction(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            // Execute Double logic - update chip stacks and flags
            BlackjackDoubleActionEngine.ApplyDoubleWager(
                player,
                playerIndex,
                betGameComponent,
                throwOnUnsupportedHandType: true);

            // Update turn state (card dealing is handled by CardDealt packet)
            // Automatically stand after double
            AdvanceAfterStandLikeAction(player, playerIndex, throwOnUnsupportedHandType: false);
        }

        /// <summary>
        /// Handles a Split action received from the network.
        /// Executes the Split move for the specified player.
        /// NOTE: Does NOT deal new cards - cards are dealt via CardDealt packets.
        /// </summary>
        public void HandleReceivedSplitAction(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            ApplySplitSetup(player, playerIndex);

            // Note: Additional cards will be dealt via CardDealt packets from the host
        }

        /// <summary>
        /// Handles an Insurance action received from the network.
        /// Executes the Insurance move for the specified player.
        /// </summary>
        public void HandleReceivedInsuranceAction(byte playerIndex)
        {
            if (!TryGetPlayer(playerIndex, out BlackjackPlayer player))
                return;

            ApplyInsuranceWager(player, playerIndex);
            showInsurance = false;
        }

        /// <summary>
        /// Handles a turn changed notification received from the network.
        /// Updates UI and button state to reflect the new active player.
        /// </summary>
        public void HandleReceivedTurnChanged(byte currentPlayerIndex)
        {
            // Value 255 indicates no active player (all players finished)
            if (currentPlayerIndex == 255)
            {
                Debug.WriteLine("[TurnChanged] All players have finished their turns");
                return;
            }

            if (currentPlayerIndex >= players.Count)
            {
                Debug.WriteLine($"[TurnChanged] Invalid player index: {currentPlayerIndex}");
                return;
            }

            var currentPlayer = (BlackjackPlayer)players[currentPlayerIndex];
            Debug.WriteLine($"[TurnChanged] Turn changed to player {currentPlayerIndex}: {currentPlayer.Name}");

            // The button availability will be updated in the next Update() cycle
            // via SetButtonAvailability(), which checks GetCurrentPlayer()
        }
    }
}
