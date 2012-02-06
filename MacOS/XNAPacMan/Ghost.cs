using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;


namespace XNAPacMan {

    public enum GhostState { Home, Scatter, Attack, Blue, Dead }
    public enum Ghosts { Blinky, Pinky, Inky, Clyde }


    /// <summary>
    /// One of the ghosts that try to kill Pac Man. 
    /// </summary>
    public class Ghost {
        /// <summary>
        /// Instantiates a ghost.
        /// </summary>
        /// <param name="game">A reference to the Game object, needed for access to services.</param>
        /// <param name="player">A reference to the Pac Man, needed for AI.</param>
        /// <param name="identity">Which ghost, needed for appearance and behavior.</param>
        public Ghost(Game game, Player player, Ghosts identity) {
            spriteBatch_ = (SpriteBatch)game.Services.GetService(typeof(SpriteBatch));
            ghostBase1_ = game.Content.Load<Texture2D>("sprites/GhostBase");
            ghostBase2_ = game.Content.Load<Texture2D>("sprites/GhostBase2");
            ghostChased_ = game.Content.Load<Texture2D>("sprites/GhostChased");
            eyesBase_ = game.Content.Load<Texture2D>("sprites/GhostEyes");
            eyesCenter_ = game.Content.Load<Texture2D>("sprites/GhostEyesCenter");
            colorBase_ = Constants.colors(identity);
            identity_ = identity;
            previousNumCrumps_ = 0;
            Reset(true, player);
            wiggle_ = true;
            direction_ = new Direction();
            lastJunction_ = new Point();
            scatterTiles_ = Constants.scatterTiles(identity);
        }

        /// <summary>
        /// Put the ghosts back in their home, ready to begin a new attack
        /// </summary>
        /// <param name="newLevel">True at level start, false otherwise</param>
        /// <param name="player">The pac man. Pac Man is often respawned with the ghosts, so they need to know about the new Pac Man.</param>
        public void Reset(bool newLevel, Player player) {
            State = GhostState.Home; // Ghosts start at home
            previousState_ = GhostState.Home; // Sounds are played when currentState != previousState.
            // So to get them playing at level start, we do this simple hack.
            updateCount_ = 0;
            initialJumps_ = Constants.InitialJumps(identity_, newLevel);
            position_ = Constants.startPosition(identity_);
            scheduleStateEval_ = true;
            lastJunction_ = new Point();
            player_ = player;
            scatterModesLeft_ = 4;
            UpdateSpeed();
        }

        /// <summary>
        /// When we need to lock the game, make sure the timers here are updated to reflect
        /// the "waste of time".
        /// </summary>
        public void LockTimer(GameTime gameTime) {
            timeInCurrentState += gameTime.ElapsedGameTime;
        }

        /// <summary>
        /// In case this ghost is Inky, he will need a reference to blinky in order
        /// to find his way around the maze. Otherwise, setting this has no effect.
        /// </summary>
        /// <param name="blinky">A reference to Blinky.</param>
        public void SetBlinky(Ghost blinky) {
            blinky_ = blinky;
        }

        /// <summary>
        /// Call this every game update to get those ghosts moving around.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Update(GameTime gameTime) {
            if (scheduleStateEval_) {
                StateEval();
            }
            // After the AI has had the occasion to run, update lastJuntion.
            if (position_.DeltaPixel == Point.Zero && IsAJunction(position_.Tile)) {
                lastJunction_ = position_.Tile;
            }
            Move();
        }


        void PlaySound() {
            if (State == GhostState.Attack || State == GhostState.Scatter || State == GhostState.Home) {
                if (Grid.NumCrumps < 50) {
                    GhostSoundsManager.playLoopAttackVeryFast();
                } else if (Grid.NumCrumps < 120) {
                    GhostSoundsManager.playLoopAttackFast();
                } else {
                    GhostSoundsManager.playLoopAttack();
                }
            } else if (State == GhostState.Blue) {
                GhostSoundsManager.playLoopBlue();
            } else if (State == GhostState.Dead) {
                GhostSoundsManager.playLoopDead();
            }
        }

