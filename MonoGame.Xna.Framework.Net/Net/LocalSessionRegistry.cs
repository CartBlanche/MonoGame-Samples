using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Xna.Framework.Net
{
    internal static class LocalSessionRegistry
    {
        private static readonly List<NetworkSession> sessions = new List<NetworkSession>();

        public static void RegisterSession(NetworkSession session)
        {
            lock (sessions)
            {
                sessions.Add(session);
            }
        }

        public static IEnumerable<AvailableNetworkSession> FindSessions(int maxLocalGamers)
        {
            lock (sessions)
            {
                return sessions
                    .Where(s => s.MaxGamers >= maxLocalGamers && s.SessionState == NetworkSessionState.Lobby)
                    .Select(s => new AvailableNetworkSession(
                        sessionName: "LocalSession",
                        hostGamertag: s.Host?.Gamertag ?? "Host",
                        currentGamerCount: s.AllGamers.Count,
                        openPublicGamerSlots: s.MaxGamers - s.AllGamers.Count,
                        openPrivateGamerSlots: s.PrivateGamerSlots,
                        sessionType: s.SessionType,
                        sessionProperties: s.SessionProperties,
                        sessionId: s.sessionId))
                    .ToList();
            }
        }

        public static NetworkSession GetSessionById(string sessionId)
        {
            lock (sessions)
            {
                return sessions.FirstOrDefault(s => s.sessionId == sessionId);
            }
        }
    }
}
