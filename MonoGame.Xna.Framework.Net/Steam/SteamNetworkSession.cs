using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Steam vertical-slice INetworkSession implementation.
    ///
    /// This implementation provides a Steam-like host/find/join/reliable-message flow using
    /// an in-memory backend so the integration seam is testable before wiring native Steamworks APIs.
    /// </summary>
    public sealed class SteamNetworkSession : INetworkSession
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, SteamNetworkGamer> gamers = new Dictionary<string, SteamNetworkGamer>();
        private readonly Queue<(byte[] Data, string SenderId)> inboundPackets = new Queue<(byte[] Data, string SenderId)>();

        private bool disposed;
        private NetworkSessionState state;
        private NetworkSessionType sessionType;

        public SteamNetworkSession()
        {
            state = NetworkSessionState.Creating;
        }

        public IReadOnlyList<INetworkGamer> AllGamers
        {
            get
            {
                lock (gate)
                {
                    return gamers.Values.Cast<INetworkGamer>().ToList();
                }
            }
        }

        public ILocalNetworkGamer LocalGamer { get; private set; }

        public NetworkSessionState State => state;

        public string SessionId { get; private set; }

        internal bool IsHostSession => LocalGamer != null && LocalGamer.IsHost;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<GamerJoinedEventArgs> GamerJoined;
        public event EventHandler<GamerLeftEventArgs> GamerLeft;
        public event EventHandler<GameStartedEventArgs> GameStarted;
        public event EventHandler<GameEndedEventArgs> GameEnded;
        public event EventHandler<NetworkSessionEndedEventArgs> SessionEnded;

        public Task CreateAsync(NetworkSessionType sessionType, int maxGamers, int privateGamerSlots)
        {
            ThrowIfDisposed();

            if (maxGamers < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxGamers));
            }

            this.sessionType = sessionType;
            SessionId = Guid.NewGuid().ToString("N");

            var local = new SteamLocalNetworkGamer(NewGamerId(), BuildDefaultGamertag("steam_host"), isHost: true);

            lock (gate)
            {
                LocalGamer = local;
                gamers[local.Id] = local;
            }

            state = NetworkSessionState.Lobby;
            SteamSessionDirectory.RegisterHost(this, SessionId, sessionType, maxGamers);
            return Task.CompletedTask;
        }

        public Task JoinAsync(string hostAddress)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                throw new ArgumentException("hostAddress is required.", nameof(hostAddress));
            }

            SessionId = hostAddress.Trim();
            var local = new SteamLocalNetworkGamer(NewGamerId(), BuildDefaultGamertag("steam_client"), isHost: false);

            lock (gate)
            {
                LocalGamer = local;
                gamers[local.Id] = local;
                state = NetworkSessionState.Joining;
            }

            SteamSessionDirectory.JoinLobby(SessionId, this);
            return Task.CompletedTask;
        }

        public void SendMessage(INetworkMessage message, INetworkGamer recipient)
        {
            ThrowIfDisposed();
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (recipient == null)
            {
                throw new ArgumentNullException(nameof(recipient));
            }

            if (LocalGamer == null)
            {
                throw new InvalidOperationException("Session is not initialized.");
            }

            var payload = SerializeMessage(message);
            SteamSessionDirectory.SendReliable(recipient.Id, payload, LocalGamer.Id);
        }

        public void BroadcastMessage(INetworkMessage message)
        {
            ThrowIfDisposed();
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (LocalGamer == null)
            {
                throw new InvalidOperationException("Session is not initialized.");
            }

            var payload = SerializeMessage(message);
            var remoteRecipientIds = AllGamers.Where(g => !g.IsLocal).Select(g => g.Id).ToList();
            foreach (var recipientId in remoteRecipientIds)
            {
                SteamSessionDirectory.SendReliable(recipientId, payload, LocalGamer.Id);
            }
        }

        public void Update(GameTime gameTime)
        {
            ThrowIfDisposed();
            ProcessInboundPackets();
        }

        public Task CloseAsync()
        {
            if (disposed)
            {
                return Task.CompletedTask;
            }

            SteamSessionDirectory.RemoveSession(this);
            ForceSessionEnded(NetworkSessionEndReason.ClientSignedOut);
            disposed = true;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            CloseAsync().GetAwaiter().GetResult();
        }

        internal void EnsureRemoteGamer(string gamerId, string gamertag, bool isHost)
        {
            lock (gate)
            {
                if (LocalGamer != null && gamerId == LocalGamer.Id)
                {
                    return;
                }

                if (!gamers.ContainsKey(gamerId))
                {
                    gamers[gamerId] = new SteamNetworkGamer(gamerId, gamertag, isLocal: false, isHost: isHost);
                }
            }
        }

        internal void NotifyGamerJoined(string gamerId)
        {
            INetworkGamer joined;
            lock (gate)
            {
                gamers.TryGetValue(gamerId, out var resolved);
                joined = resolved;
            }

            if (joined != null)
            {
                GamerJoined?.Invoke(this, new GamerJoinedEventArgs(joined));
            }
        }

        internal void NotifyGamerLeft(string gamerId)
        {
            INetworkGamer left;
            lock (gate)
            {
                gamers.TryGetValue(gamerId, out var resolved);
                left = resolved;
            }

            if (left != null)
            {
                GamerLeft?.Invoke(this, new GamerLeftEventArgs(left));
            }
        }

        internal void RemoveRemoteGamer(string gamerId)
        {
            lock (gate)
            {
                if (LocalGamer != null && LocalGamer.Id == gamerId)
                {
                    return;
                }

                gamers.Remove(gamerId);
            }
        }

        internal void EnqueueInbound(byte[] data, string senderId)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            var copy = new byte[data.Length];
            Buffer.BlockCopy(data, 0, copy, 0, data.Length);

            lock (gate)
            {
                inboundPackets.Enqueue((copy, senderId));
            }
        }

        internal void ForceSessionEnded(NetworkSessionEndReason reason)
        {
            if (state == NetworkSessionState.Ended)
            {
                return;
            }

            state = NetworkSessionState.Ended;
            SessionEnded?.Invoke(this, new NetworkSessionEndedEventArgs(reason));
            GameEnded?.Invoke(this, new GameEndedEventArgs());
        }

        internal void PromoteToPlayingIfNeeded()
        {
            bool raiseStarted = false;

            lock (gate)
            {
                if (state == NetworkSessionState.Lobby || state == NetworkSessionState.Joining)
                {
                    state = NetworkSessionState.Playing;
                    raiseStarted = true;
                }
            }

            if (raiseStarted)
            {
                GameStarted?.Invoke(this, new GameStartedEventArgs());
            }
        }

        private void ProcessInboundPackets()
        {
            while (true)
            {
                (byte[] Data, string SenderId) next;
                lock (gate)
                {
                    if (inboundPackets.Count == 0)
                    {
                        break;
                    }

                    next = inboundPackets.Dequeue();
                }

                if (next.Data.Length < 1)
                {
                    continue;
                }

                var reader = new PacketReader(next.Data);
                var typeId = reader.ReadByte();
                var message = NetworkMessageRegistry.CreateMessage(typeId);
                if (message == null)
                {
                    continue;
                }

                message.Deserialize(reader);

                INetworkGamer sender = null;
                lock (gate)
                {
                    gamers.TryGetValue(next.SenderId, out var resolvedSender);
                    sender = resolvedSender;
                }

                var args = new MessageReceivedEventArgs(message, null)
                {
                    Sender = sender
                };

                MessageReceived?.Invoke(this, args);
            }
        }

        private static byte[] SerializeMessage(INetworkMessage message)
        {
            var writer = new PacketWriter();
            writer.Write(message.MessageType);
            message.Serialize(writer);
            return writer.GetData();
        }

        private static string NewGamerId() => Guid.NewGuid().ToString("N");

        private static string BuildDefaultGamertag(string prefix)
        {
            var user = Environment.UserName;
            if (string.IsNullOrWhiteSpace(user))
            {
                user = prefix;
            }

            return $"{user}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SteamNetworkSession));
            }
        }
    }
}
