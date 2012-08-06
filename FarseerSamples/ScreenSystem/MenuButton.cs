using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarseerPhysics.SamplesFramework
{
    /// <summary>
    /// Helper class represents a single entry in a MenuScreen. By default this
    /// just draws the entry text string, but it can be customized to display menu
    /// entries in different ways. This also provides an event that will be raised
    /// when the menu entry is selected.
    /// </summary>
    public sealed class MenuButton
    {
        private Vector2 _baseOrigin;
        private bool _flip;
        private bool _hover;

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

        private Texture2D _sprite;

        /// <summary>
        /// Constructs a new menu entry with the specified text.
        /// </summary>
        public MenuButton(Texture2D sprite, bool flip, Vector2 position, GameScreen screen)
        {
            _screen = screen;
            _scale = 1f;
            _sprite = sprite;
            _baseOrigin = new Vector2(_sprite.Width / 2f, _sprite.Height / 2f);
            _hover = false;
            _flip = flip;
            Position = position;
        }

        /// <summary>
        /// Gets or sets the position at which to draw this menu entry.
        /// </summary>
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public bool Hover
        {
            get { return _hover; }
            set { _hover = value; }
        }

        public GameScreen Screen
        {
            get { return _screen; }
        }

        /// <summary>
        /// Updates the menu entry.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float fadeSpeed = (float)gameTime.ElapsedGameTime.TotalSeconds * 4;
            if (_hover)
            {
                _selectionFade = Math.Min(_selectionFade + fadeSpeed, 1f);
            }
            else
            {
                _selectionFade = Math.Max(_selectionFade - fadeSpeed, 0f);
            }
            _scale = 1f + 0.1f * _selectionFade;
        }

        public void Collide(Vector2 position)
        {
            Rectangle collisonBox = new Rectangle((int)(Position.X - _sprite.Width / 2f),
                                                  (int)(Position.Y - _sprite.Height / 2f),
                                                  (_sprite.Width),
                                                  (_sprite.Height));

            if (collisonBox.Contains((int)position.X, (int)position.Y))
            {
                _hover = true;
            }
            else
            {
                _hover = false;
            }
        }

        /// <summary>
        /// Draws the menu entry. This can be overridden to customize the appearance.
        /// </summary>
        public void Draw()
        {
            SpriteBatch batch = _screen.ScreenManager.SpriteBatch;
            Color color = Color.Lerp(Color.White, new Color(255, 210, 0), _selectionFade);

            batch.Draw(_sprite, _position - _baseOrigin * _scale, null, color, 0f, Vector2.Zero,
                        _scale, _flip ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
        }
    }
}