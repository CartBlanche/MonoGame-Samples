using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Reply message sent in response to a heartbeat, used for RTT calculation.
    /// </summary>
    public class HeartbeatReplyMessage : INetworkMessage
    {
        public byte MessageType => 8;
        
        /// <summary>
        /// Echo back the request timestamp for RTT calculation.
        /// </summary>
        public long RequestTimestamp { get; set; }
        
        /// <summary>
        /// Reply's own timestamp.
        /// </summary>
        public long ReplyTimestamp { get; set; }
        
        /// <summary>
        /// ID of the gamer sending the reply.
        /// </summary>
        public string GamerId { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write(RequestTimestamp);
            writer.Write(ReplyTimestamp);
            writer.Write(GamerId ?? string.Empty);
        }

        public void Deserialize(PacketReader reader)
        {
            // Reader is positioned after the type byte
            RequestTimestamp = reader.ReadInt64();
            ReplyTimestamp = reader.ReadInt64();
            GamerId = reader.ReadString();
        }
    }
}
