using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CardsFramework
{
    /// <summary>
    /// Default IFontProvider that reproduces the existing framework font-loading
    /// behaviour: MenuFont, Regular and Bold loaded from the standard Fonts/ content folder, with a
    /// per-language CJK font (e.g. Regular_ja) or the older shared *_CJK font for Chinese, Japanese and Korean.
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

        /// <summary>
        /// Short code for languages that need a CJK font ("zh", "ja", "ko"), or null for languages the Latin fonts cover.
        /// </summary>
        private static string CjkLanguageCode(string language) => language switch
        {
            "中文"   => "zh",
            "日本語" => "ja",
            "한국어" => "ko",
            _        => null
        };

        private SpriteFont TryLoad(string assetName)
        {
            try
            {
                return _content.Load<SpriteFont>(Path.Combine("Fonts", assetName));
            }
            catch (ContentLoadException)
            {
                return null;
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// For Chinese, Japanese and Korean the per-language font (for example <c>Regular_ja</c>) is used when the game
        /// ships one; otherwise the older shared <c>*_CJK</c> font; otherwise the Latin font.
        /// </remarks>
        public SpriteFont GetFont(FontRole role, string language)
        {
            string baseName = role switch
            {
                FontRole.Menu    => "MenuFont",
                FontRole.Regular => "Regular",
                FontRole.Bold    => "Bold",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown FontRole")
            };

            SpriteFont font = null;
            string assetName = baseName;

            string code = CjkLanguageCode(language);
            if (code != null)
            {
                assetName = $"{baseName}_{code}";
                font = TryLoad(assetName);
                if (font == null)
                {
                    assetName = $"{baseName}_CJK";
                    font = TryLoad(assetName);
                }
            }

            if (font == null)
            {
                assetName = baseName;
                font = _content.Load<SpriteFont>(Path.Combine("Fonts", assetName));
            }

            if (font == null)
                throw new InvalidOperationException(
                    $"DefaultFontProvider: content returned null for asset 'Fonts/{assetName}'.");

            return font;
        }
    }
}
