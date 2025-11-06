using Microsoft.Xna.Framework.Net;
using CardsFramework;

namespace Blackjack.Networking
{
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
        public int BetAmount { get; set; }
        public void Serialize(PacketWriter writer)
        {
            writer.Write(BetAmount);
        }
        public static BetPlacedPacket Deserialize(PacketReader reader)
        {
            return new BetPlacedPacket
            {
                BetAmount = reader.ReadInt32()
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
            return new CardDealtPacket
            {
                PlayerIndex = reader.ReadByte(),
                Card = reader.ReadCard(),
                FaceDown = reader.ReadBoolean(),
                HandType = (HandTypes)reader.ReadByte()
            };
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
