using System;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Represents a network session for multiplayer gaming.
    /// </summary>
    public class NetworkSession : IDisposable, IAsyncDisposable
    {
        // Event for received messages
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        private readonly List<NetworkGamer> gamers;
        private readonly GamerCollection gamerCollection;
        private readonly NetworkSessionType sessionType;
        private readonly int maxGamers;
        private readonly int privateGamerSlots;
        private readonly Dictionary<string, IPEndPoint> gamerEndpoints;
        private readonly object lockObject = new object();

        private INetworkTransport networkTransport;
        internal NetworkSessionState sessionState;
        private bool disposed;
        private bool isHost;
        internal string sessionId;
        private Task receiveTask;
        private CancellationTokenSource cancellationTokenSource;

        // Events
        public event EventHandler<GameStartedEventArgs> GameStarted;
        public event EventHandler<GameEndedEventArgs> GameEnded;

        private event EventHandler<GamerJoinedEventArgs> gamerJoined;
        private bool isGamerJoinedSubscribed = false;
        public event EventHandler<GamerJoinedEventArgs> GamerJoined
        {
            add
            {
                lock (lockObject)
                {
                    gamerJoined += value;
                    isGamerJoinedSubscribed = true;

                    // Notify pending gamers if this is the first subscription
                    NotifyPendingGamers();
                }
            }
            remove
            {
                lock (lockObject)
                {
                    gamerJoined -= value;
                    isGamerJoinedSubscribed = gamerJoined != null;
                }
            }
        }

        public event EventHandler<GamerLeftEventArgs> GamerLeft;
        public event EventHandler<NetworkSessionEndedEventArgs> SessionEnded;

        // Static event for invite acceptance
        public static event EventHandler<InviteAcceptedEventArgs> InviteAccepted;

        /// <summary>
        /// Allows changing the network transport implementation.
        /// </summary>
        public INetworkTransport NetworkTransport
        {
            get => networkTransport;
            set => networkTransport = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Initializes a new NetworkSession.
        /// </summary>
        private NetworkSession(NetworkSessionType sessionType, int maxGamers, int privateGamerSlots, bool isHost)
        {
            // Register message types (can be moved to static constructor)
            NetworkMessageRegistry.Register<PlayerMoveMessage>(1);

            this.sessionType = sessionType;
            this.maxGamers = maxGamers;
            this.privateGamerSlots = privateGamerSlots;
            this.isHost = isHost;
            this.sessionId = Guid.NewGuid().ToString();
            this.sessionState = NetworkSessionState.Creating;

            gamers = new List<NetworkGamer>();
            gamerCollection = new GamerCollection(gamers);
            gamerEndpoints = new Dictionary<string, IPEndPoint>();

            networkTransport = new UdpTransport();
            cancellationTokenSource = new CancellationTokenSource();

            // Add local gamer
            var gamerGuid = Guid.NewGuid().ToString();
            var localGamer = new LocalNetworkGamer(this, gamerGuid, isHost, $"{SignedInGamer.Current?.Gamertag ?? "Player"}_{gamerGuid.Substring(0, 8)}");
            NetworkGamer.LocalGamer = localGamer;
            AddGamer(localGamer);

            // Start receive loop for SystemLink sessions
            if (sessionType == NetworkSessionType.SystemLink)
            {
                receiveTask = Task.Run(() => ReceiveLoopAsync(cancellationTokenSource.Token));
            }
        }

        // Internal constructor for SystemLink join
        internal NetworkSession(NetworkSessionType sessionType, int maxGamers, int privateGamerSlots, bool isHost, string sessionId)
            : this(sessionType, maxGamers, privateGamerSlots, isHost)
        {
            this.sessionId = sessionId;
            this.sessionState = NetworkSessionState.Lobby;
        }

        /// <summary>
        /// Gets all gamers in the session.
        /// </summary>
        public GamerCollection AllGamers => gamerCollection;

        /// <summary>
        /// Gets local gamers in the session.
        /// </summary>
        public LocalGamerCollection LocalGamers
        {
            get
            {
                var localGamers = gamers.Where(g => g.IsLocal).OfType<LocalNetworkGamer>().ToList();
                return new LocalGamerCollection(localGamers);
            }
        }

        /// <summary>
        /// Gets remote gamers in the session.
        /// </summary>
        public GamerCollection RemoteGamers
        {
            get
            {
                var remoteGamers = gamers.Where(g => !g.IsLocal).ToList();
                return new GamerCollection(remoteGamers);
            }
        }

        /// <summary>
        /// Gets the host gamer.
        /// </summary>
        public NetworkGamer Host => AllGamers.Host;

        /// <summary>
        /// Gets whether this machine is the host.
        /// </summary>
        public bool IsHost => isHost;

        /// <summary>
        /// Gets whether everyone is ready to start the game.
        /// </summary>
        public bool IsEveryoneReady => AllGamers.All(g => g.IsReady);

        /// <summary>
        /// Gets the maximum number of gamers.
        /// </summary>
        public int MaxGamers => maxGamers;

        /// <summary>
        /// Gets the number of private gamer slots.
        /// </summary>
        public int PrivateGamerSlots => privateGamerSlots;

        /// <summary>
        /// Gets the session type.
        /// </summary>
        public NetworkSessionType SessionType => sessionType;

        /// <summary>
        /// Gets the current session state.
        /// </summary>
        public NetworkSessionState SessionState => sessionState;

        /// <summary>
        /// Gets the bytes per second being sent.
        /// </summary>
        public int BytesPerSecondSent => 0; // Mock implementation

        /// <summary>
        /// Gets the bytes per second being received.
        /// </summary>
        public int BytesPerSecondReceived => 0; // Mock implementation

        /// <summary>
        /// Gets whether the session allows host migration.
        /// </summary>
        public bool AllowHostMigration { get; set; } = true;

        /// <summary>
        /// Gets whether the session allows gamers to join during gameplay.
        /// </summary>
        public bool AllowJoinInProgress { get; set; } = true;

        private IDictionary<string, object> sessionProperties = new Dictionary<string, object>();
        /// <summary>
        /// Gets or sets custom session properties.
        /// </summary>
        public IDictionary<string, object> SessionProperties
        {
            get => sessionProperties;
            set
            {
                sessionProperties = value ?? throw new ArgumentNullException(nameof(value));

                // Automatically broadcast changes if this machine is the host
                if (IsHost)
                {
                    BroadcastSessionProperties();
                }
            }
        }

        // Simulation properties for testing network conditions
        private TimeSpan simulatedLatency = TimeSpan.Zero;
        private float simulatedPacketLoss = 0.0f;

        /// <summary>
        /// Gets or sets the simulated network latency for testing purposes.
        /// </summary>
        public TimeSpan SimulatedLatency
        {
            get => simulatedLatency;
            set => simulatedLatency = value;
        }

        /// <summary>
        /// Gets or sets the simulated packet loss percentage for testing purposes.
        /// </summary>
        public float SimulatedPacketLoss
        {
            get => simulatedPacketLoss;
            set => simulatedPacketLoss = Math.Max(0.0f, Math.Min(1.0f, value));
        }

        /// <summary>
        /// Cancels all ongoing async operations for this session.
        /// </summary>
        public void Cancel()
        {
            cancellationTokenSource?.Cancel();
        }

        /// <summary>
        /// Asynchronously creates a new network session.
        /// </summary>
        public static async Task<NetworkSession> CreateAsync(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateGamerSlots, IDictionary<string, object> sessionProperties, CancellationToken cancellationToken = default)
        {
            if (maxLocalGamers < 1 || maxLocalGamers > 4)
                throw new ArgumentOutOfRangeException(nameof(maxLocalGamers));
            if (privateGamerSlots < 0 || privateGamerSlots > maxGamers)
                throw new ArgumentOutOfRangeException(nameof(privateGamerSlots));

            NetworkSession session = null;
            switch (sessionType)
            {
                case NetworkSessionType.Local:
                    // Local session: in-memory only
                    await Task.Delay(5, cancellationToken);
                    session = new NetworkSession(sessionType, maxGamers, privateGamerSlots, true);
                    session.sessionState = NetworkSessionState.Lobby;
                    // Register in local session list for FindAsync
                    LocalSessionRegistry.RegisterSession(session);
                    break;
                case NetworkSessionType.SystemLink:
                    // SystemLink: start UDP listener and broadcast session
                    session = new NetworkSession(sessionType, maxGamers, privateGamerSlots, true);
                    session.sessionState = NetworkSessionState.Lobby;
                    _ = SystemLinkSessionManager.AdvertiseSessionAsync(session, cancellationToken); // Fire-and-forget
                    break;
                default:
                    // Not implemented
                    throw new NotSupportedException($"SessionType {sessionType} not supported yet.");
            }
            return session;
        }

        /// <summary>
        /// Synchronous wrapper for CreateAsync (for XNA compatibility).
        /// </summary>
        public static NetworkSession Create(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers, int privateGamerSlots, IDictionary<string, object> sessionProperties)
        {
            return CreateAsync(sessionType, maxLocalGamers, maxGamers, privateGamerSlots, sessionProperties).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously finds available network sessions.
        /// </summary>
        public static async Task<AvailableNetworkSessionCollection> FindAsync(NetworkSessionType sessionType, int maxLocalGamers, IDictionary<string, object> sessionProperties, CancellationToken cancellationToken = default)
        {
            switch (sessionType)
            {
                case NetworkSessionType.Local:
                    await Task.Delay(5, cancellationToken);
                    // Return sessions in local registry
                    var localSessions = LocalSessionRegistry.FindSessions(maxLocalGamers).ToList();
                    return new AvailableNetworkSessionCollection(localSessions);
                case NetworkSessionType.SystemLink:
                    // Discover sessions via UDP broadcast
                    var systemLinkSessions = (await SystemLinkSessionManager.DiscoverSessionsAsync(maxLocalGamers, cancellationToken)).ToList();
                    return new AvailableNetworkSessionCollection(systemLinkSessions);
                default:
                    throw new NotSupportedException($"SessionType {sessionType} not supported yet.");
            }
        }

        /// <summary>
        /// Synchronous wrapper for FindAsync (for XNA compatibility).
        /// </summary>
        public static AvailableNetworkSessionCollection Find(NetworkSessionType sessionType, int maxLocalGamers, IDictionary<string, object> sessionProperties)
        {
            return FindAsync(sessionType, maxLocalGamers, sessionProperties).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously joins an available network session.
        /// </summary>
        public static async Task<NetworkSession> JoinAsync(AvailableNetworkSession availableSession, CancellationToken cancellationToken = default)
        {
            switch (availableSession.SessionType)
            {
                case NetworkSessionType.Local:
                    // Attach to local session
                    var localSession = LocalSessionRegistry.GetSessionById(availableSession.SessionId);
                    if (localSession == null)
                        throw new NetworkSessionJoinException(NetworkSessionJoinError.SessionNotFound);
                    // Add local gamer
                    var newGamer = new LocalNetworkGamer(localSession, Guid.NewGuid().ToString(), false, SignedInGamer.Current?.Gamertag ?? "Player");
                    localSession.AddGamer(newGamer);
                    return localSession;
                case NetworkSessionType.SystemLink:
                    // Connect to host via network
                    var joinedSession = await SystemLinkSessionManager.JoinSessionAsync(availableSession, cancellationToken);
                    return joinedSession;
                default:
                    throw new NotSupportedException($"SessionType {availableSession.SessionType} not supported yet.");
            }
        }

        public static Task<NetworkSession> JoinInvitedAsync(IEnumerable<SignedInGamer> localGamers, object state = null, CancellationToken cancellationToken = default)
        {
            if (localGamers == null || !localGamers.Any())
                throw new ArgumentException("At least one local gamer must be provided.", nameof(localGamers));

            // Simulate invite acceptance logic
            var inviteAcceptedEventArgs = new InviteAcceptedEventArgs(localGamers.First(), true);
            InviteAccepted?.Invoke(null, inviteAcceptedEventArgs);

            if (!inviteAcceptedEventArgs.IsSignedInGamer)
                throw new InvalidOperationException("The gamer is not signed in.");

            // Simulate finding the session associated with the invite
            var availableSession = new AvailableNetworkSession(
                sessionName: "InvitedSession",
                hostGamertag: "HostPlayer",
                currentGamerCount: 1,
                openPublicGamerSlots: 3,
                openPrivateGamerSlots: 1,
                sessionType: NetworkSessionType.PlayerMatch,
                sessionProperties: new Dictionary<string, object>(),
                sessionId: Guid.NewGuid().ToString()
            );

            // Join the session
            var joinedSession = SystemLinkSessionManager.JoinSessionAsync(availableSession, cancellationToken);
            return joinedSession;
        }

        /// <summary>
        /// Creates a new network session synchronously with default properties.
        /// </summary>
        public static NetworkSession Create(NetworkSessionType sessionType, int maxLocalGamers, int maxGamers)
        {
            return CreateAsync(sessionType, maxLocalGamers, maxGamers, 0, new Dictionary<string, object>()).GetAwaiter().GetResult();
        }

        /// <summary>

        /// <summary>
        /// Finds available network sessions synchronously with default properties.
        /// </summary>
        public static AvailableNetworkSessionCollection Find(NetworkSessionType sessionType, int maxLocalGamers)
        {
            return FindAsync(sessionType, maxLocalGamers, new Dictionary<string, object>()).GetAwaiter().GetResult();
        }

        /// <summary>

        /// <summary>
        /// Joins an available network session synchronously.
        /// </summary>
        public static NetworkSession Join(AvailableNetworkSession availableSession)
        {
            return JoinAsync(availableSession).GetAwaiter().GetResult();
        }

        public static NetworkSession JoinInvited(IEnumerable<SignedInGamer> localGamers, object state = null)
        {
            return JoinInvitedAsync(localGamers, state).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the network session.
        /// </summary>
        public void Update()
        {
            if (disposed)
                return;

            // Process any pending network messages
            ProcessIncomingMessages();
        }

        /// <summary>
        /// Sends data to all gamers in the session.
        /// </summary>
        public void SendToAll(PacketWriter writer, SendDataOptions options)
        {
            SendToAll(writer, options, NetworkGamer.LocalGamer);
        }

        /// <summary>
        /// Sends data to all gamers except the specified sender.
        /// </summary>
        public void SendToAll(PacketWriter writer, SendDataOptions options, NetworkGamer sender)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            byte[] data = writer.GetData();

            lock (lockObject)
            {
                foreach (var gamer in gamers)
                {
                    if (gamer != sender && !gamer.IsLocal)
                    {
                        SendDataToGamer(gamer, data, options);
                    }
                }
            }
        }

        /// <summary>
        /// Starts the game.
        /// </summary>
        public void StartGame()
        {
            if (sessionState == NetworkSessionState.Lobby)
            {
                sessionState = NetworkSessionState.Playing;
                OnGameStarted();
            }
        }

        /// <summary>
        /// Ends the game and returns to lobby.
        /// </summary>
        public void EndGame()
        {
            if (sessionState == NetworkSessionState.Playing)
            {
                sessionState = NetworkSessionState.Lobby;
                OnGameEnded();
            }
        }

        /// <summary>
        /// Notifies when a gamer's readiness changes.
        /// </summary>
        internal void NotifyReadinessChanged(NetworkGamer gamer)
        {
            // Send readiness update to other players
            if (IsHost)
            {
                var writer = new PacketWriter();
                writer.Write("ReadinessUpdate");
                writer.Write(gamer.Id);
                writer.Write(gamer.IsReady);
                SendToAll(writer, SendDataOptions.Reliable, gamer);
            }
        }

        /// <summary>
        /// Sends data to a specific gamer.
        /// </summary>
        internal void SendDataToGamer(NetworkGamer gamer, PacketWriter writer, SendDataOptions options)
        {
            SendDataToGamer(gamer, writer.GetData(), options);
        }

        internal void SendDataToGamer(NetworkGamer gamer, byte[] data, SendDataOptions options)
        {
            if (gamerEndpoints.TryGetValue(gamer.Id, out IPEndPoint endpoint))
            {
                try
                {
                    networkTransport.Send(data, endpoint);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to send data to {gamer.Gamertag}: {ex.Message}");
                }
            }
        }

        public void AddLocalGamer(SignedInGamer gamer)
        {
            throw new NotImplementedException();
        }


        private void AddGamer(NetworkGamer gamer)
        {
            lock (lockObject)
            {
                gamers.Add(gamer);
            }
            OnGamerJoined(gamer);
        }

        private void RemoveGamer(NetworkGamer gamer)
        {
            lock (lockObject)
            {
                gamers.Remove(gamer);
                gamerEndpoints.Remove(gamer.Id);
            }
            OnGamerLeft(gamer);
        }

        // Modern async receive loop for SystemLink
        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Ensure the network transport is bound before receiving data
                    if (!networkTransport.IsBound)
                    {
                        networkTransport.Bind();
                    }

                    var (data, senderEndpoint) = await networkTransport.ReceiveAsync();
                    if (data.Length > 1)
                    {
                        NetworkGamer senderGamer = null;
                        lock (lockObject)
                        {
                            senderGamer = gamers.FirstOrDefault(g => gamerEndpoints.TryGetValue(g.Id, out var ep) && ep.Equals(senderEndpoint));
                        }

                        if (senderGamer != null)
                        {
                            senderGamer.EnqueueIncomingPacket(data, senderGamer);
                        }

                        var typeId = data[0];
                        var reader = new PacketReader(data); // Use only the byte array
                        var message = NetworkMessageRegistry.CreateMessage(typeId);
                        message?.Deserialize(reader);
                        OnMessageReceived(new MessageReceivedEventArgs(message, senderEndpoint));
                    }
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ReceiveLoop error: {ex.Message}");
                }
            }
        }

        private void OnMessageReceived(MessageReceivedEventArgs e)
        {
            // Raise the MessageReceived event
            var handler = MessageReceived;
            if (handler != null)
                handler(this, e);
        }

        private void OnGameStarted()
        {
            GameStarted?.Invoke(this, new GameStartedEventArgs());
        }

        private void OnGameEnded()
        {
            GameEnded?.Invoke(this, new GameEndedEventArgs());
        }

        private void OnGamerJoined(NetworkGamer gamer)
        {
            gamerJoined?.Invoke(this, new GamerJoinedEventArgs(gamer));
        }

        private void OnGamerLeft(NetworkGamer gamer)
        {
            GamerLeft?.Invoke(this, new GamerLeftEventArgs(gamer));
        }

        private void OnSessionEnded(NetworkSessionEndReason reason)
        {
            sessionState = NetworkSessionState.Ended;
            SessionEnded?.Invoke(this, new NetworkSessionEndedEventArgs(reason));
        }

        /// <summary>
        /// Disposes the network session.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                cancellationTokenSource?.Cancel();
                receiveTask?.Wait(1000); // Wait up to 1 second
                networkTransport?.Dispose();
                cancellationTokenSource?.Dispose();
                OnSessionEnded(NetworkSessionEndReason.ClientSignedOut);
                disposed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!disposed)
            {
                cancellationTokenSource?.Cancel();
                if (receiveTask != null)
                    await receiveTask;
                if (networkTransport is IAsyncDisposable asyncTransport)
                    await asyncTransport.DisposeAsync();
                else
                    networkTransport?.Dispose();
                cancellationTokenSource?.Dispose();
                OnSessionEnded(NetworkSessionEndReason.ClientSignedOut);
                disposed = true;
            }
        }

        internal byte[] SerializeSessionPropertiesBinary()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(SessionProperties.Count);
                foreach (var kvp in SessionProperties)
                {
                    writer.Write(kvp.Key);
                    // Write type info and value
                    if (kvp.Value is int i)
                    {
                        writer.Write((byte)1); // type marker
                        writer.Write(i);
                    }
                    else if (kvp.Value is bool b)
                    {
                        writer.Write((byte)2);
                        writer.Write(b);
                    }
                    else if (kvp.Value is string s)
                    {
                        writer.Write((byte)3);
                        writer.Write(s ?? "");
                    }
                    // Add more types as needed
                    else
                    {
                        writer.Write((byte)255); // unknown type
                        writer.Write(kvp.Value?.ToString() ?? "");
                    }
                }
                return ms.ToArray();
            }
        }

        internal void DeserializeSessionPropertiesBinary(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                SessionProperties.Clear();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    string key = reader.ReadString();
                    byte type = reader.ReadByte();
                    object value = null;
                    switch (type)
                    {
                        case 1: value = reader.ReadInt32(); break;
                        case 2: value = reader.ReadBoolean(); break;
                        case 3: value = reader.ReadString(); break;
                        default: value = reader.ReadString(); break;
                    }
                    SessionProperties[key] = value;
                }
            }
        }

        private void BroadcastSessionProperties()
        {
            var writer = new PacketWriter();
            writer.Write("SessionPropertiesUpdate");
            writer.Write(SerializeSessionPropertiesBinary());
            SendToAll(writer, SendDataOptions.Reliable);
        }

        private void ProcessIncomingMessages()
        {
            foreach (var gamer in gamers)
            {
                while (gamer.IsDataAvailable)
                {
                    var reader = new PacketReader();
                    gamer.ReceiveData(reader, out var sender);

                    var messageType = reader.ReadString();
                    if (messageType == "SessionPropertiesUpdate")
                    {
                        var propertiesData = reader.ReadBytes();
                        DeserializeSessionPropertiesBinary(propertiesData);
                    }
                }
            }
        }
        private void NotifyPendingGamers()
        {
            if (isGamerJoinedSubscribed && sessionState == NetworkSessionState.Lobby)
            {
                // Notify all pending gamers that they can join
                foreach (var gamer in gamers/*.Where(g => !g.IsLocal && !g.IsReady)*/)
                {
                    gamerJoined?.Invoke(this, new GamerJoinedEventArgs(gamer));
                }
			}
		}
    }
}