        void StateEval() {

            GhostState initialState = State;
            scheduleStateEval_ = false;

            switch (State) {
                case GhostState.Home:
                    // Ghost exit the home state for the scatter state when they get to the row
                    // above the home
                    if (position_.Tile.Y == 11 && position_.DeltaPixel.Y == 0) {
                        // Select inital direction based on scatter tiles
                        if (Constants.scatterTiles(identity_)[0].X < 13) {
                            direction_ = Direction.Left;
                        } else {
                            direction_ = Direction.Right;
                        }
                        if (scatterModesLeft_ > 0) {
                            State = GhostState.Scatter;
                        } else {
                            State = GhostState.Attack;
                        }
                        return;
                    }
                        // Ghosts move up when they are aligned with the entrance
                    else if (position_.Tile.X == 13 && position_.DeltaPixel.X == 8) {
                        direction_ = Direction.Up;
                    }
                        // When on one side, move towards middle when on the bottom and time's up
                        // If time's not up, keep bouncing up and down
                    else if ((position_.DeltaPixel.Y == 8) &&
                            ((position_.Tile.X == 11 && position_.DeltaPixel.X == 8) ||
                             (position_.Tile.X == 15 && position_.DeltaPixel.X == 8))) {
                        if (position_.Tile.Y == 14) {
                            initialJumps_--;
                            if (initialJumps_ == 0) {
                                if (position_.Tile.X == 11) {
                                    direction_ = Direction.Right;
                                } else {
                                    direction_ = Direction.Left;
                                }
                            } else {
                                direction_ = Direction.Up;
                            }
                        } else if (position_.Tile.Y == 13) {
                            direction_ = Direction.Down;
                        }
                    }
                    break;
                case GhostState.Scatter:
                    // Attempt to reverse direction upon entering this state
                    if (previousState_ == GhostState.Attack) {
                        scatterModesLeft_--;
                        if (NextTile(OppositeDirection(direction_)).IsOpen) {
                            direction_ = OppositeDirection(direction_);
                        }
                    }
                    AIScatter();
                    int timeInScatterMode = scatterModesLeft_ <= 2 ? 5 : 7;
                    if ((DateTime.Now - timeInCurrentState) > TimeSpan.FromSeconds(timeInScatterMode)) {
                        State = GhostState.Attack;
                    }
                    break;
                case GhostState.Dead:
                    // Attempt to reverse direction upon entering this state
                    if (previousState_ != GhostState.Dead && previousState_ != GhostState.Blue) {
                        if (NextTile(OppositeDirection(direction_)).IsOpen) {
                            direction_ = OppositeDirection(direction_);
                        }
                    } else {
                        AIDead();
                    }
                    if (position_.DeltaPixel.X == 8 && position_.DeltaPixel.Y == 8) {
                        if (position_.Tile.Y == 14) {
                            State = GhostState.Home;
                        }
                    }
                    break;
                case GhostState.Attack:
                    // Attempt to reverse direction upon entering this state
                    if (previousState_ != GhostState.Attack && previousState_ != GhostState.Blue) {
                        if (NextTile(OppositeDirection(direction_)).IsOpen) {
                            direction_ = OppositeDirection(direction_);
                        }
                    } else {
                        AIAttack();
                    }

                    if ((DateTime.Now - timeInCurrentState) > TimeSpan.FromSeconds(20)) {
                        State = GhostState.Scatter;
                    }
                    break;
                case GhostState.Blue:
                    // Attempt to reverse direction upon entering this state
                    if (previousState_ != GhostState.Blue) {
                        if (NextTile(OppositeDirection(direction_)).IsOpen) {
                            direction_ = OppositeDirection(direction_);
                        }
                    } else {
                        // TODO : make special blue AI
                        AIAttack();
                    }

                    // When blue time is over, revert to attack mode.
                    if ((DateTime.Now - timeInCurrentState) > TimeSpan.FromSeconds(Constants.BlueTime())) {
                        State = GhostState.Attack;
                    }
                    break;
            }

            // TODO : move all these magic numbers to the Constants class.
            // We select a new sound only upon some state change, or when 
            // the number of crumps goes below certain thresholds.
            if ((initialState != previousState_) ||
                (Grid.NumCrumps == 199 && previousNumCrumps_ == 200) ||
                (Grid.NumCrumps == 19 && previousNumCrumps_ == 20)) {
                PlaySound();
            }
            previousState_ = initialState;
        }

