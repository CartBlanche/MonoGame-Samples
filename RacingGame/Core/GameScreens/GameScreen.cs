//-----------------------------------------------------------------------------
// GameScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using RacingGame.GameLogic;
using RacingGame.Graphics;
using RacingGame.Helpers;
using RacingGame.Properties;
using RacingGame.Landscapes;
using Microsoft.Xna.Framework;
using RacingGame.Shaders;
using RacingGame.Sounds;

namespace RacingGame.GameScreens
{
    /// <summary>
    /// GameScreen, just manages the on screen display for the game.
    /// </summary>
    class GameScreen : IGameScreen
    {
        /// <summary>
        /// Create game screen
        /// </summary>
        public GameScreen()
        {
            // Load level (built-in or custom)
            if (TrackSelection.CustomTrack != null)
                RacingGameManager.LoadCustomTrack(TrackSelection.CustomTrack);
            else
                RacingGameManager.LoadLevel(TrackSelection.SelectedTrack);

            // Reset player variables (start new game, reset time and position)
            RacingGameManager.Player.Reset();

            // Lighting setting and time of day of this race (the track's own or the override)
            BaseGame.Lighting.BeginRace(TrackLighting.RaceTimeOfDay());

            // Fix light direction (was changed by CarSelection screen!)
            // LightDirection will normalize
            BaseGame.LightDirection = LensFlare.DefaultLightPos;

            // Start gear sound
            Sound.StartGearSound();

            // Play game music
            Sound.Play(Sound.Sounds.GameMusic);
        }
        /// <summary>
        /// Render the cars of other players. Nothing in single player.
        /// </summary>
        protected virtual void RenderOtherCars()
        {
        }

        /// <summary>
        /// Does the player want to leave the game screen (back to the menu)?
        /// </summary>
        protected virtual bool WantsToLeave()
        {
            return Input.KeyboardEscapeJustPressed ||
                Input.GamePadBackJustPressed ||
                (RacingGameManager.Player.GameOver &&
                (Input.KeyboardSpaceJustPressed ||
                Input.KeyboardKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Enter) ||
                Input.GamePadAJustPressed ||
                Input.GamePadBJustPressed ||
                Input.GamePadXJustPressed ||
                Input.GamePadXJustPressed ||
                Input.MouseLeftButtonJustPressed));
        }

        /// <summary>
        /// Race against the ghost of the best lap. Not in multiplayer.
        /// </summary>
        protected virtual bool ShowGhostCar
        {
            get { return true; }
        }

		/// <summary>
		/// Unimplemented
		/// </summary>
		/// <param name="gameTime"></param>
		public virtual void Update(GameTime gameTime)
		{

		}
        /// <summary>
        /// The shadow car (best replay), once the game has started.
        /// </summary>
        void RenderGhostCar()
        {
            // Use data from best replay for the shadow car
            Matrix bestReplayCarMatrix =
                RacingGameManager.Landscape.BestReplay.GetCarMatrixAtTime(
                RacingGameManager.Player.GameTimeMilliseconds / 1000.0f);
            // For rendering rotate car to stay correctly on the road
            bestReplayCarMatrix =
                Matrix.CreateRotationX(MathHelper.Pi / 2.0f) *
                Matrix.CreateRotationZ(MathHelper.Pi) *
                bestReplayCarMatrix;

            // Also render the shadow car (if the game has started)!
            if (ShowGhostCar &&
                RacingGameManager.Player.GameTimeMilliseconds > 0)
                RacingGameManager.CarModel.RenderCar(
                    0, RacingGameManager.CarColor,
                    true, bestReplayCarMatrix);
        }

		/// <summary>
        /// Render game screen. Called each frame.
        /// </summary>
        public virtual bool Render()
        {
            // Lighting and shadow maps, then the scene shared with the menu demo
            BaseGame.UI.PrepareScene(true);

            // This starts both menu and in game post screen shader!
			if( BaseGame.UsePostScreenShaders)
            	BaseGame.UI.PostScreenGlowShader.Start();

            BaseGame.UI.RenderScene(
                RacingGameManager.currentCarNumber,
                RacingGameManager.CarColor,
                // Other players' cars (multiplayer)
                RenderOtherCars,
                RenderGhostCar);

            // Apply post screen shader here before doing the UI
			if (BaseGame.UsePostScreenShaders)
            	BaseGame.UI.PostScreenGlowShader.Show();

            // Play motor sound
            Sound.UpdateGearSound(RacingGameManager.Player.Speed,
                RacingGameManager.Player.Acceleration);

            // Show on screen UI for the game.
            // Note: Could be improved by using the latest checkpoints and
            // check times this way!
            BaseGame.UI.RenderGameUI(
                (int)RacingGameManager.Player.GameTimeMilliseconds,
                // Best time and current lap
                (int)RacingGameManager.Player.BestTimeMilliseconds,
                RacingGameManager.Player.CurrentLap + 1,
                RacingGameManager.Player.Speed * CarPhysics.MeterPerSecToMph,
                // Gear logic with sound (could be improved ^^)
                1 + (int)(5 * RacingGameManager.Player.Speed /
                CarPhysics.MaxPossibleSpeed),
                // Motormeter
                0.5f * RacingGameManager.Player.Speed /
                CarPhysics.MaxPossibleSpeed +
                // This could be improved
                0.5f * RacingGameManager.Player.Acceleration,
                RacingGameManager.Landscape.CurrentTrackName,
                Highscores.GetTop5LapTimes(TrackSelection.HighscoreLevel));

            if (WantsToLeave())
            {
                // Stop motor sound
                Sound.StopGearSound();

                // Play menu music again
                Sound.Play(Sound.Sounds.MenuMusic);

                // Menus use the day look again
                BaseGame.Lighting.EndGame();

                // Return to menu
                return true;
            }

            return false;
        }
    }
}
