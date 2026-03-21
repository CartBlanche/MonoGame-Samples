//-----------------------------------------------------------------------------
// AnimationConstants.cs
//
// Framework-level animation rendering constants for animated game components
//-----------------------------------------------------------------------------

namespace CardsFramework
{
    /// <summary>
    /// Defines animation rendering multipliers for AnimatedGameComponent and related classes.
    /// These constants control how game objects (cards, chips) are scaled during rendering.
    /// </summary>
    public static class AnimationConstants
    {
        /// <summary>
        /// Scale multiplier for drawing cards during animation (1.25x = 25% larger than layout size)
        /// </summary>
        public const float CardDrawScaleMultiplier = 1.25f;

        /// <summary>
        /// Scale multiplier for drawing chips during animation (0.75x = 75% of layout size)
        /// </summary>
        public const float ChipDrawScaleMultiplier = 0.75f;

        /// <summary>
        /// Scale multiplier for shadow effects relative to the sprite size (1.05x = 5% larger shadow)
        /// </summary>
        public const float ShadowDrawScaleMultiplier = 1.05f;
    }
}
