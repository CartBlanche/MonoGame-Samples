using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Default factory that creates UDP-based network sessions.
    /// This is the v1.0 implementation. Steam support can be added in v1.5.
    /// </summary>
    public class UdpNetworkSessionFactory : INetworkSessionFactory
    {
        /// <summary>
        /// Gets the name of this networking backend.
        /// </summary>
        public string BackendName => "UDP/SystemLink";

        /// <summary>
        /// Creates a new UDP-based network session.
        /// </summary>
        /// <returns>A new INetworkSession instance configured for UDP.</returns>
        public INetworkSession CreateSession()
        {
            return new UdpNetworkSession();
        }

        /// <summary>
        /// Finds available UDP sessions on the local network.
        /// </summary>
        public async Task<IEnumerable<SessionInfo>> FindSessionsAsync(NetworkSessionType sessionType)
        {
            var sessions = new List<SessionInfo>();

            // Query available sessions from NetworkSession
            try
            {
                var availableSessions = await NetworkSession.FindAsync(
                    sessionType,
                    maxLocalGamers: 1,
                    sessionProperties: null
                );

                // Convert AvailableNetworkSession to SessionInfo
                foreach (var session in availableSessions)
                {
                    sessions.Add(new SessionInfo
                    {
                        SessionId = session.SessionId,
                        JoinAddress = session.HostEndpoint?.ToString() ?? "",
                        HostName = session.HostGamertag,
                        CurrentPlayerCount = session.CurrentGamerCount,
                        MaxPlayerCount = session.OpenPublicGamerSlots + session.OpenPrivateGamerSlots,
                        IsPasswordProtected = false,
                        SessionType = sessionType
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finding sessions: {ex}");
            }

            return sessions;
        }
    }
}
