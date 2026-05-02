using System;

namespace Microsoft.Xna.Framework.Net
{
    public static class NetworkServiceProvider
    {
        private static INetworkSessionFactory sessionFactory;
        private static readonly object lockObject = new object();

        public static INetworkSessionFactory SessionFactory
        {
            get
            {
                lock (lockObject)
                {
                    if (sessionFactory == null)
                        throw new InvalidOperationException("Network session factory not configured. Call NetworkServiceProvider.SetSessionFactory() during game initialization.");
                    return sessionFactory;
                }
            }
        }

        public static string ActiveBackendName
        {
            get
            {
                lock (lockObject)
                {
                    return sessionFactory?.BackendName;
                }
            }
        }

        public static void SetSessionFactory(INetworkSessionFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            lock (lockObject)
            {
                sessionFactory = factory;
                System.Diagnostics.Debug.WriteLine($"[Network] Using {factory.BackendName} backend");
            }
        }

        public static void ResetToDefault()
        {
            lock (lockObject)
            {
                sessionFactory = new UdpNetworkSessionFactory();
                System.Diagnostics.Debug.WriteLine("[Network] Reset to default UDP backend");
            }
        }

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
