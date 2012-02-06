using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;


namespace XNAPacMan {
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class HighScores : Microsoft.Xna.Framework.DrawableGameComponent {
        public HighScores(Game game)
            : base(game) {
            // TODO: Construct any child components here
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize() {
            scores_ = new List<string>(10);
            const string fileName = "highscores.txt";
            if (File.Exists(fileName)) {
                scores_ = File.ReadAllLines(fileName).ToList<string>();
                scores_.Sort((a, b) => Convert.ToInt32(a).CompareTo(Convert.ToInt32(b)));
                scores_.Reverse();
            }
            scoreFont_ = Game.Content.Load<SpriteFont>("Score");
            itemFont_ = Game.Content.Load<SpriteFont>("MenuItem");
            selectionArrow_ = Game.Content.Load<Texture2D>("sprites/Selection");
            spriteBatch_ = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));
            graphics_ = (GraphicsDeviceManager)Game.Services.GetService(typeof(GraphicsDeviceManager));
            oldState_ = Keyboard.GetState();
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime) {
            // TODO: Add your update code here
            if (Keyboard.GetState().GetPressedKeys().Length > 0 && oldState_.GetPressedKeys().Length == 0) {
                Game.Components.Remove(this);
                Game.Components.Add(new Menu(Game, null));
            }
            oldState_ = Keyboard.GetState();
            base.Update(gameTime);
        }

        /// <summary>
        /// Allows the component to draw itself
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values</param>
        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);
            Vector2 position = new Vector2(graphics_.PreferredBackBufferWidth / 2 - 150, graphics_.PreferredBackBufferHeight / 2 - 200);
            spriteBatch_.Begin();
            for (int i = 0; i < 10; i++) {
                spriteBatch_.DrawString(scoreFont_, (i + 1).ToString() + ".", new Vector2(position.X, position.Y + (30 * i)), Color.White);
                if (i < scores_.Count) {
                    spriteBatch_.DrawString(scoreFont_, scores_[i], new Vector2(position.X + 50, position.Y + (30 * i)), Color.White);
                }
            }

            Vector2 itemPosition;
            itemPosition.X = (graphics_.PreferredBackBufferWidth / 2) - 100;
            itemPosition.Y = (graphics_.PreferredBackBufferHeight / 2) + 200;
            spriteBatch_.Draw(selectionArrow_, new Vector2(itemPosition.X - 50, itemPosition.Y), Color.White);
            spriteBatch_.DrawString(itemFont_, "Return", itemPosition, Color.Yellow);

            spriteBatch_.End();
            

        }

        List<string> scores_;
        SpriteFont scoreFont_;
        SpriteFont itemFont_;
        Texture2D selectionArrow_;
        SpriteBatch spriteBatch_;
        GraphicsDeviceManager graphics_;
        KeyboardState oldState_;
    }
}