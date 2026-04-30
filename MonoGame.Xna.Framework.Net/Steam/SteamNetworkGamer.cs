using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Steam backend gamer model used by the Steam vertical-slice session.
    /// </summary>
    public class SteamNetworkGamer : INetworkGamer
    {
        private bool isReady;

        internal SteamNetworkGamer(string id, string gamertag, bool isLocal, bool isHost)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Gamertag = string.IsNullOrWhiteSpace(gamertag) ? "SteamPlayer" : gamertag;
            IsLocal = isLocal;
            IsHost = isHost;
        }

        public string Id { get; }

        public string Gamertag { get; }

        public bool IsLocal { get; }

        public bool IsHost { get; protected set; }

        public bool IsReady
        {
            get => isReady;
            set => isReady = value;
        }

        public TimeSpan RoundtripTime => TimeSpan.FromMilliseconds(35);

        public object Tag { get; set; }
    }

    /// <summary>
    /// Steam backend local gamer implementation.
    /// </summary>
    public sealed class SteamLocalNetworkGamer : SteamNetworkGamer, ILocalNetworkGamer
    {
        internal SteamLocalNetworkGamer(string id, string gamertag, bool isHost)
            : base(id, gamertag, isLocal: true, isHost: isHost)
        {
        }

        bool ILocalNetworkGamer.IsHost
        {
            get => IsHost;
            set
            {
                if (value != IsHost)
                {
                    throw new InvalidOperationException("Cannot change IsHost after session creation.");
                }
            }
        }

        bool ILocalNetworkGamer.IsReady
        {
            get => IsReady;
            set => IsReady = value;
        }
    }
}