        void Move() {
            updateCount_++;
            updateCount_ %= updatesPerPixel_;
            if (updateCount_ == 0) {
                // There is no point running the full AI each update, especially considering we
                // want this code to run at 1000 updates per second. We run it each time it moves by a pixel.
                scheduleStateEval_ = true;

                // Now is a nice time to evaluate whether our current speed is correct. This variable
                // may change under a wide array of circumstances, so by putting it here we probably generate
                // too many checks but we ensure correct behavior.
                UpdateSpeed();

                switch (direction_) {
                    case Direction.Up:
                        position_.DeltaPixel.Y--;
                        if (position_.DeltaPixel.Y < 0) {
                            position_.DeltaPixel.Y = 15;
                            position_.Tile.Y--;
                        }
                        break;
                    case Direction.Down:
                        position_.DeltaPixel.Y++;
                        if (position_.DeltaPixel.Y > 15) {
                            position_.DeltaPixel.Y = 0;
                            position_.Tile.Y++;
                        }
                        break;
                    case Direction.Left:
                        position_.DeltaPixel.X--;
                        if (position_.DeltaPixel.X < 0) {
                            position_.DeltaPixel.X = 15;
                            position_.Tile.X--;
                            // Special case : the tunnel
                            if (position_.Tile.X < 0) {
                                position_.Tile.X = Grid.Width - 1;
                            }
                        }
                        break;
                    case Direction.Right:
                        position_.DeltaPixel.X++;
                        if (position_.DeltaPixel.X > 15) {
                            position_.DeltaPixel.X = 0;
                            position_.Tile.X++;
                            // Special case : the tunnel
                            if (position_.Tile.X > Grid.Width - 1) {
                                position_.Tile.X = 0;
                            }
                        }
                        break;
                }
            }
        }

        void UpdateSpeed() {
            int baseSpeed = Constants.GhostSpeed();
            if (State == GhostState.Home) {
                updatesPerPixel_ = baseSpeed * 2;
            } else if (State == GhostState.Blue) {
                updatesPerPixel_ = (int)(baseSpeed * 1.5);
            } else if (identity_ == Ghosts.Blinky && Grid.NumCrumps <= Constants.CruiseElroyTimer()) {
                updatesPerPixel_ = baseSpeed - 1;
            }
                // If in the tunnel, reduce speed
              else if (position_.Tile.Y == 14 &&
                  ((0 <= position_.Tile.X && position_.Tile.X <= 5) ||
                    (22 <= position_.Tile.X && position_.Tile.X <= 27))) {
                updatesPerPixel_ = baseSpeed + 5;
            } else {
                updatesPerPixel_ = baseSpeed;
            }

        }

        /// <summary>
        /// Guides the ghost towards his "favored" area of the board, as defined by scatterTiles_.
        /// </summary> 
        void AIScatter() {
            // As with AIAttack(), all the method does is change direction if necessary,
            // which only happens when exactly at a junction
            if (position_.DeltaPixel != Point.Zero || !IsAJunction(position_.Tile)) {
                return;
            }

            // Scatter AI may be overriden by certain tiles
            if (AIOverride()) {
                return;
            }

            // If we are on one of our favored tiles, go on to the next
            int favoredTile = scatterTiles_.FindIndex(i => i == position_.Tile);
            int nextFavoredTile = (favoredTile + 1) % (scatterTiles_.Count);
            if (favoredTile != -1) {
                direction_ = FindDirection(scatterTiles_[nextFavoredTile]);
            }
                // If our last junction was a favored tile and this one isn't, ignore it
            else if (!scatterTiles_.Contains(lastJunction_)) {
                // Otherwise, find scatter tile closest to our position and head for it.
                List<Point> orderedTiles = scatterTiles_.ToList();
                orderedTiles.Sort((a, b) => Vector2.Distance(new Vector2(position_.Tile.X, position_.Tile.Y),
                                                             new Vector2(a.X, a.Y)).
                                            CompareTo(
                                            Vector2.Distance(new Vector2(position_.Tile.X, position_.Tile.Y),
                                                             new Vector2(b.X, b.Y))));
                direction_ = FindDirection(orderedTiles[0]);
            }
        }

