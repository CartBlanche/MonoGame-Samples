using System;
using Microsoft.Xna.Framework.Input;
using Shooter.Gameplay.Enemies;
using Shooter.Scene;
using Shooter.UI;
using XInput = Microsoft.Xna.Framework.Input;

namespace Shooter.Gameplay
{
    /// <summary>Headless checks of milestone 4c-3d-4b (run as part of --test-physics): GameFlowManager timings and the menu screens' layout / input.</summary>
    public static class GameFlowSelfTest
    {
        private const float Dt = 1f / 60f;
        private static string F(float v) { return PhysicsSelfTest.F(v); }

        private static void Run(GameFlow flow, float seconds)
        {
            int n = (int)Math.Round(seconds / Dt);
            for (int i = 0; i < n; i++) flow.Update(Dt);
        }

        public static void Run(LoadedScene loaded)
        {
            Console.WriteLine("Game flow and menus (4c-3d-4b)");
            PlayerSettings ps = PlayerSettings.FromScene(loaded.Data).Clone();
            var player = new PlayerController(loaded.Collision, ps);
            var director = new EnemyDirector(loaded);
            director.SetPlayer(player, ps);

            // 1. lose: the player's death starts the 3 s fade (alpha and master volume follow it), the Lose screen is due once at the end
            {
                var flow = new GameFlow();
                int lose = 0, win = 0, victory = 0; string message = null;
                flow.EndScreen += w => { if (w) win++; else lose++; };
                flow.PlayVictory += () => victory++;
                flow.DisplayMessage += (t, d) => message = t;
                flow.Bind(director);
                Run(flow, 1f);
                bool idle = !flow.GameIsEnding && flow.FadeAlpha == 0f && flow.MasterVolume == 1f;
                director.PlayerHealth.Kill();
                Run(flow, 1.5f);
                float midAlpha = flow.FadeAlpha, midVol = flow.MasterVolume;
                bool endingMid = flow.GameIsEnding && lose == 0;
                Run(flow, 1.6f);
                PhysicsSelfTest.Check("lose: player death fades to black over 3 s (alpha 0.5 and volume 0.5 at 1.5 s) and the Lose screen is due once",
                    idle && endingMid && Math.Abs(midAlpha - 0.5f) < 0.03f && Math.Abs(midVol - 0.5f) < 0.03f && lose == 1 && win == 0 && !flow.GameIsEnding && flow.FadeAlpha == 1f && victory == 0 && message == null,
                    "idle " + idle + ", mid alpha " + F(midAlpha) + " volume " + F(midVol) + ", lose x" + lose + ", win x" + win + ", victory x" + victory);
            }

            // 2. win: the last kill completes the objective -> message after 2 s with the sound, black from 4 s, Win screen at 3 + 4 = 7 s
            {
                director.Reset();
                director.SetPlayer(player, ps);
                var flow = new GameFlow();
                int lose = 0, win = 0, victory = 0; string message = null; float messageDelay = -1f;
                float victoryAt = -1f, t = 0f;
                flow.EndScreen += w => { if (w) win++; else lose++; };
                flow.PlayVictory += () => { victory++; victoryAt = t; };
                flow.DisplayMessage += (m, d) => { message = m; messageDelay = d; };
                flow.Bind(director);
                Run(flow, 0.5f);
                bool notYet = !flow.GameIsEnding;
                foreach (Enemy e in director.AllEnemies) e.Health.Kill();
                for (int i = 0; i < 2; i++) flow.Update(Dt);   // ObjectiveManager sees the completed objective on the next Update
                bool started = flow.GameIsEnding && flow.Win && message == GameFlow.WinGameMessage && messageDelay == GameFlow.DelayBeforeWinMessage;
                // victory sound 2 s after the end started
                t = 0f;
                for (int i = 0; i < (int)(2.5f / Dt); i++) { t += Dt; flow.Update(Dt); }
                bool soundAt2 = victory == 1 && victoryAt > 1.9f && victoryAt < 2.1f;
                float alphaAt25 = flow.FadeAlpha, volAt25 = flow.MasterVolume;   // still before the fade (starts at 4 s)
                Run(flow, 3.0f);   // 5.5 s
                float alphaAt55 = flow.FadeAlpha;
                Run(flow, 1.6f);   // 7.1 s
                PhysicsSelfTest.Check("win: objective completed -> message (delay 2 s), victory sound once at 2 s, no fade before 4 s, half at 5.5 s, Win screen once at 7 s",
                    notYet && started && soundAt2 && alphaAt25 == 0f && volAt25 == 1f && Math.Abs(alphaAt55 - 0.5f) < 0.04f && win == 1 && lose == 0 && victory == 1 && flow.FadeAlpha == 1f,
                    "started " + started + ", sound at " + F(victoryAt) + " s x" + victory + ", alpha 2.5 s " + F(alphaAt25) + ", 5.5 s " + F(alphaAt55) + ", win x" + win + ", lose x" + lose);
            }

            // 3. a reload (Bind again) forgets the old run
            {
                director.Reset();
                director.SetPlayer(player, ps);
                var flow = new GameFlow();
                int lose = 0;
                flow.EndScreen += w => { if (!w) lose++; };
                flow.Bind(director);
                director.PlayerHealth.Kill();
                Run(flow, 1f);
                director.Reset();
                director.SetPlayer(player, ps);
                flow.Bind(director);
                Run(flow, 4f);
                PhysicsSelfTest.Check("binding a fresh director resets the flow (no fade, volume 1, no screen from the old run)",
                    !flow.GameIsEnding && flow.FadeAlpha == 0f && flow.MasterVolume == 1f && lose == 0,
                    "ending " + flow.GameIsEnding + ", alpha " + F(flow.FadeAlpha) + ", volume " + F(flow.MasterVolume) + ", lose x" + lose);
            }

            // 4. enemies do not fire while the game is ending (EnemyController.TryAtack), and do again once it is not
            {
                director.Reset();
                director.SetPlayer(player, ps);
                bool ending = true;
                director.GameIsEnding = () => ending;
                Enemy bot = director.AllEnemies.Find(e => e.Data.Kind == "HoverBot");
                Microsoft.Xna.Framework.Vector3 target = bot.Position + new Microsoft.Xna.Framework.Vector3(0f, 1f, -5f);
                bool fired1 = bot.TryAttack(target);
                int shots1 = director.EnemyProjectiles.Live.Count;
                ending = false;
                bool fired2 = bot.TryAttack(target);
                int shots2 = director.EnemyProjectiles.Live.Count;
                director.GameIsEnding = null;
                PhysicsSelfTest.Check("enemy attacks do nothing while GameIsEnding (no shot), and fire normally otherwise",
                    !fired1 && shots1 == 0 && fired2 && shots2 == 1,
                    "ending: fired " + fired1 + ", projectiles " + shots1 + "; not ending: fired " + fired2 + ", projectiles " + shots2);
            }

            // 5. footsteps: one Footstep event per 1 / FootstepSfxFrequency metres on the ground (0.3 walking, 0.2 sprinting in the scene), none in the air, one Jumped per jump
            {
                PlayerSettings fs = PlayerSettings.FromScene(loaded.Data).Clone();
                var walker = new PlayerController(loaded.Collision, fs);
                walker.Respawn(fs.StartPosition, fs.StartYawDegrees);
                for (int i = 0; i < 90; i++) walker.Update(Dt, default(PlayerInput));   // settle on the floor
                int steps = 0, jumps = 0;
                walker.Footstep += () => steps++;
                walker.Jumped += () => jumps++;
                Microsoft.Xna.Framework.Vector3 from = walker.Position;
                var walk = new PlayerInput { Move = new Microsoft.Xna.Framework.Vector2(0f, 1f) };
                for (int i = 0; i < 180; i++) walker.Update(Dt, walk);   // 3 s forward
                float walked = Microsoft.Xna.Framework.Vector3.Distance(from, walker.Position);
                int walkSteps = steps;
                float expectedWalk = walked * fs.FootstepSfxFrequency;

                steps = 0;
                from = walker.Position;
                var sprint = new PlayerInput { Move = new Microsoft.Xna.Framework.Vector2(0f, 1f), SprintHeld = true };
                for (int i = 0; i < 120; i++) walker.Update(Dt, sprint);
                float sprinted = Microsoft.Xna.Framework.Vector3.Distance(from, walker.Position);
                int sprintSteps = steps;
                float expectedSprint = sprinted * fs.FootstepSfxFrequencyWhileSprinting;

                steps = 0;
                walker.Update(Dt, new PlayerInput { JumpDown = true });
                int airSteps0 = steps;
                for (int i = 0; i < 20; i++) walker.Update(Dt, default(PlayerInput));   // in the air: no steps
                PhysicsSelfTest.Check("footsteps: one event per 1 / frequency metres walked (0.3) or sprinted (0.2), none while airborne; a jump raises Jumped once",
                    fs.FootstepSfxFrequency == 0.3f && fs.FootstepSfxFrequencyWhileSprinting == 0.2f && Math.Abs(walkSteps - expectedWalk) <= 1.5f && walkSteps >= 3 && Math.Abs(sprintSteps - expectedSprint) <= 1.5f && sprintSteps >= 2 && jumps == 1 && steps == airSteps0,
                    "walked " + F(walked) + " m -> " + walkSteps + " steps (expected " + F(expectedWalk) + "), sprinted " + F(sprinted) + " m -> " + sprintSteps + " (expected " + F(expectedSprint) + "), jumps " + jumps + ", airborne steps " + (steps - airSteps0));
            }

            RunPauseMenu();
            RunMenus();
            RunEnemySystems();
        }

