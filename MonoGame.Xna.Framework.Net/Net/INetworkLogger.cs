using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Interface for network logging.
    /// </summary>
    public interface INetworkLogger
    {
        /// <summary>
        /// Logs an informational message.
        /// </summary>
        void LogInfo(string message);
        
        /// <summary>
        /// Logs a warning message.
        /// </summary>
        void LogWarning(string message);
        
        /// <summary>
        /// Logs an error message with optional exception.
        /// </summary>
        void LogError(string message, Exception ex = null);
    }

    /// <summary>
    /// Console-based network logger implementation.
    /// </summary>
    public class ConsoleNetworkLogger : INetworkLogger
    {
        public void LogInfo(string message)
        {
            Console.WriteLine($"[NET] {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[NET-WARN] {message}");
        }

        public void LogError(string message, Exception ex = null)
        {
            Console.WriteLine($"[NET-ERROR] {message}");
            if (ex != null)
                Console.WriteLine($"  {ex}");
        }
    }

    /// <summary>
    /// Null logger that does nothing (for production builds).
    /// </summary>
    public class NullNetworkLogger : INetworkLogger
    {
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception ex = null) { }
    }
}
