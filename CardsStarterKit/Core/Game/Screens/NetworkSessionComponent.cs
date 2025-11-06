//----------------------------------------------------------------------------- 
// NetworkSessionComponent.cs
// Adapted from NetworkStateManagement sample for Blackjack
//-----------------------------------------------------------------------------
using System;
using GameStateManagement;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;

namespace Blackjack
{
    /// <summary>
    /// Component in charge of owning and updating the current NetworkSession object.
    /// Responsible for calling NetworkSession.Update and exposing the session as a service.
    /// </summary>
    class NetworkSessionComponent : GameComponent
    {
        ScreenManager screenManager;
        NetworkSession networkSession;
        bool notifyWhenPlayersJoinOrLeave;
        string sessionEndMessage;

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
            game.Services.AddService(typeof(NetworkSession), networkSession);
            game.Components.Add(new NetworkSessionComponent(screenManager, networkSession));
        }

        public override void Initialize()
        {
            base.Initialize();
            // Optionally hook up message display here if needed
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Game.Components.Remove(this);
                Game.Services.RemoveService(typeof(NetworkSession));
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
                LeaveSession();
            }
        }

        void GamerJoined(object sender, GamerJoinedEventArgs e)
        {
            // Optionally display message or update UI
        }

        void GamerLeft(object sender, GamerLeftEventArgs e)
        {
            // Optionally display message or update UI
        }

        void NetworkSessionEnded(object sender, NetworkSessionEndedEventArgs e)
        {
            sessionEndMessage = "Session ended.";
            LeaveSession();
        }

        void LeaveSession()
        {
            Dispose(true);
            // Optionally transition to main menu or show message
        }
    }
}
