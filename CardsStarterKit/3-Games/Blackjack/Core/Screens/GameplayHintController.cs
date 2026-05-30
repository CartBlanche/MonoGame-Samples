using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    /// <summary>
    /// Encapsulates gameplay hint state transitions and rendering.
    /// </summary>
    internal sealed class GameplayHintController
    {
        private readonly Rectangle safeArea;
        private readonly Texture2D gradientTexture;

        private bool showHints;
        private int currentHintIndex = -1;
        private TimeSpan timeSinceLastHint;
        private readonly HashSet<int> shownHints = new HashSet<int>();
        private BlackjackGameState lastGameState = BlackjackGameState.Betting;

        public GameplayHintController(Rectangle safeArea, Texture2D gradientTexture, bool showHints)
        {
            this.safeArea = safeArea;
            this.gradientTexture = gradientTexture;
            this.showHints = showHints;
        }

        public void Update(GameTime gameTime, BlackjackCardGame blackJackGame)
        {
            if (!showHints || blackJackGame == null)
                return;

            var currentState = blackJackGame.State;
            var betComponent = blackJackGame.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
            if (betComponent == null)
                return;

            var player = blackJackGame.Players.FirstOrDefault() as BlackjackPlayer;
            bool hasBet = player?.BetAmount > 0;

            int desiredHint = -1;

            if (currentState == BlackjackGameState.Betting)
            {
                if (!hasBet && !shownHints.Contains(0))
                {
                    desiredHint = 0;
                }
                else if (hasBet && !shownHints.Contains(1))
                {
                    desiredHint = 1;
                }
            }
            else if ((currentState == BlackjackGameState.Playing || currentState == BlackjackGameState.Dealing)
                     && !shownHints.Contains(2))
            {
                desiredHint = 2;
            }

            bool stateChanged = currentState != lastGameState;
            bool enoughTimePassed = currentHintIndex == -1 ||
                                   gameTime.TotalGameTime - timeSinceLastHint > TimeSpan.FromSeconds(5);

            if (desiredHint != -1 && desiredHint != currentHintIndex)
            {
                if (stateChanged || enoughTimePassed)
                {
                    currentHintIndex = desiredHint;
                    timeSinceLastHint = gameTime.TotalGameTime;
                    shownHints.Add(desiredHint);
                }
            }
            else if (desiredHint == 2 && currentHintIndex != 2 &&
                    (currentState == BlackjackGameState.Playing || currentState == BlackjackGameState.Dealing))
            {
                currentHintIndex = desiredHint;
                timeSinceLastHint = gameTime.TotalGameTime;
                shownHints.Add(desiredHint);
            }
            else if (currentHintIndex != -1 && desiredHint == -1 &&
                    gameTime.TotalGameTime - timeSinceLastHint > TimeSpan.FromSeconds(5))
            {
                currentHintIndex = -1;

                if (shownHints.Count >= 3)
                {
                    showHints = false;
                }
            }

            lastGameState = currentState;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont font, BlackjackCardGame blackJackGame)
        {
            if (!showHints || currentHintIndex < 0 || blackJackGame == null)
                return;

            const int hPad = 32;
            const int vPad = 16;

            Rectangle backgroundRectangle = Rectangle.Empty;
            string message = string.Empty;

            var betComponent = blackJackGame.Game.Components.OfType<BetGameComponent>().FirstOrDefault();
            if (betComponent == null)
                return;

            switch (currentHintIndex)
            {
                case 0:
                    message = Resources.HintPlaceBet;
                    Vector2 textSize0 = font.MeasureString(message);
                    Rectangle dealBounds = betComponent.DealButtonBounds;

                    backgroundRectangle = new Rectangle(
                        dealBounds.Right + 120,
                        dealBounds.Y - 20,
                        (int)(textSize0.X + hPad * 1.5),
                        (int)(textSize0.Y + vPad * 1.5));
                    break;

                case 1:
                    message = Resources.HintRemoveBet;
                    Vector2 textSize1 = font.MeasureString(message);
                    Vector2 chipStackPos = betComponent.GetHumanPlayerChipStackPosition();

                    backgroundRectangle = new Rectangle(
                        (int)chipStackPos.X - (int)(textSize1.X / 2) - hPad + 180,
                        (int)chipStackPos.Y - (int)textSize1.Y - vPad * 2 - 80,
                        (int)(textSize1.X + hPad * 1.5),
                        (int)(textSize1.Y + vPad * 1.5));
                    break;

                case 2:
                    message = Resources.HintGameGoal;
                    Vector2 textSize2 = font.MeasureString(message);
                    backgroundRectangle = new Rectangle(
                        safeArea.Center.X - (int)(textSize2.X / 2) - hPad,
                        safeArea.Top + 140,
                        (int)(textSize2.X + hPad * 1.5),
                        (int)(textSize2.Y + vPad * 1.5));
                    break;
            }

            Vector2 textPosition = new Vector2(backgroundRectangle.X + hPad, backgroundRectangle.Y + vPad - 7);

            spriteBatch.Draw(gradientTexture, backgroundRectangle, Color.Black * 0.7f);

            int borderThickness = 3;
            Color borderColor = Color.LimeGreen;

            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.X, backgroundRectangle.Y, backgroundRectangle.Width, borderThickness), borderColor);
            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.X, backgroundRectangle.Bottom - borderThickness, backgroundRectangle.Width, borderThickness), borderColor);
            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.X, backgroundRectangle.Y, borderThickness, backgroundRectangle.Height), borderColor);
            spriteBatch.Draw(gradientTexture, new Rectangle(backgroundRectangle.Right - borderThickness, backgroundRectangle.Y, borderThickness, backgroundRectangle.Height), borderColor);

            spriteBatch.DrawString(font, message, textPosition, Color.White);
        }

        public bool IsActive => showHints && currentHintIndex >= 0;
    }
}