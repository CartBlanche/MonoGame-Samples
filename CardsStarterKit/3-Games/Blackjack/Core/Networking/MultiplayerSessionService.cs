using System;
using System.Diagnostics;
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

        private static bool TryConfigureSteamBackend(string gameName)
        {
            var factoryType = Type.GetType(SteamFactoryTypeName, throwOnError: false);
            if (factoryType == null)
            {
                return false;
            }

            object factoryInstance = null;

            if (!string.IsNullOrWhiteSpace(gameName))
            {
                var gameTagCtor = factoryType.GetConstructor(new[] { typeof(string) });
                if (gameTagCtor != null)
                {
                    factoryInstance = gameTagCtor.Invoke(new object[] { gameName });
                }
            }

            factoryInstance ??= Activator.CreateInstance(factoryType);

            if (factoryInstance is INetworkSessionFactory sessionFactory)
            {
                NetworkServiceProvider.SetSessionFactory(sessionFactory);
                return true;
            }

            return false;
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