using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Abstraction for a multiplayer network session.
    /// Supports different networking backends (UDP, Steam, etc.) through dependency injection.
    /// </summary>
    public interface INetworkSession : IDisposable
    {
        /// <summary>
        /// Gets all gamers in the session (local and remote).
        /// </summary>
        IReadOnlyList<INetworkGamer> AllGamers { get; }

        /// <summary>
        /// Gets the local player.
        /// </summary>
        ILocalNetworkGamer LocalGamer { get; }

        /// <summary>
        /// Gets the current session state.
        /// </summary>
        NetworkSessionState State { get; }

        /// <summary>
        /// Gets the unique session identifier.
        /// </summary>
        string SessionId { get; }

        /// <summary>
        /// Occurs when a network message is received from any remote gamer.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        /// Occurs when a remote gamer joins the session.
        /// </summary>
        event EventHandler<GamerJoinedEventArgs> GamerJoined;

        /// <summary>
        /// Occurs when a remote gamer leaves the session.
        /// </summary>
        event EventHandler<GamerLeftEventArgs> GamerLeft;

        /// <summary>
        /// Occurs when the game is ready to start (all players ready).
        /// </summary>
        event EventHandler<GameStartedEventArgs> GameStarted;

        /// <summary>
        /// Occurs when the game has ended.
        /// </summary>
        event EventHandler<GameEndedEventArgs> GameEnded;

        /// <summary>
        /// Occurs when the session ends.
        /// </summary>
        event EventHandler<NetworkSessionEndedEventArgs> SessionEnded;

        /// <summary>
        /// Creates a new session as the host.
        /// </summary>
        /// <param name="sessionType">The type of session (SystemLink, etc.)</param>
        /// <param name="maxGamers">Maximum number of gamers in the session.</param>
        /// <param name="privateGamerSlots">Number of private (invitation-only) slots.</param>
        Task CreateAsync(NetworkSessionType sessionType, int maxGamers, int privateGamerSlots);

        /// <summary>
        /// Joins an existing session.
        /// </summary>
        /// <param name="hostAddress">Address or identifier of the session to join.</param>
        Task JoinAsync(string hostAddress);

        /// <summary>
        /// Sends a network message to a specific gamer.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="recipient">The gamer to send to.</param>
        void SendMessage(INetworkMessage message, INetworkGamer recipient);

        /// <summary>
        /// Broadcasts a message to all remote gamers.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
        void BroadcastMessage(INetworkMessage message);

        /// <summary>
        /// Updates the session (processes incoming messages, etc.).
        /// Should be called once per game frame.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        void Update(GameTime gameTime);

        /// <summary>
        /// Closes the session and disconnects all players.
        /// </summary>
        Task CloseAsync();
    }
}
