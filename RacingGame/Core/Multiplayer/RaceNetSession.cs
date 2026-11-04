using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Net;

namespace RacingGame.Multiplayer
{
    /// <summary>
    /// Who a packet type may come from
    /// </summary>
    public enum PacketAudience
    {
        Any,            // accept from anybody
        FromHost,       // we are a client and the host sent it
        FromClient,     // we are the host and a client sent it
    }

    /// <summary>
    /// Owns the network session during a LAN race: pumps it every frame, sends packets (reliably
    /// or not, through the library's delivery guarantees) and routes received packets to the
    /// handler registered for their type. Modelled on ShipGame's NetworkManager and
    /// ShipPacketDispatcher.
    ///
    /// The topology is a star: a client only knows the host, so the host relays car states between
    /// clients. Everything runs on the game thread; the library raises its events on a network
    /// thread, so they are queued and raised from <see cref="Update"/>.
    /// </summary>
    public sealed class RaceNetSession : IDisposable
    {
        const int ReceiveBufferSize = 2048;
        const int MaxPacketsPerUpdate = 1000;

        struct Entry
        {
            public PacketAudience Audience;
            public Action<NetworkGamer, BinaryReader> Handler;
        }

        readonly NetworkSession session;
        readonly Dictionary<RacePacketType, Entry> handlers = new Dictionary<RacePacketType, Entry>();
        readonly byte[] receiveBuffer = new byte[ReceiveBufferSize];
        readonly ConcurrentQueue<NetworkGamer> joinedQueue = new ConcurrentQueue<NetworkGamer>();
        readonly ConcurrentQueue<NetworkGamer> leftQueue = new ConcurrentQueue<NetworkGamer>();
        int sessionEnded;
        string endReason = "";
        bool disposed;

        public RaceNetSession(NetworkSession session)
        {
            this.session = session ?? throw new ArgumentNullException("session");
            NetLog.Write("Session started, this machine is the " + (session.IsHost ? "host" : "client"));
            session.GamerJoined += OnGamerJoined;
            session.GamerLeft += OnGamerLeft;
            session.SessionEnded += OnSessionEnded;
        }

        public NetworkSession Session { get { return session; } }
        public bool IsHost { get { return session.IsHost; } }

        /// <summary>
        /// The gamer on this machine
        /// </summary>
        public NetworkGamer LocalGamer
        {
            get { return session.LocalGamers.Count > 0 ? session.LocalGamers[0] : null; }
        }

        /// <summary>
        /// True once the session has ended (host left, network error, disposed)
        /// </summary>
        public bool HasEnded { get { return disposed || sessionEnded != 0; } }

        /// <summary>
        /// Why the session ended (empty while it is running)
        /// </summary>
        public string EndReason { get { return endReason; } }

        // raised from Update on the game thread
        public event Action<NetworkGamer> GamerJoined;
        public event Action<NetworkGamer> GamerLeft;
        public event Action SessionEnded;

        #region Handlers

        public void Register(RacePacketType type, PacketAudience audience, Action<NetworkGamer, BinaryReader> handler)
        {
            handlers[type] = new Entry { Audience = audience, Handler = handler };
        }

        public void Unregister(RacePacketType type)
        {
            handlers.Remove(type);
        }

        void Dispatch(NetworkGamer sender, byte[] payload)
        {
            RacePacketType type = (RacePacketType)payload[0];

            Entry entry;
            if (!handlers.TryGetValue(type, out entry))
            {
                Debug.WriteLine("[NET] No handler for packet type " + payload[0]);
                return;
            }

            bool accepted =
                entry.Audience == PacketAudience.Any ||
                (entry.Audience == PacketAudience.FromHost && !session.IsHost && sender != null && sender.IsHost) ||
                (entry.Audience == PacketAudience.FromClient && session.IsHost);
            if (!accepted)
            {
                Debug.WriteLine("[NET] Rejected " + type + " from " + (sender == null ? "?" : sender.Gamertag));
                return;
            }

            try
            {
                using (MemoryStream stream = new MemoryStream(payload, 1, payload.Length - 1, false))
                using (BinaryReader reader = new BinaryReader(stream))
                    entry.Handler(sender, reader);
            }
            catch (Exception ex)
            {
                // a bad packet must not take the game down, but it must not vanish either
                NetLog.Write("Error handling " + type + " from " + (sender == null ? "?" : sender.Gamertag) +
                    ": " + ex.GetType().Name + " " + ex.Message);
            }
        }

        #endregion

        #region Sending

        public void Send(NetworkGamer recipient, INetworkPacket packet, bool reliable)
        {
            SendPayload(recipient, Serialize(packet), reliable);
        }

