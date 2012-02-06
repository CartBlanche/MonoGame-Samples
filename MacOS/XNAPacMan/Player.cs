using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace XNAPacMan {
    /// <summary>
    /// Defines the position of an entity (player, ghost) on the board. 
    /// </summary>
    public struct Position {
        public Position(Point Tile, Point DeltaPixel) {
            this.Tile = Tile;
            this.DeltaPixel = DeltaPixel;
        }
        /// <summary>
        /// The tile the entity is on.
        /// </summary>
        public Point Tile;
        /// <summary>
        /// How many pixels the entity is off its nominal tile.
        /// </summary>
        public Point DeltaPixel;
    }

    public enum Direction { Up, Down, Left, Right };
    public enum State { Start, Normal, Dying };

    /// <summary>
    /// This is the yellow pac man that eat dots and gets killed 
    /// repetitively unless you're good.
    /// </summary>
    public class Player {

        public Player(Game game) {
            Reset();
            this.game = game;
            updatesPerPixel_ = Constants.PacManSpeed();
            spriteBatch_ = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            eatingFrames_ = new Texture2D[] {
                game.Content.Load<Texture2D>("sprites/PacManEating1"),
                game.Content.Load<Texture2D>("sprites/PacManEating2"),
                game.Content.Load<Texture2D>("sprites/PacManEating3"),
                game.Content.Load<Texture2D>("sprites/PacManEating4"),
                game.Content.Load<Texture2D>("sprites/PacManEating5"),
                game.Content.Load<Texture2D>("sprites/PacManEating6"),
                game.Content.Load<Texture2D>("sprites/PacManEating7"),
                game.Content.Load<Texture2D>("sprites/PacManEating8"),
                game.Content.Load<Texture2D>("sprites/PacManEating9"),
            };
            dyingFrames_ = game.Content.Load<Texture2D>("sprites/DyingSheetNew");
        }


        /// <summary>
        /// Allows the Player to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Update(GameTime gameTime) {

            // First, deal with keyboard input. Get only the direction keys.
            Keys[] validKeys = { Keys.Up, Keys.Down, Keys.Left, Keys.Right };
            Keys[] pressedKeys = (from k in Keyboard.GetState().GetPressedKeys()
                                  where validKeys.Contains(k)
                                  select k).ToArray();

            // If the player is pressing more than one key, we don't turn. Trying to filter keys would be
            // either bogus or overly complex for the needs of this game, and yes, this is from experience.
            if (pressedKeys.Length == 1) {

                // At level start, Pac Man is facing right, although he is drawn full. Pressing
                // Right simply makes him start moving in that direction; pressing Left make him change direction
                // first; this must be handled separately from the usual case.
                if (state_ == State.Start) {
                    if (pressedKeys[0] == Keys.Left || pressedKeys[0] == Keys.Right) {
                        state_ = State.Normal;
                    }
                    if (pressedKeys[0] == Keys.Left) {
                        TryTurn(pressedKeys[0]);
                    }
                }
                // Normal case : turn if required direction != current direction.
                else if ((direction_.ToString() != pressedKeys[0].ToString())) {
                    TryTurn(pressedKeys[0]);
                }
            }

            TryMove();
        }

        /// <summary>
        /// Ensures that if the Pac Man moves, it is a legal move
        /// </summary>
        void TryMove() {
            if (state_ == State.Start) {
                return;
            }
            // If between two tiles, movement is always allowed since TryTurn()
            // always ensures direction is valid
            if (position_.DeltaPixel != Point.Zero) {
                DoMove();
            }
            // Special case : the tunnel.
            else if ((position_.Tile == new Point(0, 14) && direction_ == Direction.Left) ||
                     (position_.Tile == new Point(27, 14) && direction_ == Direction.Right)) {
                DoMove();
            }
            // If on a tile, we only move if the next tile in our direction is open
            else if ((direction_ == Direction.Up && Grid.TileGrid[position_.Tile.X, position_.Tile.Y - 1].Type == TileTypes.Open) ||
                      (direction_ == Direction.Down && Grid.TileGrid[position_.Tile.X, position_.Tile.Y + 1].Type == TileTypes.Open) ||
                      (direction_ == Direction.Left && Grid.TileGrid[position_.Tile.X - 1, position_.Tile.Y].Type == TileTypes.Open) ||
                      (direction_ == Direction.Right && Grid.TileGrid[position_.Tile.X + 1, position_.Tile.Y].Type == TileTypes.Open)) {
                DoMove();
            }

        }


        /// <summary>
        /// Effectively moves the Pac Man according to member variable direction_.
        /// </summary>
        void DoMove() {
            // Everytime the updateCount == updatesPerPixel, move one pixel in
            // the desired direction and reset the updateCount.
            updateCount_++;
            updateCount_ %= updatesPerPixel_;
            if (updateCount_ == 0) {

                // Now is a nice time to update our speed, like we do for the ghosts.
                if (Ghost.NextTile(direction_, position_).HasCrump) {
                    updatesPerPixel_ = Constants.PacManSpeed() + 2;
                }
                else {
                    updatesPerPixel_ = Constants.PacManSpeed();
                }

                // Move one pixel in the desired direction
                if (direction_ == Direction.Up) {
                    position_.DeltaPixel.Y--;
                }
                else if (direction_ == Direction.Down) {
                    position_.DeltaPixel.Y++;
                }
                else if (direction_ == Direction.Left) {
                    position_.DeltaPixel.X--;
                }
                else if (direction_ == Direction.Right) {
                    position_.DeltaPixel.X++;
                }

                // By moving one pixel we might have moved to another tile completely
                if (position_.DeltaPixel.X == 16) {
                    // Special case : the tunnel
                    if (position_.Tile.X == 27) {
                        position_.Tile.X = 0;
                    }
                    else {
                        position_.Tile.X++;
                    }
                    position_.DeltaPixel.X = 0;
                }
                else if (position_.DeltaPixel.X == -16) {
                    // Special case : the tunnel
                    if (position_.Tile.X == 0) {
                        position_.Tile.X = 27;
                    }
                    else {
                        position_.Tile.X--;
                    }
                    position_.DeltaPixel.X = 0;
                }
                else if (position_.DeltaPixel.Y == 16) {
                    position_.Tile.Y++;
                    position_.DeltaPixel.Y = 0;
                }
                else if (position_.DeltaPixel.Y == -16) {
                    position_.Tile.Y--;
                    position_.DeltaPixel.Y = 0;
                }
            }
        }

        /// <summary>
        /// Allows the Player to be drawn to the screen. Assumes spritebatch.begin() has been called, and
        /// spritebatch.end() will be called afterwards.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Draw(GameTime gameTime, Vector2 boardPosition) {
            // The position is taken as a function of the board position, the tile on which the pac man is, how many
            // pixels he is off from this tile, and the size of the pac man itself versus the size of a tile (16).
            Vector2 position;
            position.X = boardPosition.X + (position_.Tile.X * 16) + position_.DeltaPixel.X - ((eatingFrames_[0].Width - 16) / 2);
            position.Y = boardPosition.Y + (position_.Tile.Y * 16) + position_.DeltaPixel.Y - ((eatingFrames_[0].Height - 16) / 2);

            // At level start, just draw the full pac man and exit
            if (state_ == State.Start) {
                spriteBatch_.Draw(eatingFrames_[0], position, Color.White);
            }

            else if (state_ == State.Normal) {
                // The frame index is taken as a function of how much the pac man is off from a tile, the size of
                // a tile (always 16 pixels), and how many frames we use for the animation.
                int frame = Math.Abs(position_.DeltaPixel.X + position_.DeltaPixel.Y) / (16 / usedFramesIndex_.Length);
                frame = (int)MathHelper.Clamp(frame, 0, usedFramesIndex_.Length - 1);
                RenderSprite(eatingFrames_[usedFramesIndex_[frame]], null, boardPosition, position);
            }

            else if (state_ == State.Dying) {
                int timeBetweenFrames = 90; // Sound "Death" is 1811 milliseconds long, we have 20 frames to go through.
                //timer_ += gameTime.ElapsedRealTime;
                timer_ += gameTime.ElapsedGameTime;
                int index = (timer_.Seconds * 1000 + timer_.Milliseconds) / timeBetweenFrames;
                if (index > 19) {
                    return;
                }
                RenderSprite(dyingFrames_, new Rectangle(26 * index, 0, 26, 26), boardPosition, position);
            }

        }


        /// <summary>
        /// Allows rendering across the tunnel, which is tricky.
        /// </summary>
        /// <param name="spriteSheet">Source texture</param>
        /// <param name="rectangle">Portion of the source to render. Pass null for rendering the whole texture.</param>
        /// <param name="boardPosition">Top-left pixel of the board in the screen</param>
        /// <param name="position">Where to render the texture.</param>
        void RenderSprite(Texture2D spriteSheet, Rectangle? rectangle, Vector2 boardPosition, Vector2 position) {

            Rectangle rect = rectangle == null ? new Rectangle(0, 0, spriteSheet.Width, spriteSheet.Height) :
                                                rectangle.Value;

            // What happens when we are crossing to the other end by the tunnel?
            // We detect if part of the pacman is rendered outside of the board.
            // First, to the left.
            if (position.X < boardPosition.X) {
                int deltaPixel = (int)(boardPosition.X - position.X); // Number of pixels off the board
                var leftPortion = new Rectangle(rect.X + deltaPixel, rect.Y, 26 - deltaPixel, 26);
                var leftPortionPosition = new Vector2(boardPosition.X, position.Y);
                var rightPortion = new Rectangle(rect.X, rect.Y, deltaPixel, 26);
                var rightPortionPosition = new Vector2(boardPosition.X + (16 * 28) - deltaPixel, position.Y);
                spriteBatch_.Draw(spriteSheet, leftPortionPosition, leftPortion, Color.White);
                spriteBatch_.Draw(spriteSheet, rightPortionPosition, rightPortion, Color.White);
            }
            // Next, to the right
            else if (position.X > (boardPosition.X + (16 * 28) - 26)) {
                int deltaPixel = (int)((position.X + 26) - (boardPosition.X + (16 * 28))); // Number of pixels off the board
                var leftPortion = new Rectangle(rect.X + 26 - deltaPixel, rect.Y, deltaPixel, 26);
                var leftPortionPosition = new Vector2(boardPosition.X, position.Y);
                var rightPortion = new Rectangle(rect.X, rect.Y, 26 - deltaPixel, 26);
                var rightPortionPosition = new Vector2(position.X, position.Y);
                spriteBatch_.Draw(spriteSheet, leftPortionPosition, leftPortion, Color.White);
                spriteBatch_.Draw(spriteSheet, rightPortionPosition, rightPortion, Color.White);
            }
            // Draw normally - in one piece
            else {
                spriteBatch_.Draw(spriteSheet, position, rect, Color.White);
            }
        }

        /// <summary>
        /// Should be called anytime the Pac Man needs to be reset (game start, level start)
        /// </summary>
        void Reset() {
            state_ = State.Start;
            direction_ = Direction.Right;
            usedFramesIndex_ = new int[] { 0, 1, 2 };
            position_ = new Position { Tile = new Point(13, 23), DeltaPixel = new Point(8, 0) };
            updateCount_ = 0;
        }



        /// <summary>
        /// Ensures that if the Pac Man turns, it's in a valid direction.
        /// </summary>
        /// <param name="input">Direction the player tries to steer the Pac Man towards</param>
        void TryTurn(Keys input) {

            // If we're between two tiles, we can only turn 180
            if (position_.DeltaPixel != Point.Zero) {
                if ((direction_ == Direction.Up && input == Keys.Down) ||
                    (direction_ == Direction.Down && input == Keys.Up) ||
                    (direction_ == Direction.Left && input == Keys.Right) ||
                    (direction_ == Direction.Right && input == Keys.Left)) {
                    // Turning 180 between two tiles implies destination tile is open, 
                    // no other validation to be done
                    DoTurn(input);
                }
            }
            // Special case : the tunnel.
            else if ((input == Keys.Left && position_.Tile.X == 0) ||
                      (input == Keys.Right && position_.Tile.X == 27)) {
                DoTurn(input);
            }
            // We're exactly on a tile; this is the "general" case
            // Do turn if the destination tile is open
            else if ((input == Keys.Up && Grid.TileGrid[position_.Tile.X, position_.Tile.Y - 1].Type == TileTypes.Open) ||
                      (input == Keys.Down && Grid.TileGrid[position_.Tile.X, position_.Tile.Y + 1].Type == TileTypes.Open) ||
                      (input == Keys.Left && Grid.TileGrid[position_.Tile.X - 1, position_.Tile.Y].Type == TileTypes.Open) ||
                      (input == Keys.Right && Grid.TileGrid[position_.Tile.X + 1, position_.Tile.Y].Type == TileTypes.Open)) {
                DoTurn(input);
            }

        }


        /// <summary>
        /// This effectively makes Pac Man turn.
        /// We have to update the sprites used for animation,
        /// and if the Pac Man is between two tiles, change his Position.
        /// </summary>
        /// <param name="newDirection">Direction to turn towards</param>
        void DoTurn(Keys newDirection) {

            switch (newDirection) {
                case Keys.Up:
                    direction_ = Direction.Up;
                    usedFramesIndex_ = new int[] { 0, 7, 8 };
                    if (position_.DeltaPixel != Point.Zero) {
                        position_.Tile.Y += 1;
                        position_.DeltaPixel.Y -= 16;
                    }
                    break;
                case Keys.Down:
                    direction_ = Direction.Down;
                    usedFramesIndex_ = new int[] { 0, 3, 4 };
                    if (position_.DeltaPixel != Point.Zero) {
                        position_.Tile.Y -= 1;
                        position_.DeltaPixel.Y += 16;
                    }
                    break;
                case Keys.Left:
                    direction_ = Direction.Left;
                    usedFramesIndex_ = new int[] { 0, 5, 6 };
                    if (position_.DeltaPixel != Point.Zero) {
                        position_.Tile.X += 1;
                        position_.DeltaPixel.X -= 16;
                    }
                    break;
                case Keys.Right:
                    direction_ = Direction.Right;
                    usedFramesIndex_ = new int[] { 0, 1, 2 };
                    if (position_.DeltaPixel != Point.Zero) {
                        position_.Tile.X -= 1;
                        position_.DeltaPixel.X += 16;
                    }
                    break;
            }
        }

        Game game;
        TimeSpan timer_;
        SpriteBatch spriteBatch_;
        Texture2D dyingFrames_;
        Texture2D[] eatingFrames_;
        int[] usedFramesIndex_;
        int updatesPerPixel_;
        int updateCount_;
        Position position_;
        Direction direction_;
        State state_;

        public State State { get { return state_; } set { state_ = value; } }
        public Direction Direction { get { return direction_; } }
        public Position Position { get { return position_; } }
        
    }
}