using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Adapter that wraps the existing NetworkSession to implement INetworkSession.
    /// This allows NetworkSession to be used transparently through the INetworkSession interface,
    /// enabling future alternative implementations (e.g., Steam) to be swapped in via dependency injection.
    /// </summary>
    internal class UdpNetworkSession : INetworkSession
    {
        private NetworkSession innerSession;
        private ILocalNetworkGamer localGamerAdapter;
        private Dictionary<string, INetworkGamer> gamerAdapters;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of UdpNetworkSession with an underlying NetworkSession.
        /// </summary>
        internal UdpNetworkSession(NetworkSession underlyingSession)
        {
            this.innerSession = underlyingSession;
            this.gamerAdapters = new Dictionary<string, INetworkGamer>();
            this.disposed = false;
        }

        /// <summary>
        /// Initializes a new instance of UdpNetworkSession (default constructor for factory).
        /// </summary>
        internal UdpNetworkSession() : this(null)
        {
        }

        /// <summary>
        /// Gets all gamers (local and remote) in the session.
        /// </summary>
        public IReadOnlyList<INetworkGamer> AllGamers
        {
            get
            {
                if (innerSession == null) return new List<INetworkGamer>();
                return innerSession.AllGamers.Select(AdaptGamer).ToList();
            }
        }

        /// <summary>
        /// Gets the local player in the session.
        /// </summary>
        public ILocalNetworkGamer LocalGamer
        {
            get
            {
                if (innerSession == null) return null;

                var local = innerSession.LocalGamers.FirstOrDefault();
                if (local == null) return null;

                if (localGamerAdapter == null)
                {
                    localGamerAdapter = new LocalNetworkGamerAdapter(local);
                }
                return localGamerAdapter;
            }
        }

        /// <summary>
        /// Gets the current state of the session.
        /// </summary>
        public NetworkSessionState State
        {
            get
            {
                if (innerSession == null) return NetworkSessionState.Creating;
                return innerSession.SessionState;
            }
        }

        /// <summary>
        /// Gets the unique identifier for this session.
        /// </summary>
        public string SessionId
        {
            get
            {
                if (innerSession == null) return null;
                return innerSession.sessionId;
            }
        }

        /// <summary>
        /// Occurs when a message is received from a remote gamer.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived
        {
            add
            {
                if (innerSession != null)
                    innerSession.MessageReceived += value;
            }
            remove
            {
                if (innerSession != null)
                    innerSession.MessageReceived -= value;
            }
        }

        /// <summary>
        /// Occurs when a remote gamer joins the session.
        /// </summary>
        public event EventHandler<GamerJoinedEventArgs> GamerJoined
        {
            add
            {
                if (innerSession != null)
                    innerSession.GamerJoined += value;
            }
            remove
            {
                if (innerSession != null)
                    innerSession.GamerJoined -= value;
            }
        }

        /// <summary>
        /// Occurs when a remote gamer leaves the session.
        /// </summary>
        public event EventHandler<GamerLeftEventArgs> GamerLeft
        {
            add
            {
                if (innerSession != null)
                    innerSession.GamerLeft += value;
            }
            remove
            {
                if (innerSession != null)
                    innerSession.GamerLeft -= value;
            }
        }

        /// <summary>
        /// Occurs when the game is ready to start.
        /// </summary>
        public event EventHandler<GameStartedEventArgs> GameStarted
        {
            add
            {
                if (innerSession != null)
                    innerSession.GameStarted += value;
            }
            remove
            {
                if (innerSession != null)
                    innerSession.GameStarted -= value;
            }
        }

        /// <summary>
        /// Occurs when the game has ended.
        /// </summary>
        public event EventHandler<GameEndedEventArgs> GameEnded
        {
            add
            {
                if (innerSession != null)
                    innerSession.GameEnded += value;
            }
            remove
            {
                if (innerSession != null)
                    innerSession.GameEnded -= value;
            }
        }

        /// <summary>
        /// Occurs when the session ends.
        /// </summary>
        public event EventHandler<NetworkSessionEndedEventArgs> SessionEnded
        {
            add
            {
                if (innerSession != null)
                    innerSession.SessionEnded += value;
            }
            remove
            {
                if (innerSession != null)
                    innerSession.SessionEnded -= value;
            }
        }

        /// <summary>
        /// Creates a new session as host.
        /// </summary>
        public async System.Threading.Tasks.Task CreateAsync(NetworkSessionType sessionType, int maxGamers, int privateGamerSlots)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(UdpNetworkSession));

            var newSession = await NetworkSession.CreateAsync(
                sessionType,
                maxLocalGamers: 1,
                maxGamers: maxGamers,
                privateGamerSlots: privateGamerSlots,
                sessionProperties: null
            );

            // Replace inner session
            ReplaceInnerSession(newSession);
        }

        /// <summary>
        /// Joins an existing session.
        /// </summary>
        public async System.Threading.Tasks.Task JoinAsync(string hostAddress)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(UdpNetworkSession));

            // Parse hostAddress to find the session
            var availableSessions = await NetworkSession.FindAsync(
                NetworkSessionType.SystemLink,
                maxLocalGamers: 1,
                sessionProperties: null
            );

            var session = availableSessions.FirstOrDefault(s => s.HostEndpoint?.ToString() == hostAddress);
            if (session != null)
            {
                var newSession = await NetworkSession.JoinAsync(session);
                ReplaceInnerSession(newSession);
            }
            else
            {
                throw new InvalidOperationException($"Session not found: {hostAddress}");
            }
        }

        /// <summary>
        /// Sends a message to a specific gamer.
        /// </summary>
        public void SendMessage(INetworkMessage message, INetworkGamer recipient)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(UdpNetworkSession));

            if (innerSession == null)
                throw new InvalidOperationException("Session not initialized");

            // Find the underlying NetworkGamer and use session's SendDataToGamer
            var networkGamer = innerSession.AllGamers.FirstOrDefault(g => g.Id == recipient.Id);
            if (networkGamer != null)
            {
                var writer = new PacketWriter();
                message.Serialize(writer);
                innerSession.SendDataToGamer(networkGamer, writer, SendDataOptions.Reliable);
            }
        }

        /// <summary>
        /// Broadcasts a message to all remote gamers.
        /// </summary>
        public void BroadcastMessage(INetworkMessage message)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(UdpNetworkSession));

            if (innerSession == null)
                throw new InvalidOperationException("Session not initialized");

            // Broadcast to all remote gamers using SendToAll
            var writer = new PacketWriter();
            message.Serialize(writer);
            innerSession.SendToAll(writer, SendDataOptions.Reliable);
        }

        /// <summary>
        /// Updates the session (processes incoming messages, etc.).
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (disposed || innerSession == null)
                return;

            // The underlying NetworkSession handles updates internally
            // This is a pass-through for interface compliance
        }

        /// <summary>
        /// Closes the session and disconnects.
        /// </summary>
        public async System.Threading.Tasks.Task CloseAsync()
        {
            if (disposed || innerSession == null)
                return;

            await innerSession.DisposeAsync();
        }

        /// <summary>
        /// Disposes the session.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            innerSession?.Dispose();
            gamerAdapters.Clear();
            disposed = true;
        }

        // Helper methods

        private INetworkGamer AdaptGamer(NetworkGamer gamer)
        {
            if (gamer == null) return null;

            if (!gamerAdapters.TryGetValue(gamer.Id, out var adapter))
            {
                adapter = gamer.IsLocal
                    ? (INetworkGamer)new LocalNetworkGamerAdapter(gamer as LocalNetworkGamer)
                    : new NetworkGamerAdapter(gamer);

                gamerAdapters[gamer.Id] = adapter;
            }

            return adapter;
        }

        private void ReplaceInnerSession(NetworkSession newSession)
        {
            if (innerSession != null)
            {
                innerSession.Dispose();
            }

            innerSession = newSession;

            gamerAdapters.Clear();
            localGamerAdapter = null;
        }
    }

    /// <summary>
    /// Adapter for NetworkGamer to implement INetworkGamer.
    /// </summary>
    internal class NetworkGamerAdapter : INetworkGamer
    {
        private readonly NetworkGamer innerGamer;

        internal NetworkGamerAdapter(NetworkGamer gamer)
        {
            this.innerGamer = gamer ?? throw new ArgumentNullException(nameof(gamer));
        }

        public string Id => innerGamer.Id;
        public string Gamertag => innerGamer.Gamertag;
        public bool IsLocal => innerGamer.IsLocal;
        public bool IsHost => innerGamer.IsHost;
        public bool IsReady
        {
            get => innerGamer.IsReady;
            set => innerGamer.IsReady = value;
        }

        public TimeSpan RoundtripTime => innerGamer.RoundtripTime;

        public object Tag
        {
            get => innerGamer.Tag;
            set => innerGamer.Tag = value;
        }
    }

    /// <summary>
    /// Adapter for LocalNetworkGamer to implement ILocalNetworkGamer.
    /// </summary>
    internal class LocalNetworkGamerAdapter : NetworkGamerAdapter, ILocalNetworkGamer
    {
        private readonly LocalNetworkGamer innerLocalGamer;

        internal LocalNetworkGamerAdapter(LocalNetworkGamer gamer)
            : base(gamer)
        {
            this.innerLocalGamer = gamer ?? throw new ArgumentNullException(nameof(gamer));
        }

        bool INetworkGamer.IsHost => innerLocalGamer.IsHost;
        bool INetworkGamer.IsReady
        {
            get => innerLocalGamer.IsReady;
            set => innerLocalGamer.IsReady = value;
        }

        bool ILocalNetworkGamer.IsHost
        {
            get => innerLocalGamer.IsHost;
            set
            {
                // IsHost is readonly in NetworkGamer - it's set during construction
                // This setter exists for interface compatibility but cannot modify the value
                if (value != innerLocalGamer.IsHost)
                {
                    throw new InvalidOperationException("Cannot change IsHost after session creation. Host status is determined at session creation time.");
                }
            }
        }

        bool ILocalNetworkGamer.IsReady
        {
            get => innerLocalGamer.IsReady;
            set => innerLocalGamer.IsReady = value;
        }
    }
}
