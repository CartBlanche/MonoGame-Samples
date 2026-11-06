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
            BaseGame.UI.UpdateCarInMenu();

            ShadowMapShader.PrepareGameShadows();

            // Render background and black bar
            BaseGame.UI.RenderGameBackground();
            BaseGame.UI.RenderMenuTrackBackground();
            BaseGame.UI.RenderBlackBar(518, 61);

            // Show shadows we calculated above
            if (BaseGame.AllowShadowMapping)
                ShaderEffect.shadowMapping.ShowShadows();

            // Show Press Start to continue. 
            if ((int)(BaseGame.TotalTime / 0.375f) % 3 != 0)
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

            // Clicking or pressing start will go to the menu
            return Input.MouseLeftButtonJustPressed ||
                Input.KeyboardSpaceJustPressed ||
                Input.KeyboardKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Enter) ||
                Input.KeyboardEscapeJustPressed ||
                Input.GamePadStartPressed;
        }
    }
}
