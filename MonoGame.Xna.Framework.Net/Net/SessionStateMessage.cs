using Microsoft.Xna.Framework.Net;
using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Message broadcast by host to synchronize session state transitions across all clients.
    /// Ensures all players see the same state (Lobby → Playing → Ended) at the same time.
    /// </summary>
    public class SessionStateMessage : INetworkMessage
    {
        /// <summary>
        /// Message type ID for SessionStateMessage.
        /// </summary>
        public byte MessageType => 10;

        /// <summary>
        /// The new session state.
        /// </summary>
        public NetworkSessionState NewState { get; set; }

        /// <summary>
        /// UTC timestamp when the state change occurred (milliseconds since Unix epoch).
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// Optional reason for the state change (e.g., "Host started game", "Game completed").
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// Serializes the message to a packet writer.
        /// </summary>
        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write((byte)NewState);
            writer.Write(Timestamp);
            writer.Write(Reason ?? string.Empty);
        }

        /// <summary>
        /// Deserializes the message from a packet reader.
        /// </summary>
        public void Deserialize(PacketReader reader)
        {
            NewState = (NetworkSessionState)reader.ReadByte();
            Timestamp = reader.ReadInt64();
            Reason = reader.ReadString();
        }
    }
}
