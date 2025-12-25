//----------------------------------------------------------------------------- 
// SessionBrowserScreen.cs
//
// Shows available sessions and allows hosting a new game.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using GameStateManagement;
using CardsFramework;

namespace Blackjack
{
    class SessionBrowserScreen : MenuScreen
    {
        MenuEntry hostGameMenuEntry;
        MenuEntry refreshMenuEntry;
        MenuEntry backMenuEntry;
        List<MenuEntry> sessionEntries = new List<MenuEntry>();
        List<AvailableNetworkSession> availableSessions = new List<AvailableNetworkSession>();

        TimeSpan timeSinceLastSearch = TimeSpan.Zero;
        const float AutoRefreshInterval = 5.0f; // Auto-refresh every 5 seconds
        bool isSearching = false;

        public SessionBrowserScreen()
            : base(Resources.JoinOrHostGame)
        {
            // Enable tap gestures for mobile input
            EnabledGestures = GestureType.Tap;
        }

        public override void LoadContent()
        {
            hostGameMenuEntry = new MenuEntry(Resources.HostNewGame);
            refreshMenuEntry = new MenuEntry(Resources.Refresh);
            backMenuEntry = new MenuEntry(Resources.Back);

            hostGameMenuEntry.Selected += HostGameMenuEntrySelected;
            refreshMenuEntry.Selected += RefreshMenuEntrySelected;
            backMenuEntry.Selected += OnCancel;

            MenuEntries.Add(hostGameMenuEntry);
            MenuEntries.Add(refreshMenuEntry);
            MenuEntries.Add(backMenuEntry);

            // Start async session discovery
            BeginFindSessions();

            base.LoadContent();
        }

        void RefreshMenuEntrySelected(object sender, EventArgs e)
        {
            // Manually trigger a refresh
            if (!isSearching)
            {
                BeginFindSessions();
                timeSinceLastSearch = TimeSpan.Zero;
            }
        }

        void HostGameMenuEntrySelected(object sender, EventArgs e)
        {
            // Host a new session and go to lobby
            // Use SystemLink for local network testing (PlayerMatch requires online services)
            var asyncResult = NetworkSession.CreateAsync(
                NetworkSessionType.SystemLink,
                BlackjackConstants.MinPlayers, // local gamers
                BlackjackConstants.MaxPlayers, // max gamers
                0, // private slots
                null);
            var busyScreen = new NetworkBusyScreen<NetworkSession>(asyncResult);
            busyScreen.OperationCompleted += (s, evt) =>
            {
                var networkSession = evt.Result as NetworkSession;
                if (networkSession != null)
                {
                    NetworkSessionComponent.Create(ScreenManager, networkSession);
                    ScreenManager.AddScreen(new BlackjackLobbyScreen(networkSession), null);
                }
                else
                {
                    ScreenManager.AddScreen(new MessageBoxScreen(Resources.FailedToCreateSession), null);
                }
            };
            ScreenManager.AddScreen(busyScreen, null);
        }

        void JoinSessionMenuEntrySelected(object sender, EventArgs e)
        {
            var entry = sender as AvailableSessionMenuEntry;
            var availableSession = entry?.AvailableSession;
            if (availableSession != null)
            {
                var asyncResult = NetworkSession.JoinAsync(availableSession);
                var busyScreen = new NetworkBusyScreen<NetworkSession>(asyncResult);
                busyScreen.OperationCompleted += (s, evt) =>
                {
                    var networkSession = evt.Result as NetworkSession;
                    if (networkSession != null)
                    {
                        NetworkSessionComponent.Create(ScreenManager, networkSession);
                        ScreenManager.AddScreen(new BlackjackLobbyScreen(networkSession), null);
                    }
                    else
                    {
                        ScreenManager.AddScreen(new MessageBoxScreen(Resources.FailedToJoinSession), null);
                    }
                };
                ScreenManager.AddScreen(busyScreen, null);
            }
        }

        void RefreshSessionList()
        {
            // Remove all existing session entries from MenuEntries  
            foreach (var entry in sessionEntries)
            {
                MenuEntries.Remove(entry);
            }
            sessionEntries.Clear();

            // Don't add session entries to MenuEntries - we'll draw them separately
            // Just create the entries for the available sessions
            foreach (var session in availableSessions)
            {
                var entry = new AvailableSessionMenuEntry(session);
                entry.Selected += JoinSessionMenuEntrySelected;
                sessionEntries.Add(entry);
            }
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Auto-refresh session list periodically
            if (!isSearching && !coveredByOtherScreen)
            {
                timeSinceLastSearch += gameTime.ElapsedGameTime;
                if (timeSinceLastSearch.TotalSeconds >= AutoRefreshInterval)
                {
                    BeginFindSessions();
                    timeSinceLastSearch = TimeSpan.Zero;
                }
            }

            // Update refresh button text to show status
            if (refreshMenuEntry != null)
            {
                refreshMenuEntry.Text = isSearching ? Resources.NetworkBusy : Resources.Refresh;
            }
        }

