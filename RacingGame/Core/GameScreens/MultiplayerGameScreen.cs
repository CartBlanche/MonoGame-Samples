using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Multiplayer;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// A LAN race. Each machine simulates only its own car with the normal physics; the other
    /// players' cars are puppets (<see cref="RemoteCar"/>) drawn from the states they send.
    ///
    /// Start: every machine loads the track and waits at the start line (the countdown is held)
    /// until the host has heard that everybody is ready and says go.
    /// Rules: first to finish all the laps wins. A player that leaves the track, or the race, is
    /// out (DNF). Each machine reports its own finish reliably, the host passes it on, so every
    /// machine ends up with the same results.
    /// </summary>
    class MultiplayerGameScreen : GameScreen
    {
        // seconds between car states (about 20 per second)
        const double SendInterval = 0.05;

        // lateral distance between two neighbouring start grid slots, in meters
        const float GridSpacing = 3.0f;

        // the host starts the countdown anyway after this long, even if a machine is not ready
        const double ReadyTimeoutSeconds = 20.0;

        struct Result
        {
            public RaceOutcome Outcome;
            public float TimeSeconds;
        }

        struct NameTag
        {
            public Vector3 Position;
            public string Text;
        }

        readonly RaceNetSession net;
        readonly RacePlayerInfo me;
        readonly List<RacePlayerInfo> roster;
        readonly Dictionary<int, RemoteCar> remoteCars = new Dictionary<int, RemoteCar>();
        readonly Dictionary<int, Result> results = new Dictionary<int, Result>();
        readonly HashSet<int> ready = new HashSet<int>();
        readonly List<NameTag> nameTags = new List<NameTag>();
        double nextSendAt;
        double readyDeadline;
        ushort sequence;
        bool started;
        bool reportedFinish;
        bool leftTheRace;
        string message = "";

        public MultiplayerGameScreen(RaceNetSession session, List<RacePlayerInfo> players, RacePlayerInfo localPlayer, int laps)
        {
            net = session;
            me = localPlayer;
            roster = players;

            // the base constructor loaded the track and put the car at the start line: move
            // it sideways to its own slot so the cars do not start inside each other
            int count = players.Count;
            float lane = (me.Slot - (count - 1) / 2.0f) * GridSpacing;
            RacingGameManager.Player.SetCarPosition(
                RacingGameManager.Player.CarPosition + RacingGameManager.Player.CarRight * lane,
                RacingGameManager.Player.CarDirection,
                RacingGameManager.Player.CarUpVector);

            // a LAN race: no single-player highscores, and wait for the host's go
            RacingGameManager.Player.MultiplayerRace = true;
            RacingGameManager.Player.HoldCountdown = true;
            Player.RaceLaps = laps;

            foreach (RacePlayerInfo player in players)
                if (player.Id != me.Id)
                    remoteCars[player.Slot] = new RemoteCar(player);

            net.Register(RacePacketType.CarState, PacketAudience.Any, OnCarState);
            net.Register(RacePacketType.PlayerFinished, PacketAudience.Any, OnPlayerFinished);
            if (net.IsHost)
                net.Register(RacePacketType.Ready, PacketAudience.FromClient, OnReady);
            else
                net.Register(RacePacketType.Go, PacketAudience.FromHost, OnGo);
            net.GamerLeft += OnGamerLeft;
            net.SessionEnded += OnSessionEnded;

            if (net.IsHost)
            {
                ready.Add(me.Slot);
                readyDeadline = Now + ReadyTimeoutSeconds;
            }
            else
            {
                net.SendToHost(new ReadyPacket(), true);
            }
        }

        // no ghost car in multiplayer
        protected override bool ShowGhostCar { get { return false; } }

        double Now { get { return BaseGame.TotalTimeMilliseconds / 1000.0; } }

        int SlotOf(NetworkGamer gamer)
        {
            RacePlayerInfo info = roster.Find(p => p.Id == gamer.GamerId);
            return info == null ? -1 : info.Slot;
        }

        #region Start

        void OnReady(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            int slot = SlotOf(sender);
            if (slot >= 0)
                ready.Add(slot);
        }

        void CheckAllReady()
        {
            if (started || !net.IsHost)
                return;

            if (ready.Count >= roster.Count || Now > readyDeadline)
            {
                net.Broadcast(new GoPacket(), true);
                StartCountdown();
            }
        }

        void OnGo(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            StartCountdown();
        }

        void StartCountdown()
        {
            started = true;
            RacingGameManager.Player.HoldCountdown = false;
        }

        #endregion

        #region In-race menu (Resume | Leave)

        // The race cannot be paused (the other players keep driving), so "Resume" just closes the
        // menu. While it is open the car ignores the controls.
        bool menuOpen;
        int menuSelection;
        bool leaveRequested;

        Rectangle MenuButtonRect
        {
            get
            {
                return new Rectangle(BaseGame.Width - BaseGame.XToRes(140), BaseGame.YToRes(12),
                    BaseGame.XToRes(125), BaseGame.YToRes(44));
            }
        }

        Rectangle MenuItemRect(int index)
        {
            int cx = BaseGame.Width / 2 + (index == 0 ? -1 : 1) * BaseGame.XToRes(150);
            return new Rectangle(cx - BaseGame.XToRes(110), BaseGame.Height / 2 - BaseGame.YToRes(30),
                BaseGame.XToRes(220), BaseGame.YToRes(60));
        }

        void UpdateMenu()
        {
            bool toggle = Input.KeyboardEscapeJustPressed || Input.GamePadBackJustPressed ||
                (menuOpen && Input.GamePadBJustPressed);
            if (!menuOpen && Input.MouseLeftButtonJustPressed && Input.MouseInBox(MenuButtonRect))
                toggle = true;

            if (toggle)
            {
                menuOpen = !menuOpen;
                menuSelection = 0;
            }
            else if (menuOpen)
            {
                if (Input.KeyboardLeftJustPressed || Input.GamePadLeftJustPressed)
                    menuSelection = 0;
                if (Input.KeyboardRightJustPressed || Input.GamePadRightJustPressed)
                    menuSelection = 1;

                int clicked = -1;
                for (int i = 0; i < 2; i++)
                    if (Input.MouseInBox(MenuItemRect(i)))
                    {
                        if (Input.HasMouseMoved)
                            menuSelection = i;
                        if (Input.MouseLeftButtonJustPressed)
                            clicked = i;
                    }

                bool confirm = Input.KeyboardSpaceJustPressed || Input.GamePadAJustPressed ||
                    Input.KeyboardKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Enter);
                if (clicked >= 0)
                {
                    menuSelection = clicked;
                    confirm = true;
                }

                if (confirm)
                {
                    if (menuSelection == 1)
                        leaveRequested = true;
                    menuOpen = false;
                }
            }

            Input.DrivingLocked = menuOpen;
        }

        void RenderMenu()
        {
            if (!menuOpen)
            {
                TextureFont.WriteTextCentered(BaseGame.Width - BaseGame.XToRes(77), BaseGame.YToRes(34),
                    "Menu", Color.White, 0.8f);
                return;
            }

            string[] items = { "Resume", "Leave" };
            for (int i = 0; i < 2; i++)
            {
                Rectangle r = MenuItemRect(i);
                bool selected = i == menuSelection;
                TextureFont.WriteTextCentered(r.X + r.Width / 2, r.Y + r.Height / 2,
                    selected ? "> " + items[i] + " <" : items[i],
                    selected ? Color.Yellow : Color.White, 1.3f);
            }
            TextureFont.WriteTextCentered(BaseGame.Width / 2, BaseGame.Height / 2 + BaseGame.YToRes(60),
                "The race keeps going while this menu is open", Color.Gray, 0.7f);
        }

        #endregion

        public override void Update(GameTime gameTime)
        {
            if (leftTheRace)
                return;

            UpdateMenu();

            CheckAllReady();
            CheckLocalFinish();

            if (Now >= nextSendAt)
            {
                nextSendAt = Now + SendInterval;
                SendCarState();
            }
        }

        #region Sending and receiving

        void SendCarState()
        {
            Player player = RacingGameManager.Player;
            Matrix matrix = player.CarRenderMatrix;

            net.Broadcast(new CarStatePacket
            {
                Slot = me.Slot,
                Sequence = sequence++,
                Position = matrix.Translation,
                Rotation = Quaternion.CreateFromRotationMatrix(matrix),
                WheelPos = player.CarWheelPos,
                Speed = player.Speed,
                Lap = (byte)Math.Max(0, Math.Min(255, player.CurrentLap)),
                Progress = player.TrackProgress,
            }, false);
        }

        void OnCarState(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            CarStatePacket state = CarStatePacket.Deserialize(reader);

            RemoteCar car;
            if (state.Slot == me.Slot || !remoteCars.TryGetValue(state.Slot, out car))
                return;

            car.Add(state, Now);

            // the host passes a client's state on to the other clients
            if (net.IsHost)
                net.Relay(state, false, sender);
        }

        #endregion

        #region Results

        /// <summary>
        /// This machine's own car finished the race or left the track: tell everybody once
        /// </summary>
        void CheckLocalFinish()
        {
            Player player = RacingGameManager.Player;
            if (reportedFinish || !player.GameOver)
                return;

            reportedFinish = true;
            PlayerFinishedPacket packet = new PlayerFinishedPacket
            {
                Slot = me.Slot,
                Outcome = player.Victory ? RaceOutcome.Finished : RaceOutcome.DidNotFinish,
                TimeSeconds = player.Victory ? player.TotalLapTimeSeconds : 0,
            };
            RecordResult(packet);
            net.Broadcast(packet, true);
        }

        void OnPlayerFinished(NetworkGamer sender, System.IO.BinaryReader reader)
        {
            PlayerFinishedPacket packet = PlayerFinishedPacket.Deserialize(reader);

            // a client only tells the host about itself
            if (net.IsHost && SlotOf(sender) != packet.Slot)
                return;

            RecordResult(packet);

            // the host passes it on to the other clients
            if (net.IsHost)
                net.Relay(packet, true, sender);
        }

        void RecordResult(PlayerFinishedPacket packet)
        {
            if (packet.Slot >= roster.Count || results.ContainsKey(packet.Slot))
                return;     // unknown player, or already known (reliable packets can be repeated)

            results[packet.Slot] = new Result { Outcome = packet.Outcome, TimeSeconds = packet.TimeSeconds };
        }

        bool IsResolved(int slot)
        {
            return results.ContainsKey(slot);
        }

        bool AllResolved
        {
            get { return results.Count >= roster.Count; }
        }

        void OnGamerLeft(NetworkGamer gamer)
        {
            int slot = SlotOf(gamer);
            if (slot < 0)
                return;

            RemoteCar car;
            if (remoteCars.TryGetValue(slot, out car))
            {
                message = car.Info.Name + " left the race";
                remoteCars.Remove(slot);
            }

            // a player that has gone is out. The host says so for the others, who cannot see it.
            if (!IsResolved(slot))
            {
                PlayerFinishedPacket packet = new PlayerFinishedPacket { Slot = (byte)slot, Outcome = RaceOutcome.DidNotFinish };
                RecordResult(packet);
                if (net.IsHost)
                    net.Broadcast(packet, true);
            }
        }

        void OnSessionEnded()
        {
            if (net.IsHost)
            {
                message = "The session ended";
                return;
            }

            // the host has gone: nobody else can be heard from, so the others are out
            message = "The host left the race";
            foreach (RacePlayerInfo player in roster)
                if (player.Slot != me.Slot)
                    RecordResult(new PlayerFinishedPacket { Slot = player.Slot, Outcome = RaceOutcome.DidNotFinish });
            remoteCars.Clear();
        }

        /// <summary>
        /// Everybody's place, best first: those that finished (by time), then those still racing
        /// (by how far they have got), then those that are out
        /// </summary>
        List<int> Standings()
        {
            Player player = RacingGameManager.Player;
            List<int> slots = new List<int>();
            foreach (RacePlayerInfo info in roster)
                slots.Add(info.Slot);

            Func<int, double[]> key = slot =>
            {
                Result result;
                if (results.TryGetValue(slot, out result))
                {
                    if (result.Outcome == RaceOutcome.Finished)
                        return new double[] { 0, result.TimeSeconds, slot };
                    return new double[] { 2, 0, slot };
                }

                int lap;
                float progress;
                RemoteCar car;
                if (slot == me.Slot)
                {
                    lap = player.CurrentLap;
                    progress = player.TrackProgress;
                }
                else if (remoteCars.TryGetValue(slot, out car))
                {
                    lap = car.Lap;
                    progress = car.Progress;
                }
                else
                {
                    return new double[] { 2, 0, slot };
                }
                // further along is better, so negate it
                return new double[] { 1, -(lap * 1000000.0 + progress), slot };
            };

            slots.Sort((a, b) =>
            {
                double[] ka = key(a), kb = key(b);
                for (int i = 0; i < 3; i++)
                {
                    int c = ka[i].CompareTo(kb[i]);
                    if (c != 0)
                        return c;
                }
                return 0;
            });
            return slots;
        }

        string NameOf(int slot)
        {
            RacePlayerInfo info = roster.Find(p => p.Slot == slot);
            return info == null ? "?" : info.Name;
        }

        internal static string Ordinal(int n)
        {
            if (n % 100 >= 11 && n % 100 <= 13)
                return n + "th";
            switch (n % 10)
            {
                case 1: return n + "st";
                case 2: return n + "nd";
                case 3: return n + "rd";
                default: return n + "th";
            }
        }

        static string FormatTime(float seconds)
        {
            int total = (int)(seconds * 100);
            return (total / 6000).ToString("00") + ":" + ((total / 100) % 60).ToString("00") + "." + (total % 100).ToString("00");
        }

        #endregion

        #region Rendering

        protected override void RenderOtherCars()
        {
            nameTags.Clear();

            foreach (RemoteCar car in remoteCars.Values)
            {
                Matrix matrix;
                float wheelPos;
                if (!car.Sample(Now, out matrix, out wheelPos))
                    continue;

                int color = car.Info.Color;
                RacingGameManager.CarModel.RenderCar(
                    car.Info.Car,
                    RacingGameManager.CarColors[color % RacingGameManager.CarColors.Count],
                    false, matrix, wheelPos, color != 0);

                string text = car.Info.Name;
                Result result;
                if (results.TryGetValue(car.Info.Slot, out result))
                    text += result.Outcome == RaceOutcome.Finished ? " (finished)" : " (out)";
                nameTags.Add(new NameTag { Position = matrix.Translation, Text = text });
            }
        }

        public override bool Render()
        {
            bool exit = base.Render();

            int centerX = BaseGame.Width / 2;
            int top = BaseGame.Height / 8;

            // name tags above the other cars
            Viewport viewport = BaseGame.Device.Viewport;
            foreach (NameTag tag in nameTags)
            {
                Vector3 screen = viewport.Project(tag.Position + new Vector3(0, 0, 2.2f),
                    BaseGame.ProjectionMatrix, BaseGame.ViewMatrix, Matrix.Identity);
                if (screen.Z > 0 && screen.Z < 1 &&
                    screen.X > 0 && screen.X < BaseGame.Width && screen.Y > 0 && screen.Y < BaseGame.Height)
                    TextureFont.WriteTextCentered((int)screen.X, (int)screen.Y, tag.Text, Color.White, 0.7f);
            }

            if (!started)
            {
                TextureFont.WriteTextCentered(centerX, top, "Waiting for the other players...", Color.Yellow, 1.0f);
            }
            else if (AllResolved)
            {
                RenderResults(centerX, top);
            }
            else if (IsResolved(me.Slot))
            {
                Result mine = results[me.Slot];
                LanRace.LocalPlace = mine.Outcome == RaceOutcome.Finished
                    ? Standings().IndexOf(me.Slot) + 1 : 0;
                TextureFont.WriteTextCentered(centerX, top,
                    mine.Outcome == RaceOutcome.Finished
                        ? "Finished!  " + FormatTime(mine.TimeSeconds)
                        : "DNF",
                    mine.Outcome == RaceOutcome.Finished ? Color.LightGreen : Color.Red, 1.25f);
                TextureFont.WriteTextCentered(centerX, top + BaseGame.YToRes(45),
                    "Waiting for the other players to finish...", Color.White, 0.8f);
            }
            else
            {
                List<int> standings = Standings();
                TextureFont.WriteTextCentered(centerX, top / 2,
                    Ordinal(standings.IndexOf(me.Slot) + 1) + " of " + roster.Count, Color.White, 1.0f);
            }

            RenderMenu();

            if (message.Length > 0)
                TextureFont.WriteTextCentered(centerX, BaseGame.Height - BaseGame.YToRes(110),
                    message, Color.Yellow, 0.9f);

            if (exit && !leftTheRace)
                LeaveRace();

            return exit;
        }

        void RenderResults(int centerX, int top)
        {
            TextureFont.WriteTextCentered(centerX, top, "Race results", Color.White, 1.25f);

            List<int> standings = Standings();
            int y = top + BaseGame.YToRes(55);
            for (int i = 0; i < standings.Count; i++)
            {
                int slot = standings[i];
                Result result = results[slot];
                string line = result.Outcome == RaceOutcome.Finished
                    ? Ordinal(i + 1) + "  " + NameOf(slot) + "  " + FormatTime(result.TimeSeconds)
                    : "--  " + NameOf(slot) + "  DNF";
                TextureFont.WriteTextCentered(centerX, y, line,
                    slot == me.Slot ? Color.LightGreen : Color.White, 1.0f);
                y += BaseGame.YToRes(40);
            }

            TextureFont.WriteTextCentered(centerX, y + BaseGame.YToRes(20),
                "Press Space to leave", Color.Gray, 0.8f);
        }

        #endregion

        protected override bool WantsToLeave()
        {
            return leaveRequested ||
                (AllResolved && !menuOpen &&
                (Input.KeyboardSpaceJustPressed || Input.GamePadAJustPressed || Input.MouseLeftButtonJustPressed));
        }

        void LeaveRace()
        {
            leftTheRace = true;
            LanRace.LocalPlace = 0;
            Input.DrivingLocked = false;
            RacingGameManager.Player.MultiplayerRace = false;
            RacingGameManager.Player.HoldCountdown = false;
            Player.RaceLaps = Player.LapCount;
            LanRace.Leave(null);
        }
    }
}
