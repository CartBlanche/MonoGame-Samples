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
        bool recording;
        int drawCount, savedFrames;
        const int RecordEvery = 4; // 60 fps / 4 = 15 frames a second

        public static void AddIfRequested(Game game, ScreenManager screens)
        {
            string folder = null, lan = null;
            bool clip = false, play = false;
            string name = null;
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

            if (folder == null)
                return;

            game.Components.Add(new ScreenshotDirector(game, screens, folder, lan, clip, play));
        }

        ScreenshotDirector(Game game, ScreenManager screens, string folder, string lan, bool clip, bool play)
            : base(game)
        {
            this.clip = clip;
            this.play = play;
            this.screens = screens;
            this.folder = Path.GetFullPath(folder);
            Directory.CreateDirectory(this.folder);

            UpdateOrder = 1000;
            DrawOrder = 1000; // after everything else, so the frame is complete

            // Quiet while it plays itself
            SoundEffect.MasterVolume = 0f;
            try { MediaPlayer.Volume = 0f; } catch (Exception) { }

#if !SINGLE_PLAYER
            if (lan == "host" || lan == "client")
            {
                script = LanScript(lan == "host");
                return;
            }
#endif
            script = RoundScript();
        }

        public override void Update(GameTime gameTime)
        {
            clock += (float)gameTime.ElapsedGameTime.TotalSeconds;

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

        // Presses the right menu entry on whichever menu is showing
        void Select(GameScreen top, bool host)
        {
            if (top.ScreenState != ScreenState.Active)
                return;

            int entry = -1;
            if (top is MainMenuScreen) entry = 0;                    // Play
            else if (top is PlayMenuScreen) entry = 1;               // Multi Player on LAN
            else if (top is CreateOrFindSessionScreen) entry = host ? 0 : 1; // Create / Find
            else if (top is JoinSessionScreen) entry = 0;            // first game found

            if (entry < 0 || !(top is MenuScreen))
                return;

            var method = typeof(MenuScreen).GetMethod("OnSelectEntry",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(int), typeof(PlayerIndex) }, null);
            method.Invoke(top, new object[] { entry, PlayerIndex.One });
        }
#endif
    }
}
