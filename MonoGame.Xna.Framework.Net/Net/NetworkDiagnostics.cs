using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Tracks network statistics and diagnostics for a session.
    /// </summary>
    public class NetworkDiagnostics
    {
        /// <summary>
        /// Total packets sent.
        /// </summary>
        public long PacketsSent { get; set; }
        
        /// <summary>
        /// Total packets received.
        /// </summary>
        public long PacketsReceived { get; set; }
        
        /// <summary>
        /// Total bytes sent.
        /// </summary>
        public long BytesSent { get; set; }
        
        /// <summary>
        /// Total bytes received.
        /// </summary>
        public long BytesReceived { get; set; }
        
        /// <summary>
        /// Estimated packet loss rate (0.0 to 1.0).
        /// </summary>
        public float PacketLossRate { get; set; }
        
        /// <summary>
        /// Average round-trip time.
        /// </summary>
        public TimeSpan AverageRtt { get; set; }
        
        /// <summary>
        /// Minimum round-trip time observed.
        /// </summary>
        public TimeSpan MinRtt { get; set; } = TimeSpan.MaxValue;
        
        /// <summary>
        /// Maximum round-trip time observed.
        /// </summary>
        public TimeSpan MaxRtt { get; set; }
        
        /// <summary>
        /// When the session started.
        /// </summary>
        public DateTime SessionStartTime { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// How long the session has been running.
        /// </summary>
        public TimeSpan Uptime => DateTime.UtcNow - SessionStartTime;

        /// <summary>
        /// Gets a human-readable summary of diagnostics.
        /// </summary>
        public string GetSummary()
        {
            return $@"Network Diagnostics:
  Uptime: {Uptime}
  Packets: {PacketsReceived} received, {PacketsSent} sent
  Traffic: {BytesReceived / 1024.0:F1} KB in, {BytesSent / 1024.0:F1} KB out
  Loss Rate: {PacketLossRate * 100:F2}%
  RTT: avg={AverageRtt.TotalMilliseconds:F1}ms, min={MinRtt.TotalMilliseconds:F1}ms, max={MaxRtt.TotalMilliseconds:F1}ms";
        }

        /// <summary>
        /// Records a packet sent.
        /// </summary>
        public void RecordPacketSent(int bytes)
        {
            PacketsSent++;
            BytesSent += bytes;
        }

        /// <summary>
        /// Records a packet received.
        /// </summary>
        public void RecordPacketReceived(int bytes)
        {
            PacketsReceived++;
            BytesReceived += bytes;
        }

        /// <summary>
        /// Records an RTT measurement.
        /// </summary>
        public void RecordRtt(TimeSpan rtt)
        {
            if (rtt < MinRtt)
                MinRtt = rtt;
            if (rtt > MaxRtt)
                MaxRtt = rtt;
            
            // Simple moving average (can be improved with proper averaging)
            if (AverageRtt == TimeSpan.Zero)
                AverageRtt = rtt;
            else
                AverageRtt = TimeSpan.FromMilliseconds((AverageRtt.TotalMilliseconds * 0.9) + (rtt.TotalMilliseconds * 0.1));
        }
    }
}
