using Microsoft.Xna.Framework.Net;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Message sent when a gamer explicitly leaves the session (graceful disconnect).
    /// This allows immediate notification instead of waiting for heartbeat timeout (9 seconds).
    /// </summary>
    public class GamerLeavingMessage : INetworkMessage
    {
        /// <summary>
        /// Message type ID for GamerLeavingMessage.
        /// </summary>
        public byte MessageType => 9;

        /// <summary>
        /// The unique ID of the gamer who is leaving.
        /// </summary>
        public string GamerId { get; set; }

        /// <summary>
        /// Optional reason for leaving (e.g., "User quit", "Application closing").
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Serializes the message to a packet writer.
        /// </summary>
        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write(GamerId ?? string.Empty);
            writer.Write(Reason ?? string.Empty);
        }

        /// <summary>
        /// Deserializes the message from a packet reader.
        /// </summary>
        public void Deserialize(PacketReader reader)
        {
            GamerId = reader.ReadString();
            Reason = reader.ReadString();
        }
    }
}
