using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Supplies SpriteFont instances to framework systems (ScreenManager, CardsGame)
    /// by role and current language, decoupling font asset paths from the framework.
    /// Games that want custom or SDF fonts implement this interface and inject it at
    /// construction time; games that do nothing continue to receive the default fonts.
    /// </summary>
    public interface IFontProvider
    {
        /// <summary>
        /// Returns the SpriteFont for the given role in the given language.
        /// </summary>
        /// <param name="role">The logical font role (Menu, Regular, Bold).</param>
        /// <param name="language">
        /// The current language tag (e.g. "日本語", "中文", "English").
        /// Implementations use this to switch between Latin and CJK atlases.
        /// </param>
        SpriteFont GetFont(FontRole role, string language);
    }
}
