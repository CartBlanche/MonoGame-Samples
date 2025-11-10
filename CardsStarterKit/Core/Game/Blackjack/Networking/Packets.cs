using Microsoft.Xna.Framework.Net;
using CardsFramework;
using System.Collections.Generic;

namespace Blackjack.Networking
{
    public class PlayerInfo
    {
        public string Name { get; set; }
        public bool IsAI { get; set; }
    }
    
    public class PlayerListSyncPacket
    {
        public List<PlayerInfo> Players { get; set; }
        
        public void Serialize(PacketWriter writer)
        {
            writer.Write((byte)Players.Count);
            foreach (var player in Players)
            {
                writer.Write(player.Name);
                writer.Write(player.IsAI);
            }
        }
        
        public static PlayerListSyncPacket Deserialize(PacketReader reader)
        {
            var packet = new PlayerListSyncPacket { Players = new List<PlayerInfo>() };
            byte count = reader.ReadByte();
            for (int i = 0; i < count; i++)
            {
                packet.Players.Add(new PlayerInfo
                {
                    Name = reader.ReadString(),
                    IsAI = reader.ReadBoolean()
                });
            }
            return packet;
        }
    }

    public class PlayerActionPacket
    {
        public BlackjackAction Action { get; set; }
        public void Serialize(PacketWriter writer)
        {
            writer.Write((byte)Action);
        }
        public static PlayerActionPacket Deserialize(PacketReader reader)
        {
            return new PlayerActionPacket
            {
                Action = (BlackjackAction)reader.ReadByte()
            };
        }
    }

    public class BetPlacedPacket
    {
        public byte PlayerIndex { get; set; }
        public int BetAmount { get; set; }
        
        public void Serialize(PacketWriter writer)
        {
            writer.Write(PlayerIndex);
            writer.Write(BetAmount);
        }
        
        public static BetPlacedPacket Deserialize(PacketReader reader)
        {
            return new BetPlacedPacket
            {
                PlayerIndex = reader.ReadByte(),
                BetAmount = reader.ReadInt32()
            };
        }
    }

    public class ChipAddedPacket
    {
        public byte PlayerIndex { get; set; }
        public int ChipValue { get; set; }
        
        public void Serialize(PacketWriter writer)
        {
            writer.Write(PlayerIndex);
            writer.Write(ChipValue);
        }
        
        public static ChipAddedPacket Deserialize(PacketReader reader)
        {
            return new ChipAddedPacket
            {
                PlayerIndex = reader.ReadByte(),
                ChipValue = reader.ReadInt32()
            };
        }
    }

    public class CardDealtPacket
    {
        public byte PlayerIndex { get; set; }
        public TraditionalCard Card { get; set; }
        public bool FaceDown { get; set; }
        public HandTypes HandType { get; set; }
        public void Serialize(PacketWriter writer)
        {
            writer.Write(PlayerIndex);
            writer.Write(Card);
            writer.Write(FaceDown);
            writer.Write((byte)HandType);
        }
        public static CardDealtPacket Deserialize(PacketReader reader)
        {
            try
            {
                var playerIndex = reader.ReadByte();
                System.Console.WriteLine($"[CardDealtPacket] PlayerIndex: {playerIndex}");
                
                var card = reader.ReadCard();
                System.Console.WriteLine($"[CardDealtPacket] Card: {card.Type} {card.Value}");
                
                var faceDown = reader.ReadBoolean();
                System.Console.WriteLine($"[CardDealtPacket] FaceDown: {faceDown}");
                
                var handType = (HandTypes)reader.ReadByte();
                System.Console.WriteLine($"[CardDealtPacket] HandType: {handType}");
                
                return new CardDealtPacket
                {
                    PlayerIndex = playerIndex,
                    Card = card,
                    FaceDown = faceDown,
                    HandType = handType
                };
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CardDealtPacket ERROR] Failed to deserialize: {ex.Message}");
                throw;
            }
        }
    }

    public class ShuffleSeedPacket
    {
        public int Seed { get; set; }
        public void Serialize(PacketWriter writer)
        {
            writer.Write(Seed);
        }
        public static ShuffleSeedPacket Deserialize(PacketReader reader)
        {
            return new ShuffleSeedPacket
            {
                Seed = reader.ReadInt32()
            };
        }
    }

    public class TurnChangedPacket
    {
        public byte CurrentPlayerIndex { get; set; }
        public void Serialize(PacketWriter writer)
        {
            writer.Write(CurrentPlayerIndex);
        }
        public static TurnChangedPacket Deserialize(PacketReader reader)
        {
            return new TurnChangedPacket
            {
                CurrentPlayerIndex = reader.ReadByte()
            };
        }
    }

    public class BalanceUpdatePacket
    {
        public byte PlayerIndex { get; set; }
        public float NewBalance { get; set; }
        public void Serialize(PacketWriter writer)
        {
            writer.Write(PlayerIndex);
            writer.Write(NewBalance);
        }
        public static BalanceUpdatePacket Deserialize(PacketReader reader)
        {
            return new BalanceUpdatePacket
            {
                PlayerIndex = reader.ReadByte(),
                NewBalance = reader.ReadSingle()
            };
        }
    }
}
