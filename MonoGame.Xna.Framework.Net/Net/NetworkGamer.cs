using Microsoft.Xna.Framework.GamerServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Represents a player in a network session.
    /// </summary>
    public class NetworkGamer
    {
        private static NetworkGamer localGamer;
        private readonly NetworkSession session;
        private readonly string id;
        private readonly bool isLocal;
        private readonly bool isHost;
        private string gamertag;
        private bool isReady;
        private object tag;

        // Network performance properties
        private TimeSpan roundtripTime = TimeSpan.Zero;

        /// <summary>
        /// Gets the unique identifier for this gamer.
        /// </summary>
        public string Id => id;

        /// <summary>
        /// Gets whether this gamer is the local player.
        /// </summary>
        public bool IsLocal => isLocal;

        /// <summary>
        /// Gets whether this gamer is the host of the session.
        /// </summary>
        public bool IsHost => isHost;

        /// <summary>
        /// Gets or sets the gamertag for this gamer.
        /// </summary>
        public string Gamertag
        {
            get => gamertag;
            set => gamertag = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets or sets whether this gamer is ready to start the game.
        /// </summary>
        public bool IsReady
        {
            get => isReady;
            set
            {
                if (isReady != value)
                {
                    isReady = value;
                    // Notify session of readiness change if this is the local gamer
                    if (isLocal)
                    {
                        session?.NotifyReadinessChanged(this);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets custom data associated with this gamer.
        /// </summary>
        public object Tag
        {
            get => tag;
            set => tag = value;
        }

        /// <summary>
        /// Gets the machine for this network gamer.
        /// </summary>
        public NetworkMachine Machine => null; // Mock implementation

        /// <summary>
        /// Gets whether data is available to be received from this gamer.
        /// </summary>
        public bool IsDataAvailable => incomingPackets.Count > 0;

        /// <summary>
        /// Gets the SignedInGamer associated with this NetworkGamer.
        /// </summary>
        public SignedInGamer SignedInGamer => SignedInGamer.Current;

        /// <summary>
        /// Gets whether this gamer is muted by the local user.
        /// </summary>
        public bool IsMutedByLocalUser => false; // Mock implementation

        /// <summary>
        /// Gets whether this gamer is currently talking.
        /// </summary>
        public bool IsTalking => false; // Mock implementation

        /// <summary>
        /// Gets whether this gamer has voice capabilities.
        /// </summary>
        public bool HasVoice => false; // Mock implementation

        // Add to NetworkGamer class
        private readonly Queue<(byte[] Data, NetworkGamer Sender)> incomingPackets = new Queue<(byte[], NetworkGamer)>();

        internal NetworkGamer(NetworkSession session, string id, bool isLocal, bool isHost, string gamertag)
        {
            this.session = session;
            this.id = id;
            this.isLocal = isLocal;
            this.isHost = isHost;
            this.gamertag = gamertag;
        }

        /// <summary>
        /// Receives data from this gamer.
        /// </summary>
        /// <param name="data">Array to receive the data into.</param>
        /// <param name="sender">The gamer who sent the data.</param>
        /// <returns>The number of bytes received.</returns>
        public int ReceiveData(byte[] data, out NetworkGamer sender)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            sender = null;

            // Check if data is available
            if (!IsDataAvailable)
                return 0;

            var packet = incomingPackets.Dequeue();
            sender = packet.Sender;
            int length = Math.Min(data.Length, packet.Data.Length);
            Array.Copy(packet.Data, data, length);
            return length;
        }

        /// <summary>
        /// Receives data from this gamer using PacketReader.
        /// </summary>
        /// <param name="data">Array to receive the data into.</param>
        /// <param name="sender">The gamer who sent the data.</param>
        /// <returns>The number of bytes received.</returns>
        public int ReceiveData(out PacketReader reader, out NetworkGamer sender)
        {
            // Ensure the session is valid
            if (session == null)
                throw new InvalidOperationException("Network session is not initialized.");

            reader = null;
            sender = null;

            // Check if data is available
            if (!IsDataAvailable)
                return 0;

            var packet = incomingPackets.Dequeue();
            sender = packet.Sender;
            reader = new PacketReader(packet.Data);
            return packet.Data.Length;
        }

        /// <summary>
        /// Sends data to other gamers in the session.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="options">Send options.</param>
        public void SendData(byte[] data, SendDataOptions options)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (!Enum.IsDefined(typeof(SendDataOptions), options))
                throw new ArgumentOutOfRangeException(nameof(options), "Invalid send data option.");

            SendDataInternal(data, options, session.AllGamers);
        }

        /// <summary>
        /// Sends data to specific gamers in the session.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="options">Send options.</param>
        /// <param name="recipients">The gamers to send to.</param>
        public void SendData(byte[] data, SendDataOptions options, IEnumerable<NetworkGamer> recipients)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (!Enum.IsDefined(typeof(SendDataOptions), options))
                throw new ArgumentOutOfRangeException(nameof(options), "Invalid send data option.");

            SendDataInternal(data, options, recipients);
        }

        /// <summary>
        /// Sends data using PacketWriter.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="options">Send options.</param>
        public void SendData(PacketWriter data, SendDataOptions options)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "PacketWriter cannot be null.");

            if (!Enum.IsDefined(typeof(SendDataOptions), options))
                throw new ArgumentOutOfRangeException(nameof(options), "Invalid send data option.");

            byte[] serializedData = data.GetData();
            SendDataInternal(serializedData, options, session.AllGamers);
        }

        /// <summary>
        /// Sends data using PacketWriter to specific recipients.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="options">Send options.</param>
        /// <param name="recipients">The gamers to send to.</param>
        public void SendData(PacketWriter data, SendDataOptions options, IEnumerable<NetworkGamer> recipients)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "PacketWriter cannot be null.");

            if (recipients == null)
                throw new ArgumentNullException(nameof(recipients));

            byte[] serializedData = data.GetData();
            SendDataInternal(serializedData, options, recipients);
        }

        /// <summary>
        /// Sends data using PacketWriter to a specific recipient.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="options">Send options.</param>
        /// <param name="recipient">The gamer to send to.</param>
        public void SendData(PacketWriter data, SendDataOptions options, NetworkGamer recipient)
        {
            SendData(data, options, new[] { recipient });
        }

        /// <summary>
        /// Sends byte array data to a specific recipient.
        /// </summary>
        /// <param name="data">The data to send.</param>
        /// <param name="options">Send options.</param>
        /// <param name="recipient">The gamer to send to.</param>
        public void SendData(byte[] data, SendDataOptions options, NetworkGamer recipient)
        {
            SendData(data, options, new[] { recipient });
        }

        /// <summary>
        /// Gets the current round-trip time for network communication with this gamer.
        /// </summary>
        public TimeSpan RoundtripTime => roundtripTime;

        /// <summary>
        /// Updates the round-trip time (internal use).
        /// </summary>
        internal void UpdateRoundtripTime(TimeSpan newRoundtripTime)
        {
            roundtripTime = newRoundtripTime;
        }

        public override string ToString()
        {
            return $"{Gamertag} ({(IsLocal ? "Local" : "Remote")}, {(IsHost ? "Host" : "Player")})";
        }

        public override bool Equals(object obj)
        {
            return obj is NetworkGamer other && Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Gets the local gamer.
        /// </summary>
        public static NetworkGamer LocalGamer
        {
            get => localGamer;
            internal set => localGamer = value;
        }

        /// <summary>
        /// Implicit conversion from NetworkGamer to SignedInGamer.
        /// </summary>
        public static implicit operator SignedInGamer(NetworkGamer gamer)
        {
            return gamer?.SignedInGamer;
        }

        private void SendDataInternal(byte[] data, SendDataOptions options, IEnumerable<NetworkGamer> recipients)
        {
            switch (session.SessionType)
            {
                case NetworkSessionType.SystemLink:
                    foreach (var recipient in recipients)
                    {
                        session.SendDataToGamer(recipient, data, options);
                    }
                    break;

                case NetworkSessionType.Local:
                    // TODO: session.ProcessIncomingMessages(); // Simulate message delivery
                    break;

                case NetworkSessionType.PlayerMatch:
                    // Placeholder for future implementation
                    throw new NotImplementedException("PlayerMatch session type is not yet supported.");

                case NetworkSessionType.Ranked:
                    // Placeholder for future implementation
                    throw new NotImplementedException("Ranked session type is not yet supported.");

                default:
                    throw new ArgumentOutOfRangeException(nameof(session.SessionType), $"Unsupported session type: {session.SessionType}");
            }
        }

        internal void EnqueueIncomingPacket(byte[] data, NetworkGamer sender)
        {
            lock (incomingPackets)
            {
                incomingPackets.Enqueue((data, sender));
            }
        }
    }
}