        /// <summary>
        /// Guides the ghost to try to reach the player
        /// </summary>
        void AIAttack() {
            // All this method does is change direction if necessary.
            // There is only one case in which we may potentially change direction :
            // when the ghost is exactly at a junction.
            if (position_.DeltaPixel != Point.Zero || !IsAJunction(position_.Tile)) {
                return;
            }

            // Attack AI may be overriden by certain tiles
            if (AIOverride()) {
                return;
            }

            switch (identity_) {
                case Ghosts.Blinky:
                    AttackAIBlinky();
                    break;
                case Ghosts.Pinky:
                    AttackAIPinky();
                    break;
                case Ghosts.Inky:
                    AttackAIInky();
                    break;
                case Ghosts.Clyde:
                    AttackAIClyde();
                    break;
                default:
                    throw new ArgumentException();
            }
        }

        void AIDead() {

            // When aligned with the entrance, go down. When the ghost has fully entered the house,
            // stateEval will change state to Home.
            if (position_.Tile.Y == 11 && position_.Tile.X == 13 && position_.DeltaPixel.X == 8) {
                direction_ = Direction.Down;
            }
                // Otherwise, only change direction if the ghost is exactly on a tile
            else if (position_.DeltaPixel == Point.Zero) {
                // We direct the ghost towards one of the two squares above the home. When he reaches one,
                // he still has to move slightly right or left in order to align with the entrance.
                if (position_.Tile.X == 13 && position_.Tile.Y == 11) {
                    direction_ = Direction.Right;
                } else if (position_.Tile.X == 14 && position_.Tile.Y == 11) {
                    direction_ = Direction.Left;
                }
                    // Otherwise, when at a junction, head for the square above home closest to us.
                else if (IsAJunction(position_.Tile)) {
                    if (position_.Tile.X > 13) {
                        direction_ = FindDirection(new Point(14, 11));
                    } else {
                        direction_ = FindDirection(new Point(13, 11));
                    }
                }
            }
        }

        /// <summary>
        /// Blinky is extremely straightforward : head directly for the player
        /// </summary>
        void AttackAIBlinky() {
            direction_ = FindDirection(player_.Position.Tile);
        }

        /// <summary>
        /// Pinky tries to head for two tiles ahead of the player.
        /// </summary>
        void AttackAIPinky() {
            Tile nextTile = NextTile(player_.Direction, player_.Position);
            Tile nextNextTile = NextTile(player_.Direction, new Position(nextTile.ToPoint, Point.Zero));
            direction_ = FindDirection(nextNextTile.ToPoint);
        }

        /// <summary>
        /// Inky is a bit more random. He will try to head for a square situated across
        /// the pac man from blinky's location.
        /// </summary>
        void AttackAIInky() {
            Tile nextTile = NextTile(player_.Direction, player_.Position);
            Tile nextNextTile = NextTile(player_.Direction, new Position(nextTile.ToPoint, Point.Zero));
            Vector2 line = new Vector2(blinky_.Position.Tile.X - nextNextTile.ToPoint.X, blinky_.Position.Tile.Y - nextNextTile.ToPoint.Y);
            line *= 2;
            Point destination = new Point(position_.Tile.X + (int)line.X, position_.Tile.Y + (int)line.Y);
            // Prevent out-of-bounds exception
            destination.X = (int)MathHelper.Clamp(destination.X, 0, Grid.Width - 1);
            destination.Y = (int)MathHelper.Clamp(destination.Y, 0, Grid.Height - 1);
            direction_ = FindDirection(destination);
        }

        /// <summary>
        /// Clyde is the bizarre one. When within 8 tiles of the player,
        /// he will head for his favored corner (AIScatter). When farther away,
        /// he will near the player using Blinky's AI.
        /// </summary>
        void AttackAIClyde() {
            float distanceToPlayer = Vector2.Distance(
                new Vector2(player_.Position.Tile.X, player_.Position.Tile.Y),
                new Vector2(position_.Tile.X, position_.Tile.Y));
            if (distanceToPlayer >= 8) {
                AttackAIBlinky();
            } else {
                AIScatter();
            }
        }

        // On certain tiles, we force all the ghosts in a particular direction.
        // This is to prevent them from bunching up too much, and give attentive
        // players some sure-fire ways to escape.
        bool AIOverride() {
            if (position_.Tile.Y == 11 && (position_.Tile.X == 12 || position_.Tile.X == 15)) {
                return (direction_ == Direction.Right || direction_ == Direction.Left);
            } else if (position_.Tile.Y == 20 && (position_.Tile.X == 9 || position_.Tile.X == 18)) {
                return (direction_ == Direction.Right || direction_ == Direction.Left);
            } else {
                return false;
            }
        }


