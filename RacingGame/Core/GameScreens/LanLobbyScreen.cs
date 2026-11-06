using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Multiplayer;
using RacingGame.Tracks;

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
            if (start.Track == RaceLobby.CustomTrackNumber)
            {
                // a custom track: the one in the Tracks folder or received from the host
                SharedTrack shared = SharedTracks.Resolve(start.CustomId);
                if (shared == null)
                {
                    LanRace.Leave("The race track is missing.");
                    leave = true;
                    return;
                }
                TrackSelection.CustomTrack = shared.Info;
            }
            else
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

        // a text scale at which the text fits the width (never above maxScale)
        static float FitScale(string text, float width, float maxScale)
        {
            int w = TextureFont.GetTextWidth(text);
            return w <= 0 ? maxScale : Math.Min(maxScale, width / w);
        }

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
                    !player.HasTrack ? "getting the track..." : player.Ready ? "READY" : "not ready",
                    !player.HasTrack ? Color.Yellow : player.Ready ? Color.LightGreen : Color.Gray);

                rowY += RowHeight;
            }

            // ---- the track and the laps (the track art is a tall picture: keep it small, below the logo)
            const int TrackH = 175, TrackTop = 262, TrackCenterX = 900;
            Rectangle trackBox;
            if (lobby.IsCustomTrack)
            {
                // the track drawn on a light tile, as in the track selection (the tile's proportions)
                Rectangle tile = TrackSelection.TrackButtonGfxRect(0);
                int trackW = (int)(TrackH * tile.Width / (float)tile.Height);
                trackBox = Box(TrackCenterX - trackW / 2, TrackTop, trackW, TrackH);
                BaseGame.UI.RenderLightPanel(trackBox, Color.White);

                SharedTrack shared = SharedTracks.Get(lobby.CustomTrackId);
                if (shared == null || !TrackPreview.Draw(shared.Info, trackBox,
                    (int)(trackBox.Width * 0.18f), Math.Max(3, trackBox.Width / 20)))
                    TextureFont.WriteTextCentered(trackBox.X + trackBox.Width / 2, trackBox.Y + trackBox.Height / 2,
                        "...", Color.Black, 1.0f, 2.5f);     // (the track has not arrived yet)
            }
            else
            {
                Rectangle trackGfx = TrackSelection.TrackButtonGfxRect(lobby.Track);
                int trackW = (int)(TrackH * trackGfx.Width / (float)trackGfx.Height);
                trackBox = Box(TrackCenterX - trackW / 2, TrackTop, trackW, TrackH);
                BaseGame.UI.Buttons.RenderOnScreen(trackBox, trackGfx);
            }

            Rectangle trackLabel = Box(610, 310, 220, 50);
            string trackText = "Track: " + (lobby.IsCustomTrack ? lobby.CustomTrackName : ((RacingGameManager.Level)lobby.Track).ToString());
            TextureFont.WriteTextCentered(trackLabel.X + trackLabel.Width / 2, trackLabel.Y + trackLabel.Height / 2,
                trackText, Color.Yellow, FitScale(trackText, trackLabel.Width, 1.0f));
            if (lobby.IsCustomTrack && lobby.CustomTrackAuthor.Length > 0)
            {
                string by = "by " + lobby.CustomTrackAuthor;
                TextureFont.WriteTextCentered(trackLabel.X + trackLabel.Width / 2, BaseGame.YToRes(352),
                    by, Color.LightGray, FitScale(by, trackLabel.Width, 0.7f));
            }
            Rectangle lapsBox = Box(610, 380, 220, 50);
            TextureFont.WriteTextCentered(lapsBox.X + lapsBox.Width / 2, lapsBox.Y + lapsBox.Height / 2,
                "Laps: " + lobby.Laps, Color.Yellow, 1.0f);
            if (lobby.IsHost)
                TextureFont.WriteTextCentered(trackLabel.X + trackLabel.Width / 2, BaseGame.YToRes(432),
                    "click to change", Color.Gray, 0.6f);

            if (lobby.IsHost)
            {
                if (click && (Input.MouseInBox(trackBox) || Input.MouseInBox(trackLabel)))
                    lobby.ChangeTrack(1);
                if (click && Input.MouseInBox(lapsBox))
                    lobby.ChangeLaps(lobby.Laps >= RaceLobby.MaxLaps ? -(RaceLobby.MaxLaps - RaceLobby.MinLaps) : 1);
            }

            // ---- buttons: all on one row. Custom is under the track picture.
            const int ButtonRowY = 462, ButtonW = 210, ButtonH = 54;
            if (BaseGame.UI.PanelButton(Box(60, ButtonRowY, ButtonW, ButtonH),
                lobby.LocalReady ? "Ready" : "Not ready", 1.1f, false,
                lobby.LocalReady ? Color.LightGreen : Color.White))
                toggleReady = true;

            bool start = false;
            if (lobby.IsHost)
            {
                if (BaseGame.UI.PanelButton(Box(300, ButtonRowY, ButtonW, ButtonH), "Start Race", 1.1f, false,
                    lobby.CanStart ? Color.LightGreen : Color.Gray))
                    start = true;

                bool pickTrack = BaseGame.UI.PanelButton(
                    Box(TrackCenterX - ButtonW / 2, ButtonRowY, ButtonW, ButtonH), CustomTracks.ButtonLabel, 1.1f);
                if (pickTrack || Input.KeyboardKeyJustPressed(Keys.C) || Input.GamePadYJustPressed)
                {
                    message = "";
                    TrackSelection.OpenCustomTracks(info =>
                    {
                        string problem = lobby.ChooseCustomTrack(info);
                        if (problem != null)
                            message = problem;
                    });
                }
            }

            int y = BaseGame.YToRes(545);
            if (message.Length > 0)
                TextureFont.WriteTextCentered(centerX, y, message, Color.Red, 0.9f);
            else if (!lobby.LocalHasTrack && lobby.TrackStatus != null)
                TextureFont.WriteTextCentered(centerX, y, lobby.TrackStatus, Color.Yellow, 0.9f);
            else if (lobby.IsHost && lobby.CanStart)
                TextureFont.WriteTextCentered(centerX, y, "Everyone is ready. Start the race!", Color.LightGreen, 0.9f);

            // Shared bottom buttons: Back = leave, Select = ready / not ready
            if (BaseGame.UI.RenderBottomButtons(false))
                toggleReady = true;
            TextureFont.WriteTextCentered(centerX, BaseGame.Height - BaseGame.YToRes(45),
                lobby.IsHost
                    ? "Left/Right: track   Up/Down: laps   C: custom track   Space: start"
                    : "Click your own entry to get ready",
                Color.Gray, 0.7f);

            // ---- keyboard and gamepad
            if (Input.KeyboardKeyJustPressed(Keys.Enter) || Input.GamePadAJustPressed)
                toggleReady = true;

            if (toggleReady)
            {
                message = "";
                if (!lobby.LocalHasTrack)
                    message = "Wait for the track to arrive first.";
                else
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
