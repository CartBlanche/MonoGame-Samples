using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Factory for Steam vertical-slice sessions.
    /// Uses an in-memory Steam-like backend so host/find/join/message flow is testable end-to-end.
    /// </summary>
    public sealed class SteamNetworkSessionFactory : INetworkSessionFactory
    {
        public string BackendName => "Steam";

        public INetworkSession CreateSession()
        {
            return new SteamNetworkSession();
        }

        public Task<IEnumerable<SessionInfo>> FindSessionsAsync(NetworkSessionType sessionType)
        {
            return Task.FromResult(SteamSessionDirectory.FindSessions(sessionType));
        }
    }

    internal static class SteamSessionDirectory
    {
        private sealed class SteamLobby
        {
            public string SessionId;
            public NetworkSessionType SessionType;
            public int MaxGamers;
            public SteamNetworkSession Host;
            public List<SteamNetworkSession> Participants = new List<SteamNetworkSession>();
        }

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, SteamLobby> Lobbies = new Dictionary<string, SteamLobby>();
        private static readonly Dictionary<string, SteamNetworkSession> LocalOwnerByGamerId = new Dictionary<string, SteamNetworkSession>();

        public static void RegisterHost(SteamNetworkSession host, string sessionId, NetworkSessionType sessionType, int maxGamers)
        {
            lock (Gate)
            {
                var lobby = new SteamLobby
                {
                    SessionId = sessionId,
                    SessionType = sessionType,
                    MaxGamers = maxGamers,
                    Host = host
                };

                lobby.Participants.Add(host);
                Lobbies[sessionId] = lobby;
                LocalOwnerByGamerId[host.LocalGamer.Id] = host;
            }
        }

        public static IEnumerable<SessionInfo> FindSessions(NetworkSessionType sessionType)
        {
            lock (Gate)
            {
                var result = new List<SessionInfo>();
                foreach (var lobby in Lobbies.Values)
                {
                    if (lobby.SessionType != sessionType)
                    {
                        continue;
                    }

                    var currentPlayers = lobby.Participants.Count;
                    result.Add(new SessionInfo
                    {
                        SessionId = lobby.SessionId,
                        JoinAddress = lobby.SessionId,
                        HostName = lobby.Host.LocalGamer.Gamertag,
                        CurrentPlayerCount = currentPlayers,
                        MaxPlayerCount = lobby.MaxGamers,
                        IsPasswordProtected = false,
                        SessionType = lobby.SessionType
                    });
                }

                return result;
            }
        }

        public static void JoinLobby(string sessionId, SteamNetworkSession joiningSession)
        {
            List<SteamNetworkSession> toPromote;
            lock (Gate)
            {
                if (!Lobbies.TryGetValue(sessionId, out var lobby))
                {
                    throw new InvalidOperationException($"Steam session not found: {sessionId}");
                }

                if (lobby.Participants.Count >= lobby.MaxGamers)
                {
                    throw new InvalidOperationException("Steam session is full.");
                }

                foreach (var participant in lobby.Participants)
                {
                    joiningSession.EnsureRemoteGamer(participant.LocalGamer.Id, participant.LocalGamer.Gamertag, participant.IsHostSession);
                    participant.EnsureRemoteGamer(joiningSession.LocalGamer.Id, joiningSession.LocalGamer.Gamertag, isHost: false);
                    participant.NotifyGamerJoined(joiningSession.LocalGamer.Id);
                }

                lobby.Participants.Add(joiningSession);
                LocalOwnerByGamerId[joiningSession.LocalGamer.Id] = joiningSession;

                // Snapshot under lock; promote outside to avoid deadlock if a GameStarted
                // handler re-enters directory operations (e.g. CloseAsync → RemoveLobby).
                toPromote = new List<SteamNetworkSession>(lobby.Participants);
            }

            foreach (var participant in toPromote)
            {
                participant.PromoteToPlayingIfNeeded();
            }
        }

        public static void SendReliable(string recipientGamerId, byte[] data, string senderGamerId)
        {
            lock (Gate)
            {
                if (!LocalOwnerByGamerId.TryGetValue(recipientGamerId, out var owner))
                {
                    return;
                }

                owner.EnqueueInbound(data, senderGamerId);
            }
        }

        public static void RemoveSession(SteamNetworkSession session)
        {
            lock (Gate)
            {
                if (session?.SessionId == null || !Lobbies.TryGetValue(session.SessionId, out var lobby))
                {
                    return;
                }

                LocalOwnerByGamerId.Remove(session.LocalGamer.Id);

                if (ReferenceEquals(lobby.Host, session))
                {
                    foreach (var participant in lobby.Participants)
                    {
                        if (!ReferenceEquals(participant, session))
                        {
                            participant.ForceSessionEnded(NetworkSessionEndReason.HostEndedSession);
                            participant.RemoveRemoteGamer(session.LocalGamer.Id);
                        }
                    }

                    Lobbies.Remove(lobby.SessionId);
                    return;
                }

                lobby.Participants.Remove(session);

                foreach (var participant in lobby.Participants)
                {
                    participant.RemoveRemoteGamer(session.LocalGamer.Id);
                    participant.NotifyGamerLeft(session.LocalGamer.Id);
                }
            }
        }
    }
}
