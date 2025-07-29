using System.Collections.ObjectModel;

namespace Microsoft.Xna.Framework.Net
{
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
}
