using Microsoft.Xna.Framework.Net;
using CardsFramework;

namespace Blackjack.Networking
{
    public static class NetworkSerializationExtensions
    {
        public static void Write(this PacketWriter writer, TraditionalCard card)
        {
            writer.Write((byte)card.Type); // Suit
            writer.Write((byte)card.Value); // Value
        }

        public static TraditionalCard ReadCard(this PacketReader reader)
        {
            var suit = (CardSuit)reader.ReadByte();
            var value = (CardValue)reader.ReadByte();
            return TraditionalCard.Create(suit, value);
        }

        public static void Write(this PacketWriter writer, Hand hand)
        {
            writer.Write(hand.Count);
            for (int i = 0; i < hand.Count; i++)
            {
                writer.Write(hand[i]);
            }
        }

        public static Hand ReadHand(this PacketReader reader)
        {
            var hand = new Hand();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                // Add is internal, so this may need to be called from within the same assembly
                var card = reader.ReadCard();
                hand.GetType().GetMethod("Add", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(hand, new object[] { card });
            }
            return hand;
        }
    }
}
