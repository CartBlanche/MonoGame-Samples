//-----------------------------------------------------------------------------
// SearchResultsScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;

namespace NetRumble
{
    /// <summary>
    /// The search-results screen shows the results of a network-session
    /// search, allowing the player to pick the game to join.
    /// </summary>
    public class SearchResultsScreen : MenuScreen
    {


        /// <summary>
        /// The maximum number of session results to display.
        /// </summary>
        const int maximumSessions = 8;






        /// <summary>
        /// The type of networking session that was requested.
        /// </summary>
        private NetworkSessionType sessionType;


        /// <summary>
        /// The collection of search results.
        /// </summary>
        private AvailableNetworkSessionCollection availableSessions = null;






        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        /// <param name="sessionType">The type of session searched for.</param>
        public SearchResultsScreen(NetworkSessionType sessionType) : base()
        {
            // apply the parameters
            this.sessionType = sessionType;

            // set the transition times
            TransitionOnTime = TimeSpan.FromSeconds(1.0);
            TransitionOffTime = TimeSpan.FromSeconds(0.0);

            // Start async session search immediately
            try
            {
                // You may want to adjust the parameters for your game
                var findTask = NetworkSession.FindAsync(
                    sessionType,
                    1, // max local gamers
                    null // session properties
                );
                var busyScreen = new NetworkBusyScreen<AvailableNetworkSessionCollection>("Searching for sessions...", findTask);
                busyScreen.OperationCompleted += SessionsFound;
                ScreenManager.AddScreen(busyScreen);
            }
            catch (Exception ex)
            {
                string message = ex is GamerPrivilegeException
                    ? "You do not have permission to search for a session."
                    : "Failed searching for the session.";
                MessageBoxScreen messageBox = new MessageBoxScreen(message);
                messageBox.Accepted += FailedMessageBox;
                messageBox.Cancelled += FailedMessageBox;
                ScreenManager.AddScreen(messageBox);
                System.Console.WriteLine($"Failed to search for session:  {ex.Message}");
            }
        }






        /// <summary>
        /// Updates the screen. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(Microsoft.Xna.Framework.GameTime gameTime,
            bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            bool signedIntoLive = false;
            if (Gamer.SignedInGamers.Count > 0)
            {
                foreach (SignedInGamer signedInGamer in Gamer.SignedInGamers)
                {
                    if (signedInGamer.IsSignedInToLive)
                    {
                        signedIntoLive = true;
                        break;
                    }
                }
                if (!signedIntoLive &&
                    ((sessionType == NetworkSessionType.PlayerMatch) ||
                     (sessionType == NetworkSessionType.Ranked)) && !IsExiting)
                {
                    ExitScreen();
                }
            }
            else if (!IsExiting)
            {
                ExitScreen();
            }

            if (coveredByOtherScreen && !IsExiting)
            {
                ExitScreen();
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }


        /// <summary>
        /// Responds to user menu selections.
        /// </summary>
        protected override void OnSelectEntry(int entryIndex)
        {
            if ((availableSessions != null) && (entryIndex >= 0) && 
                (entryIndex < availableSessions.Count))
            {
                try
                {
                    // Use the new async/await pattern for joining a session
                    var joinTask = NetworkSession.JoinAsync(availableSessions[entryIndex]);
                    var busyScreen = new NetworkBusyScreen<NetworkSession>("Joining the session...", joinTask);
                    busyScreen.OperationCompleted += LoadLobbyScreenAsync;
                    ScreenManager.AddScreen(busyScreen);
                }
                catch (Exception ex)
                {
                    string message = ex is GamerPrivilegeException
                        ? "You do not have permission to join a session."
                        : "Failed joining the session.";
                    MessageBoxScreen messageBox = new MessageBoxScreen(message);
                    messageBox.Accepted += FailedMessageBox;
                    messageBox.Cancelled += FailedMessageBox;
                    ScreenManager.AddScreen(messageBox);
                    System.Console.WriteLine($"Failed to join session:  {ex.Message}");
                }
            }
        }


        /// <summary>
        /// When the user cancels the screen.
        /// </summary>
        protected override void OnCancel()
        {
            if (availableSessions != null)
            {
                ExitScreen();
            }
        }