        /// <summary>
        /// Send a packet to the host (clients only)
        /// </summary>
        public void SendToHost(INetworkPacket packet, bool reliable)
        {
            if (session.IsHost)
                throw new InvalidOperationException("The host has no host to send to.");
            Send(session.Host, packet, reliable);
        }

        /// <summary>
        /// Send a packet to every other machine (the host sends to all clients, a client to the host)
        /// </summary>
        public void Broadcast(INetworkPacket packet, bool reliable)
        {
            Relay(packet, reliable, null);
        }

        /// <summary>
        /// Send a packet to every other machine except one. The host uses this to pass a packet
        /// it received from one client on to the other clients.
        /// </summary>
        public void Relay(INetworkPacket packet, bool reliable, NetworkGamer except)
        {
            byte[] payload = Serialize(packet);
            foreach (NetworkGamer peer in session.RemoteGamers)
                if (except == null || peer.GamerId != except.GamerId)
                    SendPayload(peer, payload, reliable);
        }

        void SendPayload(NetworkGamer recipient, byte[] payload, bool reliable)
        {
            if (HasEnded || recipient == null || LocalGamer == null)
                return;

            // a byte[] and not a PacketWriter: nothing is left over from an earlier packet
            LocalGamer.SendData(payload, reliable ? SendDataOptions.Reliable : SendDataOptions.None, recipient);
        }

        static byte[] Serialize(INetworkPacket packet)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((byte)packet.Type);
                packet.Serialize(writer);
                writer.Flush();
                return stream.ToArray();
            }
        }

        #endregion

        /// <summary>
        /// Pump the session: receive packets, resend unacknowledged ones and raise queued events.
        /// Call once per frame.
        /// </summary>
        public void Update()
        {
            if (HasEnded)
            {
                RaiseEvents();
                return;
            }

            session.Update();
            ReceivePackets();
            RaiseEvents();
        }

        void ReceivePackets()
        {
            NetworkGamer local = LocalGamer;
            if (local == null)
                return;

            for (int i = 0; i < MaxPacketsPerUpdate && local.IsDataAvailable; i++)
            {
                NetworkGamer sender;
                int length = local.ReceiveData(receiveBuffer, out sender);

                // data for everyone also comes back to the sender (as in XNA)
                if (sender == null || sender.IsLocal || length <= 0)
                    continue;

                byte[] payload = new byte[length];
                Buffer.BlockCopy(receiveBuffer, 0, payload, 0, length);
                Dispatch(sender, payload);
            }
        }

        void RaiseEvents()
        {
            NetworkGamer gamer;
            while (joinedQueue.TryDequeue(out gamer))
                if (GamerJoined != null)
                    GamerJoined(gamer);

            while (leftQueue.TryDequeue(out gamer))
                if (GamerLeft != null)
                    GamerLeft(gamer);

            if (sessionEnded == 1)
            {
                sessionEnded = 2;
                if (SessionEnded != null)
                    SessionEnded();
            }
        }

        #region Session events (network thread)

        void OnGamerJoined(object sender, GamerJoinedEventArgs e)
        {
            NetworkGamer gamer = e.Gamer as NetworkGamer;
            if (gamer != null)
            {
                NetLog.Write("Gamer joined: " + gamer.Gamertag);
                joinedQueue.Enqueue(gamer);
            }
        }

        void OnGamerLeft(object sender, GamerLeftEventArgs e)
        {
            NetworkGamer gamer = e.Gamer as NetworkGamer;
            if (gamer != null)
            {
                NetLog.Write("Gamer left: " + gamer.Gamertag);
                leftQueue.Enqueue(gamer);
            }
        }

        void OnSessionEnded(object sender, NetworkSessionEndedEventArgs e)
        {
            endReason = e.EndReason.ToString();
            NetLog.Write("Session ended: " + endReason);
            System.Threading.Interlocked.CompareExchange(ref sessionEnded, 1, 0);
        }

        #endregion

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            session.GamerJoined -= OnGamerJoined;
            session.GamerLeft -= OnGamerLeft;
            session.SessionEnded -= OnSessionEnded;
            session.Dispose();
        }
    }

    /// <summary>
    /// A small log of what happened on the network, written to RacingGame-net.log in the temp
    /// folder. A packaged game has no console, so without it there is nothing to look at when a
    /// race ends unexpectedly.
    /// </summary>
    public static class NetLog
    {
        static readonly object sync = new object();

        public static readonly string Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "RacingGame-net.log");

        public static void Write(string message)
        {
            Debug.WriteLine("[NET] " + message);
            try
            {
                lock (sync)
                    File.AppendAllText(Path, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message + Environment.NewLine);
            }
            catch (Exception)
            {
                // a log that cannot be written must never stop the game
            }
        }
    }
}
