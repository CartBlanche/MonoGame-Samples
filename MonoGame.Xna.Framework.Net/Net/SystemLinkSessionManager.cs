using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                using (var udpClient = new UdpClient(AddressFamily.InterNetwork))
                {
                    udpClient.EnableBroadcast = true;
                    var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
                    var localhostEndpoint = new IPEndPoint(IPAddress.Loopback, BroadcastPort);

                    session.Logger?.LogInfo($"Starting session advertisement on port {BroadcastPort}");
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
                        try
                        {
                            await udpClient.SendAsync(message, message.Length, broadcastEndpoint);
                        }
                        catch (SocketException ex)
                        {
                            session.Logger?.LogWarning($"Broadcast advertisement send failed: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            session.Logger?.LogWarning($"Broadcast advertisement failed: {ex.Message}");
                        }

                        // ALSO send to localhost for same-machine testing
                        try
                        {
                            await udpClient.SendAsync(message, message.Length, localhostEndpoint);
                            session.Logger?.LogInfo($"Also sent advertisement to localhost (127.0.0.1:{BroadcastPort})");
                        }
                        catch (SocketException ex)
                        {
                            session.Logger?.LogWarning($"Localhost advertisement send failed: {ex.Message}");
                        }

                        broadcastCount++;
                        session.Logger?.LogInfo($"Sent broadcast #{broadcastCount} - SessionID: {session.sessionId}, Gamers: {session.AllGamers.Count}/{session.MaxGamers}");

                        await Task.Delay(750, cancellationToken); // Broadcast every 750ms for faster discovery
                    }

                    session.Logger?.LogInfo($"Stopped broadcasting. Reason: Cancelled={cancellationToken.IsCancellationRequested}, Full={session.AllGamers.Count >= session.MaxGamers}, Ended={session.sessionState == NetworkSessionState.Ended}");
                }
            }, cancellationToken);
        }

        public static async Task<IEnumerable<AvailableNetworkSession>> DiscoverSessionsAsync(int maxLocalGamers, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"[DISCOVERY] Starting session discovery on port {BroadcastPort}");

            // Use dictionary to deduplicate sessions by ID (in case we receive multiple broadcasts from same host)
            var sessionsDict = new Dictionary<string, AvailableNetworkSession>();

            try
            {
                using (var udpClient = new UdpClient(AddressFamily.InterNetwork))
                {
                    // Enable port reuse for multiple instances on same machine
                    udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, BroadcastPort));

                    Debug.WriteLine($"[DISCOVERY] Successfully bound to port {BroadcastPort}");

                    udpClient.EnableBroadcast = true;
                    // DON'T set ReceiveTimeout - it interferes with ReceiveAsync()

                    // Phase 1: Listen for 1.5 seconds to catch at least 1 broadcast cycle (hosts broadcast every 1 second)
                    // Reduced from 2.5s for faster discovery while still being reliable
                    var startTime = DateTime.UtcNow;
                    var endTime = startTime.AddSeconds(1.5);
                    int receiveAttempts = 0;
                    int packetsReceived = 0;

                    while (DateTime.UtcNow < endTime && !cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            receiveAttempts++;
                            // Try to receive with reduced timeout for faster response
                            var receiveTask = udpClient.ReceiveAsync();
                            var timeoutTask = Task.Delay(100, cancellationToken);
                            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                            if (completedTask == receiveTask)
                            {
                                var result = await receiveTask;
                                var buffer = result.Buffer;
                                packetsReceived++;

                                Debug.WriteLine($"[DISCOVERY] Received packet #{packetsReceived} from {result.RemoteEndPoint} ({buffer.Length} bytes)");

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
                                    Debug.WriteLine($"[DISCOVERY] Parsed header: {headerString}");

                                    var parts = headerString.Split(':');
                                    var sessionId = parts[1];
                                    var maxGamers = int.Parse(parts[2]);
                                    var privateSlots = int.Parse(parts[3]);
                                    var hostGamertag = parts[4];
                                    var gamePort = int.Parse(parts[5]);

                                    // Binary session properties start after headerEnd
                                    var propertiesBytes = new byte[buffer.Length - headerEnd];
                                    Buffer.BlockCopy(buffer, headerEnd, propertiesBytes, 0, propertiesBytes.Length);

                                    // Parse session properties without creating a full NetworkSession (avoids resource leaks,
                                    // port 31338 contention, and static NetworkGamer.LocalGamer overwrite)
                                    var sessionProperties = NetworkSession.DeserializeSessionPropertiesStatic(propertiesBytes);

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

                                    Debug.WriteLine($"[DISCOVERY] Added session: {hostGamertag} ({sessionId})");
                                }
                                else
                                {
                                    Debug.WriteLine($"[DISCOVERY] Invalid packet - expected 6 colons, found {colonCount}");
                                }
                            }
                            // If timeout occurs, continue listening until endTime
                        }
                        catch (SocketException ex)
                        {
                            Debug.WriteLine($"[DISCOVERY] SocketException: {ex.Message}");
                            // Socket timeout or other network error - continue listening
                        }
                        catch (ObjectDisposedException)
                        {
                            Debug.WriteLine("[DISCOVERY] Socket disposed");
                            // Socket was disposed (shouldn't happen, but handle gracefully)
                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[DISCOVERY] Unexpected error: {ex.GetType().Name} - {ex.Message}");
                        }
                    }

                    var elapsed = DateTime.UtcNow - startTime;
                    Debug.WriteLine($"[DISCOVERY] Completed after {elapsed.TotalSeconds:F2}s. Attempts: {receiveAttempts}, Packets: {packetsReceived}, Sessions: {sessionsDict.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DISCOVERY] Fatal error: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            }

            return sessionsDict.Values;
        }

        public static async Task<NetworkSession> JoinSessionAsync(AvailableNetworkSession availableSession, CancellationToken cancellationToken)
        {
            // Phase 1: Reliable join with timeout and retry
            const int MAX_RETRIES = 3;
            const int TIMEOUT_MS = 300; // Faster timeout for LAN (reduced from 500ms)

            Debug.WriteLine($"[JOIN] Starting join process for session {availableSession.SessionId}");

            // Create client session in Joining state
            var session = new NetworkSession(NetworkSessionType.SystemLink,
                availableSession.OpenPublicGamerSlots + availableSession.CurrentGamerCount,
                availableSession.OpenPrivateGamerSlots,
                false,
                availableSession.SessionId);
            session.sessionState = NetworkSessionState.Joining; // Phase 1: Use new Joining state

            // Copy session properties from AvailableNetworkSession to NetworkSession
            foreach (var kvp in availableSession.SessionProperties)
                session.SessionProperties[kvp.Key] = kvp.Value;

            // Bind client transport on join so it can receive packets
            if (!session.NetworkTransport.IsBound)
            {
                session.NetworkTransport.Bind();
            }

            // Create a synthetic remote host gamer and record endpoint so SendToAll can reach host
            if (availableSession.HostEndpoint == null)
            {
                throw new NetworkSessionJoinException("Host endpoint is null", NetworkSessionJoinError.SessionNotFound);
            }

            var hostGamer = new NetworkGamer(session, Guid.NewGuid().ToString(), isLocal: false, isHost: true, gamertag: availableSession.HostGamertag);
            session.AcceptGamer(hostGamer);
            session.RegisterGamerEndpoint(hostGamer, availableSession.HostEndpoint);

            // Phase 1: Send join request with retry logic
            for (int attempt = 0; attempt < MAX_RETRIES; attempt++)
            {
                Debug.WriteLine($"[JOIN] Sending join request (attempt {attempt + 1}/{MAX_RETRIES})");

                // CRITICAL: Use the session's own local gamer, not the static NetworkGamer.LocalGamer
                // The static property can be stale when multiple sessions exist (e.g., testing on same machine)
                var localGamer = session.LocalGamers.FirstOrDefault();
                if (localGamer == null)
                    throw new InvalidOperationException("No local gamer found in session");

                Debug.WriteLine($"[JOIN] Sending as gamer: {localGamer.Gamertag} (ID: {localGamer.Id})");

                var joinRequest = new JoinRequestMessage
                {
                    GamerId = localGamer.Id,
                    Gamertag = localGamer.Gamertag,
                    ProtocolVersion = JoinRequestMessage.CURRENT_PROTOCOL_VERSION
                };
                var writer = new PacketWriter();
                joinRequest.Serialize(writer);
                session.NetworkTransport.Send(writer.GetData(), availableSession.HostEndpoint);

                // Wait for response (NetworkSession.OnMessageReceived will update state to Lobby if accepted)
                var waitStart = DateTime.UtcNow;
                while ((DateTime.UtcNow - waitStart).TotalMilliseconds < TIMEOUT_MS)
                {
                    if (session.sessionState == NetworkSessionState.Lobby)
                    {
                        Debug.WriteLine($"[JOIN] Successfully joined session after {attempt + 1} attempt(s)");
                        // Phase 1: Start connection monitoring for client
                        session.StartConnectionMonitoring();
                        return session;
                    }

                    // Check if we received rejection (would still be in Joining state but we can check for it)
                    // For now, just wait
                    await Task.Delay(50, cancellationToken);
                }

                Debug.WriteLine($"[JOIN] Attempt {attempt + 1} timed out");
            }

            // After MAX_RETRIES attempts, give up
            Debug.WriteLine($"[JOIN] Failed to join session after {MAX_RETRIES} attempts");
            session.Dispose();
            throw new NetworkSessionJoinException(
                $"Failed to join session after {MAX_RETRIES} attempts. Host may be unreachable or session is full.",
                NetworkSessionJoinError.Timeout);
        }
    }
}