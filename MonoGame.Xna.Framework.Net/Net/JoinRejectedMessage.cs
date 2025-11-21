using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Message sent by the host to reject a join request.
    /// </summary>
    public class JoinRejectedMessage : INetworkMessage
    {
        public byte MessageType => 6;
        
        /// <summary>
        /// The reason for the rejection.
        /// </summary>
        public NetworkSessionJoinError ErrorCode { get; set; }
        
        /// <summary>
        /// Human-readable reason for rejection.
        /// </summary>
        public string Reason { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write((byte)ErrorCode);
            writer.Write(Reason ?? string.Empty);
        }

        public void Deserialize(PacketReader reader)
        {
            // Reader is positioned after the type byte
            ErrorCode = (NetworkSessionJoinError)reader.ReadByte();
            Reason = reader.ReadString();
        }
    }
}
