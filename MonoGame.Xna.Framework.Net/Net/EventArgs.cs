using Microsoft.Xna.Framework.GamerServices;
using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Event arguments for when an invite is accepted.
    /// </summary>
    public class InviteAcceptedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the gamer who accepted the invite.
        /// </summary>
        public SignedInGamer Gamer { get; internal set; }

        /// <summary>
        /// Gets whether the operation completed successfully.
        /// </summary>
        public bool IsSignedInGamer { get; internal set; }

        internal InviteAcceptedEventArgs(SignedInGamer gamer, bool isSignedInGamer)
        {
            Gamer = gamer;
            IsSignedInGamer = isSignedInGamer;
        }
    }

    /// <summary>
    /// Event arguments for when a game ends.
    /// </summary>
    public class GameEndedEventArgs : EventArgs
    {
        internal GameEndedEventArgs() { }
    }

    /// <summary>
    /// Event arguments for when a network session ends.
    /// </summary>
    public class NetworkSessionEndedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the reason the session ended.
        /// </summary>
        public NetworkSessionEndReason EndReason { get; internal set; }

        internal NetworkSessionEndedEventArgs(NetworkSessionEndReason endReason)
        {
            EndReason = endReason;
        }
    }

    /// <summary>
    /// Event arguments for when a gamer leaves the session.
    /// </summary>
    public class GamerLeftEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the gamer who left.
        /// </summary>
        public NetworkGamer Gamer { get; internal set; }

        internal GamerLeftEventArgs(NetworkGamer gamer)
        {
            Gamer = gamer;
        }
    }

    /// <summary>
    /// Event arguments for when a game starts.
    /// </summary>
    public class GameStartedEventArgs : EventArgs
    {
        internal GameStartedEventArgs() { }
    }

    /// <summary>
    /// Event arguments for when a gamer joins the session.
    /// </summary>
    public class GamerJoinedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the gamer who joined.
        /// </summary>
        public NetworkGamer Gamer { get; internal set; }

        internal GamerJoinedEventArgs(NetworkGamer gamer)
        {
            Gamer = gamer;
        }
    }

    /// <summary>
    /// Reasons why a network session ended.
    /// </summary>
    public enum NetworkSessionEndReason
    {
        /// <summary>
        /// The session ended normally.
        /// </summary>
        ClientSignedOut,

        /// <summary>
        /// The host ended the session.
        /// </summary>
        HostEndedSession,

		/// <summary>
		/// The host removed the user.
		/// </summary>
		RemovedByHost,

		/// <summary>
		/// The session was disconnected.
		/// </summary>
		Disconnected,

        /// <summary>
        /// A network error occurred.
        /// </summary>
        NetworkError
    }
}
