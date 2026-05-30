using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    internal static class GameplayPacketDispatcherFactory
    {
        public static Blackjack.Networking.GameplayPacketDispatcher Create(
            Action<NetworkGamer, PacketReader> handlePlayerListSyncPacket,
            Action<NetworkGamer, PacketReader> handleCardDealtPacket,
            Action<NetworkGamer, PacketReader> handleBetPlacedPacket,
            Action<NetworkGamer, PacketReader> handleChipAddedPacket,
            Action<NetworkGamer, PacketReader> handlePlayerActionPacket,
            Action<NetworkGamer, PacketReader> handleShuffleSeedPacket,
            Action<NetworkGamer, PacketReader> handleHitActionPacket,
            Action<NetworkGamer, PacketReader> handleStandActionPacket,
            Action<NetworkGamer, PacketReader> handleDoubleActionPacket,
            Action<NetworkGamer, PacketReader> handleSplitActionPacket,
            Action<NetworkGamer, PacketReader> handleInsuranceActionPacket,
            Action<NetworkGamer, PacketReader> handleTurnChangedPacket)
        {
            return new Blackjack.Networking.GameplayPacketDispatcher(
                new Dictionary<Blackjack.Networking.PacketType, Action<NetworkGamer, PacketReader>>
                {
                    { Blackjack.Networking.PacketType.PlayerListSync, handlePlayerListSyncPacket },
                    { Blackjack.Networking.PacketType.CardDealt, handleCardDealtPacket },
                    { Blackjack.Networking.PacketType.BetPlaced, handleBetPlacedPacket },
                    { Blackjack.Networking.PacketType.ChipAdded, handleChipAddedPacket },
                    { Blackjack.Networking.PacketType.PlayerAction, handlePlayerActionPacket },
                    { Blackjack.Networking.PacketType.ShuffleSeed, handleShuffleSeedPacket },
                    { Blackjack.Networking.PacketType.HitAction, handleHitActionPacket },
                    { Blackjack.Networking.PacketType.StandAction, handleStandActionPacket },
                    { Blackjack.Networking.PacketType.DoubleAction, handleDoubleActionPacket },
                    { Blackjack.Networking.PacketType.SplitAction, handleSplitActionPacket },
                    { Blackjack.Networking.PacketType.InsuranceAction, handleInsuranceActionPacket },
                    { Blackjack.Networking.PacketType.TurnChanged, handleTurnChangedPacket },
                });
        }
    }
}
