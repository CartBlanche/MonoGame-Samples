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
                    udpClient.EnableBroadcast = true;
                    var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
                    var localhostEndpoint = new IPEndPoint(IPAddress.Loopback, BroadcastPort);

                    Console.WriteLine($"[BROADCAST] Starting session advertisement on port {BroadcastPort}");
                    int broadcastCount = 0;

                    while (!cancellationToken.IsCancellationRequested && session.AllGamers.Count < session.MaxGamers && session.sessionState != NetworkSessionState.Ended)
                    {
                        var propertiesBytes = session.SerializeSessionPropertiesBinary();
                        // Include gameplay port in the header so joiners know where to send join requests
                        var header = $"SESSION:{session.sessionId}:{session.MaxGamers}:{session.PrivateGamerSlots}:{session.Host?.Gamertag ?? "Host"}:{GamePort}:";
                        var headerBytes = Encoding.UTF8.GetBytes(header);
                        var message = new byte[headerBytes.Length + propertiesBytes.Length];
                        Buffer.BlockCopy(headerBytes, 0, message, 0, headerBytes.Length);
                        Buffer.BlockCopy(propertiesBytes, 0, message, headerBytes.Length, propertiesBytes.Length);

                        // Send to broadcast address for LAN discovery
                        await udpClient.SendAsync(message, message.Length, broadcastEndpoint);

                        // ALSO send to localhost for same-machine testing
                        try
                        {
                            await udpClient.SendAsync(message, message.Length, localhostEndpoint);
                            Console.WriteLine($"[BROADCAST] Also sent to localhost (127.0.0.1:{BroadcastPort})");
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"[BROADCAST] Localhost send failed: {ex.Message}");
                        }

                        broadcastCount++;
                        Console.WriteLine($"[BROADCAST] Sent broadcast #{broadcastCount} - SessionID: {session.sessionId}, Gamers: {session.AllGamers.Count}/{session.MaxGamers}");

                        await Task.Delay(1000, cancellationToken); // Broadcast every second
                    }

                    Console.WriteLine($"[BROADCAST] Stopped broadcasting. Reason: Cancelled={cancellationToken.IsCancellationRequested}, Full={session.AllGamers.Count >= session.MaxGamers}, Ended={session.sessionState == NetworkSessionState.Ended}");
                }
            }, cancellationToken);
        }

        public static async Task<IEnumerable<AvailableNetworkSession>> DiscoverSessionsAsync(int maxLocalGamers, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[DISCOVERY] Starting session discovery on port {BroadcastPort}");

            // Use dictionary to deduplicate sessions by ID (in case we receive multiple broadcasts from same host)
            var sessionsDict = new Dictionary<string, AvailableNetworkSession>();

            try
            {
                using (var udpClient = new UdpClient())
                {
                    // Enable port reuse for multiple instances on same machine
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, BroadcastPort));

                    Console.WriteLine($"[DISCOVERY] Successfully bound to port {BroadcastPort}");

                    udpClient.EnableBroadcast = true;
                    // DON'T set ReceiveTimeout - it interferes with ReceiveAsync()

                    // Listen for 2 seconds to catch at least 2 broadcast cycles (hosts broadcast every 1 second)
                    var startTime = DateTime.UtcNow;
                    var endTime = startTime.AddSeconds(2);
                    int receiveAttempts = 0;
                    int packetsReceived = 0;

                    while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            receiveAttempts++;
                            // Try to receive with timeout
                            var receiveTask = udpClient.ReceiveAsync();
                            var timeoutTask = Task.Delay(150, cancellationToken);
                            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                            if (completedTask == receiveTask)
                            {
                                var result = await receiveTask;
                                var buffer = result.Buffer;
                                packetsReceived++;

                                Console.WriteLine($"[DISCOVERY] Received packet #{packetsReceived} from {result.RemoteEndPoint} ({buffer.Length} bytes)");

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
                                    Console.WriteLine($"[DISCOVERY] Parsed header: {headerString}");

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

                                    // Add to dictionary (will replace if we get multiple broadcasts from same session)
                                    sessionsDict[sessionId] = new AvailableNetworkSession(
                                        sessionName: "SystemLinkSession",
                                        hostGamertag: hostGamertag,
                                        currentGamerCount: 1,
                                        openPublicGamerSlots: maxGamers - 1,
                                        openPrivateGamerSlots: privateSlots,
                                        sessionType: NetworkSessionType.SystemLink,
                                        sessionProperties: sessionProperties,
                                        sessionId: sessionId,
                                        hostEndpoint: hostEndpoint);

                                    Console.WriteLine($"[DISCOVERY] Added session: {hostGamertag} ({sessionId})");
                                }
                                else
                                {
                                    Console.WriteLine($"[DISCOVERY] Invalid packet - expected 6 colons, found {colonCount}");
                                }
                            }
                            // If timeout occurs, continue listening until endTime
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine($"[DISCOVERY] SocketException: {ex.Message}");
                            // Socket timeout or other network error - continue listening
                        }
                        catch (ObjectDisposedException)
                        {
                            Console.WriteLine($"[DISCOVERY] Socket disposed");
                            // Socket was disposed (shouldn't happen, but handle gracefully)
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DISCOVERY] Unexpected error: {ex.GetType().Name} - {ex.Message}");
                        }
                    }

                    var elapsed = DateTime.UtcNow - startTime;
                    Console.WriteLine($"[DISCOVERY] Completed after {elapsed.TotalSeconds:F2}s. Attempts: {receiveAttempts}, Packets: {packetsReceived}, Sessions: {sessionsDict.Count}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCOVERY] Fatal error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            }

            return sessionsDict.Values;
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
                session.GetType().GetMethod("AddGamer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.Invoke(session, new object[] { hostGamer });
                session.RegisterGamerEndpoint(hostGamer, availableSession.HostEndpoint);

                // Proactively send a JoinRequest to the host so it can add this client
                var joinRequest = new JoinRequestMessage
                {
                    GamerId = NetworkGamer.LocalGamer.Id,
                    Gamertag = NetworkGamer.LocalGamer.Gamertag
                };
                var writer = new PacketWriter();
                joinRequest.Serialize(writer);
                session.NetworkTransport.Send(writer.GetData(), availableSession.HostEndpoint);
            }

            return session;
        }
    }
}