        private static MouseState MouseAt(int x, int y, bool down)
        {
            return new MouseState(x, y, 0, down ? XInput.ButtonState.Pressed : XInput.ButtonState.Released, XInput.ButtonState.Released, XInput.ButtonState.Released, XInput.ButtonState.Released, XInput.ButtonState.Released);
        }

        private static void ClickAt(PauseMenu m, int w, int h, Microsoft.Xna.Framework.Point p)
        {
            m.Update(0.1f, w, h, MouseAt(p.X, p.Y, true), MouseAt(p.X, p.Y, false), default(KeyboardState), default(KeyboardState));
            m.Update(0.1f, w, h, MouseAt(p.X, p.Y, false), MouseAt(p.X, p.Y, true), default(KeyboardState), default(KeyboardState));
        }

        private static void Press(PauseMenu m, int w, int h, Keys k)
        {
            m.Update(0.1f, w, h, default(MouseState), default(MouseState), new KeyboardState(k), default(KeyboardState));
        }

        // 6. the pause menu (InGameMenuManager): keys, mouse, slider, toggles, controls pop-up, framerate counter
        private static void RunPauseMenu()
        {
            const int W = 1280, H = 720;
            var m = new PauseMenu();
            int opened = 0, closed = 0, shots = 0, shadowEvents = 0, invEvents = 0, fpsEvents = 0;
            float lastSens = -1f;
            m.Opened += () => opened++;
            m.Closed += () => closed++;
            m.ScreenshotRequested += () => shots++;
            m.ShadowsChanged += on => shadowEvents++;
            m.InvincibleChanged += on => invEvents++;
            m.FramerateChanged += on => fpsEvents++;
            m.LookSensitivityChanged += v => lastSens = v;
            m.Update(0f, W, H, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));   // lay out

