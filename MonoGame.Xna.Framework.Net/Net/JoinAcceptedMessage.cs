using System;
using Microsoft.Xna.Framework.Net;

namespace Microsoft.Xna.Framework.Net
{
    public class JoinAcceptedMessage : INetworkMessage
    {
        public byte MessageType => 3;
        public string SessionId { get; set; }
        public string HostGamerId { get; set; }
        public string HostGamertag { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write(SessionId);
            writer.Write(HostGamerId);
            writer.Write(HostGamertag);
        }

        public void Deserialize(PacketReader reader)
        {
            SessionId = reader.ReadString();
            HostGamerId = reader.ReadString();
            HostGamertag = reader.ReadString();
        }
    }
}
