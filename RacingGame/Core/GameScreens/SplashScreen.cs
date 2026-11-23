//-----------------------------------------------------------------------------
// SplashScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using RacingGame.Graphics;
using RacingGame.GameLogic;
using RacingGame.Shaders;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// Splash screen
    /// </summary>
    class SplashScreen : IGameScreen
    {
        /// <summary>How long the loading card takes to fade away, in seconds</summary>
        const float CrossFadeTime = 0.45f;
        float startTime = -1;

		/// <summary>
		/// Unimplemented
		/// </summary>
		/// <param name="gameTime"></param>
		public void Update(GameTime gameTime)
		{

		}
        /// <summary>
        /// Render splash screen
        /// </summary>
        public bool Render()
        {
#if DEVSHOTS
            if (RacingGame.Helpers.DevShots.Active)
                RacingGame.Helpers.DevShots.Splash(startTime < 0 ? 0.0f : BaseGame.TotalTime - startTime);
#endif
            BaseGame.UI.UpdateCarInMenu();

            // The demo scene: sky, track, car, glows and shadows
            BaseGame.UI.RenderDemoScene();

            // Black bar and clock
#if DEVSHOTS
            bool videoShot = RacingGame.Helpers.DevShots.Active;   // the video wants the clean demo
#else
            const bool videoShot = false;
#endif
            if (!videoShot)
                BaseGame.UI.RenderBlackBar(518, 61);
            BaseGame.UI.RenderDemoClock();

            // Show Press Start to continue. 
            if (!videoShot && (int)(BaseGame.TotalTime / 0.375f) % 3 != 0)
                BaseGame.UI.Headers.RenderOnScreen(
                    BaseGame.CalcRectangleCenteredWithGivenHeight(
                    512, 518 + 61 / 2, 26, UIRenderer.PressStartGfxRect),
                    UIRenderer.PressStartGfxRect);

            // Fade the loading card away (same card, so it looks like a cross-fade)
            if (startTime < 0)
                startTime = BaseGame.TotalTime;
            float fade = 1 - (BaseGame.TotalTime - startTime) / CrossFadeTime;
            if (fade > 0)
                LoadingScreen.RenderCard(fade, 1, null, null, 0);
#if DEVSHOTS
            RacingGame.Helpers.DevShots.RenderOverlay();
#endif

            // Clicking or pressing start will go to the menu
            return Input.MouseLeftButtonJustPressed ||
                Input.KeyboardSpaceJustPressed ||
                Input.KeyboardKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Enter) ||
                Input.KeyboardEscapeJustPressed ||
                Input.GamePadStartPressed;
        }
    }
}
