using System;
using Microsoft.Xna.Framework;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Types of network sessions that can be created or joined.
    /// </summary>
    public enum NetworkSessionType
    {
        /// <summary>
        /// Local session for single-machine multiplayer.
        /// </summary>
        Local,

        /// <summary>
        /// System link session for LAN multiplayer.
        /// </summary>
        SystemLink,

        /// <summary>
        /// Player match session for online multiplayer.
        /// </summary>
        PlayerMatch,

        /// <summary>
        /// Ranked session for competitive online multiplayer.
        /// </summary>
        Ranked
    }

    /// <summary>
    /// Options for sending data over the network.
    /// </summary>
    public enum SendDataOptions
    {
        /// <summary>
        /// No special options - unreliable delivery.
        /// </summary>
        None,

        /// <summary>
        /// Reliable delivery - guarantees the data will arrive.
        /// </summary>
        Reliable,

        /// <summary>
        /// In-order delivery - data arrives in the order sent.
        /// </summary>
        InOrder,

        /// <summary>
        /// Reliable and in-order delivery.
        /// </summary>
        ReliableInOrder = Reliable | InOrder
    }

    /// <summary>
    /// Current state of a network session.
    /// </summary>
    public enum NetworkSessionState
    {
        /// <summary>
        /// Session is being created.
        /// </summary>
        Creating,

        /// <summary>
        /// Session is in the lobby, waiting for players.
        /// </summary>
        Lobby,

        /// <summary>
        /// Session is in an active game.
        /// </summary>
        Playing,

        /// <summary>
        /// Session has ended.
        /// </summary>
        Ended
    }

    /// <summary>
    /// Defines the different types of gamers in a networked session.
    /// </summary>
    public enum GamerType
    {
        /// <summary>
        /// A local gamer.
        /// </summary>
        Local,

        /// <summary>
        /// A remote gamer.
        /// </summary>
        Remote
    }
}

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Exception thrown when network operations fail.
    /// </summary>
    public class NetworkException : Exception
    {
        public NetworkException() : base() { }
        public NetworkException(string message) : base(message) { }
        public NetworkException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when gamer privilege operations fail.
    /// </summary>
    public class GamerPrivilegeException : Exception
    {
        public GamerPrivilegeException() : base() { }
        public GamerPrivilegeException(string message) : base(message) { }
        public GamerPrivilegeException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown when network session join operations fail.
    /// </summary>
    public class NetworkSessionJoinException : Exception
    {
        /// <summary>
        /// Gets the join error type.
        /// </summary>
        public NetworkSessionJoinError JoinError { get; }

        public NetworkSessionJoinException() : base() 
        {
            JoinError = NetworkSessionJoinError.Unknown;
        }

        public NetworkSessionJoinException(string message) : base(message) 
        {
            JoinError = NetworkSessionJoinError.Unknown;
        }

        public NetworkSessionJoinException(string message, Exception innerException) : base(message, innerException) 
        {
            JoinError = NetworkSessionJoinError.Unknown;
        }

        public NetworkSessionJoinException(NetworkSessionJoinError joinError) : base() 
        {
            JoinError = joinError;
        }

        public NetworkSessionJoinException(string message, NetworkSessionJoinError joinError) : base(message) 
        {
            JoinError = joinError;
        }
    }

    /// <summary>
    /// Types of network session join errors.
    /// </summary>
    public enum NetworkSessionJoinError
    {
        /// <summary>
        /// Unknown error.
        /// </summary>
        Unknown,

        /// <summary>
        /// Session is full.
        /// </summary>
        SessionFull,

        /// <summary>
        /// Session not found.
        /// </summary>
        SessionNotFound,

        /// <summary>
        /// Session not joinable.
        /// </summary>
        SessionNotJoinable
    }
}
