using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    internal sealed class GameplayPacketHandlers
    {
        private readonly BlackjackCardGame blackJackGame;
        private readonly GameplayPacketProcessingPolicy packetProcessingPolicy;
        private readonly GameplayPlayerListSyncCoordinator playerListSyncCoordinator;

        public GameplayPacketHandlers(
            BlackjackCardGame blackJackGame,
            GameplayPacketProcessingPolicy packetProcessingPolicy,
            GameplayPlayerListSyncCoordinator playerListSyncCoordinator)
        {
            this.blackJackGame = blackJackGame ?? throw new ArgumentNullException(nameof(blackJackGame));
            this.packetProcessingPolicy = packetProcessingPolicy ?? throw new ArgumentNullException(nameof(packetProcessingPolicy));
            this.playerListSyncCoordinator = playerListSyncCoordinator ?? throw new ArgumentNullException(nameof(playerListSyncCoordinator));
        }

        public void HandlePlayerListSyncPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.PlayerListSyncPacket.Deserialize(reader);
            packetProcessingPolicy.ProcessOnClientFromHost(sender, () => playerListSyncCoordinator.Process(packet));
        }

        public void HandleCardDealtPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.CardDealtPacket.Deserialize(reader);
            packetProcessingPolicy.ProcessOnClientFromHost(sender, () =>
                blackJackGame.HandleReceivedCardDealt(packet.Card, packet.PlayerIndex, packet.FaceDown, packet.HandType));
        }

        public void HandleBetPlacedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.BetPlacedPacket.Deserialize(reader);
            packetProcessingPolicy.ProcessHostRelayPacket(
                sender,
                hostApply: () => blackJackGame.HandleReceivedBetPlaced(packet.PlayerIndex, packet.BetAmount),
                hostBroadcast: () => blackJackGame.BroadcastBetPlaced(packet.PlayerIndex, packet.BetAmount),
                clientApply: () => blackJackGame.HandleReceivedBetPlaced(packet.PlayerIndex, packet.BetAmount));
        }

        public void HandleChipAddedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.ChipAddedPacket.Deserialize(reader);
            packetProcessingPolicy.ProcessHostRelayPacket(
                sender,
                hostApply: () => blackJackGame.HandleReceivedChipAdded(packet.PlayerIndex, packet.ChipValue),
                hostBroadcast: () => blackJackGame.BroadcastChipAdded(packet.PlayerIndex, packet.ChipValue),
                clientApply: () => blackJackGame.HandleReceivedChipAdded(packet.PlayerIndex, packet.ChipValue));
        }

        public void HandlePlayerActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.PlayerActionPacket.Deserialize(reader);
            packetProcessingPolicy.ProcessOnHost(() => blackJackGame.ExecutePlayerAction(packet.Action, packet.PlayerIndex));
        }

        public void HandleShuffleSeedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.ShuffleSeedPacket.Deserialize(reader);
            packetProcessingPolicy.ProcessOnClientFromHost(sender, () => blackJackGame.ReceiveShuffleSeed(packet.Seed));
        }

        public void HandleHitActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.HitActionPacket.Deserialize(reader);
            packetProcessingPolicy.HandleClientPlayerAction(sender, packet.PlayerIndex, blackJackGame.HandleReceivedHitAction);
        }

        public void HandleStandActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.StandActionPacket.Deserialize(reader);
            packetProcessingPolicy.HandleClientPlayerAction(sender, packet.PlayerIndex, blackJackGame.HandleReceivedStandAction);
        }

        public void HandleDoubleActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.DoubleActionPacket.Deserialize(reader);
            packetProcessingPolicy.HandleClientPlayerAction(sender, packet.PlayerIndex, blackJackGame.HandleReceivedDoubleAction);
        }

        public void HandleSplitActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.SplitActionPacket.Deserialize(reader);
            packetProcessingPolicy.HandleClientPlayerAction(sender, packet.PlayerIndex, blackJackGame.HandleReceivedSplitAction);
        }

        public void HandleInsuranceActionPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.InsuranceActionPacket.Deserialize(reader);
            packetProcessingPolicy.HandleClientPlayerAction(sender, packet.PlayerIndex, blackJackGame.HandleReceivedInsuranceAction);
        }

        public void HandleTurnChangedPacket(NetworkGamer sender, PacketReader reader)
        {
            var packet = Blackjack.Networking.TurnChangedPacket.Deserialize(reader);
            Debug.WriteLine($"[PACKET] Turn changed from {sender.Gamertag}, current player index: {packet.CurrentPlayerIndex}");
            packetProcessingPolicy.ProcessOnClientFromHost(sender, () =>
                blackJackGame.HandleReceivedTurnChanged(packet.CurrentPlayerIndex));
        }
    }
}