            Press(m, W, H, Keys.Tab);
            bool tabOpens = m.IsOpen && opened == 1;
            Press(m, W, H, Keys.Tab);
            bool tabCloses = !m.IsOpen && closed == 1;
            Press(m, W, H, Keys.P);
            bool pOpens = m.IsOpen && opened == 2;
            Press(m, W, H, Keys.Escape);
            bool escCloses = !m.IsOpen && closed == 2;
            PhysicsSelfTest.Check("pause menu: Tab and P open it, Tab and Cancel close it (Opened / Closed raised once each time)", tabOpens && tabCloses && pOpens && escCloses,
                "tab " + tabOpens + "/" + tabCloses + ", p " + pOpens + ", esc " + escCloses);

            m.Open();
            // toggles by mouse
            ClickAt(m, W, H, m.Controls[(int)PauseMenu.Row.Shadows].Center);
            ClickAt(m, W, H, m.Controls[(int)PauseMenu.Row.Invincibility].Center);
            ClickAt(m, W, H, m.Controls[(int)PauseMenu.Row.Framerate].Center);
            bool toggles = !m.ShadowsOn && m.Invincible && m.FramerateOn && shadowEvents == 1 && invEvents == 1 && fpsEvents == 1;
            // slider: ends and middle, then a keyboard step
            var sl = m.Controls[(int)PauseMenu.Row.Sensitivity];
            int inset = (int)Math.Round(10f * m.Scale);
            ClickAt(m, W, H, new Microsoft.Xna.Framework.Point(sl.X + inset, sl.Center.Y));
            float low = m.LookSensitivity;
            ClickAt(m, W, H, new Microsoft.Xna.Framework.Point(sl.Right - inset, sl.Center.Y));
            float high = m.LookSensitivity;
            ClickAt(m, W, H, new Microsoft.Xna.Framework.Point(sl.Center.X, sl.Center.Y));
            float mid = m.LookSensitivity;
            Press(m, W, H, Keys.Right);
            float stepped = m.LookSensitivity;
            PhysicsSelfTest.Check("pause menu: the three toggles flip and raise their events; the look sensitivity slider spans 0.1 - 3 (middle ~1.55) and a key step moves it by a tenth of the range",
                toggles && Math.Abs(low - 0.1f) < 0.01f && Math.Abs(high - 3f) < 0.01f && Math.Abs(mid - 1.55f) < 0.06f && Math.Abs(stepped - (mid + 0.29f)) < 0.02f && Math.Abs(lastSens - stepped) < 1e-5f,
                "toggles " + toggles + ", slider " + F(low) + " / " + F(mid) + " / " + F(high) + ", step -> " + F(stepped));

