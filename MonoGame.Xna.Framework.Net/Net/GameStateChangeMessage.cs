namespace Microsoft.Xna.Framework.Net
{
    public enum GameStateChangeKind : byte
    {
        Started = 1,
        Ended = 2
    }

    public class GameStateChangeMessage : INetworkMessage
    {
        public byte MessageType => 5;
        public GameStateChangeKind Kind { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write((byte)Kind);
        }

        public void Deserialize(PacketReader reader)
        {
            Kind = (GameStateChangeKind)reader.ReadByte();
        }
    }
}
