using System;
using Microsoft.Xna.Framework.Net;

namespace Microsoft.Xna.Framework.Net
{
    public class JoinRequestMessage : INetworkMessage
    {
        public const byte CURRENT_PROTOCOL_VERSION = 1;
        
        public byte MessageType => 2;
        public byte ProtocolVersion { get; set; } = CURRENT_PROTOCOL_VERSION;
        public string GamerId { get; set; }
        public string Gamertag { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write(ProtocolVersion);
            writer.Write(GamerId);
            writer.Write(Gamertag);
        }

        public void Deserialize(PacketReader reader)
        {
            // Reader is positioned after the type byte
            ProtocolVersion = reader.ReadByte();
            GamerId = reader.ReadString();
            Gamertag = reader.ReadString();
        }
    }
}