            // keyboard navigation and activation, controls pop-up, screenshot button
            m.Close(); m.Open();
            Press(m, W, H, Keys.Down);
            int first = m.Selected;
            Press(m, W, H, Keys.Down);
            Press(m, W, H, Keys.Enter);   // Shadows toggles back on
            bool kbToggle = m.ShadowsOn && shadowEvents == 2;
            ClickAt(m, W, H, m.Controls[(int)PauseMenu.Row.Controls].Center);
            bool controlsOpen = m.ControlsOpen;
            ClickAt(m, W, H, m.Controls[(int)PauseMenu.Row.Shadows].Center);   // behind the pop-up: nothing happens
            bool blocked = m.ShadowsOn && shadowEvents == 2;
            Press(m, W, H, Keys.Escape);   // closes the controls image first, the menu stays
            bool escControlsFirst = !m.ControlsOpen && m.IsOpen;
            ClickAt(m, W, H, m.Controls[(int)PauseMenu.Row.Controls].Center);
            ClickAt(m, W, H, m.CloseRect.Center);
            bool closeButton = !m.ControlsOpen && m.IsOpen;
            ClickAt(m, W, H, m.Controls[(int)PauseMenu.Row.Screenshot].Center);
            PhysicsSelfTest.Check("pause menu: the first Down selects the slider, Enter toggles the selected row; Controls shows the pop-up (blocks the rows, Cancel closes it before the menu, so does its close button); Take new raises ScreenshotRequested",
                first == 0 && kbToggle && controlsOpen && blocked && escControlsFirst && closeButton && shots == 1,
                "first " + first + ", kbToggle " + kbToggle + ", controls " + controlsOpen + ", blocked " + blocked + ", esc " + escControlsFirst + ", close " + closeButton + ", screenshots " + shots);

            // framerate counter: frames over 0.5 s
            var fps = new PauseMenu();
            for (int i = 0; i < 31; i++) fps.CountFrame(1f / 60f);
            PhysicsSelfTest.Check("framerate counter: 30 frames in 0.5 s read 60", fps.Framerate == 60, "reads " + fps.Framerate);
        }

