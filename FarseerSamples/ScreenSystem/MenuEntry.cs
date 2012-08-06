using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    public enum EntryType
    {
        Screen,
        Separator,
        ExitItem
    }

    /// <summary>
    /// Helper class represents a single entry in a MenuScreen. By default this
    /// just draws the entry text string, but it can be customized to display menu
    /// entries in different ways. This also provides an event that will be raised
    /// when the menu entry is selected.
    /// </summary>
    public sealed class MenuEntry
    {
        private float _alpha;
        private Vector2 _baseOrigin;

        private float _height;
        private MenuScreen _menu;

        /// <summary>
        /// The position at which the entry is drawn. This is set by the MenuScreen
        /// each frame in Update.
        /// </summary>
        private Vector2 _position;

        private float _scale;
        private GameScreen _screen;

        /// <summary>
        /// Tracks a fading selection effect on the entry.
        /// </summary>
        /// <remarks>
        /// The entries transition out of the selection effect when they are deselected.
        /// </remarks>
        private float _selectionFade;

        /// <summary>
        /// The text rendered for this entry.
        /// </summary>
        private string _text;

        private EntryType _type;
        private float _width;

        /// <summary>
        /// Constructs a new menu entry with the specified text.
        /// </summary>
        public MenuEntry(MenuScreen menu, string text, EntryType type, GameScreen screen)
        {
            _text = text;
            _screen = screen;
            _type = type;
            _menu = menu;
            _scale = 0.9f;
            _alpha = 1.0f;
        }


        /// <summary>
        /// Gets or sets the text of this menu entry.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        /// <summary>
        /// Gets or sets the position at which to draw this menu entry.
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public float Alpha
        {
            get { return _alpha; }
            set { _alpha = value; }
        }

        public GameScreen Screen
        {
            get { return _screen; }
        }

        public void Initialize()
        {
            SpriteFont font = _menu.ScreenManager.Fonts.MenuSpriteFont;

            _baseOrigin = new Vector2(font.MeasureString(Text).X, font.MeasureString("M").Y) * 0.5f;

            _width = font.MeasureString(Text).X * 0.8f;
            _height = font.MeasureString("M").Y * 0.8f;
        }

        public bool IsExitItem()
        {
            return _type == EntryType.ExitItem;
        }

        public bool IsSelectable()
        {
            return _type != EntryType.Separator;
        }

        /// <summary>
        /// Updates the menu entry.
        /// </summary>
        public void Update(bool isSelected, GameTime gameTime)
        {
            // there is no such thing as a selected item on Windows Phone, so we always
            // force isSelected to be false
#if WINDOWS_PHONE
            isSelected = false;
#endif
            // When the menu selection changes, entries gradually fade between
            // their selected and deselected appearance, rather than instantly
            // popping to the new state.
            if (_type != EntryType.Separator)
            {
                float fadeSpeed = (float)gameTime.ElapsedGameTime.TotalSeconds * 4;
                if (isSelected)
                {
                    _selectionFade = Math.Min(_selectionFade + fadeSpeed, 1f);
                }
                else
                {
                    _selectionFade = Math.Max(_selectionFade - fadeSpeed, 0f);
                }
                _scale = 0.7f + 0.1f * _selectionFade;
            }
        }

        /// <summary>
        /// Draws the menu entry. This can be overridden to customize the appearance.
        /// </summary>
        public void Draw()
        {
            SpriteFont font = _menu.ScreenManager.Fonts.MenuSpriteFont;
            SpriteBatch batch = _menu.ScreenManager.SpriteBatch;

            Color color;
            if (_type == EntryType.Separator)
            {
                color = Color.DarkOrange;
            }
            else
            {
                // Draw the selected entry in yellow, otherwise white
                color = Color.Lerp(Color.White, new Color(255, 210, 0), _selectionFade);
            }
            color *= _alpha;

            // Draw text, centered on the middle of each line.
            batch.DrawString(font, _text, _position - _baseOrigin * _scale + Vector2.One,
                              Color.DarkSlateGray * _alpha * _alpha, 0, Vector2.Zero, _scale, SpriteEffects.None, 0);
            batch.DrawString(font, _text, _position - _baseOrigin * _scale, color, 0, Vector2.Zero, _scale,
                              SpriteEffects.None, 0);
        }

        /// <summary>
        /// Queries how much space this menu entry requires.
        /// </summary>
        public int GetHeight()
        {
            return (int)_height;
        }

        /// <summary>
        /// Queries how wide the entry is, used for centering on the screen.
        /// </summary>
        public int GetWidth()
        {
            return (int)_width;
        }
    }
}