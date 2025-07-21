using System;

namespace Microsoft.Xna.Framework.Net
{

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
