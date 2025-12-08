using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Factory abstraction for creating network sessions.
    /// Allows different networking backends (UDP, Steam, etc.) to be selected at runtime.
    /// </summary>
    public interface INetworkSessionFactory
    {
        /// <summary>
        /// Creates a new network session instance.
        /// </summary>
        /// <returns>A new INetworkSession instance.</returns>
        INetworkSession CreateSession();

        /// <summary>
        /// Finds available sessions for joining.
        /// </summary>
        /// <param name="sessionType">The type of session to search for.</param>
        /// <returns>A collection of available session information.</returns>
        Task<IEnumerable<SessionInfo>> FindSessionsAsync(NetworkSessionType sessionType);

        /// <summary>
        /// Gets the name/identifier of this networking backend.
        /// </summary>
        string BackendName { get; }
    }

    /// <summary>
    /// Information about a discoverable network session.
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Gets or sets the unique identifier for this session.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the address or identifier to use when joining.
        /// </summary>
        public string JoinAddress { get; set; }

        /// <summary>
        /// Gets or sets the name of the session host.
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the current number of players in the session.
        /// </summary>
        public int CurrentPlayerCount { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of players allowed.
        /// </summary>
        public int MaxPlayerCount { get; set; }

        /// <summary>
        /// Gets or sets whether the session requires a password to join.
        /// </summary>
        public bool IsPasswordProtected { get; set; }

        /// <summary>
        /// Gets or sets the session type.
        /// </summary>
        public NetworkSessionType SessionType { get; set; }
    }
}
