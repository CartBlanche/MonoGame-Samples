//-----------------------------------------------------------------------------
// BlackjackGame.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Input.Touch;
using CardsFramework;
using CardsFramework.Core;
using System.Globalization;


namespace Blackjack
{
    /// <summary>
    /// This is the main game type.
    /// </summary>
    public class BlackjackGame : Game
    {
        GraphicsDeviceManager graphicsDeviceManager;
        ScreenManager screenManager;
        AchievementToastManager achievementToasts;
        MouseState lastMouseState;

        /// <summary>
        /// Initializes a new instance of the game.
        /// </summary>
        public BlackjackGame()
        {
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";

            if (UIUtility.IsMobile)
            {
                graphicsDeviceManager.IsFullScreen = true;
                IsMouseVisible = false;
            }
            else if (UIUtility.IsDesktop)
            {
                graphicsDeviceManager.IsFullScreen = false;
                graphicsDeviceManager.PreferredBackBufferWidth = ScreenManager.BASE_BUFFER_WIDTH;
                graphicsDeviceManager.PreferredBackBufferHeight = ScreenManager.BASE_BUFFER_HEIGHT;
                IsMouseVisible = true;
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            screenManager = new ScreenManager(this, () => GameSettings.Instance.Language);

            // Show splash screen on startup (it will add BackgroundScreen and MainMenuScreen after 3 seconds)
            screenManager.AddScreen(new SplashScreen(() => new GameScreen[]
            {
                new BackgroundScreen(),
                new MainMenuScreen()
            }), null);

            Components.Add(screenManager);

            // Initialize sound system
            AudioManager.Initialize(this,
                getSoundVolume: () => GameSettings.Instance.SoundVolume,
                getMusicVolume: () => GameSettings.Instance.MusicVolume);
        }

        protected override void Initialize()
        {
            BlackjackAchievements.RegisterCatalogDefinitions();
            BlackjackAchievements.AchievementUnlocked += OnAchievementUnlocked;
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Game-specific sounds.
            AudioManager.LoadSound("Bet", "Bet");
            AudioManager.LoadSound("CardFlip", "Flip");
            AudioManager.LoadSound("CardsShuffle", "Shuffle");
            AudioManager.LoadSound("Deal", "Deal");
            AudioManager.LoadSound("Click", "Click");
            AudioManager.LoadSound("Win", "Win");
            AudioManager.LoadSound("CardRemoval", "CardRemoval");

            // Game-specific music and playlist defaults.
            AudioManager.LoadSong("sunsides-neo-soul-night-210447", "NeoSoul");
            AudioManager.LoadSong("sunsides-jazzy-soul-207549", "JazzySoul");
            AudioManager.LoadSong("freesound_community-casino-ambiance-19130", "CasinoAmbiance");
            AudioManager.SetPlaylist("NeoSoul", "JazzySoul");

            achievementToasts = new AchievementToastManager(GraphicsDevice, screenManager);
            achievementToasts.ToastActivated += OnAchievementToastActivated;

            base.LoadContent();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (UIUtility.IsDesktop)
            {
                KeyboardState keyboardState = Keyboard.GetState();

                // Check if Alt+Enter is pressed
                if ((keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt))
                    && keyboardState.IsKeyDown(Keys.Enter))
                {
                    // Use a static flag to prevent multiple toggles on held keys
                    if (!wasFullscreenTogglePressed)
                    {
                        graphicsDeviceManager.ToggleFullScreen();

                        wasFullscreenTogglePressed = true;
                    }
                }
                else
                {
                    wasFullscreenTogglePressed = false;
                }
            }

            PumpNetworkToasts();
            achievementToasts?.Update(gameTime);
            HandleAchievementToastInput();
            lastMouseState = Mouse.GetState();

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clear the screen to prevent rendering artifacts when switching between windowed and fullscreen
            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
            achievementToasts?.Draw();
        }

        protected override void UnloadContent()
        {
            BlackjackAchievements.AchievementUnlocked -= OnAchievementUnlocked;

            if (achievementToasts != null)
                achievementToasts.ToastActivated -= OnAchievementToastActivated;

            achievementToasts?.Dispose();
            achievementToasts = null;

            base.UnloadContent();
        }

        // Flag to prevent multiple fullscreen toggles when keys are held
        private bool wasFullscreenTogglePressed = false;

        private void OnAchievementUnlocked(string achievementKey, string displayName)
        {
            achievementToasts?.Enqueue(achievementKey, displayName);
        }

        private void PumpNetworkToasts()
        {
            var component = Services.GetService(typeof(NetworkSessionComponent)) as NetworkSessionComponent;
            if (component == null || achievementToasts == null)
                return;

            while (component.TryDequeueSystemMessage(out var message))
            {
                string text = FormatNetworkToastMessage(message);
                if (!string.IsNullOrWhiteSpace(text))
                    achievementToasts.EnqueueNetworkMessage(text);
            }
        }

        private static string FormatNetworkToastMessage(NetworkSessionComponent.SystemMessage message)
        {
            if (message == null)
                return string.Empty;

            switch (message.Kind)
            {
                case NetworkSessionComponent.SystemMessageKind.GamerJoined:
                    return string.Format(Resources.MessageGamerJoined, message.GamerTag ?? string.Empty);
                case NetworkSessionComponent.SystemMessageKind.GamerLeft:
                    return string.Format(Resources.MessageGamerLeft, message.GamerTag ?? string.Empty);
                case NetworkSessionComponent.SystemMessageKind.SessionEnded:
                    return message.Message ?? "Session ended.";
                case NetworkSessionComponent.SystemMessageKind.NetworkError:
                    return message.Message ?? "Network error.";
                default:
                    return message.Message ?? string.Empty;
            }
        }

        private void OnAchievementToastActivated(object sender, AchievementToastManager.ToastActivatedEventArgs e)
        {
            if (e == null)
                return;

            OpenAchievementsScreen();
        }

        private void HandleAchievementToastInput()
        {
            if (achievementToasts == null)
                return;

            Matrix inverse = Matrix.Invert(screenManager.GlobalTransformation);

            MouseState mouse = Mouse.GetState();
            bool mouseReleased = mouse.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released;
            if (mouseReleased)
            {
                Vector2 logical = Vector2.Transform(new Vector2(mouse.X, mouse.Y), inverse);
                if (achievementToasts.HandleMouseRelease(new Point((int)logical.X, (int)logical.Y)))
                    return;
            }

            foreach (TouchLocation touch in TouchPanel.GetState())
            {
                if (touch.State != TouchLocationState.Pressed)
                    continue;

                Vector2 logical = Vector2.Transform(touch.Position, inverse);
                if (achievementToasts.HandleTap(logical))
                    return;
            }
        }

        private void OpenAchievementsScreen()
        {
            if (IsAchievementsScreenOpen())
                return;

            screenManager.AddScreen(new BlackjackAchievementsScreen(), null);
        }

        private bool IsAchievementsScreenOpen()
        {
            foreach (GameScreen screen in screenManager.GetScreens())
            {
                if (screen is BlackjackAchievementsScreen)
                    return true;
            }

            return false;
        }
    }
}