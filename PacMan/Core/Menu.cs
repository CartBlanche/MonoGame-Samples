using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
//using Microsoft.Xna.Framework.Net;
//using Microsoft.Xna.Framework.Storage;


namespace PacMan {
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    /// Optionally takes a GameLoop argument, when the menu must be able to
    /// resume the current GameLoop. Otherwise, the reference would be lost
    /// and the gameLoop garbage collected.
    public class Menu : Microsoft.Xna.Framework.DrawableGameComponent {
        public Menu(Game game, GameLoop gameLoop)
            : base(game) {
            gameLoop_ = gameLoop;
            gameStart_ = (gameLoop == null);
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize() {
            spriteBatch_ = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));
            graphics_ = (GraphicsDeviceManager)Game.Services.GetService(typeof(GraphicsDeviceManager));
            soundBank_ = (SoundBank)Game.Services.GetService(typeof(SoundBank));
            selection_ = 0;
            if (gameLoop_ == null) {
                items_ = new string[] { "New Game", "High Scores", "Quit" };
            }
            else {
                items_ = new string[] { "Resume", "Quit Game" };
            }
            menuItem_ = Game.Content.Load<SpriteFont>("MenuItem");
            title_ = Game.Content.Load<Texture2D>("sprites/Title");
            selectionArrow_ = Game.Content.Load<Texture2D>("sprites/Selection");
            oldState_ = Keyboard.GetState();
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
            // Wonder why we test for this condition? Just replace gameStart_ by true and
            // try running the game. The answer should be instantaneous.
            if (gameStart_) {
                soundBank_.PlayCue("NewLevel");
                gameStart_ = false;
            }
            
            KeyboardState newState = Keyboard.GetState();
            
            // Get keys pressed now that weren't pressed before
            var newPressedKeys = from k in newState.GetPressedKeys()
                                 where !(oldState_.GetPressedKeys().Contains(k))
                                 select k;             

            // Scroll through menu items
            if (newPressedKeys.Contains(Keys.Down)) {
                selection_++;
                selection_ %= items_.Length;
                soundBank_.PlayCue("PacMAnEat1");
            }
            else if (newPressedKeys.Contains(Keys.Up)) {
                selection_--;
                selection_ = (selection_ < 0? items_.Length -1 : selection_);
                soundBank_.PlayCue("PacManEat2");
            }
            else if (newPressedKeys.Contains(Keys.Enter)) {
                menuAction();
            }

            // Update keyboard state for next update
            oldState_ = newState;

            
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            // The menu is a main component, so it is responsible for initializing the sprite batch each frame
            spriteBatch_.Begin();

            // Draw title
            spriteBatch_.Draw(title_, new Vector2((graphics_.PreferredBackBufferWidth / 2) - (title_.Width / 2), 75), Color.White);

            // Draw items
            Vector2 itemPosition;
            itemPosition.X = (graphics_.PreferredBackBufferWidth / 2) - 100;
            for (int i = 0; i < items_.Length; i++) {

                itemPosition.Y = (graphics_.PreferredBackBufferHeight / 2) - 60 + (60 * i);
                if (i == selection_) {
                    spriteBatch_.Draw(selectionArrow_, new Vector2(itemPosition.X - 50, itemPosition.Y), Color.White);
                }
                spriteBatch_.DrawString(menuItem_, items_[i], itemPosition, Color.Yellow);
            }

            spriteBatch_.End();
        }

        void menuAction() {
            Game.Components.Remove(this);
            switch (items_[selection_]) {
                case ("Resume"):
                    Game.Components.Add(gameLoop_);
                    break;
                case ("New Game") :
                    Game.Components.Add(new GameLoop(Game));
                    break;
                case ("High Scores"):
                    Game.Components.Add(new HighScores(Game));
                    break;
                case ("Quit"):
                    Game.Exit();
                    break;
                case ("Quit Game"):
                    Game.Components.Add(new Menu(Game, null));
                    SaveHighScore(gameLoop_.Score);
                    break;
                default:
                    throw new ArgumentException("\"" + items_[selection_] + "\" is not a valid case");

            }
        }

        /// <summary>
        /// Keep a history of the best 10 scores
        /// </summary>
        /// <param name="highScore">New score to save, might make it inside the list, might not.</param>
        public static void SaveHighScore(int highScore) {
            const string fileName = "highscores.txt";
            if (!File.Exists(fileName)) {
                File.WriteAllLines(fileName, new string[] { highScore.ToString() });
            }
            else {
                List<string> contents = File.ReadAllLines(fileName).ToList<string>();
                contents.Add(highScore.ToString());
                if (contents.Count >= 10) {
                    contents.Sort((a, b) => Convert.ToInt32(a).CompareTo(Convert.ToInt32(b)));
                    while (contents.Count > 10) {
                        contents.RemoveAt(0);
                    }
                }
                File.WriteAllLines(fileName, contents.ToArray());
            }
        }

        GameLoop gameLoop_;
        SoundBank soundBank_;
        GraphicsDeviceManager graphics_;
        SpriteBatch spriteBatch_;
        SpriteFont menuItem_;
        string[] items_;
        int selection_;
        bool gameStart_;
        Texture2D title_;
        Texture2D selectionArrow_;

        KeyboardState oldState_;
    }
}