namespace Microsoft.Xna.Framework.Net
{
    public class PlayerMoveMessage : INetworkMessage
    {
        public byte MessageType => 1;
        public int PlayerId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write(PlayerId);
            writer.Write(X);
            writer.Write(Y);
            writer.Write(Z);
        }

        public void Deserialize(PacketReader reader)
        {
            // Reader is positioned after the type byte
            PlayerId = reader.ReadInt32();
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
        }
    }
}