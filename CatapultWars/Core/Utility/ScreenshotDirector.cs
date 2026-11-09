using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace CatapultGame
{
    /// <summary>
    /// Developer tool for the store page screenshots. Start the game with
    /// <c>--screenshots=&lt;folder&gt;</c> and it plays a scripted round against the AI on its
    /// own, saving frames as PNGs at the game's own resolution, then quits.
    /// <c>--lan=host</c> / <c>--lan=client</c> instead walk two instances into a LAN lobby (host
    /// saves the picture). Nothing here runs in a normal game.
    /// </summary>
    class ScreenshotDirector : DrawableGameComponent
    {
        readonly ScreenManager screens;
        readonly string folder;
        readonly IEnumerator<Func<bool>> script;
        Func<bool> waiting;
        string pendingShot;
        float clock;

        // --clip: saves every Nth frame of one shot (flight, slow-motion zoom, hit) to stitch into a GIF
        bool clip;
        bool play;      // --play: carry on from the lobby into a real LAN match
        bool video;     // --video: the scripted LAN match recorded for the YouTube video (needs --lan)
        bool mute;      // --mute: with --video, keeps this instance silent so two copies do not double up
        bool recording;
        int drawCount, savedFrames;
        const int RecordEvery = 4; // 60 fps / 4 = 15 frames a second

        public static void AddIfRequested(Game game, ScreenManager screens)
        {
            string folder = null, lan = null;
            bool clip = false, play = false, video = false, mute = false;
            string name = null, pos = null;
            float lead = 6f;
            try
            {
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg.Equals("--screenshots", StringComparison.OrdinalIgnoreCase))
                        folder = "screenshots";
                    else if (arg.StartsWith("--screenshots=", StringComparison.OrdinalIgnoreCase))
                        folder = arg.Substring("--screenshots=".Length);
                    else if (arg.Equals("--clip", StringComparison.OrdinalIgnoreCase))
                        clip = true;
                    else if (arg.Equals("--play", StringComparison.OrdinalIgnoreCase))
                        play = true;
                    else if (arg.Equals("--video", StringComparison.OrdinalIgnoreCase))
                        video = true;
                    else if (arg.Equals("--mute", StringComparison.OrdinalIgnoreCase))
                        mute = true;
                    else if (arg.StartsWith("--lead=", StringComparison.OrdinalIgnoreCase))
                        float.TryParse(arg.Substring("--lead=".Length), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out lead);
                    else if (arg.StartsWith("--pos=", StringComparison.OrdinalIgnoreCase))
                        pos = arg.Substring("--pos=".Length);
                    else if (arg.StartsWith("--name=", StringComparison.OrdinalIgnoreCase))
                        name = arg.Substring("--name=".Length);
                    else if (arg.StartsWith("--lan=", StringComparison.OrdinalIgnoreCase))
                        lan = arg.Substring("--lan=".Length).ToLowerInvariant();
                }
            }
            catch (Exception)
            {
            }

#if !SINGLE_PLAYER
            // --name="Some Name" works on its own: handy for starting a second copy as another player
            if (!string.IsNullOrEmpty(name))
                SetLocalName(name);
#endif

            if (folder == null && video)
                folder = "video";
            if (folder == null)
                return;

            game.Components.Add(new ScreenshotDirector(game, screens, folder, lan, clip, play, video, mute, lead, pos));
        }

        ScreenshotDirector(Game game, ScreenManager screens, string folder, string lan, bool clip, bool play, bool video, bool mute, float lead, string pos)
            : base(game)
        {
#if !SINGLE_PLAYER
            leadIn = lead;
            string[] xy = pos == null ? null : pos.Split(',');
            int px, py;
            if (xy != null && xy.Length == 2 && int.TryParse(xy[0], out px) && int.TryParse(xy[1], out py))
                windowPos = new Point(px, py);
#endif
            this.clip = clip;
            this.play = play;
            this.video = video;
            this.mute = mute;
            this.screens = screens;
            this.folder = Path.GetFullPath(folder);
            Directory.CreateDirectory(this.folder);

            UpdateOrder = 1000;
            DrawOrder = 1000; // after everything else, so the frame is complete

            // Quiet while it plays itself. The video wants the sound, unless this copy is muted.
            if (!video || mute)
            {
                SoundEffect.MasterVolume = 0f;
                try { MediaPlayer.Volume = 0f; } catch (Exception) { }
            }

#if !SINGLE_PLAYER
            if (lan == "host" || lan == "client")
            {
                if (video)
                {
                    cueFile = Path.Combine(this.folder, "cues-" + lan + ".csv");
                    File.WriteAllText(cueFile, "seconds,event\n0.00,clock started at unix ms " +
                        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "\n");
                    cueClock = System.Diagnostics.Stopwatch.StartNew();
                    script = VideoScript(lan == "host");
                }
                else
                    script = LanScript(lan == "host");
                return;
            }
#endif
            script = RoundScript();
        }

        public override void Update(GameTime gameTime)
        {
            clock += (float)gameTime.ElapsedGameTime.TotalSeconds;
#if !SINGLE_PLAYER
            if (video)
                WatchCues();
#endif

            if (waiting == null || waiting())
            {
                if (!script.MoveNext())
                {
                    Game.Exit();
                    return;
                }
                waiting = script.Current;
            }
        }

        public override void Draw(GameTime gameTime)
        {
#if !SINGLE_PLAYER
            if (video)
                DrawCursor(gameTime);
#endif
            if (recording && drawCount++ % RecordEvery == 0)
            {
                pendingShot = "frame" + (savedFrames++).ToString("D4");
            }

            if (pendingShot == null)
                return;

            var device = GraphicsDevice;
            int w = device.PresentationParameters.BackBufferWidth;
            int h = device.PresentationParameters.BackBufferHeight;
            var data = new Color[w * h];
            device.GetBackBufferData(data);
            for (int i = 0; i < data.Length; i++)
                data[i].A = 255;

            using (var texture = new Texture2D(device, w, h))
            {
                texture.SetData(data);
                string path = Path.Combine(folder, pendingShot + ".png");
                using (var stream = File.Create(path))
                    texture.SaveAsPng(stream, w, h);
                Console.WriteLine("Saved " + path);
            }
            pendingShot = null;
        }

        // ---- script helpers -------------------------------------------------

        Func<bool> Wait(float seconds)
        {
            float end = clock + seconds;
            return () => clock >= end;
        }

        // Waits for the condition, but never longer than the timeout
        Func<bool> Until(Func<bool> condition, float timeout = 60f)
        {
            float end = clock + timeout;
            return () => condition() || clock >= end;
        }

        Func<bool> Capture(string name)
        {
            pendingShot = name;
            return () => pendingShot == null;
        }

        GameplayScreen Gameplay()
        {
            return screens.GetScreens().OfType<GameplayScreen>().FirstOrDefault();
        }

        // ---- a round against the AI ----------------------------------------

        IEnumerator<Func<bool>> RoundScript()
        {
            yield return Wait(1.5f);

            // Instructions screen
            MatchSettings.WinScore = MatchSettings.DefaultWinScore;
            var instructions = new InstructionsScreen();
            screens.AddScreen(instructions, null);
            yield return Wait(0.6f);
            if (!clip)
                yield return Capture("instructions");

            instructions.ExitScreen();
            screens.AddScreen(new GameplayScreen(), null);

            bool slowMoDone = false, hitDone = false;
            for (int shot = 0; shot < 40; shot++)
            {
                GameplayScreen gp = null;
                yield return Until(() =>
                {
                    gp = Gameplay();
                    return gp != null && gp.ScreenState == ScreenState.Active &&
                        (gp.IsGameOver || (gp.IsPlayerOneTurn && gp.PlayerOne.IsActive &&
                            gp.PlayerOne.Catapult.CurrentState == CatapultState.Idle));
                });
                yield return Wait(0.5f);
                gp = Gameplay();

                if (gp.IsGameOver)
                {
                    yield return Wait(1.2f);
                    yield return Capture("victory");
                    yield break;
                }

                Human me = gp.PlayerOne;
                Catapult c = me.Catapult;
                float enemyX = c.EnemyCentre.X;

                // Shot 0 lands on the hill, shot 1 falls short of the enemy, then every shot is a hit
                float target = shot == 0 ? Battlefield.HillCentre : shot == 1 ? enemyX - 230f : enemyX;
                float angle, speed;
                Solve(c, target, shot == 0 ? 50f : shot == 1 ? 40f : 58f, out speed, out angle);
                float strength = (speed - Player.MinShotStrength) / (Player.MaxShotStrength - Player.MinShotStrength);

                // Hold the pull for a moment, so the arrow and readouts can be seen
                float until = clock + (shot == 1 ? 1.2f : 0.5f);
                bool captured = false;
                while (clock < until)
                {
                    me.PoseDrag(angle, strength);
                    gp.SetDragging(true);
                    if (shot == 1 && !captured && clock > until - 0.6f)
                    {
                        captured = true;
                        pendingShot = "aiming";
                    }
                    yield return () => true;
                }
                me.PoseDrag(angle, strength);
                yield return () => pendingShot == null;
                if (clip && shot == 2)
                    recording = true;
                me.ReleaseDrag();
                gp.SetDragging(false);

                if (clip && shot == 2)
                {
                    // Flight, the slow-motion zoom and the hit, then a moment of the aftermath
                    yield return Until(() => gp.PlayerTwo.Catapult.CurrentState == CatapultState.Hit, 10f);
                    yield return Wait(1.1f);
                    recording = false;
                    yield break;
                }

                if (shot == 1)
                {
                    // The AI's turn: catch the red catapult mid-pull, once its arrow has grown
                    yield return Until(() => gp.PlayerTwo.isDragging, 20f);
                    yield return Wait(0.7f);
                    yield return Capture("aiming-red");
                }

                if (shot >= 2 && !slowMoDone)
                {
                    yield return Until(() => gp.InSlowMotion, 8f);
                    yield return Wait(0.4f);
                    yield return Capture("slowmo");
                    slowMoDone = true;
                }
                else if (shot >= 3 && !hitDone)
                {
                    yield return Until(() => gp.PlayerTwo.Catapult.CurrentState == CatapultState.Hit, 8f);
                    yield return Wait(0.8f);
                    yield return Capture("hit");
                    hitDone = true;
                }

                yield return Wait(0.5f);
            }
        }

        /// <summary>
        /// Tries angles around the preferred one and takes the speed whose shot, played forward on
        /// paper with the current wind and the hill in the way, lands closest to targetX.
        /// </summary>
        static void Solve(Catapult c, float targetX, float preferredDegrees, out float bestSpeed, out float bestAngle)
        {
            bestSpeed = Player.MinShotStrength;
            bestAngle = MathHelper.ToRadians(preferredDegrees);
            float bestError = float.MaxValue;

            for (int offset = 0; offset <= 20 && bestError > 3f; offset++)
            {
                foreach (int sign in new[] { 1, -1 })
                {
                    float angle = MathHelper.ToRadians(preferredDegrees + sign * offset);
                    if (angle < Ballistics.MinAngle || angle > Ballistics.MaxAngle)
                        continue;
                    for (float v = Player.MinShotStrength; v <= Player.MaxShotStrength; v += 0.25f)
                    {
                        float landing = Ballistics.LandingX(c.ProjectileStart, c.FireDirection, v, angle, c.Wind);
                        float error = Math.Abs(landing - targetX);
                        if (error < bestError)
                        {
                            bestError = error;
                            bestSpeed = v;
                            bestAngle = angle;
                        }
                    }
                }
            }
        }

