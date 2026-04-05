using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Default IFontProvider that reproduces the existing framework font-loading
    /// behaviour: MenuFont/MenuFont_CJK, Regular/Regular_CJK, Bold/Bold_CJK loaded
    /// from the standard Fonts/ content folder.
    /// Games that do not supply a custom provider receive an instance of this class.
    /// </summary>
    public class DefaultFontProvider : IFontProvider
    {
        private readonly ContentManager _content;

        /// <param name="content">The ContentManager to load font assets from.</param>
        public DefaultFontProvider(ContentManager content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <inheritdoc/>
        public SpriteFont GetFont(FontRole role, string language)
        {
            bool cjk = language == "日本語" || language == "中文";

            string assetName = role switch
            {
                FontRole.Menu    => cjk ? "MenuFont_CJK" : "MenuFont",
                FontRole.Regular => cjk ? "Regular_CJK"  : "Regular",
                FontRole.Bold    => cjk ? "Bold_CJK"     : "Bold",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown FontRole")
            };

            SpriteFont font = _content.Load<SpriteFont>(Path.Combine("Fonts", assetName));

            if (font == null)
                throw new InvalidOperationException(
                    $"DefaultFontProvider: content returned null for asset 'Fonts/{assetName}'.");

            return font;
        }
    }
}
