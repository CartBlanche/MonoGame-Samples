using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Represents a player in a network session.
    /// This interface abstracts the player representation to support multiple networking backends.
    /// </summary>
    public interface INetworkGamer
    {
        /// <summary>
        /// Gets the unique identifier for this gamer.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the gamertag (display name) for this gamer.
        /// </summary>
        string Gamertag { get; }

        /// <summary>
        /// Gets whether this gamer is the local player.
        /// </summary>
        bool IsLocal { get; }

        /// <summary>
        /// Gets whether this gamer is the host of the session.
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// Gets or sets whether this gamer is ready to start the game.
        /// </summary>
        bool IsReady { get; set; }

        /// <summary>
        /// Gets the approximate roundtrip time (latency) to this gamer.
        /// </summary>
        TimeSpan RoundtripTime { get; }

        /// <summary>
        /// Gets or sets arbitrary data associated with this gamer.
        /// </summary>
        object Tag { get; set; }
    }

    /// <summary>
    /// Represents the local player in a network session.
    /// Extends INetworkGamer with local-specific functionality.
    /// </summary>
    public interface ILocalNetworkGamer : INetworkGamer
    {
        /// <summary>
        /// Gets or sets whether this local gamer is the host (can be modified).
        /// </summary>
        new bool IsHost { get; set; }

        /// <summary>
        /// Gets or sets whether this local gamer is ready (can be modified).
        /// </summary>
        new bool IsReady { get; set; }
    }
}
