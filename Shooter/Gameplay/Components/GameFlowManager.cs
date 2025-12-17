using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Shooter.Core.Components;
using Shooter.Core.Scenes;
using Shooter.Core.Services;
using Shooter.Gameplay.Systems;

namespace Shooter.Gameplay.Components
{
    /// <summary>
    /// Manages game flow: win/loss conditions, scene transitions, and fade effects.
    /// Similar to Unity's GameFlowManager in the FPS Microgame.
    ///
    /// USAGE:
    /// - Attach to a persistent entity in the scene (or player)
    /// - Listens for player death event
    /// - Handles fade transitions and scene reloading
    /// </summary>
    public class GameFlowManager : EntityComponent
    {
        // Configuration
        private float _fadeOutDuration = 1.5f;  // Time to fade to black (seconds)
        private float _displayMessageDuration = 10.0f; // Time to show "You Died" before exiting

        // State
        private bool _isTransitioning = false;
        private float _fadeAlpha = 0f;  // 0 = transparent, 1 = fully black
        private float _transitionTimer = 0f;
        private TransitionState _transitionState = TransitionState.None;

        // References
        private GraphicsDevice? _graphicsDevice;
        private SpriteBatch? _spriteBatch;
        private Texture2D? _pixel;  // 1x1 white texture for fade overlay
        private SpriteFont? _font;  // Font for "You Died" message

        private enum TransitionState
        {
            None,
            FadingOut,      // Fading to black
            ShowingMessage  // Showing "You Died" message before exit
        }

        public override void Initialize()
        {
            base.Initialize();

            // Subscribe to health component on this entity (GameFlowManager should be attached to the player)
            if (Owner != null)
            {
                var health = Owner.GetComponent<Health>();
                if (health != null)
                {
                    health.OnDeath += OnPlayerDeath;
                    Console.WriteLine("[GameFlowManager] Subscribed to player death event");
                }
                else
                {
                    Console.WriteLine("[GameFlowManager] WARNING: Owner entity has no Health component!");
                }
            }
            else
            {
                Console.WriteLine("[GameFlowManager] WARNING: GameFlowManager has no owner entity!");
            }
        }

        /// <summary>
        /// Load content for fade overlay and "You Died" message.
        /// Call this from Game.LoadContent() after SpriteBatch is created.
        /// </summary>
        public void LoadContent(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;

            // Create 1x1 white pixel for fade overlay
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            Console.WriteLine("[GameFlowManager] Content loaded");
        }

        /// <summary>
        /// Handle player death - start fade to "You Died" screen
        /// </summary>
        private void OnPlayerDeath(DamageInfo killingBlow)
        {
            if (_isTransitioning)
                return; // Already transitioning

            Console.WriteLine("[GameFlowManager] Player died! Starting death sequence...");

            // Start fade out transition
            _isTransitioning = true;
            _transitionState = TransitionState.FadingOut;
            _transitionTimer = 0f;
            _fadeAlpha = 0f;

            // Make player invincible during transition
            if (Owner != null)
            {
                var health = Owner.GetComponent<Health>();
                if (health != null)
                {
                    health.CanTakeDamage = false;
                    Console.WriteLine("[GameFlowManager] Player made invincible during transition");
                }
            }
        }

        public override void Update(Core.Components.GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_isTransitioning)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _transitionTimer += deltaTime;

            switch (_transitionState)
            {
                case TransitionState.FadingOut:
                    UpdateFadeOut();
                    break;

                case TransitionState.ShowingMessage:
                    UpdateShowingMessage();
                    break;
            }
        }

        private void UpdateFadeOut()
        {
            // Increase fade alpha from 0 to 1
            _fadeAlpha = Math.Min(1f, _transitionTimer / _fadeOutDuration);

            if (_fadeAlpha >= 1f)
            {
                // Fade out complete, start showing "You Died" message
                _transitionState = TransitionState.ShowingMessage;
                _transitionTimer = 0f;
                Console.WriteLine("[GameFlowManager] Fade out complete, showing death message...");
            }
        }

        private void UpdateShowingMessage()
        {
            if (_transitionTimer >= _displayMessageDuration)
            {
                // Message display complete, exit the game
                Console.WriteLine("[GameFlowManager] Exiting game...");
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Draw the fade overlay and "You Died" message.
        /// Call this from your Draw() method AFTER everything else (so it's on top).
        /// </summary>
        public override void Draw(Core.Components.GameTime gameTime)
        {
            base.Draw(gameTime);

            if (_spriteBatch == null || _pixel == null || _graphicsDevice == null)
                return;

            if (_fadeAlpha <= 0f)
                return; // Nothing to draw

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            var viewport = _graphicsDevice.Viewport;
            var screenRect = new Rectangle(0, 0, viewport.Width, viewport.Height);
            var fadeColor = Color.Black * _fadeAlpha;

            // Draw black overlay
            _spriteBatch.Draw(_pixel, screenRect, fadeColor);

            // Draw "You Died" message if fully faded and in message state
            if (_fadeAlpha >= 1f && _transitionState == TransitionState.ShowingMessage && _font != null)
            {
                string message = "You Died";
                Vector2 textSize = _font.MeasureString(message);
                Vector2 textPos = new Vector2(
                    (viewport.Width - textSize.X) / 2,
                    (viewport.Height - textSize.Y) / 2
                );

                // Draw with slight shadow for readability
                _spriteBatch.DrawString(_font, message, textPos + new Vector2(2, 2), new Color(0, 0, 0, 255));
                _spriteBatch.DrawString(_font, message, textPos, Color.White);
            }

            _spriteBatch.End();
        }

        /// <summary>
        /// Get whether a transition is currently in progress
        /// </summary>
        public bool IsTransitioning => _isTransitioning;
    }
}
