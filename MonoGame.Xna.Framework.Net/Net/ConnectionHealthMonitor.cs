using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Monitors connection health using heartbeats and detects disconnections.
    /// </summary>
    public class ConnectionHealthMonitor
    {
        private const int HEARTBEAT_INTERVAL_MS = 3000;  // Every 3 seconds (LAN)
        private const int HEARTBEAT_TIMEOUT_MS = 9000;   // 3 missed = disconnect (fast for LAN)
        
        private readonly Dictionary<string, GamerConnectionState> connections = new Dictionary<string, GamerConnectionState>();
        private uint sequenceNumber = 0;
        private NetworkSession session;
        private bool isRunning = false;

        private class GamerConnectionState
        {
            public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
            public int MissedHeartbeats { get; set; } = 0;
            public TimeSpan LastRtt { get; set; } = TimeSpan.Zero;
            public uint LastSequenceNumber { get; set; } = 0;
        }

        /// <summary>
        /// Starts monitoring connection health for the given session.
        /// </summary>
        public void StartMonitoring(NetworkSession session)
        {
            if (isRunning)
                return;

            this.session = session;
            isRunning = true;

            // Initialize connection tracking for all remote gamers
            lock (connections)
            {
                foreach (var gamer in session.AllGamers.Where(g => !g.IsLocal))
                {
                    connections[gamer.Id] = new GamerConnectionState();
                }
            }

            // Start heartbeat sender task
            Task.Run(HeartbeatLoopAsync);
        }

        /// <summary>
        /// Stops monitoring.
        /// </summary>
        public void StopMonitoring()
        {
            isRunning = false;
        }

        private async Task HeartbeatLoopAsync()
        {
            while (isRunning && session != null && !session.disposed)
            {
                try
                {
                    // Get the session's local gamer (not the static one, which can be stale)
                    var localGamer = session.LocalGamers.FirstOrDefault();
                    if (localGamer == null)
                        continue; // No local gamer, skip this heartbeat
                    
                    // Send heartbeat to all remote gamers
                    var heartbeat = new HeartbeatMessage
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        SequenceNumber = sequenceNumber++,
                        GamerCount = session.AllGamers.Count,
                        GamerId = localGamer.Id
                    };

                    var writer = new PacketWriter();
                    heartbeat.Serialize(writer);
                    var data = writer.GetData();

                    // Send to each remote gamer individually (peer-to-peer)
                    var remoteGamers = session.AllGamers.Where(g => !g.IsLocal).ToList();
                    session.Logger?.LogInfo($"Sending heartbeat to {remoteGamers.Count} remote gamer(s)");
                    foreach (var gamer in remoteGamers)
                    {
                        session.SendDataToGamer(gamer, data, SendDataOptions.None);
                    }

                    // Check for disconnections
                    CheckForTimeouts();

                    await Task.Delay(HEARTBEAT_INTERVAL_MS);
                }
                catch (Exception ex)
                {
                    session?.Logger?.LogError($"Error in heartbeat loop: {ex.Message}", ex);
                }
            }
        }

        private void CheckForTimeouts()
        {
            if (session == null)
                return;

            var now = DateTime.UtcNow;
            var disconnected = new List<NetworkGamer>();

            lock (connections)
            {
                foreach (var kvp in connections.ToList())
                {
                    var state = kvp.Value;
                    var timeSinceLastHB = (now - state.LastHeartbeat).TotalMilliseconds;

                    if (timeSinceLastHB > HEARTBEAT_TIMEOUT_MS)
                    {
                        // Mark for removal
                        var gamer = session.AllGamers.FirstOrDefault(g => g.Id == kvp.Key);
                        if (gamer != null)
                        {
                            disconnected.Add(gamer);
                            session.Logger?.LogWarning($"{gamer.Gamertag} timed out (no response for {timeSinceLastHB:F0}ms)");
                        }
                    }
                }
            }

            // Remove disconnected gamers (outside lock to avoid deadlock)
            foreach (var gamer in disconnected)
            {
                session.EvictGamer(gamer);
                
                lock (connections)
                {
                    connections.Remove(gamer.Id);
                }
            }
        }

        /// <summary>
        /// Called when HeartbeatMessage is received.
        /// </summary>
        public void OnHeartbeatReceived(string gamerId, HeartbeatMessage heartbeat)
        {
            lock (connections)
            {
                if (!connections.ContainsKey(gamerId))
                {
                    // New remote gamer
                    connections[gamerId] = new GamerConnectionState();
                }

                var state = connections[gamerId];
                state.LastHeartbeat = DateTime.UtcNow;
                state.MissedHeartbeats = 0;
                state.LastSequenceNumber = heartbeat.SequenceNumber;
            }

            // Send reply for RTT measurement using session's local gamer (not static property)
            var localGamer = session?.LocalGamers.FirstOrDefault();
            var reply = new HeartbeatReplyMessage
            {
                RequestTimestamp = heartbeat.Timestamp,
                ReplyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                GamerId = localGamer?.Id ?? string.Empty
            };

            var writer = new PacketWriter();
            reply.Serialize(writer);
            
            // Find the gamer and send reply
            var gamer = session?.AllGamers.FirstOrDefault(g => g.Id == gamerId);
            if (gamer != null)
            {
                session?.SendDataToGamer(gamer, writer.GetData(), SendDataOptions.None);
            }
        }

        /// <summary>
        /// Called when HeartbeatReplyMessage is received (for RTT calculation).
        /// </summary>
        public void OnHeartbeatReplyReceived(string gamerId, HeartbeatReplyMessage reply)
        {
            lock (connections)
            {
                if (connections.TryGetValue(gamerId, out var state))
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var rtt = TimeSpan.FromMilliseconds(now - reply.RequestTimestamp);
                    state.LastRtt = rtt;

                    // Update gamer's RTT for QoS display
                    var gamer = session?.AllGamers.FirstOrDefault(g => g.Id == gamerId);
                    gamer?.UpdateRoundtripTime(rtt);
                }
            }
        }

        /// <summary>
        /// Called when a new gamer joins to start tracking them.
        /// </summary>
        public void OnGamerJoined(NetworkGamer gamer)
        {
            if (gamer == null || gamer.IsLocal)
                return;

            lock (connections)
            {
                connections[gamer.Id] = new GamerConnectionState();
            }
        }

        /// <summary>
        /// Called when a gamer leaves to stop tracking them.
        /// </summary>
        public void OnGamerLeft(NetworkGamer gamer)
        {
            if (gamer == null)
                return;

            lock (connections)
            {
                connections.Remove(gamer.Id);
            }
        }
    }
}
