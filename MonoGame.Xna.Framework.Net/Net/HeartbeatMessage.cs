namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Heartbeat message sent periodically to monitor connection health.
    /// </summary>
    public class HeartbeatMessage : INetworkMessage
    {
        public byte MessageType => 7;
        
        /// <summary>
        /// Timestamp in milliseconds since epoch for RTT calculation.
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// Sequence number for detecting packet loss.
        /// </summary>
        public uint SequenceNumber { get; set; }
        
        /// <summary>
        /// Current number of gamers in session.
        /// </summary>
        public int GamerCount { get; set; }
        
        /// <summary>
        /// ID of the gamer sending the heartbeat.
        /// </summary>
        public string GamerId { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write(Timestamp);
            writer.Write(SequenceNumber);
            writer.Write(GamerCount);
            writer.Write(GamerId ?? string.Empty);
        }

        public void Deserialize(PacketReader reader)
        {
            // Reader is positioned after the type byte
            Timestamp = reader.ReadInt64();
            SequenceNumber = reader.ReadUInt32();
            GamerCount = reader.ReadInt32();
            GamerId = reader.ReadString();
        }
    }
}