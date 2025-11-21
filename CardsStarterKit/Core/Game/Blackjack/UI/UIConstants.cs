//-----------------------------------------------------------------------------
// UIConstants.cs
//
// UI scaling constants for resolution-independent rendering in Blackjack
//-----------------------------------------------------------------------------

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Blackjack
{
    /// <summary>
    /// Defines proportional UI constants that scale with screen resolution
    /// All values are percentages or ratios of screen dimensions
    /// </summary>
    public static class UIConstants
    {
        // Button dimensions (as percentage of screen dimensions)
        public const float ButtonWidthRatio = 0.078f;         // ~100px at 1280px width
        public const float ButtonHeightRatio = 0.069f;        // ~50px at 720px height
        public const float WideButtonWidthRatio = 0.156f;     // ~200px at 1280px width

        // Spacing and padding (as percentage of screen dimensions)
        public const float SmallPaddingRatio = 0.008f;        // ~10px at 1280px
        public const float MediumPaddingRatio = 0.047f;       // ~60px at 1280px
        public const float ButtonSpacingRatio = 0.086f;       // ~110px at 1280px
        public const float ChipSpacingRatio = 0.063f;         // ~80px at 1280px

        // Card and chip scaling for 7 players (scaled down from 3 players)
        public const float CardScaleRatio = 0.65f;            // 65% of original size to fit 7 players
        public const float ChipCircleRadiusRatio = 0.055f;    // ~40px radius at 720px height (was ~70px for 3 players)
        public const float ChipScaleRatio = 0.7f;             // 70% of original chip size
        public const float RingOffsetYRatio = 0.153f;         // ~110px at 720px height - distance below card position for chip circle

        // Card dimensions and spacing (based on original card size 71x96)
        public const float CardWidthRatio = 0.055f;           // ~71px at 1280px width
        public const float CardHeightRatio = 0.133f;          // ~96px at 720px height
        public const float DealerCardSpacingRatio = 0.023f;   // ~30px at 1280px width
        public const float PlayerCardHorizontalSpacingRatio = 0.020f; // ~25px at 1280px width
        public const float PlayerCardVerticalSpacingRatio = 0.042f;   // ~30px at 720px height
        public const float SecondHandOffsetXRatio = 0.078f;   // ~100px at 1280px width
        public const float SecondHandOffsetYRatio = 0.035f;   // ~25px at 720px height
        public const float BetSecondHandOffsetXRatio = 0.020f; // ~25px at 1280px width
        public const float BetSecondHandOffsetYRatio = 0.042f; // ~30px at 720px height

        // Shuffle animation parameters
        public const float ShuffleSplitDistanceRatio = 0.117f; // ~150px at 1280px width
        public const float ShuffleCascadeHeightRatio = 0.111f; // ~80px at 720px height
        public const float DeckLayerOffsetRatio = 0.0012f;     // ~1.5px at 1280px width

        // Frame size for UI elements
        public const float FrameWidthRatio = 0.141f;          // ~180px at 1280px width
        public const float FrameHeightRatio = 0.250f;         // ~180px at 720px height

        // Insurance position
        public const float InsuranceYPositionRatio = 0.167f;  // ~120px at 720px height

        // Text scaling
        public const float RegularTextScale = 0.6f;
        public const float SmallTextScale = 0.5f;

        /// <summary>
        /// Calculate actual pixel value from screen width ratio
        /// </summary>
        public static int GetWidthScaled(int screenWidth, float ratio)
        {
            return (int)(screenWidth * ratio);
        }

        /// <summary>
        /// Calculate actual pixel value from screen height ratio
        /// </summary>
        public static int GetHeightScaled(int screenHeight, float ratio)
        {
            return (int)(screenHeight * ratio);
        }

        /// <summary>
        /// Get standard button width
        /// </summary>
        public static int GetButtonWidth(int screenWidth)
        {
            return GetWidthScaled(screenWidth, ButtonWidthRatio);
        }

        /// <summary>
        /// Get wide button width (for insurance, new game, etc.)
        /// </summary>
        public static int GetWideButtonWidth(int screenWidth)
        {
            return GetWidthScaled(screenWidth, WideButtonWidthRatio);
        }

        /// <summary>
        /// Get button height
        /// </summary>
        public static int GetButtonHeight(int screenHeight)
        {
            return GetHeightScaled(screenHeight, ButtonHeightRatio);
        }

        /// <summary>
        /// Get small padding (10px equivalent)
        /// </summary>
        public static int GetSmallPadding(int screenWidth)
        {
            return GetWidthScaled(screenWidth, SmallPaddingRatio);
        }

        /// <summary>
        /// Get medium padding (60px equivalent)
        /// </summary>
        public static int GetMediumPadding(int screenHeight)
        {
            return GetHeightScaled(screenHeight, MediumPaddingRatio);
        }

        /// <summary>
        /// Get button spacing (110px equivalent)
        /// </summary>
        public static int GetButtonSpacing(int screenWidth)
        {
            return GetWidthScaled(screenWidth, ButtonSpacingRatio);
        }

        /// <summary>
        /// Get chip spacing (80px equivalent)
        /// </summary>
        public static int GetChipSpacing(int screenHeight)
        {
            return GetHeightScaled(screenHeight, ChipSpacingRatio);
        }

        /// <summary>
        /// Get chip circle radius for player positions
        /// </summary>
        public static int GetChipCircleRadius(int screenHeight)
        {
            return GetHeightScaled(screenHeight, ChipCircleRadiusRatio);
        }

        /// <summary>
        /// Get card scale for 7 players
        /// </summary>
        public static float GetCardScale()
        {
            return CardScaleRatio;
        }

        /// <summary>
        /// Get chip scale for 7 players
        /// </summary>
        public static float GetChipScale()
        {
            return ChipScaleRatio;
        }

        /// <summary>
        /// Get ring offset (distance below card position for chip circle)
        /// </summary>
        public static Vector2 GetRingOffset(int screenHeight)
        {
            return new Vector2(0, GetHeightScaled(screenHeight, RingOffsetYRatio));
        }

        /// <summary>
        /// Get card width based on screen width
        /// </summary>
        public static int GetCardWidth(int screenWidth)
        {
            return GetWidthScaled(screenWidth, CardWidthRatio);
        }

        /// <summary>
        /// Get card height based on screen height
        /// </summary>
        public static int GetCardHeight(int screenHeight)
        {
            return GetHeightScaled(screenHeight, CardHeightRatio);
        }

        /// <summary>
        /// Get card size as Vector2
        /// </summary>
        public static Vector2 GetCardSize(int screenWidth, int screenHeight)
        {
            return new Vector2(GetCardWidth(screenWidth), GetCardHeight(screenHeight));
        }

        /// <summary>
        /// Get dealer card spacing (horizontal spacing between cards)
        /// </summary>
        public static int GetDealerCardSpacing(int screenWidth)
        {
            return GetWidthScaled(screenWidth, DealerCardSpacingRatio);
        }

        /// <summary>
        /// Get player card horizontal spacing
        /// </summary>
        public static int GetPlayerCardHorizontalSpacing(int screenWidth)
        {
            return GetWidthScaled(screenWidth, PlayerCardHorizontalSpacingRatio);
        }

        /// <summary>
        /// Get player card vertical spacing
        /// </summary>
        public static int GetPlayerCardVerticalSpacing(int screenHeight)
        {
            return GetHeightScaled(screenHeight, PlayerCardVerticalSpacingRatio);
        }

        /// <summary>
        /// Get second hand offset for split hands
        /// </summary>
        public static Vector2 GetSecondHandOffset(int screenWidth, int screenHeight)
        {
            return new Vector2(
                GetWidthScaled(screenWidth, SecondHandOffsetXRatio),
                GetHeightScaled(screenHeight, SecondHandOffsetYRatio));
        }

        /// <summary>
        /// Get second hand offset for betting component
        /// </summary>
        public static Vector2 GetBetSecondHandOffset(int screenWidth, int screenHeight)
        {
            return new Vector2(
                GetWidthScaled(screenWidth, BetSecondHandOffsetXRatio),
                GetHeightScaled(screenHeight, BetSecondHandOffsetYRatio));
        }

        /// <summary>
        /// Get shuffle split distance
        /// </summary>
        public static float GetShuffleSplitDistance(int screenWidth)
        {
            return GetWidthScaled(screenWidth, ShuffleSplitDistanceRatio);
        }

        /// <summary>
        /// Get shuffle cascade height
        /// </summary>
        public static float GetShuffleCascadeHeight(int screenHeight)
        {
            return GetHeightScaled(screenHeight, ShuffleCascadeHeightRatio);
        }

        /// <summary>
        /// Get deck layer offset for stacked deck display
        /// </summary>
        public static Vector2 GetDeckLayerOffset(int screenWidth)
        {
            float offset = GetWidthScaled(screenWidth, DeckLayerOffsetRatio);
            return new Vector2(offset, offset);
        }

        /// <summary>
        /// Get frame size for UI elements
        /// </summary>
        public static Vector2 GetFrameSize(int screenWidth, int screenHeight)
        {
            return new Vector2(
                GetWidthScaled(screenWidth, FrameWidthRatio),
                GetHeightScaled(screenHeight, FrameHeightRatio));
        }

        /// <summary>
        /// Get insurance Y position
        /// </summary>
        public static float GetInsuranceYPosition(int screenHeight)
        {
            return GetHeightScaled(screenHeight, InsuranceYPositionRatio);
        }

        /// <summary>
        /// Calculate the Y position for gameplay buttons (Deal, Clear, Hit, Stand, etc.)
        /// This ensures consistent button positioning across betting and gameplay phases.
        /// </summary>
        /// <param name="chipTextureHeight">The actual height of the chip texture in pixels</param>
        /// <param name="screenWidth">The screen width (for calculating padding)</param>
        /// <param name="screenHeight">The screen height (for calculating button height)</param>
        /// <returns>The Y coordinate where buttons should be positioned</returns>
        public static int GetGameplayButtonYPosition(int chipTextureHeight, int screenWidth, int screenHeight)
        {
            int smallPadding = GetSmallPadding(screenWidth);
            int buttonHeight = GetButtonHeight(screenHeight);

            // Position buttons above the chips with consistent spacing
            // Bottom of screen - chip height - padding below chips - button height - padding between buttons and chips
            return screenHeight - chipTextureHeight - smallPadding - buttonHeight - (smallPadding * 2);
        }

        /// <summary>
        /// Calculate button width based on text size with padding.
        /// Returns the larger of minWidth or the width required to fit the text.
        /// </summary>
        /// <param name="text">The button text to measure</param>
        /// <param name="font">The font used to render the text</param>
        /// <param name="minWidth">The minimum button width</param>
        /// <param name="screenWidth">The screen width (for calculating padding)</param>
        /// <returns>The calculated button width in pixels</returns>
        public static int CalculateButtonWidth(string text, SpriteFont font, int minWidth, int screenWidth)
        {
            if (string.IsNullOrEmpty(text) || font == null)
                return minWidth;

            Vector2 textSize = font.MeasureString(text);
            int padding = GetSmallPadding(screenWidth) * 2; // Padding on both sides
            int requiredWidth = (int)textSize.X + padding;

            return Math.Max(minWidth, requiredWidth);
        }
    }
}