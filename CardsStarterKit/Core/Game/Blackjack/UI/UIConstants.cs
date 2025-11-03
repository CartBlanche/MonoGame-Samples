//-----------------------------------------------------------------------------
// UIConstants.cs
//
// UI scaling constants for resolution-independent rendering in Blackjack
//-----------------------------------------------------------------------------

using Microsoft.Xna.Framework;

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
    }
}
