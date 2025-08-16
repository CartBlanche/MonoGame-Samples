using System;
using Microsoft.Xna.Framework.Net;

namespace Microsoft.Xna.Framework.Net
{
    public class JoinRequestMessage : INetworkMessage
    {
        public byte MessageType => 2;
        public string GamerId { get; set; }
        public string Gamertag { get; set; }

        public void Serialize(PacketWriter writer)
        {
            writer.Write(MessageType);
            writer.Write(GamerId);
            writer.Write(Gamertag);
        }

        public void Deserialize(PacketReader reader)
        {
            GamerId = reader.ReadString();
            Gamertag = reader.ReadString();
        }
    }
}
