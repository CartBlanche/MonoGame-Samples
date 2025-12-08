using System;

namespace Microsoft.Xna.Framework.Net
{
    /// <summary>
    /// Service locator for network session factories.
    /// Manages the global network session factory instance for the application.
    /// This enables switching between different networking backends (UDP, Steam, etc.) at runtime.
    /// </summary>
    public static class NetworkServiceProvider
    {
        private static INetworkSessionFactory sessionFactory;
        private static readonly object lockObject = new object();

        /// <summary>
        /// Gets the current network session factory.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if no factory has been configured.</exception>
        public static INetworkSessionFactory SessionFactory
        {
            get
            {
                lock (lockObject)
                {
                    if (sessionFactory == null)
                    {
                        throw new InvalidOperationException(
                            "Network session factory not configured. " +
                            "Call NetworkServiceProvider.SetSessionFactory() during game initialization.");
                    }
                    return sessionFactory;
                }
            }
        }

        /// <summary>
        /// Sets the network session factory.
        /// This should be called once during game initialization.
        /// </summary>
        /// <param name="factory">The network session factory to use.</param>
        /// <exception cref="ArgumentNullException">Thrown if factory is null.</exception>
        public static void SetSessionFactory(INetworkSessionFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (lockObject)
            {
                sessionFactory = factory;
                System.Diagnostics.Debug.WriteLine($"[Network] Using {factory.BackendName} backend");
            }
        }

        /// <summary>
        /// Resets the session factory to default (UDP-based).
        /// </summary>
        public static void ResetToDefault()
        {
            lock (lockObject)
            {
                sessionFactory = new UdpNetworkSessionFactory();
                System.Diagnostics.Debug.WriteLine("[Network] Reset to default UDP backend");
            }
        }

        /// <summary>
        /// Gets a value indicating whether a session factory has been configured.
        /// </summary>
        public static bool IsConfigured
        {
            get
            {
                lock (lockObject)
                {
                    return sessionFactory != null;
                }
            }
        }
    }
}