        // 7. the enemies' alert / steam / sparks systems are not dropped by the manager while idle (non-looping, not playing: AutoRemoveFinished would remove them), and play when told to
        private static void RunEnemySystems()
        {
            string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Content", "Particles", "Enemy_HoverBot.particles.json");
            if (!System.IO.File.Exists(path))
                return;
            System.Collections.Generic.List<Shooter.Rendering.Particles.ParticleSystemData.Placement> pl;
            using (System.IO.FileStream fs = System.IO.File.OpenRead(path)) pl = Shooter.Rendering.Particles.ParticleSystemData.Load(fs);
            Shooter.Rendering.Particles.ParticleSystemData.Placement alert = pl.Find(p => p.Data.Name == "VFX_Alert");
            var manager = new Shooter.Rendering.Particles.ParticleManager(null, null) { AutoRemoveFinished = true };
            var kept = new Shooter.Rendering.Particles.ParticleSystemInstance(alert.Data, null, Microsoft.Xna.Framework.Matrix.Identity, 4);
            kept.Stop(true);
            kept.KeepWhenFinished = true;
            var dropped = new Shooter.Rendering.Particles.ParticleSystemInstance(alert.Data, null, Microsoft.Xna.Framework.Matrix.Identity, 4);
            dropped.Stop(true);
            manager.Add(kept);
            manager.Add(dropped);
            for (int i = 0; i < 60; i++) manager.Update(1f / 60f);
            bool keptIdle = System.Linq.Enumerable.Contains(manager.Systems, kept) && !System.Linq.Enumerable.Contains(manager.Systems, dropped);
            kept.Play();
            for (int i = 0; i < 20; i++) manager.Update(1f / 60f);
            PhysicsSelfTest.Check("enemy alert system: an idle non-looping system marked KeepWhenFinished stays in the manager (the unmarked one is dropped) and shows its mark once played",
                keptIdle && kept.ParticleCount > 0, "kept idle " + keptIdle + ", particles after Play " + kept.ParticleCount);
        }

        private static MouseState Mouse(int x, int y, bool down)
        {
            return new MouseState(x, y, 0, down ? XInput.ButtonState.Pressed : XInput.ButtonState.Released, XInput.ButtonState.Released, XInput.ButtonState.Released, XInput.ButtonState.Released, XInput.ButtonState.Released);
        }

        private static MenuAction Click(MenuScreens m, int w, int h, int px = 0, int py = 0, int rx = -1, int ry = -1)
        {
            if (rx < 0) { rx = px; ry = py; }
            m.Update(0.1f, w, h, Mouse(px, py, true), Mouse(px, py, false), default(KeyboardState), default(KeyboardState));
            return m.Update(0.1f, w, h, Mouse(rx, ry, false), Mouse(px, py, true), default(KeyboardState), default(KeyboardState));
        }

        private static MenuAction Key(MenuScreens m, int w, int h, Keys k)
        {
            return m.Update(0.1f, w, h, default(MouseState), default(MouseState), new KeyboardState(k), default(KeyboardState));
        }

