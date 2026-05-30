using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Net;

namespace Blackjack.Networking
{
    /// <summary>
    /// Centralizes the packet receive loop and packet-type dispatch for gameplay.
    /// GameplayScreen supplies per-packet handlers so behavior remains unchanged.
    /// </summary>
    internal sealed class GameplayPacketDispatcher
    {
        private readonly IReadOnlyDictionary<PacketType, Action<NetworkGamer, PacketReader>> handlers;

        public GameplayPacketDispatcher(IReadOnlyDictionary<PacketType, Action<NetworkGamer, PacketReader>> handlers)
        {
            this.handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        public void Process(NetworkSession networkSession)
        {
            if (networkSession == null || networkSession.LocalGamers.Count == 0)
                return;

            var localGamer = networkSession.LocalGamers[0];
            var packetReader = new PacketReader();
            while (localGamer.IsDataAvailable)
            {
                NetworkGamer sender;
                localGamer.ReceiveData(packetReader, out sender);

                try
                {
                    var packetType = (PacketType)packetReader.ReadByte();
                    Debug.WriteLine($"[PACKET] Received {packetType} from {sender.Gamertag}");

                    if (handlers.TryGetValue(packetType, out var handler))
                    {
                        handler(sender, packetReader);
                    }
                    else
                    {
                        Debug.WriteLine($"[PACKET] Unknown packet type: {(byte)packetType}");
                    }
                }
                catch (EndOfStreamException ex)
                {
                    Debug.WriteLine($"[PACKET ERROR] EndOfStreamException while processing packet from {sender.Gamertag}: {ex.Message}");
                    Debug.WriteLine($"[PACKET ERROR] Stack trace: {ex.StackTrace}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PACKET ERROR] Exception while processing packet from {sender.Gamertag}: {ex.GetType().Name} - {ex.Message}");
                }
            }
        }
    }
}