#if !SINGLE_PLAYER
        // The name the network layer uses for this machine's player. Its setter is internal to the
        // library, so this reaches it by reflection. Until there is a name field in Settings, this is
        // the only way to play as someone other than the OS user.
        static void SetLocalName(string name)
        {
            try
            {
                var gamer = Microsoft.Xna.Framework.GamerServices.SignedInGamer.Current;
                typeof(Microsoft.Xna.Framework.GamerServices.SignedInGamer)
                    .GetMethod("SetGamertag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(gamer, new object[] { name });
            }
            catch (Exception)
            {
            }
        }
#endif

#if !SINGLE_PLAYER
        // ---- two instances meet in a LAN lobby ------------------------------

        IEnumerator<Func<bool>> LanScript(bool host)
        {
            yield return Wait(1.5f);

            float lobbySince = -1f;
            float deadline = clock + 90f;
            while (clock < deadline)
            {
                var top = screens.GetScreens().Last();
                var lobby = screens.GetScreens().OfType<LobbyScreen>().FirstOrDefault();

                if (lobby != null)
                {
                    var session = lobby.Session;
                    if (!host && session != null && session.LocalGamers.Count > 0)
                        session.LocalGamers[0].IsReady = true;

                    if (host && session != null && session.AllGamers.Count >= 2)
                    {
                        if (lobbySince < 0)
                            lobbySince = clock;
                        if (clock - lobbySince > 3f)
                        {
                            yield return Capture("lan-lobby");
                            if (!play)
                            {
                                yield return Wait(1f);
                                yield break;
                            }

                            // Ready up, which starts the match
                            session.LocalGamers[0].IsReady = true;
                            foreach (var step in LanMatch(true))
                                yield return step;
                            yield break;
                        }
                    }
                    else if (!host && session != null && session.AllGamers.Count >= 2)
                    {
                        if (lobbySince < 0)
                            lobbySince = clock;
                        if (play)
                        {
                            foreach (var step in LanMatch(false))
                                yield return step;
                            yield break;
                        }
                        if (clock - lobbySince > 12f)
                            yield break;
                    }
                }
                else
                {
                    Select(top, host);
                }

                yield return Wait(0.8f);
            }
        }

        // The match itself: the host (blue, left) fires first, then the guest (red, right) pulls back
        // and fires. Each side saves what it sees while the red catapult is being pulled.
        IEnumerable<Func<bool>> LanMatch(bool host)
        {
            GameplayScreen gp = null;
            yield return Until(() =>
            {
                gp = Gameplay();
                return gp != null && gp.ScreenState == ScreenState.Active;
            }, 60f);
            if (gp == null)
                yield break;

            if (host)
            {
                yield return Until(() => gp.IsPlayerOneTurn && gp.PlayerOne.IsActive &&
                    gp.PlayerOne.Catapult.CurrentState == CatapultState.Idle, 40f);
                yield return Wait(0.6f);

                Catapult c = gp.PlayerOne.Catapult;
                float angle, speed;
                Solve(c, c.EnemyCentre.X - 230f, 40f, out speed, out angle);
                float strength = (speed - Player.MinShotStrength) / (Player.MaxShotStrength - Player.MinShotStrength);
                float until = clock + 0.8f;
                while (clock < until)
                {
                    gp.PlayerOne.PoseDrag(angle, strength);
                    gp.SetDragging(true);
                    yield return () => true;
                }
                gp.PlayerOne.ReleaseDrag();
                gp.SetDragging(false);

                // The guest takes their turn: this is what the host sees of it
                yield return Until(() => gp.PlayerTwo.isDragging, 45f);
                yield return Wait(1.4f);
                yield return Capture("lan-host-view");
                yield return Wait(7f);
            }
            else
            {
                yield return Until(() => !gp.IsPlayerOneTurn && gp.PlayerTwo.IsActive &&
                    gp.PlayerTwo.Catapult.CurrentState == CatapultState.Idle, 60f);
                yield return Wait(0.6f);

                Catapult c = gp.PlayerTwo.Catapult;
                float angle, speed;
                Solve(c, c.EnemyCentre.X + 230f, 40f, out speed, out angle);
                float strength = (speed - Player.MinShotStrength) / (Player.MaxShotStrength - Player.MinShotStrength);
                float start = clock;
                bool captured = false;
                while (clock < start + 3.4f)
                {
                    gp.PlayerTwo.PoseDrag(angle, strength);
                    gp.SetDragging(true);
                    if (!captured && clock > start + 1.6f)
                    {
                        captured = true;
                        pendingShot = "lan-client-view";
                    }
                    yield return () => true;
                }
                yield return () => pendingShot == null;
                gp.PlayerTwo.ReleaseDrag();
                gp.SetDragging(false);
                yield return Wait(7f);
            }
        }

        // ---- the YouTube video ----------------------------------------------
        // Each theme is recorded as its own run of the same match (same --seed, same plans), so the
        // runs can be cut together in post. Dean (the guest) wins; Dominique lands two hits.
        // A plan entry is: 0 = a hit, HillShot = into the hill, anything else = pixels short of the enemy.
        const float HillShot = -1f;
        static readonly float[] HostPlan = { HillShot, 230f, 0f, 170f, 0f, 200f, 120f };
        static readonly float[] GuestPlan = { 230f, 0f, 0f, 170f, 0f, 0f, 0f };

        float leadIn = 6f;       // --lead=N: seconds on the main menu before it starts, to press record
        Point? windowPos;        // --pos=x,y: where the window goes, so two copies sit side by side
        bool windowPlaced;

        // Cue log: what happened when, in wall-clock seconds since the script started, for the edit
        string cueFile;
        System.Diagnostics.Stopwatch cueClock;
        CatapultState lastStateOne, lastStateTwo;
        int lastScoreOne, lastScoreTwo;
        bool lastGameOver;

        void Cue(string text)
        {
            if (cueFile == null)
                return;
            try
            {
                File.AppendAllText(cueFile, cueClock.Elapsed.TotalSeconds.ToString("F2",
                    System.Globalization.CultureInfo.InvariantCulture) + "," + text + "\n");
            }
            catch (Exception)
            {
            }
        }

        void WatchCues()
        {
            if (!windowPlaced && windowPos.HasValue)
            {
                windowPlaced = true;
                try { Game.Window.Position = windowPos.Value; } catch (Exception) { }
            }

            var gp = Gameplay();
            if (gp == null || gp.PlayerOne == null || gp.PlayerTwo == null)
                return;

            Watch(gp.PlayerOne, ref lastStateOne, ref lastScoreOne);
            Watch(gp.PlayerTwo, ref lastStateTwo, ref lastScoreTwo);
            if (gp.IsGameOver != lastGameOver)
            {
                lastGameOver = gp.IsGameOver;
                if (lastGameOver)
                    Cue("game over");
            }
        }

        void Watch(Human p, ref CatapultState last, ref int lastScore)
        {
            if (p.Catapult.CurrentState != last)
            {
                last = p.Catapult.CurrentState;
                Cue(p.Name + " " + last);
            }
            if (p.Score != lastScore)
            {
                lastScore = p.Score;
                Cue(p.Name + " score " + lastScore);
            }
        }

        // -- the on-screen mouse cursor, so the menu walk looks like someone clicking through it

        SpriteBatch cursorBatch;
        Texture2D cursorTexture;
        Vector2 cursor = new Vector2(430f, 455f);
        bool cursorVisible;
        float rippleAt = -10f;

        void DrawCursor(GameTime gameTime)
        {
            if (!cursorVisible)
                return;

            if (cursorBatch == null)
            {
                cursorBatch = new SpriteBatch(GraphicsDevice);
                Vector2[] arrow = { new Vector2(0, 0), new Vector2(0, 17), new Vector2(4, 13), new Vector2(7, 19.5f),
                    new Vector2(9.6f, 18.4f), new Vector2(6.6f, 12.2f), new Vector2(12, 12) };
                var layers = new List<(ShapeArt.Shape, Color)>();
                foreach (Vector2 o in new[] { new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1),
                    new Vector2(-1, -1), new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1) })
                    layers.Add((ShapeArt.Poly(Offset(arrow, o + Vector2.One)), Color.Black));
                layers.Add((ShapeArt.Poly(Offset(arrow, Vector2.One)), Color.White));
                cursorTexture = ShapeArt.Render(GraphicsDevice, 16, 24, layers.ToArray());
            }

            cursorBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                null, null, null, screens.GlobalTransformation);

            float t = clock - rippleAt;
            if (t >= 0f && t < .45f)
            {
                float k = t / .45f;
                Texture2D disc = ThemeArt.Get(GraphicsDevice).SoftDisc;
                float size = 16f + 48f * k;
                cursorBatch.Draw(disc, cursor, null, Color.White * (.7f * (1f - k)), 0f,
                    new Vector2(disc.Width / 2f, disc.Height / 2f), size / disc.Width, SpriteEffects.None, 0f);
            }
            cursorBatch.Draw(cursorTexture, cursor - Vector2.One, Color.White);
            cursorBatch.End();
        }

        static Vector2[] Offset(Vector2[] points, Vector2 by)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++)
                result[i] = points[i] + by;
            return result;
        }

        IEnumerable<Func<bool>> MoveCursor(Vector2 to, float seconds)
        {
            cursorVisible = true;
            Vector2 from = cursor;
            float start = clock;
            while (clock < start + seconds)
            {
                float k = (clock - start) / seconds;
                k = k * k * (3f - 2f * k);
                cursor = Vector2.Lerp(from, to, k);
                yield return () => true;
            }
            cursor = to;
        }

        IEnumerable<Func<bool>> Click(Vector2 at)
        {
            foreach (var step in MoveCursor(at, 0.9f))
                yield return step;
            yield return Wait(0.25f);
            rippleAt = clock;
            yield return Wait(0.2f);
        }

        // Where an entry's text sits, in the 800x480 the game draws in
        static bool EntryCentre(GameScreen screen, int entry, out Vector2 centre)
        {
            centre = Vector2.Zero;
            var menu = screen as MenuScreen;
            if (menu == null)
                return false;
            var list = typeof(MenuScreen).GetField("menuEntries",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(menu) as System.Collections.IList;
            if (list == null || entry >= list.Count)
                return false;
            var e = (MenuEntry)list[entry];
            centre = e.Position + new Vector2(e.GetWidth(menu) / 2f, e.GetHeight(menu) / 2f + 4f);
            return true;
        }

        static Vector2 ReadyButtonCentre()
        {
            Rectangle r = new Rectangle(470, 412, 150, 52);
            try
            {
                var field = typeof(LobbyScreen).GetField("readyButton",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (field != null)
                    r = (Rectangle)field.GetValue(null);
            }
            catch (Exception)
            {
            }
            return new Vector2(r.Center.X, r.Center.Y + 4f);
        }

        // Main menu to the lobby with a visible cursor, then the match, then the victory screen
        IEnumerator<Func<bool>> VideoScript(bool host)
        {
            yield return Wait(leadIn);
            Cue("start");

            float deadline = clock + 150f;
            while (clock < deadline)
            {
                var top = screens.GetScreens().Last();
                var lobby = screens.GetScreens().OfType<LobbyScreen>().FirstOrDefault();

                if (lobby != null)
                {
                    foreach (var step in VideoLobby(lobby))
                        yield return step;
                    foreach (var step in VideoMatch(host))
                        yield return step;
                    yield break;
                }

                int entry = EntryFor(top, host);
                Vector2 at;
                if (entry >= 0 && top.ScreenState == ScreenState.Active && EntryCentre(top, entry, out at))
                {
                    yield return Wait(0.4f);
                    foreach (var step in Click(at))
                        yield return step;
                    Cue("click " + top.GetType().Name + " entry " + entry);
                    Press(top, entry);
                    yield return Wait(1.2f);
                    continue;
                }
                yield return Wait(0.3f);
            }
        }

        // Waits for both players to be in, lets the names be read, then clicks Ready
        IEnumerable<Func<bool>> VideoLobby(LobbyScreen lobby)
        {
            yield return Until(() => lobby.Session != null && lobby.Session.AllGamers.Count >= 2 &&
                lobby.ScreenState == ScreenState.Active, 150f);
            Cue("both in the lobby");
            yield return Wait(2.5f);

            foreach (var step in Click(ReadyButtonCentre()))
                yield return step;
            Cue("ready");
            if (lobby.Session != null)
            {
                foreach (var gamer in lobby.Session.LocalGamers)
                    gamer.IsReady = true;
            }
            yield return Wait(0.4f);
            cursorVisible = false;
        }

        IEnumerable<Func<bool>> VideoMatch(bool host)
        {
            GameplayScreen gp = null;
            yield return Until(() =>
            {
                gp = Gameplay();
                return gp != null && gp.ScreenState == ScreenState.Active && gp.PlayerOne != null;
            }, 90f);
            if (gp == null)
                yield break;

            Human me = host ? gp.PlayerOne : gp.PlayerTwo;
            Cue("match start; battlefield seed " + Battlefield.Seed + ", hill " + Battlefield.HillCentre +
                ", wind " + me.Catapult.Wind + ", enemy x " + me.Catapult.EnemyCentre.X);
            float[] plan = host ? HostPlan : GuestPlan;

            for (int shot = 0; ; shot++)
            {
                yield return Until(() => gp.IsGameOver || (host == gp.IsPlayerOneTurn && me.IsActive &&
                    me.Catapult.CurrentState == CatapultState.Idle), 240f);
                if (gp.IsGameOver)
                    break;

                // The plan wins the match in the shots it lists. If it has run out and nobody has
                // won, a shot that should have hit did not: this take has diverged, so end it
                // (exit code 3) rather than play on, and let the recording script retake it.
                if (shot >= plan.Length)
                {
                    Cue("DIVERGED: the plan is used up and the match is not over");
                    Environment.ExitCode = 3;
                    yield break;
                }
                yield return Wait(0.8f);
                if (gp.IsGameOver)
                    break;

                Catapult c = me.Catapult;
                float miss = plan[shot];
                float enemyX = c.EnemyCentre.X;
                float target, preferred;
                if (miss == HillShot) { target = Battlefield.HillCentre; preferred = 50f; }
                else if (miss > 0f) { target = enemyX + (host ? -miss : miss); preferred = 40f; }
                else { target = enemyX; preferred = 58f; }

                float angle, speed;
                Solve(c, target, preferred, out speed, out angle);
                float strength = (speed - Player.MinShotStrength) / (Player.MaxShotStrength - Player.MinShotStrength);
                Cue(me.Name + " shot " + (shot + 1) + (miss == 0f ? " aims to hit" : " aims to miss"));

                // Pull back slowly so the arrow and readouts can be read, then hold
                const float pull = 1.8f, hold = 0.6f;
                float start = clock;
                while (clock < start + pull + hold)
                {
                    float k = MathHelper.Clamp((clock - start) / pull, 0f, 1f);
                    k = k * k * (3f - 2f * k);
                    me.PoseDrag(MathHelper.Lerp(angle - .17f, angle, k), MathHelper.Lerp(.08f, strength, k));
                    gp.SetDragging(true);
                    yield return () => true;
                }
                me.PoseDrag(angle, strength);
                yield return () => true;
                me.ReleaseDrag();
                gp.SetDragging(false);
                yield return Wait(0.5f);
            }

            // The victory screen, for the recording to hold on before the game quits
            yield return Wait(9f);
            Cue("end");
        }

        // Which entry to press on whichever menu is showing, or -1
        static int EntryFor(GameScreen top, bool host)
        {
            int entry = -1;
            if (top is MainMenuScreen) entry = 0;                    // Play
            else if (top is PlayMenuScreen) entry = 1;               // Multi Player on LAN
            else if (top is CreateOrFindSessionScreen) entry = host ? 0 : 1; // Create / Find
            else if (top is JoinSessionScreen) entry = 0;            // first game found
            return top is MenuScreen ? entry : -1;
        }

        static void Press(GameScreen top, int entry)
        {
            var method = typeof(MenuScreen).GetMethod("OnSelectEntry",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(int), typeof(PlayerIndex) }, null);
            method.Invoke(top, new object[] { entry, PlayerIndex.One });
        }

        // Presses the right menu entry on whichever menu is showing
        void Select(GameScreen top, bool host)
        {
            if (top.ScreenState != ScreenState.Active)
                return;

            int entry = EntryFor(top, host);
            if (entry >= 0)
                Press(top, entry);
        }
#endif
    }
}