        /// <summary>
        /// Returns the opposite of the specified direction 
        /// </summary>
        /// <param name="d">direction</param>
        /// <returns>opposite direction</returns>
        Direction OppositeDirection(Direction d) {
            switch (d) {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Returns in what direction we should go next in order to reach the destination.
        /// </summary>
        /// <param name="destination">Where we want to go</param>
        /// <returns>Where to go next</returns>
        Direction FindDirection(Point destination) {
            // We use a stupid but very efficient algorithm : go in the direction that
            // closes most distance between position and destination. This works well given
            // there are no dead ends and multiple paths leading to the destination.
            // However, we can never turn 180, and will rather choose a less-than-optimal path.
            int xDistance = destination.X - position_.Tile.X;
            int yDistance = destination.Y - position_.Tile.Y;

            var directions = new List<Direction>(4);

            // Order directions by shortest path. Note: there is probably a better way to do this, probably if I hadn't used an
            // enum for directions in the first place. 
            if (Math.Abs(xDistance) > Math.Abs(yDistance)) {
                if (xDistance > 0) {
                    if (yDistance < 0) { // Direction is Up-Right, favor Right
                        directions = new List<Direction> { Direction.Right, Direction.Up, Direction.Down, Direction.Left };
                    } else { // Direction is Down-Right, favor Right
                        directions = new List<Direction> { Direction.Right, Direction.Down, Direction.Up, Direction.Left };
                    }
                } else {
                    if (yDistance < 0) { // Direction is Up-Left, favor Left
                        directions = new List<Direction> { Direction.Left, Direction.Up, Direction.Down, Direction.Right };
                    } else { // Direction is Down-Left, favor Left
                        directions = new List<Direction> { Direction.Left, Direction.Down, Direction.Up, Direction.Right };
                    }
                }
            } else {
                if (xDistance > 0) {
                    if (yDistance < 0) { // Direction is Up-Right, favor Up
                        directions = new List<Direction> { Direction.Up, Direction.Right, Direction.Left, Direction.Down };
                    } else { // Direction is Down-Right, favor Down
                        directions = new List<Direction> { Direction.Down, Direction.Right, Direction.Left, Direction.Up };
                    }
                } else {
                    if (yDistance < 0) { // Direction is Up-Left, favor Up
                        directions = new List<Direction> { Direction.Up, Direction.Left, Direction.Right, Direction.Down };
                    } else { // Direction is Down-Left, favor Down
                        directions = new List<Direction> { Direction.Down, Direction.Left, Direction.Right, Direction.Up };
                    }
                }
            }

            // Select first item in the list to meet two essential conditions : the path ahead is open,
            // and it's not the opposite direction. If we can't meet both conditions, just return the first
            // direction that leads to an open square. 
            int index = directions.FindIndex(i => i != OppositeDirection(direction_) && NextTile(i).IsOpen);
            if (index != -1) {
                return directions[index];
            } else {
                // Put a breakpoint here, this should never happen.
                return directions.Find(i => NextTile(i).IsOpen);
            }
        }


        /// <summary>
        /// Retrieves the next tile in the specified direction from the ghost's position.
        /// </summary>
        /// <param name="d">Direction in which to look</param>
        /// <returns>The tile</returns>
        Tile NextTile(Direction d) {
            return NextTile(d, position_);
        }

        /// <summary>
        /// Retrieves the next tile in the specified direction from the specified position.
        /// </summary>
        /// <param name="d">Direction in which to look</param>
        /// <param name="p">Position from which to look</param>
        /// <returns>The tile</returns>
        public static Tile NextTile(Direction d, Position p) {
            switch (d) {
                case Direction.Up:
                    if (p.Tile.Y - 1 < 0) {
                        return Grid.TileGrid[p.Tile.X, p.Tile.Y];
                    } else {
                        return Grid.TileGrid[p.Tile.X, p.Tile.Y - 1];
                    }
                case Direction.Down:
                    if (p.Tile.Y + 1 >= Grid.Height) {
                        return Grid.TileGrid[p.Tile.X, p.Tile.Y];
                    } else {
                        return Grid.TileGrid[p.Tile.X, p.Tile.Y + 1];
                    }
                case Direction.Left:
                    // Special case : the tunnel
                    if (p.Tile.X == 0) {
                        return Grid.TileGrid[Grid.Width - 1, p.Tile.Y];
                    } else {
                        return Grid.TileGrid[p.Tile.X - 1, p.Tile.Y];
                    }
                case Direction.Right:
                    // Special case : the tunnel
                    if (p.Tile.X + 1 >= Grid.Width) {
                        return Grid.TileGrid[0, p.Tile.Y];
                    } else {
                        return Grid.TileGrid[p.Tile.X + 1, p.Tile.Y];
                    }
                default:
                    throw new ArgumentException();
            }
        }

        /// <summary>
        /// Returns whether the specified tile is a junction.
        /// </summary>
        /// <param name="tile">Tile to check</param>
        /// <returns>whether the specified tile is a junction</returns>
        bool IsAJunction(Point tile) {
            if (NextTile(direction_).Type == TileTypes.Open) {
                // If the path ahead is open, we're at a junction if it's also open
                // to one of our sides
                if (direction_ == Direction.Up || direction_ == Direction.Down) {
                    return ((NextTile(Direction.Left).IsOpen) ||
                            (NextTile(Direction.Right).IsOpen));
                } else {
                    return ((NextTile(Direction.Up).IsOpen) ||
                            (NextTile(Direction.Down).IsOpen));
                }
            }
                // If the path ahead is blocked, then we're definitely at a junction, because there are no dead-ends
            else {
                return true;
            }
        }

        /// <summary>
        /// Assumes spritebatch.begin() was called
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Draw(GameTime gameTime, Vector2 boardPosition) {
            // Ghosts have a two-frame animation; we use a bool to toggle between
            // the two. We divide DateTime.Now.Milliseconds by the period.
            if (((DateTime.Now.Millisecond / 125) % 2) == 0 ^ wiggle_) {
                wiggle_ = !wiggle_;
            }
            Vector2 position;
            position.X = boardPosition.X + (position_.Tile.X * 16) + (position_.DeltaPixel.X) - 6;
            position.Y = boardPosition.Y + (position_.Tile.Y * 16) + (position_.DeltaPixel.Y) - 6;
            Vector2 eyesBasePosition;
            eyesBasePosition.X = position.X + 4;
            eyesBasePosition.Y = position.Y + 6;
            Vector2 eyesCenterPosition = new Vector2();
            switch (direction_) {
                case Direction.Up:
                    eyesBasePosition.Y -= 2;
                    eyesCenterPosition.X = eyesBasePosition.X + 2;
                    eyesCenterPosition.Y = eyesBasePosition.Y;
                    break;
                case Direction.Down:
                    eyesBasePosition.Y += 2;
                    eyesCenterPosition.X = eyesBasePosition.X + 2;
                    eyesCenterPosition.Y = eyesBasePosition.Y + 6;
                    break;
                case Direction.Left:
                    eyesBasePosition.X -= 2;
                    eyesCenterPosition.X = eyesBasePosition.X;
                    eyesCenterPosition.Y = eyesBasePosition.Y + 3;
                    break;
                case Direction.Right:
                    eyesBasePosition.X += 2;
                    eyesCenterPosition.X = eyesBasePosition.X + 4;
                    eyesCenterPosition.Y = eyesBasePosition.Y + 3;
                    break;
            }
            if (State == GhostState.Blue) {
                if (((DateTime.Now - timeInCurrentState).Seconds < 0.5 * Constants.BlueTime())) {
                    RenderSprite(wiggle_ ? ghostBase1_ : ghostBase2_, null, boardPosition, position, Color.Blue);
                    RenderSprite(ghostChased_, null, boardPosition, position, Color.White);
                } else {
                    bool flash = (DateTime.Now.Second + DateTime.Now.Millisecond / 200) % 2 == 0;
                    RenderSprite(wiggle_ ? ghostBase1_ : ghostBase2_, null, boardPosition, position, flash ? Color.Blue : Color.White);
                    RenderSprite(ghostChased_, null, boardPosition, position, flash ? Color.White : Color.Blue);
                }
            } else if (State == GhostState.Dead) {
                RenderSprite(eyesBase_, null, boardPosition, eyesBasePosition, Color.White);
                RenderSprite(eyesCenter_, null, boardPosition, eyesCenterPosition, Color.White);
            } else {
                RenderSprite(wiggle_ ? ghostBase1_ : ghostBase2_, null, boardPosition, position, colorBase_);
                RenderSprite(eyesBase_, null, boardPosition, eyesBasePosition, Color.White);
                RenderSprite(eyesCenter_, null, boardPosition, eyesCenterPosition, Color.White);
            }

        }

        /// <summary>
        /// Allows rendering across the tunnel, which is tricky.
        /// </summary>
        /// <param name="spriteSheet">Source texture</param>
        /// <param name="rectangle">Portion of the source to render. Pass null for rendering the whole texture.</param>
        /// <param name="boardPosition">Top-left pixel of the board in the screen</param>
        /// <param name="position">Where to render the texture.</param>
        void RenderSprite(Texture2D spriteSheet, Rectangle? rectangle, Vector2 boardPosition, Vector2 position, Color color) {

            Rectangle rect = rectangle == null ? new Rectangle(0, 0, spriteSheet.Width, spriteSheet.Height) :
                                                rectangle.Value;
            int textureWidth = rectangle == null ? spriteSheet.Width : rectangle.Value.Width;
            int textureHeight = rectangle == null ? spriteSheet.Height : rectangle.Value.Height;

            // What happens when we are crossing to the other end by the tunnel?
            // We detect if part of the sprite is rendered outside of the board.
            // First, to the left.
            if (position.X < boardPosition.X) {
                int deltaPixel = (int)(boardPosition.X - position.X); // Number of pixels off the board
                var leftPortion = new Rectangle(rect.X + deltaPixel, rect.Y, textureWidth - deltaPixel, textureHeight);
                var leftPortionPosition = new Vector2(boardPosition.X, position.Y);
                var rightPortion = new Rectangle(rect.X, rect.Y, deltaPixel, textureHeight);
                var rightPortionPosition = new Vector2(boardPosition.X + (16 * 28) - deltaPixel, position.Y);
                spriteBatch_.Draw(spriteSheet, leftPortionPosition, leftPortion, color);
                spriteBatch_.Draw(spriteSheet, rightPortionPosition, rightPortion, color);
            }
                // Next, to the right
            else if (position.X > (boardPosition.X + (16 * 28) - textureWidth)) {
                int deltaPixel = (int)((position.X + textureWidth) - (boardPosition.X + (16 * 28))); // Number of pixels off the board
                var leftPortion = new Rectangle(rect.X + textureWidth - deltaPixel, rect.Y, deltaPixel, textureHeight);
                var leftPortionPosition = new Vector2(boardPosition.X, position.Y);
                var rightPortion = new Rectangle(rect.X, rect.Y, textureWidth - deltaPixel, textureHeight);
                var rightPortionPosition = new Vector2(position.X, position.Y);
                spriteBatch_.Draw(spriteSheet, leftPortionPosition, leftPortion, color);
                spriteBatch_.Draw(spriteSheet, rightPortionPosition, rightPortion, color);
            }
                // Draw normally - in one piece
            else {
                spriteBatch_.Draw(spriteSheet, position, rect, color);
            }
        }



        // DRAWING
        SpriteBatch spriteBatch_;
        Texture2D ghostBase1_;
        Texture2D ghostBase2_;
        Texture2D ghostChased_;
        Texture2D eyesBase_;
        Texture2D eyesCenter_;
        Color colorBase_;
        bool wiggle_;

        // LOGIC
        Ghost blinky_; // Only Inky needs this for his AI
        Ghosts identity_;
        Direction direction_;
        Position position_;
        GhostState state_;
        GhostState previousState_;
        List<Point> scatterTiles_;
        Point lastJunction_;
        DateTime timeInCurrentState;
        Player player_;
        int updatesPerPixel_;
        bool scheduleStateEval_;
        int scatterModesLeft_;
        int initialJumps_;
        int previousNumCrumps_;
        int updateCount_;

        public GhostState State { get { return state_; } set { state_ = value; timeInCurrentState = DateTime.Now; } }
        public Position Position { get { return position_; } }
        public Ghosts Identity { get { return identity_; } }
    }
}