        /// <summary>
        /// Draw the screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            string alternateString = String.Empty;

            // set an alternate string if there are no search results yet
            if (availableSessions == null)
            {
                alternateString = "Searching...";
            }
            else if (availableSessions.Count <= 0)
            {
                alternateString = "No sessions found.";
            }

            if (String.IsNullOrEmpty(alternateString))
            {
                base.Draw(gameTime);
            }
            else
            {
                Viewport viewport = ScreenManager.GraphicsDevice.Viewport;
                Vector2 viewportSize = new Vector2(viewport.Width, viewport.Height);

                Vector2 position = new Vector2(0f, viewportSize.Y * 0.65f);

                // Make the menu slide into place during transitions, using a
                // power curve to make things look more interesting (this makes
                // the movement slow down as it nears the end).
                float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

                if (ScreenState == ScreenState.TransitionOn)
                    position.Y += transitionOffset * 256;
                else
                    position.Y += transitionOffset * 512;

                // Draw each menu entry in turn.
                ScreenManager.SpriteBatch.Begin();

                Vector2 origin = new Vector2(0, ScreenManager.Font.LineSpacing / 2);
                Vector2 size = ScreenManager.Font.MeasureString(alternateString);
                position.X = viewportSize.X / 2f - size.X / 2f;
                ScreenManager.SpriteBatch.DrawString(ScreenManager.Font, 
                                                     alternateString, position, 
                                                     Color.White, 0, origin, 1.0f,
                                                     SpriteEffects.None, 0);

                ScreenManager.SpriteBatch.End();
            }
        }






        /// <summary>
        /// Callback to receive the network-session search results (async/await style).
        /// </summary>
        internal void SessionsFound(object sender, OperationCompletedEventArgs e)
        {
            // e.Result should be an AvailableNetworkSessionCollection or null
            availableSessions = e.Result as AvailableNetworkSessionCollection;
            if (e.Exception != null)
            {
                string message = e.Exception is GamerPrivilegeException
                    ? "You do not have permission to search for a session. " + e.Exception.Message
                    : "Failed searching for the session.";
                MessageBoxScreen messageBox = new MessageBoxScreen(message);
                messageBox.Accepted += FailedMessageBox;
                messageBox.Cancelled += FailedMessageBox;
                ScreenManager.AddScreen(messageBox);
                System.Console.WriteLine($"Failed to search for session:  {e.Exception.Message}");
            }
            MenuEntries.Clear();
            if (availableSessions != null)
            {
                foreach (AvailableNetworkSession availableSession in availableSessions)
                {
                    if (availableSession.CurrentGamerCount < World.MaximumPlayers)
                    {
                        MenuEntries.Add(availableSession.HostGamertag + " (" +
                            availableSession.CurrentGamerCount.ToString() + "/" +
                            World.MaximumPlayers.ToString() + ")");
                    }
                    if (MenuEntries.Count >= maximumSessions)
                    {
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Callback to load the lobby screen with the new session.
        /// </summary>
        // New async/await style lobby loader
        private void LoadLobbyScreenAsync(object sender, OperationCompletedEventArgs e)
        {
            var networkSession = e.Result as NetworkSession;
            if (e.Exception != null || networkSession == null)
            {
                string message = e.Exception is GamerPrivilegeException
                    ? "You do not have permission to join a session."
                    : "Failed joining session.";
                MessageBoxScreen messageBox = new MessageBoxScreen(message);
                messageBox.Accepted += FailedMessageBox;
                messageBox.Cancelled += FailedMessageBox;
                ScreenManager.AddScreen(messageBox);
                System.Console.WriteLine($"Failed joining session:  {e.Exception?.Message}");
                return;
            }
            LobbyScreen lobbyScreen = new LobbyScreen(networkSession);
            lobbyScreen.ScreenManager = this.ScreenManager;
            ScreenManager.AddScreen(lobbyScreen);
        }


        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to exit" message box.
        /// </summary>
        private void FailedMessageBox(object sender, EventArgs e)
        {
            ExitScreen();
        }


    }
}
