//-----------------------------------------------------------------------------
// BlackjackCardGame.Rendering.cs
//
// Partial class containing all draw/rendering methods for BlackjackCardGame.
//-----------------------------------------------------------------------------

using System;
using CardsFramework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    partial class BlackjackCardGame
    {
        /// <summary>
        /// Renders the visual elements for which the game itself is responsible.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to 
        /// this method.</param>
        public void Draw(GameTime gameTime)
        {
            screenManager.SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null,
                screenManager.GlobalTransformation);

            switch (State)
            {
                case BlackjackGameState.Playing:
                {
                    ShowPlayerValues();
                }
                    break;
                case BlackjackGameState.GameOver:
                {
                }
                    break;
                case BlackjackGameState.RoundEnd:
                {
                    if (dealerHandComponent.EstimatedTimeForAnimationsCompletion() == TimeSpan.Zero)
                    {
                        ShowDealerValue();
                    }

                    ShowPlayerValues();
                }
                    break;
                default: break;
            }

            screenManager.SpriteBatch.End();
        }

        /// <summary>
        /// Draws the dealer's hand value on the screen.
        /// </summary>
        private void ShowDealerValue()
        {
            // Calculate the value to display
            string dealerValue = dealerPlayer.FirstValue.ToString();
            if (dealerPlayer.FirstValueConsiderAce)
            {
                if (dealerPlayer.FirstValue + 10 == 21)
                {
                    dealerValue = "21";
                }
                else
                {
                    dealerValue += @"\" + (dealerPlayer.FirstValue + 10).ToString();
                }
            }

            // Draw the value
            Vector2 measure = Font.MeasureString(dealerValue);
            Vector2 position = GameTable.DealerPosition - new Vector2(measure.X + 20, 0);

            DrawTextWithBackground(dealerValue, position, Color.White);
        }

        /// <summary>
        /// Draws the players' hand value on the screen.
        /// </summary>
        private void ShowPlayerValues()
        {
            Color color = Color.Black;
            Player currentPlayer = GetCurrentPlayer();

            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {
                BlackjackPlayer player = (BlackjackPlayer)players[playerIndex];
                // The current player's hand value will be red to serve as a visual
                // prompt for who the active player is
                if (player == currentPlayer)
                {
                    color = Color.Red;
                }
                else
                {
                    color = Color.White;
                }

                // Calculate the values to draw
                string playerHandValueText;
                string playerSecondHandValueText = null;
                if (!animatedHands[playerIndex].IsAnimating)
                {
                    if (player.FirstValue > 0)
                    {
                        playerHandValueText = player.FirstValue.ToString();
                        // Take the fact that an ace is wither 1 or 11 into 
                        // consideration when calculating the value to display
                        // Since the ace already counts as 1, we add 10 to get
                        // the alternate value
                        if (player.FirstValueConsiderAce)
                        {
                            if (player.FirstValue + 10 == 21)
                            {
                                playerHandValueText = "21";
                            }
                            else
                            {
                                playerHandValueText += @"\" + (player.FirstValue + 10).ToString();
                            }
                        }

                        playerHandValueTexts[player] = playerHandValueText;
                    }
                    else
                    {
                        playerHandValueText = null;
                    }

                    if (player.IsSplit)
                    {
                        // If the player has performed a split, he has an additional
                        // hand with its own value
                        if (player.SecondValue > 0)
                        {
                            playerSecondHandValueText = player.SecondValue.ToString();
                            if (player.SecondValueConsiderAce)
                            {
                                if (player.SecondValue + 10 == 21)
                                {
                                    playerSecondHandValueText = "21";
                                }
                                else
                                {
                                    playerSecondHandValueText += @"\" + (player.SecondValue + 10).ToString();
                                }
                            }

                            playerSecondHandValueTexts[player] = playerSecondHandValueText;
                        }
                        else
                        {
                            playerSecondHandValueText = null;
                        }
                    }
                }
                else
                {
                    playerHandValueTexts.TryGetValue(player, out playerHandValueText);
                    playerSecondHandValueTexts.TryGetValue(
                        player, out playerSecondHandValueText);
                }

                if (player.IsSplit)
                {
                    // If the player has performed a split, mark the active hand alone
                    // with a red value
                    color = player.CurrentHandType == HandTypes.First &&
                            player == currentPlayer
                        ? Color.Red
                        : Color.White;

                    if (playerHandValueText != null)
                    {
                        DrawValue(animatedHands[playerIndex], playerIndex, playerHandValueText, color, true);
                    }

                    color = player.CurrentHandType == HandTypes.Second &&
                            player == currentPlayer
                        ? Color.Red
                        : Color.White;

                    if (playerSecondHandValueText != null)
                    {
                        DrawValue(animatedSecondHands[playerIndex], playerIndex, playerSecondHandValueText,
                            color, false);
                    }

                    // Grey out inactive split hand cards (only for human/local players during their turn)
                    // AI players should not split yet, so this should only affect human players
                    if (player == currentPlayer)
                    {
                        Color inactiveCardColor = new Color(128, 128, 128, 180); // Same as disabled chips
                        Color activeCardColor = Color.White;

                        // Update first hand cards
                        Color firstHandColor = player.CurrentHandType == HandTypes.First
                            ? activeCardColor
                            : inactiveCardColor;
                        foreach (var card in animatedHands[playerIndex].AnimatedCards)
                        {
                            card.Color = firstHandColor;
                        }

                        // Update second hand cards
                        Color secondHandColor = player.CurrentHandType == HandTypes.Second
                            ? activeCardColor
                            : inactiveCardColor;
                        foreach (var card in animatedSecondHands[playerIndex].AnimatedCards)
                        {
                            card.Color = secondHandColor;
                        }
                    }
                    else
                    {
                        // Reset colors for non-current players
                        foreach (var card in animatedHands[playerIndex].AnimatedCards)
                        {
                            card.Color = Color.White;
                        }

                        foreach (var card in animatedSecondHands[playerIndex].AnimatedCards)
                        {
                            card.Color = Color.White;
                        }
                    }
                }
                else
                {
                    // If there is a value to draw, draw it
                    if (playerHandValueText != null)
                    {
                        DrawValue(animatedHands[playerIndex], playerIndex, playerHandValueText, color);
                    }
                }
            }
        }

        /// <summary>
        /// Draws the value of a player's hand above his top card.
        /// The value will be drawn over a black background.
        /// </summary>
        /// <param name="animatedHand">The player's hand.</param>
        /// <param name="place">A number representing the player's position on the
        /// game table.</param>
        /// <param name="value">The value to draw.</param>
        /// <param name="valueColor">The color in which to draw the value.</param>
        private void DrawValue(AnimatedHandGameComponent animatedHand, int place,
            string value, Color valueColor, bool isFirstHandInSplit = false)
        {
            Hand hand = animatedHand.Hand;

            // Position the value to the right of the first card, not moving with each new card
            Vector2 position = GameTable.PlaceOrder(place) +
                               animatedHand.GetCardRelativePosition(0); // Use first card (index 0) instead of last card
            Vector2 measure = Font.MeasureString(value);
            int cardWidth = cardsAssets["CardBack_" + Theme].Bounds.Width;
            int cardHeight = cardsAssets["CardBack_" + Theme].Bounds.Height;

            // Position at bottom-right corner of first card, below the card edge
            position.X += cardWidth - (measure.X / 2); // Right edge of card, centered
            position.Y += cardHeight + 5; // Below the card, with small padding

            // During split, compensate for the hand offset to keep score visible
            // The hand was shifted left by secondHandOffset, but the score should stay readable
            if (isFirstHandInSplit)
            {
                // The position already includes the hand's left offset via GetCardRelativePosition
                // We don't want the score to follow that offset completely, so move it back toward center
                position.X -= secondHandOffset.X - 5.0f; // Compensate by moving right
            }

            DrawTextWithBackground(value, position, valueColor);
        }

        /// <summary>
        /// Draws text with a black background rectangle and gold border.
        /// </summary>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">The position to draw at.</param>
        /// <param name="textColor">The color of the text.</param>
        private void DrawTextWithBackground(string text, Vector2 position, Color textColor)
        {
            DrawTextWithBackground(text, position, textColor, Color.Black, Color.Gold);
        }

        /// <summary>
        /// Draws text with a background rectangle and optional border.
        /// </summary>
        /// <param name="text">The text to draw.</param>
        /// <param name="position">The position to draw at.</param>
        /// <param name="textColor">The color of the text.</param>
        /// <param name="backgroundColor">The color of the background rectangle.</param>
        /// <param name="borderColor">The color of the 2-pixel border.</param>
        private void DrawTextWithBackground(string text, Vector2 position, Color textColor, Color backgroundColor,
            Color borderColor)
        {
            Vector2 measure = Font.MeasureString(text);
            const int borderWidth = 2;
            const int padding = 2; // Padding around text
            const int verticalOffset = -1; // Fine-tune vertical centering (negative = move up)

            // Calculate box dimensions based on text size
            int boxWidth = (int)measure.X + (padding * 2);
            int boxHeight = (int)measure.Y + (padding * 2);

            // Position is the top-left corner where text should start (for backward compatibility)
            // Calculate the box top-left to add padding around the text
            Vector2 boxTopLeft = new Vector2(position.X - padding, position.Y - padding);

            // Draw border rectangle (outer)
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle(
                    (int)boxTopLeft.X - borderWidth,
                    (int)boxTopLeft.Y - borderWidth,
                    boxWidth + (borderWidth * 2),
                    boxHeight + (borderWidth * 2)),
                borderColor);

            // Draw background rectangle (inner)
            screenManager.SpriteBatch.Draw(screenManager.BlankTexture,
                new Rectangle(
                    (int)boxTopLeft.X,
                    (int)boxTopLeft.Y,
                    boxWidth,
                    boxHeight),
                backgroundColor);

            // Draw text at the original position with vertical offset for fine-tuning
            Vector2 adjustedPosition = new Vector2(position.X, position.Y + verticalOffset);
            screenManager.SpriteBatch.DrawString(Font, text, adjustedPosition, textColor);
        }
    }
}
