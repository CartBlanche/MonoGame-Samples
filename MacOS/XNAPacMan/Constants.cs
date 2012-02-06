using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XNAPacMan {
    /// <summary>
    /// This class provides global access to important game constants; what fruits appear on what levels,
    /// relative speeds of everything, timers, etc. Centralizing this data here makes it easy to change
    /// the game settings. It also relieves the data-heavy ghost and gameloop classes from a LOT of definitions.
    /// </summary>
    static class Constants {

        // Dispersion tiles for each ghost
        public static readonly List<Point> scatterTilesBlinky =  new List<Point> {   new Point(21, 1),   new Point(26, 1),
                                                                            new Point(26, 5),   new Point(21, 5)    
        };
        public static readonly List<Point> scatterTilesPinky = new List<Point> {   new Point(1, 1),    new Point(6, 1),
                                                                            new Point(6, 5),    new Point(1, 5)     
        };
        public static readonly List<Point> scatterTilesClyde = new List<Point> {   new Point(6, 23),   new Point(9, 23),
                                                                            new Point(9, 26),   new Point(12, 26),  
                                                                            new Point(12, 29),  new Point(1, 29),
                                                                            new Point(1, 26),   new Point(6, 26)    
        };
        public static readonly List<Point> scatterTilesInky = new List<Point> {   new Point(18, 23),  new Point(21, 23),
                                                                            new Point(21, 26),  new Point(26, 26),
                                                                            new Point(26, 29),  new Point(15, 29),
                                                                            new Point(15, 26),  new Point(18, 26)
        };

        public static List<Point> scatterTiles(Ghosts identity) {
            switch (identity) {
                case Ghosts.Blinky:
                    return scatterTilesBlinky;
                case Ghosts.Clyde:
                    return scatterTilesClyde;
                case Ghosts.Inky:
                    return scatterTilesInky;
                case Ghosts.Pinky:
                    return scatterTilesPinky;
                default:
                    throw new ArgumentException();
            }
        }

        public static readonly Position startPositionBlinky = new Position { Tile = new Point(13, 11), DeltaPixel = new Point(8, 0) };
        public static readonly Position startPositionPinky = new Position { Tile = new Point(13, 14), DeltaPixel = new Point(8, 8) };
        public static readonly Position startPositionInky = new Position { Tile = new Point(11, 13), DeltaPixel = new Point(8, 8) };
        public static readonly Position startPositionClyde = new Position { Tile = new Point(15, 13), DeltaPixel = new Point(8, 8) };
        public static Position startPosition(Ghosts identity) {
            switch (identity) {
                case Ghosts.Blinky:
                    return startPositionBlinky;
                case Ghosts.Pinky:
                    return startPositionPinky;
                case Ghosts.Clyde:
                    return startPositionClyde;
                case Ghosts.Inky:
                    return startPositionInky;
                default:
                    throw new ArgumentException();

            }
        }

        public static int Level = 0;
        public static int InitialJumps(Ghosts ghost, bool newLevel) {
            if (newLevel) {
                switch (ghost) {
                    case Ghosts.Inky:
                        return (int)MathHelper.Clamp((20 - Level) / 2, 0, 10);
                    case Ghosts.Clyde:
                        return InitialJumps(Ghosts.Inky, true) + 2;
                    default:
                        return 0;
                }
            }
            else {
                switch (ghost) {
                    case Ghosts.Inky:
                        return 1;
                    case Ghosts.Clyde:
                        return 2;
                    default:
                        return 0;
                }
            }
        }

        private static int[] cruiseElroyTimers_ = { 20, 30, 40, 40, 40, 50, 50, 50, 60, 60 };
        public static int CruiseElroyTimer() {
            if (Level >= 10) {
                return cruiseElroyTimers_[9];
            }
            else {
                return cruiseElroyTimers_[Level - 1];
            }
        }

        public static Color colors(Ghosts identity) {
            switch (identity) {
                case Ghosts.Blinky:
                    return Color.Red;
                case Ghosts.Clyde:
                    return Color.Orange;
                case Ghosts.Inky:
                    return Color.LightSkyBlue;
                case Ghosts.Pinky:
                    return Color.LightPink;
                default:
                    throw new ArgumentException();
            }
        }

        private static int[] blueTimes_ = { 6, 6, 4, 3, 2, 6, 2, 2, 1, 5, 2, 1, 1, 3, 1, 1, 0, 1, 0, 0, 0 };
        public static int BlueTime() {
            return Level > blueTimes_.Length - 2 ? 0 : blueTimes_[Level - 1];
        }

        private static int[] bonusScores_ = { 100, 300, 500, 700, 700, 1000, 1000, 2000, 2000, 3000, 3000, 5000, 5000, 5000 };
        public static int BonusScores() {
            return Level > bonusScores_.Length - 2 ? 5000 : bonusScores_[Level - 1];
        }

        private static string[] bonusSprites_ = { "Cherry", "Strawberry", "Apple", "Bell", "Orange", "Pear", "Pretzel", "Bell", "Banana", "Key", "Key" };
        public static string BonusSprite() {
            return Level > bonusSprites_.Length - 2 ? "Key" : bonusSprites_[Level - 1];
        }

        private static int[] pacManSpeed_ = { 7, 9, 8, 8, 9 };
        public static int PacManSpeed() {
            if (5 <= Level && Level <= 20) {
                return pacManSpeed_[4];
            }
            else if (5 > Level) {
                return pacManSpeed_[Level - 1];
            }
            else {
                return 10;
            }
        }

        private static int[] ghostSpeed_ = { 13, 11, 12, 12 };
        public static int GhostSpeed() {
            return Level > 4 ? 11 : ghostSpeed_[Level - 1];
        }
    }
}
