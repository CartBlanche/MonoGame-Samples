//-----------------------------------------------------------------------------
// UIConstants.cs
//
// UI scaling constants for resolution-independent rendering
//-----------------------------------------------------------------------------

namespace Warlords
{
    /// <summary>
    /// Defines proportional UI constants that scale with screen resolution
    /// All values are percentages or ratios of screen dimensions
    /// </summary>
    public static class UIConstants
    {
        // Layout proportions (as percentage of screen height)
        public const float TopBarHeightRatio = 0.07f;        // 7% of screen height (~50px at 720p)
        public const float HandAreaHeightRatio = 0.20f;      // 20% of screen height (~144px at 720p)
        
        // Card dimensions (as percentage of screen dimensions)
        public const float CardWidthRatio = 0.08f;           // 8% of screen width (~102px at 720p)
        public const float CardHeightRatio = 0.18f;          // 18% of screen height (~130px at 720p)
        
        // Button dimensions (as percentage of screen dimensions)
        public const float ButtonWidthRatio = 0.12f;         // 12% of screen width (~154px at 720p)
        public const float ButtonHeightRatio = 0.08f;        // 8% of screen height (~58px at 720p)
        
        // Text scaling (base scale factors)
        public const float TitleTextScale = 0.8f;
        public const float RegularTextScale = 0.6f;
        public const float SmallTextScale = 0.45f;
        public const float TinyTextScale = 0.35f;
        
        // Spacing and padding (as percentage of screen dimensions)
        public const float CardSpacingRatio = 0.01f;         // 1% of screen width
        public const float PaddingRatio = 0.015f;            // 1.5% of screen
        
        // Border thickness (fixed pixel values, but we'll scale these too)
        public const int BorderThicknessThin = 2;
        public const int BorderThicknessMedium = 3;
        public const int BorderThicknessThick = 4;
        
        /// <summary>
        /// Calculate actual pixel value from screen height ratio
        /// </summary>
        public static int GetHeightScaled(int screenHeight, float ratio)
        {
            return (int)(screenHeight * ratio);
        }
        
        /// <summary>
        /// Calculate actual pixel value from screen width ratio
        /// </summary>
        public static int GetWidthScaled(int screenWidth, float ratio)
        {
            return (int)(screenWidth * ratio);
        }
        
        /// <summary>
        /// Calculate card width based on screen width
        /// </summary>
        public static int GetCardWidth(int screenWidth)
        {
            return GetWidthScaled(screenWidth, CardWidthRatio);
        }
        
        /// <summary>
        /// Calculate card height based on screen height
        /// </summary>
        public static int GetCardHeight(int screenHeight)
        {
            return GetHeightScaled(screenHeight, CardHeightRatio);
        }
        
        /// <summary>
        /// Calculate top bar height
        /// </summary>
        public static int GetTopBarHeight(int screenHeight)
        {
            return GetHeightScaled(screenHeight, TopBarHeightRatio);
        }
        
        /// <summary>
        /// Calculate hand area height
        /// </summary>
        public static int GetHandAreaHeight(int screenHeight)
        {
            return GetHeightScaled(screenHeight, HandAreaHeightRatio);
        }
        
        /// <summary>
        /// Calculate button width
        /// </summary>
        public static int GetButtonWidth(int screenWidth)
        {
            return GetWidthScaled(screenWidth, ButtonWidthRatio);
        }
        
        /// <summary>
        /// Calculate button height
        /// </summary>
        public static int GetButtonHeight(int screenHeight)
        {
            return GetHeightScaled(screenHeight, ButtonHeightRatio);
        }
        
        /// <summary>
        /// Calculate card spacing
        /// </summary>
        public static int GetCardSpacing(int screenWidth)
        {
            return GetWidthScaled(screenWidth, CardSpacingRatio);
        }
        
        /// <summary>
        /// Calculate general padding
        /// </summary>
        public static int GetPadding(int screenSize)
        {
            return (int)(screenSize * PaddingRatio);
        }
    }
}
