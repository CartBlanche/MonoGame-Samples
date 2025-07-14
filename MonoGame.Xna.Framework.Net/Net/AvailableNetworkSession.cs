using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Represents an available network session that can be joined.
    /// </summary>
    public class AvailableNetworkSession
    {
        internal AvailableNetworkSession(
            string sessionName,
            string hostGamertag,
            int currentGamerCount,
            int openPublicGamerSlots,
            int openPrivateGamerSlots,
            NetworkSessionType sessionType,
            IDictionary<string, object> sessionProperties)
        {
            SessionName = sessionName;
            HostGamertag = hostGamertag;
            CurrentGamerCount = currentGamerCount;
            OpenPublicGamerSlots = openPublicGamerSlots;
            OpenPrivateGamerSlots = openPrivateGamerSlots;
            SessionType = sessionType;
            SessionProperties = new Dictionary<string, object>(sessionProperties);
        }

        /// <summary>
        /// Gets the name of the session.
        /// </summary>
        public string SessionName { get; }

        /// <summary>
        /// Gets the gamertag of the host.
        /// </summary>
        public string HostGamertag { get; }

        /// <summary>
        /// Gets the current number of gamers in the session.
        /// </summary>
        public int CurrentGamerCount { get; }

        /// <summary>
        /// Gets the number of open public gamer slots.
        /// </summary>
        public int OpenPublicGamerSlots { get; }

        /// <summary>
        /// Gets the number of open private gamer slots.
        /// </summary>
        public int OpenPrivateGamerSlots { get; }

        /// <summary>
        /// Gets the type of the session.
        /// </summary>
        public NetworkSessionType SessionType { get; }

        /// <summary>
        /// Gets the session properties.
        /// </summary>
        public IDictionary<string, object> SessionProperties { get; }

        /// <summary>
        /// Gets the quality of service information.
        /// </summary>
        public QualityOfService QualityOfService { get; internal set; } = new QualityOfService();
    }

    /// <summary>
    /// Collection of available network sessions.
    /// </summary>
    public class AvailableNetworkSessionCollection : ReadOnlyCollection<AvailableNetworkSession>, IDisposable
    {
        private bool disposed = false;

        internal AvailableNetworkSessionCollection() : base(new List<AvailableNetworkSession>()) { }
        
        internal AvailableNetworkSessionCollection(IList<AvailableNetworkSession> sessions) : base(sessions) { }

        /// <summary>
        /// Disposes of the collection resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the collection resources.
        /// </summary>
        /// <param name="disposing">True if disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Clean up managed resources if needed
                    // The ReadOnlyCollection doesn't need special cleanup
                }
                disposed = true;
            }
        }
    }

    /// <summary>
    /// Properties used when creating or searching for network sessions.
    /// </summary>
    public class NetworkSessionProperties : Dictionary<string, object>
    {
        /// <summary>
        /// Initializes a new NetworkSessionProperties.
        /// </summary>
        public NetworkSessionProperties() : base() { }

        /// <summary>
        /// Initializes a new NetworkSessionProperties with a specified capacity.
        /// </summary>
        public NetworkSessionProperties(int capacity) : base(capacity) { }
    }

    /// <summary>
    /// Quality of service information for a network session.
    /// </summary>
    public class QualityOfService
    {
        /// <summary>
        /// Gets the average round trip time in milliseconds.
        /// </summary>
        public TimeSpan AverageRoundTripTime { get; internal set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Gets the minimum round trip time in milliseconds.
        /// </summary>
        public TimeSpan MinimumRoundTripTime { get; internal set; } = TimeSpan.FromMilliseconds(20);

        /// <summary>
        /// Gets the maximum round trip time in milliseconds.
        /// </summary>
        public TimeSpan MaximumRoundTripTime { get; internal set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets the bytes per second being sent.
        /// </summary>
        public int BytesPerSecondSent { get; internal set; } = 0;

        /// <summary>
        /// Gets the bytes per second being received.
        /// </summary>
        public int BytesPerSecondReceived { get; internal set; } = 0;

        /// <summary>
        /// Gets whether the connection is available.
        /// </summary>
        public bool IsAvailable { get; internal set; } = true;
    }

    /// <summary>
    /// Wrapper for async operations to provide XNA-compatible IAsyncResult interface.
    /// </summary>
    internal class AsyncResultWrapper<T> : IAsyncResult
    {
        private readonly Task<T> task;
        private readonly object asyncState;

        public AsyncResultWrapper(Task<T> task, AsyncCallback callback, object asyncState)
        {
            this.task = task;
            this.asyncState = asyncState;

            if (callback != null)
            {
                task.ContinueWith(t => callback(this));
            }
        }

        public object AsyncState => asyncState;

        public System.Threading.WaitHandle AsyncWaitHandle => ((IAsyncResult)task).AsyncWaitHandle;

        public bool CompletedSynchronously => task.IsCompletedSuccessfully;

        public bool IsCompleted => task.IsCompleted;

        public T GetResult()
        {
            return task.Result;
        }
    }
}