        public override void HandleInput(InputState inputState)
        {
            // Handle input for session entries using platform-specific input handling
            bool isClicked = false;
            Vector2 clickPosition = Vector2.Zero;

            // Use transformed cursor location for all input types (handles scaling/letterboxing)
            Vector2 transformedCursorPos = inputState.CurrentCursorLocation;

            if (UIUtility.IsDesktop)
            {
                // Handle mouse click (button was pressed last frame, released this frame)
                if (inputState.IsLeftMouseButtonClicked())
                {
                    isClicked = true;
                    clickPosition = transformedCursorPos;
                }
            }
            else if (UIUtility.IsMobile)
            {
                // Handle touch input with gestures
                if (inputState.Gestures.Count > 0 && inputState.Gestures[0].GestureType == Microsoft.Xna.Framework.Input.Touch.GestureType.Tap)
                {
                    isClicked = true;
                    // Use transformed cursor position (already set by InputState)
                    clickPosition = transformedCursorPos;
                }
            }

            // Handle session selection if clicked
            if (isClicked && availableSessions.Count > 0)
            {
                SpriteFont font = ScreenManager.Font;
                Vector2 headerPosition = new Vector2(ScreenManager.SafeArea.Left + 100, ScreenManager.SafeArea.Top + 150);
                Vector2 sessionPosition = new Vector2(ScreenManager.SafeArea.Left + 120, headerPosition.Y + font.LineSpacing * 1.5f);
                float scale = 0.85f;

                for (int i = 0; i < sessionEntries.Count; i++)
                {
                    var sessionEntry = sessionEntries[i];
                    Vector2 textSize = font.MeasureString(sessionEntry.Text) * scale;
                    Rectangle hitBox = new Rectangle(
                        (int)sessionPosition.X,
                        (int)sessionPosition.Y,
                        (int)textSize.X,
                        (int)(font.LineSpacing * scale)
                    );

                    if (hitBox.Contains((int)clickPosition.X, (int)clickPosition.Y))
                    {
                        // Clicked on this session - join it
                        JoinSessionMenuEntrySelected(sessionEntry, EventArgs.Empty);
                        return;
                    }

                    sessionPosition.Y += font.LineSpacing * scale + 10;
                }
            }

            // Let base class handle button input
            base.HandleInput(inputState);
        }

        protected override void OnCancel(PlayerIndex playerIndex)
        {
            // Exit all screens and return to main menu (without BackgroundScreen to avoid logo)
            foreach (GameScreen screen in ScreenManager.GetScreens())
                screen.ExitScreen();

            ScreenManager.AddScreen(new BackgroundScreen(), null);
            ScreenManager.AddScreen(new MainMenuScreen(), null);
        }

        public override void Draw(GameTime gameTime)
        {
            // Draw solid background to cover any BackgroundScreen logo
            ScreenManager.GraphicsDevice.Clear(new Color(50, 20, 20)); // Dark red background

            // Draw menu entries (bottom buttons) and title from base class
            base.Draw(gameTime);

            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;

            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, ScreenManager.GlobalTransformation);

            // Draw "Available Games:" section header and session list
            if (availableSessions.Count > 0)
            {
                // Draw header
                Vector2 headerPosition = new Vector2(ScreenManager.SafeArea.Left + 100, ScreenManager.SafeArea.Top + 150);
                spriteBatch.DrawString(font, Resources.AvailableGames, headerPosition, Color.Yellow, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

                // Draw session entries in a vertical list below the header
                Vector2 sessionPosition = new Vector2(ScreenManager.SafeArea.Left + 120, headerPosition.Y + font.LineSpacing * 1.5f);

                for (int i = 0; i < sessionEntries.Count; i++)
                {
                    var sessionEntry = sessionEntries[i];
                    Color color = Color.White;
                    float scale = 0.85f;

                    // Draw the session entry text
                    string sessionText = sessionEntry.Text;
                    spriteBatch.DrawString(font, sessionText, sessionPosition, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

                    // Move down for next entry
                    sessionPosition.Y += font.LineSpacing * scale + 10; // Add some padding
                }
            }

            // Draw status at bottom left
            Vector2 statusPosition = new Vector2(ScreenManager.SafeArea.Left + 50, ScreenManager.SafeArea.Bottom - 80);

            var pluralGames = availableSessions.Count == 0 || availableSessions.Count > 1 ? "s" : "";
            string statusText = isSearching
                ? Resources.SearchingForGames
                : string.Format(Resources.FoundGames, availableSessions.Count, pluralGames);

            spriteBatch.DrawString(font, statusText, statusPosition, Color.LightGreen, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);

            // Show auto-refresh timer
            if (!isSearching)
            {
                int secondsUntilRefresh = (int)(AutoRefreshInterval - timeSinceLastSearch.TotalSeconds);
                string timerText = string.Format(Resources.AutoRefreshIn, secondsUntilRefresh);
                statusPosition.Y += font.LineSpacing;
                spriteBatch.DrawString(font, timerText, statusPosition, Color.Gray, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

            spriteBatch.End();
        }

        // Helper class for session info
        // Deprecated: AvailableSession replaced by AvailableNetworkSession

        void BeginFindSessions()
        {
            if (isSearching)
                return; // Already searching

            isSearching = true;

            var asyncResult = NetworkSession.FindAsync(
                NetworkSessionType.SystemLink,
                1, // local gamers
                null);
            var busyScreen = new NetworkBusyScreen<AvailableNetworkSessionCollection>(asyncResult);
            busyScreen.OperationCompleted += (s, evt) =>
            {
                isSearching = false;
                var foundSessions = evt.Result as AvailableNetworkSessionCollection;
                availableSessions.Clear();
                if (foundSessions != null)
                {
                    foreach (var session in foundSessions)
                        availableSessions.Add(session);
                }
                RefreshSessionList();
            };
            ScreenManager.AddScreen(busyScreen, null);
        }
    }
}