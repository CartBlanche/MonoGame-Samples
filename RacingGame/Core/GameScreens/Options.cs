//-----------------------------------------------------------------------------
// Options.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Properties;
using RacingGame.Sounds;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// Options
    /// </summary>
    /// <returns>IGame screen</returns>
    class Options : IGameScreen
    {
        readonly Rectangle
            Line4ArrowGfxRect = new Rectangle(154, 284, 62, 39),
            Line5ArrowGfxRect = new Rectangle(160, 354, 62, 39),
            Line6ArrowGfxRect = new Rectangle(72, 437, 62, 39),
            FullscreenGfxRect = new Rectangle(339, 182, 105, 36),
            PostScreenEffectsGfxRect = new Rectangle(339, 226, 206, 36),
            ShadowsGfxRect = new Rectangle(616, 226, 90, 36),
            HighDetailGfxRect = new Rectangle(784, 226, 120, 36),
            WaveGfxRect = new Rectangle(920, 226, 80, 36),
            SoundGfxRect = new Rectangle(384, 281, 448, 39),
            MusicGfxRect = new Rectangle(384, 354, 448, 39),
            SensitivityGfxRect = new Rectangle(384, 428, 448, 39);
        /// <summary>
        /// Current player name, copied from the settings file.
        /// Can be changed in this screen and will be saved to the settings file.
        /// Just a variable here to make it easier to change the name and
        /// because of performance (reading Settings every frame is not good).
        /// </summary>
        string currentPlayerName = GameSettings.Default.PlayerName;
        int currentOptionsNumber = 0;
        /// <summary>
        /// The resolutions that can be picked (windowed mode); the last button is Auto, which
        /// uses the desktop size. Fullscreen always uses the desktop size.
        /// </summary>
        static readonly Point[] ResolutionChoices =
        {
            new Point(1280, 720),
            new Point(1366, 768),
            new Point(1600, 900),
            new Point(1920, 1080),
        };
        /// <summary>
        /// Index into ResolutionChoices, ResolutionChoices.Length = Auto, -1 = a saved size that
        /// is not in the list any more (kept as it is until another one is picked).
        /// </summary>
        int currentResolution = ResolutionChoices.Length;
        bool fullscreen = true;
        bool usePostScreenShaders = true;
        bool useShadowMapping = true;
        bool useHighDetail = true;
        bool useWave = false;
        LightingQuality lightingQuality = LightingQuality.High;
        TimeOfDayMode timeOfDayMode = TimeOfDayMode.TrackDefault;
        static readonly string[] LightingNames = { "Off", "Low", "High" };
        static readonly string[] TimeOfDayNames = { "Track", "Day", "Dusk", "Night" };
        float currentMusicVolume = 1.0f;
        float currentSoundVolume = 1.0f;
        float currentSensitivity = 1.0f;
        /// <summary>
        /// A label followed by one panel button per choice, laid out from x to the right
        /// (x is moved past the group). Returns the (possibly new) choice.
        /// </summary>
        int ChoiceGroup(ref int x, int y, int height, string label, string[] names, int current)
        {
            const float scale = 0.7f;
            Color labelColor = new Color(215, 215, 215);
            TextureFont.WriteText(x, y + height / 4, label, labelColor);
            x += TextureFont.GetTextWidth(label) + 6;
            for (int i = 0; i < names.Length; i++)
            {
                int w = (int)(TextureFont.GetTextWidth(names[i]) * scale) + height / 4;
                Rectangle r = new Rectangle(x, y, w, height);
                if (BaseGame.UI.PanelButton(r, names[i], scale, current == i))
                {
                    Sound.Play(Sound.Sounds.ButtonClick);
                    current = i;
                }
                x += w + 2;
            }
            return current;
        }

        /// <summary>
        /// Create options
        /// </summary>
        public Options()
        {
            // Current resolution, from the saved setting (the window may be fullscreen now)
            int savedW = GameSettings.Default.ResolutionWidth;
            int savedH = GameSettings.Default.ResolutionHeight;
            if (savedW <= 0 || savedH <= 0)
                currentResolution = ResolutionChoices.Length;
            else
            {
                currentResolution = -1;
                for (int i = 0; i < ResolutionChoices.Length; i++)
                    if (ResolutionChoices[i].X == savedW && ResolutionChoices[i].Y == savedH)
                        currentResolution = i;
            }

            // Get graphics detail settings
            fullscreen = BaseGame.Fullscreen;
            usePostScreenShaders = BaseGame.UsePostScreenShaders;
            useShadowMapping = BaseGame.AllowShadowMapping;
            useHighDetail = BaseGame.HighDetail;
            useWave = GameSettings.Default.WaveEffect;
            lightingQuality = GameSettings.Default.Lighting;
            timeOfDayMode = GameSettings.Default.TimeOfDayMode;

            // Get music and sound volume
            currentMusicVolume = GameSettings.Default.MusicVolume;
            currentSoundVolume = GameSettings.Default.SoundVolume;

            // Get sensitivity
            currentSensitivity = GameSettings.Default.ControllerSensitivity;
        }
		/// <summary>
		/// Unimplemented
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update(GameTime gameTime)
		{

		}
        /// <summary>
        /// Render game screen. Called each frame.
        /// </summary>
        public bool Render()
        {
            // This starts both menu and in game post screen shader!
			if(BaseGame.UsePostScreenShaders)
            	BaseGame.UI.PostScreenMenuShader.Start();

            // Render background and black bar
            BaseGame.UI.RenderMenuBackground();

            // Options header
            int posX = 10;
            int posY = 18;
            // UWP COMMENT OUT
            //if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            //{
            //    posX += 36;
            //    posY += 26;
            //}
            BaseGame.UI.Headers.RenderOnScreenRelative1600(
                posX, posY, UIRenderer.HeaderOptionsGfxRect);

            // Options background
            BaseGame.UI.OptionsScreen.RenderOnScreenRelative4To3(
                0, 125, BaseGame.UI.OptionsScreen.GfxRectangle);
            // Edit player name
            int xPos = BaseGame.XToRes(352);
            int yPos = BaseGame.YToRes768(125 + 65 - 20);
            TextureFont.WriteText(xPos, yPos,
                currentPlayerName +
                // Add blinking |
                ((int)(BaseGame.TotalTime / 0.35f) % 2 == 0 ? "|" : ""));
            Input.HandleKeyboardInput(ref currentPlayerName);

#if !XBOX360
            // Use inverted color for selection (see below for sprite blend mode)
            Color selColor = new Color(255, 156, 0, 160);

            // Select resolution: 4 common sizes and Auto. Sizes bigger than the desktop are
            // dimmed, and everything is dimmed in fullscreen (it always uses the desktop size).
            Color dimColor = new Color(120, 120, 120);
            const int resButtonW = 107, resButtonGap = 8;
            for (int i = 0; i <= ResolutionChoices.Length; i++)
            {
                bool isAuto = i == ResolutionChoices.Length;
                bool fits = isAuto || BaseGame.WindowedSizeFits(
                    ResolutionChoices[i].X, ResolutionChoices[i].Y);
                bool available = fits && !fullscreen;
                string label = isAuto ? "Auto" :
                    ResolutionChoices[i].X + "x" + ResolutionChoices[i].Y;
                Rectangle r = BaseGame.CalcRectangleKeep4To3(new Rectangle(
                    339 + i * (resButtonW + resButtonGap), 108, resButtonW, 38));
                r.Y += BaseGame.YToRes768(125);
                float scale = Math.Min(0.8f, 0.88f * r.Width / Math.Max(1, TextureFont.GetTextWidth(label)));
                if (available)
                {
                    if (BaseGame.UI.PanelButton(r, label, scale, currentResolution == i))
                    {
                        Sound.Play(Sound.Sounds.ButtonClick);
                        currentResolution = i;
                    }
                }
                else
                    BaseGame.UI.RenderPanelButton(r, label, false, scale, dimColor);
            }
            if (fullscreen)
            {
                Rectangle noteRect = BaseGame.CalcRectangleKeep4To3(new Rectangle(339, 148, 568, 24));
                noteRect.Y += BaseGame.YToRes768(125);
                TextureFont.WriteText(noteRect.X, noteRect.Y, "Fullscreen uses the desktop size",
                    Color.Gray);
            }

            Rectangle fsRect = BaseGame.CalcRectangleKeep4To3(
                new Rectangle(339, 182, 150, 38));
            fsRect.Y += BaseGame.YToRes768(125);
            if (BaseGame.UI.PanelButton(fsRect, "Fullscreen",
                Math.Min(0.8f, 0.88f * fsRect.Width / Math.Max(1, TextureFont.GetTextWidth("Fullscreen"))),
                fullscreen))
            {
                Sound.Play(Sound.Sounds.ButtonClick);
                fullscreen = !fullscreen;
            }

            // Lighting and time of day: two button groups right of the Fullscreen button (the
            // painted background has no free line and no painted label for them). They take
            // effect when the next race starts.
            {
                Rectangle at = BaseGame.CalcRectangleKeep4To3(new Rectangle(488, 182, 100, 38));
                at.Y += BaseGame.YToRes768(125);
                int x = at.X;
                lightingQuality = (LightingQuality)ChoiceGroup(ref x, at.Y, at.Height,
                    "Lighting", LightingNames, (int)lightingQuality);
                x += 6;
                timeOfDayMode = (TimeOfDayMode)ChoiceGroup(ref x, at.Y, at.Height,
                    "Time of day", TimeOfDayNames, (int)timeOfDayMode);
            }

            Rectangle pseRect = BaseGame.CalcRectangleKeep4To3(
                PostScreenEffectsGfxRect);
            pseRect.Y += BaseGame.YToRes768(125);
            bool inPseRect = Input.MouseInBox(pseRect);
            if (usePostScreenShaders)
                BaseGame.UI.OptionsScreen.RenderOnScreen(
                    pseRect, PostScreenEffectsGfxRect,
                    selColor, BlendState.AlphaBlend);
            if (inPseRect && Input.MouseLeftButtonJustPressed)
            {
                Sound.Play(Sound.Sounds.ButtonClick);
                usePostScreenShaders = !usePostScreenShaders;
            }

            Rectangle smRect = BaseGame.CalcRectangleKeep4To3(
                ShadowsGfxRect);
            smRect.Y += BaseGame.YToRes768(125);
            bool inSmRect = Input.MouseInBox(smRect);
            if (useShadowMapping)
                BaseGame.UI.OptionsScreen.RenderOnScreen(
                    smRect, ShadowsGfxRect,
                    selColor, BlendState.AlphaBlend);
            if (inSmRect && Input.MouseLeftButtonJustPressed)
            {
                Sound.Play(Sound.Sounds.ButtonClick);
                useShadowMapping = !useShadowMapping;
            }

            Rectangle hdRect = BaseGame.CalcRectangleKeep4To3(
                HighDetailGfxRect);
            hdRect.Y += BaseGame.YToRes768(125);
            bool inHdRect = Input.MouseInBox(hdRect);
            if (useHighDetail)
                BaseGame.UI.OptionsScreen.RenderOnScreen(
                    hdRect, HighDetailGfxRect,
                    selColor, BlendState.AlphaBlend);
            if (inHdRect && Input.MouseLeftButtonJustPressed)
            {
                Sound.Play(Sound.Sounds.ButtonClick);
                useHighDetail = !useHighDetail;
            }

            // Wave effect (same line as post screen effect, shadows and high detail)
            Rectangle waveRect = BaseGame.CalcRectangleKeep4To3(WaveGfxRect);
            waveRect.Y += BaseGame.YToRes768(125);
            TextureFont.WriteText(waveRect.X, waveRect.Y,
                "Wave", useWave ? new Color(255, 156, 0) : Color.Gray);
            if (Input.MouseInBox(waveRect) && Input.MouseLeftButtonJustPressed)
            {
                Sound.Play(Sound.Sounds.ButtonClick);
                useWave = !useWave;
            }
#endif
            Rectangle soundRect = BaseGame.CalcRectangleKeep4To3(
                SoundGfxRect);
            soundRect.Y += BaseGame.YToRes768(125);
            if (Input.MouseInBox(soundRect))
            {
                if (Input.MouseLeftButtonJustPressed)
                {
                    currentSoundVolume =
                        (Input.MousePos.X - soundRect.X) / (float)soundRect.Width;
                    Sound.Play(Sound.Sounds.Highlight);
                }
            }

            // Handel controller input
            if (currentOptionsNumber == 0)
            {
                if (Input.GamePadLeftJustPressed ||
                    Input.KeyboardLeftJustPressed)
                {
                    currentSoundVolume -= 0.1f;
                    Sound.Play(Sound.Sounds.Highlight);
                }
                if (Input.GamePadRightJustPressed ||
                    Input.KeyboardRightJustPressed)
                {
                    currentSoundVolume += 0.1f;
                    Sound.Play(Sound.Sounds.Highlight);
                }
                if (currentSoundVolume < 0)
                    currentSoundVolume = 0;
                if (currentSoundVolume > 1)
                    currentSoundVolume = 1;
            }

            // Render slider handle
            Rectangle gfxRect = UIRenderer.SelectionRadioButtonGfxRect;
            BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                soundRect.X + (int)(soundRect.Width * currentSoundVolume) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                soundRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
                gfxRect);
            Rectangle musicRect = BaseGame.CalcRectangleKeep4To3(
                MusicGfxRect);
            musicRect.Y += BaseGame.YToRes768(125);
            if (Input.MouseInBox(musicRect))
            {
                if (Input.MouseLeftButtonJustPressed)
                {
                    currentMusicVolume =
                        (Input.MousePos.X - musicRect.X) / (float)musicRect.Width;
                    Sound.Play(Sound.Sounds.Highlight);
                }
            }

            // Handel controller input
            if (currentOptionsNumber == 1)
            {
                if (Input.GamePadLeftJustPressed ||
                    Input.KeyboardLeftJustPressed)
                {
                    currentMusicVolume -= 0.1f;
                    Sound.Play(Sound.Sounds.Highlight);
                }
                if (Input.GamePadRightJustPressed ||
                    Input.KeyboardRightJustPressed)
                {
                    currentMusicVolume += 0.1f;
                    Sound.Play(Sound.Sounds.Highlight);
                }
                if (currentMusicVolume < 0)
                    currentMusicVolume = 0;
                if (currentMusicVolume > 1)
                    currentMusicVolume = 1;
            }

            // Render slider handle
            BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                musicRect.X + (int)(musicRect.Width * currentMusicVolume) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                musicRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
                gfxRect);

            Sound.SetVolumes(currentSoundVolume, currentMusicVolume);
            Rectangle sensitivityRect = BaseGame.CalcRectangleKeep4To3(
                SensitivityGfxRect);
            sensitivityRect.Y += BaseGame.YToRes768(125);
            if (Input.MouseInBox(sensitivityRect))
            {
                if (Input.MouseLeftButtonJustPressed)
                {
                    currentSensitivity =
                        (Input.MousePos.X - sensitivityRect.X) /
                        (float)sensitivityRect.Width;
                    Sound.Play(Sound.Sounds.Highlight);
                }
            }

            // Handel controller input
            if (currentOptionsNumber == 2)
            {
                if (Input.GamePadLeftJustPressed ||
                    Input.KeyboardLeftJustPressed)
                {
                    currentSensitivity -= 0.1f;
                    Sound.Play(Sound.Sounds.Highlight);
                }
                if (Input.GamePadRightJustPressed ||
                    Input.KeyboardRightJustPressed)
                {
                    currentSensitivity += 0.1f;
                    Sound.Play(Sound.Sounds.Highlight);
                }
                if (currentSensitivity < 0)
                    currentSensitivity = 0;
                if (currentSensitivity > 1)
                    currentSensitivity = 1;
            }

            // Render slider handle
            BaseGame.UI.Buttons.RenderOnScreen(new Rectangle(
                sensitivityRect.X +
                (int)(sensitivityRect.Width * currentSensitivity) -
                BaseGame.XToRes(gfxRect.Width) / 2,
                sensitivityRect.Y,
                BaseGame.XToRes(gfxRect.Width), BaseGame.YToRes768(gfxRect.Height)),
                gfxRect);
            Rectangle[] lineArrowGfxRects = new Rectangle[]
            {
                Line4ArrowGfxRect,
                Line5ArrowGfxRect,
                Line6ArrowGfxRect,
            };
            for (int num = 0; num < lineArrowGfxRects.Length; num++)
            {
                Rectangle lineRect = BaseGame.CalcRectangleKeep4To3(
                    lineArrowGfxRects[num]);
                lineRect.Y += BaseGame.YToRes768(125);
                lineRect.X -= BaseGame.XToRes(8 + (int)Math.Round(8 *
                    Math.Sin(BaseGame.TotalTime / 0.21212f)));

                // Draw selection arrow
                if (currentOptionsNumber == num)
                    BaseGame.UI.Buttons.RenderOnScreen(
                        lineRect, UIRenderer.SelectionArrowGfxRect, Color.White);
            }

            // Game pad selection
            if (Input.GamePadUpJustPressed ||
                Input.KeyboardUpJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                currentOptionsNumber = (lineArrowGfxRects.Length +
                    currentOptionsNumber - 1) % lineArrowGfxRects.Length;
            }
            else if (Input.GamePadDownJustPressed ||
                Input.KeyboardDownJustPressed)
            {
                Sound.Play(Sound.Sounds.Highlight);
                currentOptionsNumber = (currentOptionsNumber + 1) %
                    lineArrowGfxRects.Length;
            }
            BaseGame.UI.RenderBottomButtons(true);
            if (Input.KeyboardEscapeJustPressed ||
                Input.GamePadBJustPressed ||
                Input.GamePadBackJustPressed ||
                BaseGame.UI.backButtonPressed)
            {
                // Apply settings, for xbox only set music/sound and sensitivity!
                GameSettings.Default.PlayerName = currentPlayerName;
                if (currentResolution >= ResolutionChoices.Length)
                {
                    // Auto: use the best resolution available
                    GameSettings.Default.ResolutionWidth = 0;
                    GameSettings.Default.ResolutionHeight = 0;
                }
                else if (currentResolution >= 0)
                {
                    GameSettings.Default.ResolutionWidth = ResolutionChoices[currentResolution].X;
                    GameSettings.Default.ResolutionHeight = ResolutionChoices[currentResolution].Y;
                }
                // else: a saved size that is not in the list, keep it
                GameSettings.Default.Fullscreen = fullscreen;
                GameSettings.Default.PostScreenEffects = usePostScreenShaders;
                GameSettings.Default.ShadowMapping = useShadowMapping;
                GameSettings.Default.HighDetail = useHighDetail;
                GameSettings.Default.WaveEffect = useWave;
                GameSettings.Default.Lighting = lightingQuality;
                GameSettings.Default.TimeOfDayMode = timeOfDayMode;
                GameSettings.Default.MusicVolume = currentMusicVolume;
                GameSettings.Default.SoundVolume = currentSoundVolume;
                GameSettings.Default.ControllerSensitivity = currentSensitivity;

                // Save all
                GameSettings.Save();
                // Update game settings
                BaseGame.CheckOptionsAndPSVersion();

                return true;
            }

            return false;
        }
    }
}
