using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Multiplayer;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// The lobby of a LAN race: the players with their cars and ready states, and the track and
    /// laps. Everybody toggles ready with Enter. The host chooses the track and laps, and starts
    /// the race with Space once at least two players are in and all are ready (as in ShipGame).
    /// </summary>
    class LanLobbyScreen : IGameScreen
    {
        bool raceStarted;
        bool leave;
        string message = "";

        public void Update(GameTime gameTime)
        {
            if (raceStarted || leave)
                return;

            RaceLobby lobby = LanRace.Lobby;
            if (lobby == null || LanRace.Net.HasEnded)
            {
                LanRace.Leave(LanRace.Net != null && LanRace.Net.IsHost ? "The race session ended." : "The host left the race.");
                leave = true;
                return;
            }

            if (lobby.PendingStart != null)
                StartRace(lobby.PendingStart);
        }

        void StartRace(RaceStartPacket start)
        {
            RacePlayerInfo me = start.Players.Find(p => p.Id == LanRace.Net.LocalGamer.GamerId);
            if (me == null)
            {
                LanRace.Leave("The race started without you.");
                leave = true;
                return;
            }

            // the race screen takes the session over from the lobby
            LanRace.Lobby.Detach();
            TrackSelection.SelectTrackNumber(start.Track);
            raceStarted = true;
            RacingGameManager.AddGameScreen(new MultiplayerGameScreen(LanRace.Net, start.Players, me, start.Laps));
        }

        // where the car thumbnails go this frame (filled by Render, drawn by PostUIRender)
        struct Thumbnail
        {
            public Rectangle Rect;
            public RacePlayerInfo Player;
        }

        readonly System.Collections.Generic.List<Thumbnail> thumbnails = new System.Collections.Generic.List<Thumbnail>();

        static Rectangle Box(int x, int y, int w, int h)
        {
            return new Rectangle(BaseGame.XToRes(x), BaseGame.YToRes(y), BaseGame.XToRes(w), BaseGame.YToRes(h));
        }

        /// <summary>
        /// A small slowly rotating 3D car beside each player's name. Called after the 2D user
        /// interface has been drawn, with the car selection screen's way of drawing cars.
        /// </summary>
        public void PostUIRender()
        {
            if (thumbnails.Count == 0)
                return;

            Viewport oldViewport = BaseGame.Device.Viewport;
            Matrix oldView = BaseGame.ViewMatrix;
            Matrix oldProjection = BaseGame.ProjectionMatrix;

            BaseGame.ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1.0f, 0.5f, 100.0f);
            BaseGame.ViewMatrix = Matrix.CreateLookAt(
                new Vector3(0, 3.95f, 1.9f), new Vector3(0, 0, 0.35f), new Vector3(0, 0, 1));
            BaseGame.LightDirection = new Vector3(0.3f, 0.6f, -0.8f);

            foreach (Thumbnail thumb in thumbnails)
            {
                BaseGame.Device.Viewport = new Viewport(thumb.Rect.X, thumb.Rect.Y, thumb.Rect.Width, thumb.Rect.Height);
                Matrix matrix = Matrix.CreateRotationZ(BaseGame.TotalTime / 1.8f + thumb.Player.Slot);
                int color = thumb.Player.Color;
                RacingGameManager.CarModel.RenderCar(
                    thumb.Player.Car % 3,
                    RacingGameManager.CarColors[color % RacingGameManager.CarColors.Count],
                    false, matrix, 0, color != 0);
                BaseGame.MeshRenderManager.Render();
            }

            BaseGame.Device.Viewport = oldViewport;
            BaseGame.ViewMatrix = oldView;
            BaseGame.ProjectionMatrix = oldProjection;
            BaseGame.WorldMatrix = Matrix.Identity;
        }

        public bool Render()
        {
            // the race is over (its screen has gone): so is the lobby
            if (raceStarted)
                return true;

            if (leave)
                return true;

            RaceLobby lobby = LanRace.Lobby;
            if (lobby == null)
                return true;

            if (BaseGame.UsePostScreenShaders)
                BaseGame.UI.PostScreenMenuShader.Start();

            BaseGame.UI.RenderMenuBackground();

            bool click = Input.MouseLeftButtonJustPressed;
            int centerX = BaseGame.Width / 2;
            TextureFont.WriteTextCentered(centerX, BaseGame.YToRes(45),
                lobby.IsHost ? "Multiplayer: Lobby (host)" : "Multiplayer: Lobby", Color.White, 1.5f);

            // ---- the players: a rotating car, the name and the ready state
            thumbnails.Clear();
            string localId = LanRace.Net.LocalGamer.GamerId;
            bool toggleReady = false;
            int rowY = 100;
            const int RowHeight = 90, Thumb = 80;
            foreach (RacePlayerInfo player in lobby.Players)
            {
                bool local = player.Id == localId;
                Color colour = RacingGameManager.CarColors[player.Color % RacingGameManager.CarColors.Count];

                Rectangle thumb = Box(60, rowY + (RowHeight - Thumb) / 2, Thumb, Thumb);
                thumbnails.Add(new Thumbnail { Rect = thumb, Player = player });

                string name = player.Name + (player.Slot == 0 ? " (host)" : "") + (local ? "  (you)" : "");
                Rectangle row = Box(50, rowY, 540, RowHeight - 8);
                if (local && Input.MouseInBox(row) && click)
                    toggleReady = true;

                TextureFont.WriteText(BaseGame.XToRes(160), BaseGame.YToRes(rowY + 18), name, colour);
                TextureFont.WriteText(BaseGame.XToRes(160), BaseGame.YToRes(rowY + 50),
                    player.Ready ? "READY" : "not ready",
                    player.Ready ? Color.LightGreen : Color.Gray);

                rowY += RowHeight;
            }

            // ---- the track and the laps (the track art is a tall picture: keep it small, below the logo)
            Rectangle trackGfx = TrackSelection.TrackButtonGfxRect(lobby.Track);
            const int TrackH = 235;
            Rectangle trackBox = Box(840, 268, (int)(TrackH * trackGfx.Width / (float)trackGfx.Height), TrackH);
            BaseGame.UI.Buttons.RenderOnScreen(trackBox, trackGfx);

            Rectangle trackLabel = Box(610, 310, 220, 50);
            TextureFont.WriteTextCentered(trackLabel.X + trackLabel.Width / 2, trackLabel.Y + trackLabel.Height / 2,
                "Track: " + (RacingGameManager.Level)lobby.Track, Color.Yellow, 1.0f);
            Rectangle lapsBox = Box(610, 380, 220, 50);
            TextureFont.WriteTextCentered(lapsBox.X + lapsBox.Width / 2, lapsBox.Y + lapsBox.Height / 2,
                "Laps: " + lobby.Laps, Color.Yellow, 1.0f);
            if (lobby.IsHost)
                TextureFont.WriteTextCentered(trackLabel.X + trackLabel.Width / 2, BaseGame.YToRes(440),
                    "click to change", Color.Gray, 0.7f);

            if (lobby.IsHost)
            {
                if (click && (Input.MouseInBox(trackBox) || Input.MouseInBox(trackLabel)))
                    lobby.ChangeTrack(1);
                if (click && Input.MouseInBox(lapsBox))
                    lobby.ChangeLaps(lobby.Laps >= RaceLobby.MaxLaps ? -(RaceLobby.MaxLaps - RaceLobby.MinLaps) : 1);
            }

            // ---- buttons
            Rectangle readyBox = Box(60, 470, 240, 50);
            TextureFont.WriteTextCentered(readyBox.X + readyBox.Width / 2, readyBox.Y + readyBox.Height / 2,
                lobby.LocalReady ? "[ Ready ]" : "[ Not ready ]",
                lobby.LocalReady ? Color.LightGreen : Color.White, 1.2f);
            if (click && Input.MouseInBox(readyBox))
                toggleReady = true;

            bool start = false;
            if (lobby.IsHost)
            {
                Rectangle startBox = Box(340, 470, 240, 50);
                TextureFont.WriteTextCentered(startBox.X + startBox.Width / 2, startBox.Y + startBox.Height / 2,
                    "[ Start race ]", lobby.CanStart ? Color.LightGreen : Color.Gray, 1.2f);
                if (click && Input.MouseInBox(startBox))
                    start = true;
            }

            int y = BaseGame.YToRes(545);
            if (message.Length > 0)
                TextureFont.WriteTextCentered(centerX, y, message, Color.Red, 0.9f);
            else if (lobby.IsHost && lobby.CanStart)
                TextureFont.WriteTextCentered(centerX, y, "Everyone is ready. Start the race!", Color.LightGreen, 0.9f);

            // Shared bottom buttons: Back = leave, Select = ready / not ready
            if (BaseGame.UI.RenderBottomButtons(false))
                toggleReady = true;
            TextureFont.WriteTextCentered(centerX, BaseGame.Height - BaseGame.YToRes(45),
                lobby.IsHost
                    ? "Left/Right: track   Up/Down: laps   Space: start"
                    : "Click your own entry to get ready",
                Color.Gray, 0.7f);

            // ---- keyboard and gamepad
            if (Input.KeyboardKeyJustPressed(Keys.Enter) || Input.GamePadAJustPressed)
                toggleReady = true;

            if (toggleReady)
            {
                message = "";
                lobby.SetLocalReady(!lobby.LocalReady);
            }

            if (lobby.IsHost)
            {
                if (Input.KeyboardLeftJustPressed) lobby.ChangeTrack(-1);
                if (Input.KeyboardRightJustPressed) lobby.ChangeTrack(1);
                if (Input.KeyboardUpJustPressed) lobby.ChangeLaps(1);
                if (Input.KeyboardDownJustPressed) lobby.ChangeLaps(-1);

                if (Input.KeyboardSpaceJustPressed || start)
                {
                    if (!lobby.TryStart())
                        message = lobby.Players.Count < 2
                            ? "Waiting for another player to join."
                            : "Everyone must be ready first.";
                    else
                        message = "";
                }
            }

            if (Input.KeyboardEscapeJustPressed || Input.GamePadBJustPressed || Input.GamePadBackJustPressed ||
                BaseGame.UI.backButtonPressed)
            {
                LanRace.Leave(null);
                return true;
            }

            return false;
        }
    }
}
