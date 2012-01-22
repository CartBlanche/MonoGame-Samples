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

        private const int NumberOfBatches = 3;
        private const int ItemsPerBatch = 3;
        private int _batchNumber = 0;

        private List<TestUpdateable> _updateables = new List<TestUpdateable>();
        private List<TestUpdateable> _updateablesInUpdateOrder = new List<TestUpdateable>();
        private bool _updateablesOrderedCorrectly;

        private List<TestDrawable> _drawables = new List<TestDrawable>();
        private List<TestDrawable> _drawablesInDrawOrder = new List<TestDrawable>();
        private bool _drawablesOrderedCorrectly;


        protected override void LoadContent()
        {
            base.LoadContent();

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("fntStandard");
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
            _font = null;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_batchNumber < NumberOfBatches &&
                gameTime.TotalGameTime >= TimeSpan.FromSeconds(_batchNumber))
            {
                for (int i = 0; i < ItemsPerBatch; ++i)
                {
                    var updateable = new TestUpdateable(this);
                    _updateables.Add(updateable);
                    Components.Add(updateable);

                    var drawable = new TestDrawable(this);
                    _drawables.Add(drawable);
                    Components.Add(drawable);
                }

                _batchNumber++;
            }


            base.Update(gameTime);
            _updateablesOrderedCorrectly = ListsEqual(_updateables, _updateablesInUpdateOrder);
            _updateablesInUpdateOrder.Clear();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            base.Draw(gameTime);
            _drawablesOrderedCorrectly = ListsEqual(_drawables, _drawablesInDrawOrder);
            _drawablesInDrawOrder.Clear();

            _spriteBatch.Begin();
            DrawStatusString(
                string.Format("{0} updateables", _updateables.Count),
                1, _updateablesOrderedCorrectly);
            DrawStatusString(
                string.Format("{0} drawables", _drawables.Count),
                0, _drawablesOrderedCorrectly);
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

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                var game = (ImplicitOrderingGame)Game;
                game._updateablesInUpdateOrder.Add(this);
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

            public override void Draw(GameTime gameTime)
            {
                var game = (ImplicitOrderingGame)Game;

                game._drawablesInDrawOrder.Add(this);

                float halfEx = game._font.MeasureString("x").X / 2;
                var position = new Vector2(_number * halfEx, 0);

                _spriteBatch.Begin();
                _spriteBatch.DrawString(game._font, _number.ToString(), position, _color);
                _spriteBatch.End();
            }
        }
    }
}
