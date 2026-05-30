using Microsoft.Xna.Framework;

namespace Blackjack
{
    /// <summary>
    /// Calculates player hand anchor positions based on current safe area and player count.
    /// </summary>
    internal static class GameplayPlayerLayoutCalculator
    {
        public static Vector2[] Calculate(Rectangle safeArea, int playerCount)
        {
            if (playerCount <= 0)
            {
                return new Vector2[0];
            }

            var playerCardOffset = new Vector2[playerCount];

            float bottomY = safeArea.Height * 0.41f;
            float topY = safeArea.Height * 0.36f;

            float leftMargin = safeArea.Width * 0.10f;
            float rightMargin = safeArea.Width * 0.15f;
            float usableWidth = safeArea.Width - leftMargin - rightMargin;

            for (int i = 0; i < playerCount; i++)
            {
                float xPosition;
                if (playerCount == 1)
                {
                    xPosition = safeArea.Width * 0.5f;
                }
                else
                {
                    float spacing = usableWidth / (playerCount - 1);
                    xPosition = leftMargin + (i * spacing);
                }

                float yPosition = (i % 2 == 0) ? bottomY : topY;
                playerCardOffset[i] = new Vector2(xPosition, yPosition);
            }

            return playerCardOffset;
        }
    }
}