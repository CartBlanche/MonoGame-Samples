using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace XNAPacMan {

    /// <summary>
    /// Defines a position on the board where a ghost has died or a fruit was eaten, as well as the score earned.
    /// This is used for knowing where to draw those scores
    /// </summary>
    struct ScoreEvent {
        public ScoreEvent(Position position, DateTime when, int score) {
            Position = position;
            When = when;
            Score = score;
        }
        public Position Position;
        public DateTime When;
        public int Score;
    }
    /// <summary>
    /// GameLoop is the main "game" object; this is basically where the action
    /// takes place. It's responsible for coordinating broad game logic,
    /// drawing the board and scores, as well as linking with the menu.
    /// </summary>
    public class GameLoop : Microsoft.Xna.Framework.DrawableGameComponent {
        public GameLoop(Game game)
            : base(game) {
            // TODO: Construct any child components here
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize() {
            // We don't want XNA calling this method each time we resume from the menu,
            // unfortunately, it'll call it whatever we try. So the only thing
            // we can do is check if it has been called already and return. Yes, it's ugly.
            if (spriteBatch_ != null) {
                GhostSoundsManager.ResumeLoops();
                return;
            }
            // Otherwise, this is the first time this component is Initialized, so proceed.

            GhostSoundsManager.Init(Game);

            Grid.Reset();
            Constants.Level = 1;
            spriteBatch_ = (SpriteBatch)Game.Services.GetService(typeof(SpriteBatch));
            graphics_ = (GraphicsDeviceManager)Game.Services.GetService(typeof(GraphicsDeviceManager));
            soundBank_ = (SoundBank)Game.Services.GetService(typeof(SoundBank));

            scoreFont_ = Game.Content.Load<SpriteFont>("Score");
            scoreEventFont_ = Game.Content.Load<SpriteFont>("ScoreEvent");
            xlife_ = Game.Content.Load<Texture2D>("sprites/ExtraLife");
            ppill_ = Game.Content.Load<Texture2D>("sprites/PowerPill");
            crump_ = Game.Content.Load<Texture2D>("sprites/Crump");
            board_ = Game.Content.Load<Texture2D>("sprites/Board");
            boardFlash_ = Game.Content.Load<Texture2D>("sprites/BoardFlash");
            bonusEaten_ = new Dictionary<string, int>();
            bonus_ = new Dictionary<string, Texture2D>(9);
            bonus_.Add("Apple", Game.Content.Load<Texture2D>("bonus/Apple"));
            bonus_.Add("Banana", Game.Content.Load<Texture2D>("bonus/Banana"));
            bonus_.Add("Bell", Game.Content.Load<Texture2D>("bonus/Bell"));
            bonus_.Add("Cherry", Game.Content.Load<Texture2D>("bonus/Cherry"));
            bonus_.Add("Key", Game.Content.Load<Texture2D>("bonus/Key"));
            bonus_.Add("Orange", Game.Content.Load<Texture2D>("bonus/Orange"));
            bonus_.Add("Pear", Game.Content.Load<Texture2D>("bonus/Pear"));
            bonus_.Add("Pretzel", Game.Content.Load<Texture2D>("bonus/Pretzel"));
            bonus_.Add("Strawberry", Game.Content.Load<Texture2D>("bonus/Strawberry"));

            scoreEvents_ = new List<ScoreEvent>(5);
            bonusPresent_ = false;
            bonusSpawned_ = 0;
            eatenGhosts_ = 0;
            Score = 0;
            xlives_ = 2;
            paChomp_ = true;
            playerDied_ = false;
            player_ = new Player(Game);
            ghosts_ = new List<Ghost> { new Ghost(Game, player_, Ghosts.Blinky), new Ghost(Game, player_, Ghosts.Clyde),
                                        new Ghost(Game, player_, Ghosts.Inky), new Ghost(Game, player_, Ghosts.Pinky)};
            ghosts_[2].SetBlinky(ghosts_[0]); // Oh, dirty hack. Inky needs this for his AI.
            soundBank_.PlayCue("Intro");
            LockTimer = TimeSpan.FromMilliseconds(4500);

            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime) {

            // Some events (death, new level, etc.) lock the game for a few moments.
            if (DateTime.Now - eventTimer_ < LockTimer) {
                ghosts_.ForEach(i => i.LockTimer(gameTime));
                // Also we need to do the same thing for our own timer concerning bonuses
                bonusSpawnedTime_ += gameTime.ElapsedGameTime;
                return;
            }

            // Remove special events older than 5 seconds
            scoreEvents_.RemoveAll(i => DateTime.Now - i.When > TimeSpan.FromSeconds(5));

            // If the player had died, spawn a new one or end game.
            if (playerDied_) {
                // extra lives are decremented here, at the same time the pac man is spawned; this makes those 
                // events seem linked.
                xlives_--;
                //xlives_++; // Give infinite lives to the evil developer;
                if (xlives_ >= 0) {
                    playerDied_ = false;
                    player_ = new Player(Game);
                    ghosts_.ForEach(i => i.Reset(false, player_));
                    scoreEvents_.Clear();
                }
                else { // The game is over
                    Menu.SaveHighScore(Score);
                    Game.Components.Add(new Menu(Game, null));
                    Game.Components.Remove(this);
                    GhostSoundsManager.StopLoops();
                    return;
                }
            }

            // When all crumps have been eaten, wait a few seconds and then spawn a new level
            if (noCrumpsLeft()) {
                if (Constants.Level < 21) {
                    bonusSpawned_ = 0;
                    Grid.Reset();
                    player_ = new Player(Game);
                    ghosts_.ForEach(i => i.Reset(true, player_));
                    soundBank_.PlayCue("NewLevel");
                    LockTimer = TimeSpan.FromSeconds(2);
                    Constants.Level++;
                    return;
                }
                else { // Game over, you win.
                    Menu.SaveHighScore(Score);
                    Game.Components.Add(new Menu(Game, null));
                    Game.Components.Remove(this);
                    GhostSoundsManager.StopLoops();
                    return;
                }
            }

            Keys[] inputKeys = Keyboard.GetState().GetPressedKeys();
            // The user may escape to the main menu with the escape key
            if (inputKeys.Contains(Keys.Escape)) {
                Game.Components.Add(new Menu(Game, this));
                Game.Components.Remove(this);
                GhostSoundsManager.PauseLoops(); // will be resumed in Initialize(). No need for stopping them
                // if the player subsequently quits the game, since we'll re-initialize GhostSoundManager in
                // Initialize() if the player wants to start a new game.
                return;
            }

            // Eat crumps and power pills.
            if (player_.Position.DeltaPixel == Point.Zero) {
                Point playerTile = player_.Position.Tile;
                if (Grid.TileGrid[playerTile.X, playerTile.Y].HasCrump) {
                    soundBank_.PlayCue(paChomp_ ? "PacMAnEat1" : "PacManEat2");
                    paChomp_ = !paChomp_;
                    Score += 10;
                    Grid.TileGrid[playerTile.X, playerTile.Y].HasCrump = false;
                    if (Grid.TileGrid[playerTile.X, playerTile.Y].HasPowerPill) {
                        Score += 40;
                        eatenGhosts_ = 0;
                        for (int i = 0; i < ghosts_.Count; i++) {
                            if (ghosts_[i].State == GhostState.Attack || ghosts_[i].State == GhostState.Scatter ||
                                ghosts_[i].State == GhostState.Blue) {
                                ghosts_[i].State = GhostState.Blue;
                            }
                        }
                        Grid.TileGrid[playerTile.X, playerTile.Y].HasPowerPill = false;
                    }

                    // If that was the last crump, lock the game for a while
                    if (noCrumpsLeft()) {
                        GhostSoundsManager.StopLoops();
                        LockTimer = TimeSpan.FromSeconds(2);
                        return;
                    }
                }
            }

            // Eat bonuses
            if (bonusPresent_ && player_.Position.Tile.Y == 17 &&
                ((player_.Position.Tile.X == 13 && player_.Position.DeltaPixel.X == 8) ||
                  (player_.Position.Tile.X == 14 && player_.Position.DeltaPixel.X == -8))) {
                LockTimer = TimeSpan.FromSeconds(1.5);
                Score += Constants.BonusScores();
                scoreEvents_.Add(new ScoreEvent(player_.Position, DateTime.Now, Constants.BonusScores()));
                soundBank_.PlayCue("fruiteat");
                bonusPresent_ = false;
                if (bonusEaten_.ContainsKey(Constants.BonusSprite())) {
                    bonusEaten_[Constants.BonusSprite()]++;
                }
                else {
                    bonusEaten_.Add(Constants.BonusSprite(), 1);
                }
            }

            // Remove bonus if time's up
            if (bonusPresent_ && ((DateTime.Now - bonusSpawnedTime_) > TimeSpan.FromSeconds(10))) {
                bonusPresent_ = false;
            }

            // Detect collision between ghosts and the player
            foreach (Ghost ghost in ghosts_) {
                Rectangle playerArea = new Rectangle((player_.Position.Tile.X * 16) + player_.Position.DeltaPixel.X,
                                                     (player_.Position.Tile.Y * 16) + player_.Position.DeltaPixel.Y,
                                                      26,
                                                      26);
                Rectangle ghostArea = new Rectangle((ghost.Position.Tile.X * 16) + ghost.Position.DeltaPixel.X,
                                                    (ghost.Position.Tile.Y * 16) + ghost.Position.DeltaPixel.Y,
                                                    22,
                                                    22);
                if (!Rectangle.Intersect(playerArea, ghostArea).IsEmpty) {
                    // If collision detected, either kill the ghost or kill the pac man, depending on state.

                    if (ghost.State == GhostState.Blue) {
                        GhostSoundsManager.StopLoops();
                        soundBank_.PlayCue("EatGhost");
                        ghost.State = GhostState.Dead;
                        eatenGhosts_++;
                        int bonus = (int)(100 * Math.Pow(2, eatenGhosts_));
                        Score += bonus;
                        scoreEvents_.Add(new ScoreEvent(ghost.Position, DateTime.Now, bonus));
                        LockTimer = TimeSpan.FromMilliseconds(900);
                        return;
                    }
                    else if (ghost.State != GhostState.Dead ) {
                        KillPacMan();
                        return;
                    }
                    // Otherwise ( = the ghost is dead), don't do anything special.
                }
            }

            // Periodically spawn a fruit, when the player isn't on the spawn location
            // otherwise we get an infinite fruit spawning bug
            if ((Grid.NumCrumps == 180 || Grid.NumCrumps == 80) && bonusSpawned_ < 2 &&
                ! (player_.Position.Tile.Y == 17 &&
                    ((player_.Position.Tile.X == 13 && player_.Position.DeltaPixel.X == 8) ||
                    (player_.Position.Tile.X == 14 && player_.Position.DeltaPixel.X == -8)))) {
                bonusPresent_ = true;
                bonusSpawned_++;
                bonusSpawnedTime_ = DateTime.Now;

            }

            // Now is the time to move player based on inputs and ghosts based on AI
            // If we have returned earlier in the method, they stay in place
            player_.Update(gameTime);
            ghosts_.ForEach(i => i.Update(gameTime));

            base.Update(gameTime);
        }


        /// <summary>
        /// Nice to have for debug purposes. We might want the level to end early.
        /// </summary>
        /// <returns>Whether there are no crumps left on the board.</returns>
        bool noCrumpsLeft() {
            return Grid.NumCrumps == 0;
        }


        /// <summary>
        /// AAAARRRGH
        /// </summary>
        void KillPacMan() {
            player_.State = State.Dying;
            GhostSoundsManager.StopLoops();
            soundBank_.PlayCue("Death");
            LockTimer = TimeSpan.FromMilliseconds(1811);
            playerDied_ = true;
            bonusPresent_ = false;
            bonusSpawned_ = 0;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            // The GameLoop is a main component, so it is responsible for initializing the sprite batch each frame
            spriteBatch_.Begin();

            Vector2 boardPosition = new Vector2(
                (graphics_.PreferredBackBufferWidth / 2) - (board_.Width / 2),
                (graphics_.PreferredBackBufferHeight / 2) - (board_.Height / 2)
                );

            // When all crumps have been eaten, flash until new level is spawned
            // Draw the player and nothing else, just end the spritebatch and return.
            if (noCrumpsLeft()) {
                spriteBatch_.Draw(((DateTime.Now.Second * 1000 + DateTime.Now.Millisecond) / 350) % 2 == 0 ? board_ : boardFlash_, boardPosition, Color.White);
                player_.Draw(gameTime, boardPosition);
                spriteBatch_.End();
                return;
            }
            // Otherwise...

            // Draw the board
            spriteBatch_.Draw(board_, boardPosition, Color.White);

            // Draw crumps and power pills
            Tile[,] tiles = Grid.TileGrid;
            for (int j = 0; j < Grid.Height; j++) {
                for (int i = 0; i < Grid.Width; i++) {
                    if (tiles[i, j].HasPowerPill) {
                        spriteBatch_.Draw(ppill_, new Vector2(
                            boardPosition.X + 3 + (i * 16),
                            boardPosition.Y + 3 + (j * 16)),
                            Color.White);
                    }
                    else if (tiles[i, j].HasCrump) {
                        spriteBatch_.Draw(crump_, new Vector2(
                            boardPosition.X + 5 + (i * 16),
                            boardPosition.Y + 5 + (j * 16)),
                            Color.White);
                    }
                }
            }

            // Draw extra lives; no more than 20 though
            for (int i = 0; i < xlives_ && i < 20; i++) {
                spriteBatch_.Draw(xlife_, new Vector2(boardPosition.X + 10 + (20 * i), board_.Height + boardPosition.Y + 10), Color.White);
            }

            // Draw current score
            spriteBatch_.DrawString(scoreFont_, "SCORE", new Vector2(boardPosition.X + 30, boardPosition.Y - 50), Color.White);
            spriteBatch_.DrawString(scoreFont_, Score.ToString(), new Vector2(boardPosition.X + 30, boardPosition.Y - 30), Color.White);

            // Draw current level
            spriteBatch_.DrawString(scoreFont_, "LEVEL", new Vector2(boardPosition.X + board_.Width - 80, boardPosition.Y - 50), Color.White);
            spriteBatch_.DrawString(scoreFont_, Constants.Level.ToString(), new Vector2(boardPosition.X + board_.Width - 80, boardPosition.Y - 30), Color.White);

            // Draw a bonus fruit if any
            if (bonusPresent_) {
                spriteBatch_.Draw(bonus_[Constants.BonusSprite()], new Vector2(boardPosition.X + (13 * 16) + 2, boardPosition.Y + (17 * 16) - 8), Color.White);
            }

            // Draw captured bonus fruits at the bottom of the screen
            int k = 0;
            foreach (KeyValuePair<string, int> kvp in bonusEaten_) {
                for (int i = 0; i < kvp.Value; i++) {
                    spriteBatch_.Draw(bonus_[kvp.Key], new Vector2(boardPosition.X + 10 + (22 * (k + i)), board_.Height + boardPosition.Y + 22), Color.White);
                }
                k += kvp.Value; 
            }

            // Draw ghosts
            ghosts_.ForEach( i => i.Draw(gameTime, boardPosition));

            // Draw player
            player_.Draw(gameTime, boardPosition);

            // Draw special scores (as when a ghost or fruit has been eaten)
            foreach (ScoreEvent se in scoreEvents_) {
                spriteBatch_.DrawString(scoreEventFont_, se.Score.ToString(), new Vector2(boardPosition.X + (se.Position.Tile.X * 16) + se.Position.DeltaPixel.X + 4,
                                                                                           boardPosition.Y + (se.Position.Tile.Y * 16) + se.Position.DeltaPixel.Y + 4), Color.White);            
            }

            // Draw GET READY ! at level start
            if (player_.State == State.Start) {
                spriteBatch_.DrawString(scoreFont_, "GET READY!", new Vector2(boardPosition.X + (board_.Width / 2) - 58, boardPosition.Y + 273), Color.Yellow);
            }

            // Display number of crumps (for debug)
            //spriteBatch_.DrawString(scoreFont_, "Crumps left :" + Grid.NumCrumps.ToString(), Vector2.Zero, Color.White);

            spriteBatch_.End();
        }



        // DRAWING
        Dictionary<string, Texture2D> bonus_;
        Texture2D xlife_;
        Texture2D board_;
        Texture2D boardFlash_;
        Texture2D crump_;
        Texture2D ppill_;
        SpriteFont scoreFont_;
        SpriteFont scoreEventFont_;
        SoundBank soundBank_;
        GraphicsDeviceManager graphics_;
        SpriteBatch spriteBatch_;

        // LOGIC
        List<Ghost> ghosts_;
        Player player_;
        TimeSpan lockTimer_;
        DateTime eventTimer_;
        int bonusSpawned_;
        bool bonusPresent_;
        DateTime bonusSpawnedTime_;
        Dictionary<string, int> bonusEaten_;
        bool playerDied_;
        bool paChomp_;
        int xlives_;
        int score_;
        int eatenGhosts_;
        List<ScoreEvent> scoreEvents_;

        /// <summary>
        /// The player's current score.
        /// </summary>
        public int Score {
            get { return score_; }
            private set {
                if ((value / 10000) > (score_ / 10000)) {
                    soundBank_.PlayCue("ExtraLife");
                    xlives_++;
                }
                score_ = value; 
            }
        }

        /// <summary>
        /// For how much time we want to lock the game.
        /// </summary>
        private TimeSpan LockTimer {
            get { return lockTimer_; }
            set { eventTimer_ = DateTime.Now; lockTimer_ = value; }
        }
    }
}