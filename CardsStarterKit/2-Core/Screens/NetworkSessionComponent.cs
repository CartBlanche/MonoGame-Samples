//----------------------------------------------------------------------------- 
// NetworkSessionComponent.cs
// Adapted from NetworkStateManagement sample for Blackjack
//-----------------------------------------------------------------------------
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace CardsFramework.Core
{
    /// <summary>
    /// Component in charge of owning and updating the current NetworkSession object.
    /// Responsible for calling NetworkSession.Update and exposing the session as a service.
    /// </summary>
    public class NetworkSessionComponent : GameComponent
    {
        public enum SystemMessageKind
        {
            GamerJoined,
            GamerLeft,
            SessionEnded,
            NetworkError,
        }

        public sealed class SystemMessage
        {
            public SystemMessageKind Kind { get; set; }
            public string GamerTag { get; set; }
            public string Message { get; set; }
        }

        ScreenManager screenManager;
        NetworkSession networkSession;
        bool notifyWhenPlayersJoinOrLeave;
        string sessionEndMessage;
        readonly ConcurrentQueue<SystemMessage> pendingSystemMessages = new ConcurrentQueue<SystemMessage>();

        /// <summary>
        /// Gets or sets whether join/leave events should be recorded.
        /// Kept as an explicit gate so callers can defer notifications during setup.
        /// </summary>
        public bool NotifyWhenPlayersJoinOrLeave
        {
            get => notifyWhenPlayersJoinOrLeave;
            set => notifyWhenPlayersJoinOrLeave = value;
        }

        /// <summary>
        /// Most recent session end message, suitable for future UI display.
        /// </summary>
        public string SessionEndMessage => sessionEndMessage;

        /// <summary>
        /// Buffered system messages (join/leave/session) for future menu/overlay UX.
        /// </summary>
        public int PendingSystemMessageCount => pendingSystemMessages.Count;

        public bool TryDequeueSystemMessage(out SystemMessage message)
        {
            return pendingSystemMessages.TryDequeue(out message);
        }

        public NetworkSessionComponent(ScreenManager screenManager, NetworkSession networkSession)
            : base(screenManager.Game)
        {
            this.screenManager = screenManager;
            this.networkSession = networkSession;
            networkSession.GamerJoined += GamerJoined;
            networkSession.GamerLeft += GamerLeft;
            networkSession.SessionEnded += NetworkSessionEnded;
        }

        public static void Create(ScreenManager screenManager, NetworkSession networkSession)
        {
            Game game = screenManager.Game;

            // Remove any existing NetworkSession service and component before adding new ones
            if (game.Services.GetService(typeof(NetworkSession)) != null)
            {
                game.Services.RemoveService(typeof(NetworkSession));
            }

            if (game.Services.GetService(typeof(NetworkSessionComponent)) != null)
            {
                game.Services.RemoveService(typeof(NetworkSessionComponent));
            }

            // Remove any existing NetworkSessionComponent
            var existingComponent = FindSessionComponent(game);
            if (existingComponent != null)
            {
                game.Components.Remove(existingComponent);
                existingComponent.Dispose();
            }

            game.Services.AddService(typeof(NetworkSession), networkSession);

            var component = new NetworkSessionComponent(screenManager, networkSession);
            game.Services.AddService(typeof(NetworkSessionComponent), component);
            game.Components.Add(component);
        }

        /// <summary>
        /// Searches through the Game.Components collection to find the NetworkSessionComponent (if any exists).
        /// </summary>
        static NetworkSessionComponent FindSessionComponent(Game game)
        {
            foreach (var component in game.Components)
            {
                if (component is NetworkSessionComponent sessionComponent)
                    return sessionComponent;
            }
            return null;
        }

        public override void Initialize()
        {
            base.Initialize();

            // Enable notifications after component initialization.
            notifyWhenPlayersJoinOrLeave = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Game.Components.Remove(this);
                Game.Services.RemoveService(typeof(NetworkSession));
                if (Game.Services.GetService(typeof(NetworkSessionComponent)) == this)
                {
                    Game.Services.RemoveService(typeof(NetworkSessionComponent));
                }
                if (networkSession != null)
                {
                    networkSession.Dispose();
                    networkSession = null;
                }
            }
            base.Dispose(disposing);
        }

        public override void Update(GameTime gameTime)
        {
            if (networkSession == null)
                return;
            try
            {
                networkSession.Update();
                if (networkSession.SessionState == NetworkSessionState.Ended)
                {
                    LeaveSession();
                }
            }
            catch (Exception)
            {
                sessionEndMessage = "Network error.";
                pendingSystemMessages.Enqueue(new SystemMessage
                {
                    Kind = SystemMessageKind.NetworkError,
                    Message = sessionEndMessage,
                });
                LeaveSession();
            }
        }

        void GamerJoined(object sender, GamerJoinedEventArgs e)
        {
            if (!notifyWhenPlayersJoinOrLeave || e?.Gamer == null)
                return;

            var message = $"{e.Gamer.Gamertag} joined the session.";
            pendingSystemMessages.Enqueue(new SystemMessage
            {
                Kind = SystemMessageKind.GamerJoined,
                GamerTag = e.Gamer.Gamertag,
                Message = message,
            });
            Debug.WriteLine($"[NetworkSessionComponent] {message}");
        }

        void GamerLeft(object sender, GamerLeftEventArgs e)
        {
            if (!notifyWhenPlayersJoinOrLeave || e?.Gamer == null)
                return;

            var message = $"{e.Gamer.Gamertag} left the session.";
            pendingSystemMessages.Enqueue(new SystemMessage
            {
                Kind = SystemMessageKind.GamerLeft,
                GamerTag = e.Gamer.Gamertag,
                Message = message,
            });
            Debug.WriteLine($"[NetworkSessionComponent] {message}");
        }

        void NetworkSessionEnded(object sender, NetworkSessionEndedEventArgs e)
        {
            sessionEndMessage = "Session ended.";
            pendingSystemMessages.Enqueue(new SystemMessage
            {
                Kind = SystemMessageKind.SessionEnded,
                Message = sessionEndMessage,
            });
            LeaveSession();
        }

        void LeaveSession()
        {
            if (!string.IsNullOrEmpty(sessionEndMessage))
            {
                // Keep this state in the component for now; UI flow can consume it later.
                Debug.WriteLine($"[NetworkSessionComponent] {sessionEndMessage}");
            }

            // Touch screen manager intentionally; this is the future integration point for UX transitions.
            _ = screenManager?.GetScreens();

            Dispose(true);
            // Optionally transition to main menu or show message
        }
    }
}