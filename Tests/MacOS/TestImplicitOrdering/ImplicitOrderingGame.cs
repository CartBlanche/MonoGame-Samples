using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TestImplicitOrdering
{
    public class ImplicitOrderingGame : Game
    {
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        public ImplicitOrderingGame()
        {
            Content.RootDirectory = "Content";
            new GraphicsDeviceManager(this);
        }

        private TestUpdateable[] _updateables;
        private List<TestUpdateable> _updateablesInUpdateOrder = new List<TestUpdateable>();
        private bool? _updateablesOrderedCorrectly;

        private TestDrawable[] _drawables;
        private List<TestDrawable> _drawablesInDrawOrder = new List<TestDrawable>();
        private bool? _drawablesOrderedCorrectly;


        protected override void LoadContent()
        {
            base.LoadContent();

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("fntStandard");

            _updateables = new TestUpdateable[5];
            for (int i = 0; i < _updateables.Length; ++i)
            {
                _updateables[i] = new TestUpdateable(this);
                Components.Add(_updateables[i]);
            }

            _drawables = new TestDrawable[9];
            for (int i = 0; i < _drawables.Length; ++i)
            {
                _drawables[i] = new TestDrawable(this);
                Components.Add(_drawables[i]);
            }
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
            _font = null;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (!_updateablesOrderedCorrectly.HasValue)
                _updateablesOrderedCorrectly = ListsEqual(_updateables, _updateablesInUpdateOrder);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            base.Draw(gameTime);

            if (!_drawablesOrderedCorrectly.HasValue)
                _drawablesOrderedCorrectly = ListsEqual(_drawables, _drawablesInDrawOrder);

            _spriteBatch.Begin();
            if (_updateablesOrderedCorrectly.HasValue)
                DrawStatusString("Updateables", 1, _updateablesOrderedCorrectly.Value);
            if (_updateablesOrderedCorrectly.HasValue)
                DrawStatusString("Drawables", 0, _drawablesOrderedCorrectly.Value);
            _spriteBatch.End();
        }

        private void DrawStatusString(string item, int linesFromBottom, bool isCorrect)
        {
            var position = new Vector2(
                10, GraphicsDevice.Viewport.Height - ((1 + linesFromBottom) * _font.LineSpacing));
            if (isCorrect)
                _spriteBatch.DrawString(_font, item + " correctly ordered!", position, Color.Lime);
            else
                _spriteBatch.DrawString(_font, item + " incorrectly ordered.", position, Color.Red);
        }

        private bool ListsEqual<T>(IList<T> a, IList<T> b)
        {
            if (a.Count != b.Count)
                return false;

            var equalityComparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a.Count; ++i)
                if (!equalityComparer.Equals(a[i], b[i]))
                    return false;
            return true;
        }

        private class TestUpdateable : GameComponent
        {
            public TestUpdateable(Game game) : base(game) { }

            private bool _firstUpdate = true;
            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (_firstUpdate)
                {
                    _firstUpdate = false;
                    var game = (ImplicitOrderingGame)Game;
                    game._updateablesInUpdateOrder.Add(this);
                }
            }
        }

        private class TestDrawable : DrawableGameComponent
        {
            private static int InstanceCount = 0;
            private static readonly Color[] Colors = new Color[]
            {
                Color.White, Color.Red, Color.Orange, Color.Yellow, Color.Green,
                Color.Blue, Color.Indigo, Color.Violet, Color.Black
            };

            private int _number;
            private Color _color;
            public TestDrawable(Game game) : base(game)
            {
                _number = ++InstanceCount;
                _color = Colors[_number % Colors.Length];
            }

            private SpriteBatch _spriteBatch;
            protected override void LoadContent()
            {
                base.LoadContent();
                _spriteBatch = new SpriteBatch(Game.GraphicsDevice);
            }

            protected override void UnloadContent()
            {
                base.UnloadContent();

                _spriteBatch.Dispose();
                _spriteBatch = null;
            }

            private bool _firstDraw = true;
            public override void Draw(GameTime gameTime)
            {
                var game = (ImplicitOrderingGame)Game;

                if (_firstDraw)
                {
                    _firstDraw = false;
                    game._drawablesInDrawOrder.Add(this);
                }

                float halfEx = game._font.MeasureString("x").X / 2;
                var position = new Vector2(_number * halfEx, 0);

                _spriteBatch.Begin();
                _spriteBatch.DrawString(game._font, _number.ToString(), position, _color);
                _spriteBatch.End();
            }
        }
    }
}
