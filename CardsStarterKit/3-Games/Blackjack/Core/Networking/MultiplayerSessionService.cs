using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Net;

namespace Blackjack.Networking
{
    public enum MultiplayerBackend
    {
        UdpSystemLink,
        Steam,
        Epic,
        GooglePlay,
        IOS
    }

    /// <summary>
    /// Centralizes backend selection and session operations for Blackjack multiplayer.
    /// </summary>
    public static class MultiplayerSessionService
    {
        private const string SteamFactoryTypeName = "Microsoft.Xna.Framework.Net.SteamNetworkSessionFactory, MonoGame.Xna.Framework.Net.Steam";

        public static MultiplayerBackend ActiveBackend { get; private set; } = MultiplayerBackend.UdpSystemLink;

        public static void ConfigureBackend(MultiplayerBackend backend)
        {
            ActiveBackend = backend;
        }

        /// <summary>
        /// Centralized backend bootstrap. This is the single enum-to-factory mapping seam.
        /// </summary>
        public static void InitializeBackend(MultiplayerBackend backend, string gameName = null)
        {
            ActiveBackend = backend;

            switch (backend)
            {
                case MultiplayerBackend.Steam:
                    if (!TryConfigureSteamBackend(gameName))
                    {
                        Debug.WriteLine("[Multiplayer] Steam factory unavailable, falling back to UDP/SystemLink.");
                        NetworkServiceProvider.ResetToDefault();
                    }
                    break;

                case MultiplayerBackend.UdpSystemLink:
                    NetworkServiceProvider.ResetToDefault();
                    break;

                case MultiplayerBackend.Epic:
                case MultiplayerBackend.GooglePlay:
                case MultiplayerBackend.IOS:
                default:
                    // Future backends can register platform-specific factories here.
                    Debug.WriteLine($"[Multiplayer] Backend {backend} not implemented yet, using UDP/SystemLink.");
                    NetworkServiceProvider.ResetToDefault();
                    break;
            }
        }

        /// <summary>
        /// Preferred typed bootstrap path for platform entry points that can reference
        /// a concrete network factory at compile time. Reflection remains as a fallback
        /// via <see cref="InitializeBackend(MultiplayerBackend, string)"/>.
        /// </summary>
        public static void InitializeBackend(MultiplayerBackend backend, INetworkSessionFactory typedFactory, string gameName = null)
        {
            if (typedFactory != null)
            {
                ActiveBackend = backend;
                NetworkServiceProvider.SetSessionFactory(typedFactory);
                Debug.WriteLine($"[Multiplayer] Registered typed session factory for backend={backend}: {typedFactory.GetType().Name}");
                return;
            }

            InitializeBackend(backend, gameName);
        }

        private static bool TryConfigureSteamBackend(string gameName)
        {
            var factoryType = Type.GetType(SteamFactoryTypeName, throwOnError: false);
            if (factoryType == null)
            {
                return false;
            }

            object factoryInstance;
            try
            {
                factoryInstance = CreateFactoryInstance(factoryType, gameName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Multiplayer] Failed to create Steam factory instance: {ex.Message}");
                return false;
            }

            if (factoryInstance is INetworkSessionFactory sessionFactory)
            {
                NetworkServiceProvider.SetSessionFactory(sessionFactory);
                return true;
            }

            return false;
        }

        private static object CreateFactoryInstance(Type factoryType, string gameName)
        {
            // Prefer any ctor whose first argument is string and all remaining args are optional.
            foreach (var ctor in factoryType.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length == 0)
                    continue;

                if (parameters[0].ParameterType != typeof(string))
                    continue;

                var args = new object[parameters.Length];
                args[0] = gameName;

                bool usable = true;
                for (int i = 1; i < parameters.Length; i++)
                {
                    if (!parameters[i].IsOptional)
                    {
                        usable = false;
                        break;
                    }

                    args[i] = Type.Missing;
                }

                if (usable)
                    return ctor.Invoke(args);
            }

            // Fall back to a true parameterless constructor when available.
            return Activator.CreateInstance(factoryType);
        }

        public static Task<NetworkSession> CreateHostSessionAsync(int minPlayers, int maxPlayers)
        {
            var sessionType = ResolveSessionType();
            Debug.WriteLine($"[Multiplayer] Create host session using backend={ActiveBackend}, sessionType={sessionType}");

            return NetworkSession.CreateAsync(
                sessionType,
                minPlayers,
                maxPlayers,
                0,
                null);
        }

        public static Task<AvailableNetworkSessionCollection> FindSessionsAsync(int maxLocalGamers)
        {
            var sessionType = ResolveSessionType();
            Debug.WriteLine($"[Multiplayer] Find sessions using backend={ActiveBackend}, sessionType={sessionType}");

            return NetworkSession.FindAsync(sessionType, maxLocalGamers, null);
        }

        public static Task<NetworkSession> JoinSessionAsync(AvailableNetworkSession session)
        {
            Debug.WriteLine($"[Multiplayer] Join session using backend={ActiveBackend}, sessionId={session?.SessionId}");
            return NetworkSession.JoinAsync(session);
        }

        private static NetworkSessionType ResolveSessionType()
        {
            // Phase 1: centralize backend policy in one place.
            // Blackjack gameplay currently depends on concrete NetworkSession packet APIs,
            // so all backends flow through SystemLink until gameplay/session plumbing is
            // fully migrated to backend-agnostic session abstractions.
            return NetworkSessionType.SystemLink;
        }
    }
}