        private static void RunMenus()
        {
            const int W = 1280, H = 720;
            var m = new MenuScreens();

            // layout: 960 x 540 canvas scaled to the width (x1.333); RestartButton 380 x 70 at the bottom centre + 25, MenuButton 380 x 30 at + 105
            m.Show(MenuScreen.Lose);
            m.Update(0f, W, H, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            var menu = m.Buttons[0].Bounds; var restart = m.Buttons[1].Bounds;
            bool layout = m.Buttons[0].Id == "MenuButton" && m.Buttons[1].Id == "RestartButton"
                && restart.Width == 507 && restart.Height == 93 && Math.Abs(restart.Bottom - (540f - 25f) * 1.3333333f) <= 1f && restart.X == (int)Math.Round(290f * 1.3333333f)
                && menu.Height == 40 && Math.Abs(menu.Bottom - (540f - 105f) * 1.3333333f) <= 1f && m.Title == "You LOSE";
            PhysicsSelfTest.Check("Lose screen layout at 1280 x 720: Play Again 507 x 93 and Menu 507 x 40 centred at the bottom, title 'You LOSE'",
                layout, "restart " + restart + ", menu " + menu + ", title " + m.Title);

            // mouse: click = press and release on the same button
            MenuAction a1 = Click(m, W, H, restart.Center.X, restart.Center.Y);
            m.Show(MenuScreen.Lose);
            m.Update(0f, W, H, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            MenuAction a2 = Click(m, W, H, menu.Center.X, menu.Center.Y);
            m.Show(MenuScreen.Win);
            m.Update(0f, W, H, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            MenuAction a3 = Click(m, W, H, restart.Center.X, restart.Center.Y, menu.Center.X, menu.Center.Y);   // released over another button
            MenuAction a4 = Click(m, W, H, 5, 5);
            PhysicsSelfTest.Check("clicks: Play Again -> PlayAgain, Menu -> Menu, press on one button and release on another or on nothing -> nothing; Win is the same screen titled 'You WIN'",
                a1 == MenuAction.PlayAgain && a2 == MenuAction.Menu && a3 == MenuAction.None && a4 == MenuAction.None && m.Title == "You WIN",
                a1 + ", " + a2 + ", " + a3 + ", " + a4 + ", " + m.Title);

            // keyboard (MenuNavigation): the first key only selects the default button, Up moves to Menu, Down back, Enter presses
            m.Show(MenuScreen.Lose);
            m.Update(0f, W, H, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            MenuAction k1 = Key(m, W, H, Keys.Enter);
            int afterFirst = m.Selected;
            Key(m, W, H, Keys.Up);
            int afterUp = m.Selected;
            Key(m, W, H, Keys.Up);
            int clampedUp = m.Selected;
            Key(m, W, H, Keys.Down);
            int afterDown = m.Selected;
            MenuAction k2 = Key(m, W, H, Keys.Space);
            m.Update(1f, W, H, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            bool tinted = m.Buttons[1].Color.X < 0.3f && m.Buttons[1].Color.Z > 0.9f;   // selected = highlighted colour (0.282, 0.725, 0.953)
            PhysicsSelfTest.Check("keyboard: first key selects Play Again without pressing it, Up -> Menu (no wrap), Down -> Play Again, Space presses it; the selection is tinted",
                k1 == MenuAction.None && afterFirst == 1 && afterUp == 0 && clampedUp == 0 && afterDown == 1 && k2 == MenuAction.PlayAgain && tinted,
                "first " + k1 + " sel " + afterFirst + ", up " + afterUp + ", up again " + clampedUp + ", down " + afterDown + ", space " + k2 + ", colour " + m.Buttons[1].Color);

            // intro: Play starts, Controls opens the pop-up which blocks the buttons behind it until its close button is clicked
            m.Show(MenuScreen.Intro);
            m.Update(0f, W, H, default(MouseState), default(MouseState), default(KeyboardState), default(KeyboardState));
            var controls = m.Buttons[0].Bounds; var play = m.Buttons[1].Bounds;
            bool introIds = m.Buttons[0].Id == "ControlsButton" && m.Buttons[1].Id == "StartButton" && m.Title == "";
            MenuAction c1 = Click(m, W, H, controls.Center.X, controls.Center.Y);
            bool open = m.ControlsOpen;
            MenuAction c2 = Click(m, W, H, play.Center.X, play.Center.Y);   // behind the pop-up: no effect
            var close = m.CloseButton.Bounds;
            MenuAction c3 = Click(m, W, H, close.Center.X, close.Center.Y);
            bool closed = !m.ControlsOpen;
            MenuAction c4 = Click(m, W, H, play.Center.X, play.Center.Y);
            PhysicsSelfTest.Check("intro: Controls opens the pop-up (blocks Play behind it), its close button closes it, Play -> Play; pop-up rect is inset 50 / 50 / 50 / 105 canvas px",
                introIds && c1 == MenuAction.None && open && c2 == MenuAction.None && c3 == MenuAction.None && closed && c4 == MenuAction.Play
                && m.ControlsRect.X == 67 && m.ControlsRect.Y == 67 && m.ControlsRect.Width == 1147 && Math.Abs(m.ControlsRect.Bottom - (540f - 105f) * 1.3333333f) <= 1f,
                "ids " + introIds + ", open " + open + ", " + c1 + "/" + c2 + "/" + c3 + "/" + c4 + ", pop-up " + m.ControlsRect);
        }
    }
}
