using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    internal static class SystemLinkSessionManager
    {
    private const int BroadcastPort = 31337;
    private const int GamePort = 31338; // Port for gameplay UDP traffic
        private static readonly List<AvailableNetworkSession> discoveredSessions = new List<AvailableNetworkSession>();

        public static Task AdvertiseSessionAsync(NetworkSession session, CancellationToken cancellationToken)
        {
            // Periodically broadcast session info on LAN until session is full or ended
            return Task.Run(async () =>
            {
        using (var udpClient = new UdpClient())
                {
                    var endpoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
                    while (!cancellationToken.IsCancellationRequested && session.AllGamers.Count < session.MaxGamers && session.sessionState != NetworkSessionState.Ended)
                    {
                        var propertiesBytes = session.SerializeSessionPropertiesBinary();
            // Include gameplay port in the header so joiners know where to send join requests
            var header = $"SESSION:{session.sessionId}:{session.MaxGamers}:{session.PrivateGamerSlots}:{session.Host?.Gamertag ?? "Host"}:{GamePort}:";
                        var headerBytes = Encoding.UTF8.GetBytes(header);
                        var message = new byte[headerBytes.Length + propertiesBytes.Length];
                        Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);
                        Buffer.BlockCopy(propertiesBytes, 0, message, headerBytes.Length, propertiesBytes.Length);
                        await udpClient.SendAsync(message, message.Length, endpoint);
                        await Task.Delay(1000, cancellationToken); // Broadcast every second
                    }
                }
            }, cancellationToken);
        }

        public static async Task<IEnumerable<AvailableNetworkSession>> DiscoverSessionsAsync(int maxLocalGamers, CancellationToken cancellationToken)
        {
            var sessions = new List<AvailableNetworkSession>();
            using (var udpClient = new UdpClient(BroadcastPort))
            {
                udpClient.EnableBroadcast = true;
                var receiveTask = udpClient.ReceiveAsync();
                var completedTask = await Task.WhenAny(receiveTask, Task.Delay(100, cancellationToken));
                if (completedTask == receiveTask)
                {
                    var result = receiveTask.Result;
                    var buffer = result.Buffer;

                    // Find the header delimiter (the last colon of the header)
            int headerEnd = 0;
            int colonCount = 0;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        if (buffer[i] == (byte)':')
                        {
                            colonCount++;
                            if (colonCount == 6)
                            {
                                headerEnd = i + 1; // header ends after 6th colon (includes game port)
                                break;
                            }
                        }
                    }

            if (colonCount == 6)
                    {
                        var headerString = Encoding.UTF8.GetString(buffer, 0, headerEnd);
                        var parts = headerString.Split(':');
                        var sessionId = parts[1];
                        var maxGamers = int.Parse(parts[2]);
                        var privateSlots = int.Parse(parts[3]);
            var hostGamertag = parts[4];
            var gamePort = int.Parse(parts[5]);

                        // Binary session properties start after headerEnd
                        var propertiesBytes = new byte[buffer.Length - headerEnd];
                        Buffer.BlockCopy(buffer, headerEnd, propertiesBytes, 0, propertiesBytes.Length);

                        var dummySession = new NetworkSession(NetworkSessionType.SystemLink, maxGamers, privateSlots, false, sessionId);
                        dummySession.DeserializeSessionPropertiesBinary(propertiesBytes);
                        var sessionProperties = dummySession.SessionProperties as Dictionary<string, object>;

                        var hostEndpoint = new IPEndPoint(result.RemoteEndPoint.Address, gamePort);
                        sessions.Add(new AvailableNetworkSession(
                            sessionName: "SystemLinkSession",
                            hostGamertag: hostGamertag,
                            currentGamerCount: 1,
                            openPublicGamerSlots: maxGamers - 1,
                            openPrivateGamerSlots: privateSlots,
                            sessionType: NetworkSessionType.SystemLink,
                            sessionProperties: sessionProperties,
                            sessionId: sessionId,
                            hostEndpoint: hostEndpoint));
                    }
                }
            }
            return sessions;
        }

        public static async Task<NetworkSession> JoinSessionAsync(AvailableNetworkSession availableSession, CancellationToken cancellationToken)
        {
            // Minimal viable join: create a new client session and remember the host endpoint
            await Task.Delay(10, cancellationToken);
            var session = new NetworkSession(NetworkSessionType.SystemLink,
                availableSession.OpenPublicGamerSlots + availableSession.CurrentGamerCount,
                availableSession.OpenPrivateGamerSlots,
                false,
                availableSession.SessionId);
            session.sessionState = NetworkSessionState.Lobby;

            // Copy session properties from AvailableNetworkSession to NetworkSession
            foreach (var kvp in availableSession.SessionProperties)
                session.SessionProperties[kvp.Key] = kvp.Value;

            // Bind client transport on join so it can receive packets
            if (!session.NetworkTransport.IsBound)
            {
                session.NetworkTransport.Bind();
            }

            // Create a synthetic remote host gamer and record endpoint so SendToAll can reach host
            if (availableSession.HostEndpoint != null)
            {
                var hostGamer = new NetworkGamer(session, Guid.NewGuid().ToString(), isLocal: false, isHost: true, gamertag: availableSession.HostGamertag);
                // Add to session
                session.GetType().GetMethod("AddGamer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(session, new object[] { hostGamer });
                // Map endpoint
                session.RegisterGamerEndpoint(hostGamer, availableSession.HostEndpoint);
            }

            return session;
        }
    }
}