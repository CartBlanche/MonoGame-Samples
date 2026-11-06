using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Multiplayer;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// The LAN race browser, the first screen of the LAN flow (as in ShipGame): the list starts
    /// with "Host", then the races found on the network, then "Back". Hosting or joining
    /// goes on to the car selection and then the lobby.
    /// </summary>
    class LanBrowserScreen : IGameScreen
    {
        const double SearchIntervalSeconds = 3.0;

        readonly List<AvailableNetworkSession> sessions = new List<AvailableNetworkSession>();
        readonly PanelButtonGroup buttons = new PanelButtonGroup();
        int selection { get { return buttons.Selection; } set { buttons.Selection = value; } }
        int firstVisible;
        const int MaxVisible = 5;

        Task<AvailableNetworkSessionCollection> findTask;
        Task<NetworkSession> sessionTask;
        string busyText;
        string message;
        double nextSearchAt;
        bool launched;      // the race flow has been pushed on top of this screen
        bool leave;
        int clickedEntry = -1;      // set by Render when a button was clicked this frame

        int EntryCount { get { return sessions.Count + 1; } }
        bool IsHostEntry(int index) { return index == 0; }

        public LanBrowserScreen()
        {
            message = LanRace.Message;
            LanRace.Message = null;
        }

        public void Update(GameTime gameTime)
        {
            // back from a race (or from leaving one): search afresh
            if (launched && !LanRace.InRace)
            {
                launched = false;
                sessions.Clear();
                selection = 0;
                nextSearchAt = 0;
                message = LanRace.Message;
                LanRace.Message = null;
            }

            try
            {
                if (sessionTask != null)
                    UpdateSessionTask();
                else
                    UpdateSearch();
            }
            catch (Exception ex)
            {
                Exception inner = ex is AggregateException && ((AggregateException)ex).InnerException != null
                    ? ((AggregateException)ex).InnerException : ex;
                NetLog.Write("LAN browser: " + inner);
                message = "Network error: " + inner.Message;
                sessionTask = null;
                findTask = null;
                busyText = null;
            }
        }

        void UpdateSearch()
        {
            double now = BaseGame.TotalTimeMilliseconds / 1000.0;

            if (findTask == null && now >= nextSearchAt)
                findTask = MultiplayerSessionService.FindSessionsAsync();

            if (findTask != null && findTask.IsCompleted)
            {
                AvailableNetworkSessionCollection found = findTask.Result;
                findTask = null;
                nextSearchAt = now + SearchIntervalSeconds;

                // keep the selected race selected as the list changes
                string selectedId = (selection >= 1 && selection <= sessions.Count) ? sessions[selection - 1].SessionId : null;
                sessions.Clear();
                foreach (AvailableNetworkSession candidate in found)
                    if (MultiplayerSessionService.IsCompatible(candidate))
                        sessions.Add(candidate);
                found.Dispose();

                int index = selectedId == null ? -1 : sessions.FindIndex(s => s.SessionId == selectedId);
                if (index >= 0)
                    selection = index + 1;
                else
                    selection = Math.Min(selection, EntryCount - 1);
            }
        }

        void UpdateSessionTask()
        {
            if (!sessionTask.IsCompleted)
                return;

            NetworkSession session = sessionTask.Result;    // throws if hosting or joining failed
            sessionTask = null;
            busyText = null;

            LanRace.Enter(new RaceNetSession(session));
            launched = true;

            // pick a car, then wait in the lobby
            RacingGameManager.AddGameScreen(new CarSelection(true));
        }

        void Select()
        {
            message = null;

            if (IsHostEntry(selection))
            {
                busyText = "Starting a race...";
                sessionTask = MultiplayerSessionService.CreateHostSessionAsync();
            }
            else
            {
                AvailableNetworkSession chosen = sessions[selection - 1];
                busyText = "Joining " + MultiplayerSessionService.DisplayName(chosen.HostGamertag) + "'s race...";
                sessionTask = MultiplayerSessionService.JoinSessionAsync(chosen);
            }
        }

        public bool Render()
        {
            if (BaseGame.UsePostScreenShaders)
                BaseGame.UI.PostScreenMenuShader.Start();

            BaseGame.UI.RenderMenuBackground();

            int centerX = BaseGame.Width / 2;
            int y = BaseGame.Height / 5;
            TextureFont.WriteTextCentered(centerX, y, "Multiplayer", Color.White, 1.5f);
            y += BaseGame.YToRes(80);

            // "Host", then a button for each race found (at most MaxVisible show; the list
            // scrolls to keep the selection in view)
            if (selection < firstVisible)
                firstVisible = selection;
            if (selection >= firstVisible + MaxVisible)
                firstVisible = selection - MaxVisible + 1;
            firstVisible = Math.Max(0, Math.Min(firstVisible, Math.Max(0, EntryCount - MaxVisible)));

            clickedEntry = -1;
            int shown = Math.Min(MaxVisible, EntryCount - firstVisible);
            int buttonWidth = BaseGame.XToRes(460);
            int buttonHeight = BaseGame.YToRes(52);
            Rectangle[] rects = new Rectangle[shown];
            string[] labels = new string[shown];
            for (int n = 0; n < shown; n++)
            {
                int i = firstVisible + n;
                rects[n] = new Rectangle(centerX - buttonWidth / 2, y - buttonHeight / 2, buttonWidth, buttonHeight);
                if (IsHostEntry(i))
                    labels[n] = "Host";
                else
                {
                    AvailableNetworkSession s = sessions[i - 1];
                    labels[n] = MultiplayerSessionService.DisplayName(s.HostGamertag) + "'s race  (" + s.CurrentGamerCount + " / " +
                        (s.CurrentGamerCount + s.OpenPublicGamerSlots) + ")";
                }
                y += buttonHeight + BaseGame.YToRes(12);
            }

            // the group indexes the visible buttons: translate the selection to it and back
            buttons.Selection = selection - firstVisible;
            int clickedVisible = buttons.Render(rects, labels, 1.1f, null, sessionTask == null && !launched);
            selection = buttons.Selection + firstVisible;
            if (clickedVisible >= 0 && sessionTask == null && !launched)
                clickedEntry = clickedVisible + firstVisible;

            y += BaseGame.YToRes(25);
            if (busyText != null)
                TextureFont.WriteTextCentered(centerX, y, busyText, Color.LightGreen, 1.0f);
            else if (message != null)
                TextureFont.WriteTextCentered(centerX, y, message, Color.Red, 0.9f);
            else
                TextureFont.WriteTextCentered(centerX, y,
                    findTask != null && sessions.Count == 0 ? "Searching for races..." :
                    sessions.Count == 0 ? "No races found. Host one, or wait." : "",
                    Color.Gray, 0.9f);

            // Shared bottom buttons: Back (left), Select (right)
            bool selectClicked = BaseGame.UI.RenderBottomButtons(false);

            if (sessionTask == null && !launched)
            {
                if (Input.KeyboardUpJustPressed || Input.GamePadUpJustPressed)
                {
                    buttons.Move(-1, EntryCount);
                    selection = buttons.Selection;
                }
                if (Input.KeyboardDownJustPressed || Input.GamePadDownJustPressed)
                {
                    buttons.Move(1, EntryCount);
                    selection = buttons.Selection;
                }

                // a click (or a tap) selects and confirms in one go
                if (clickedEntry >= 0)
                {
                    selection = clickedEntry;
                    Select();
                }
                else if (Input.KeyboardSpaceJustPressed || Input.GamePadAJustPressed ||
                    Input.KeyboardKeyJustPressed(Keys.Enter) || selectClicked)
                    Select();
            }

            if (Input.KeyboardEscapeJustPressed || Input.GamePadBJustPressed || Input.GamePadBackJustPressed ||
                BaseGame.UI.backButtonPressed)
                leave = true;

            return leave && sessionTask == null;
        }
    }
}
