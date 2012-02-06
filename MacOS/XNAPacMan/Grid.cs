using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;

namespace XNAPacMan {


    /// <summary>
    /// By who the tile can be traversed
    /// </summary>
    public enum TileTypes {
        /// <summary>
        /// Everyone can go through
        /// </summary>
        Open,
        /// <summary>
        /// No one can go through
        /// </summary>
        Closed,
        /// <summary>
        /// Under special circumstances ghosts can go there
        /// </summary>
        Home
    }

    /// <summary>
    /// Represents the maze in terms of tiles. Initializes itself from txt file.
    /// </summary>
    public static class Grid {

        /// <summary>
        /// Creates a new Grid object
        /// </summary>
        static Grid() {
            initializeFromFile();
        }


        /// <summary>
        /// Reads Grid.txt to get an object grid from the numbers.
        /// </summary>
        static void initializeFromFile() {
            TextReader tr = new StreamReader("Content/Grid.txt");
            string line = tr.ReadLine();
            int lineIndex = 0;
            int charIndex = 0;

            while (line != null) {
                foreach (char c in line) {
                    if (c == '1') {
                        TileGrid[charIndex, lineIndex] = new Tile(TileTypes.Open, true, false, new Point(charIndex, lineIndex));
                    }
                    else if (c == '0') {
                        TileGrid[charIndex, lineIndex]= new Tile(TileTypes.Closed, false, false, new Point(charIndex, lineIndex));
                    }
                    else if (c == '2') {
                        TileGrid[charIndex, lineIndex] = new Tile(TileTypes.Home, false, false, new Point(charIndex, lineIndex));
                    }
                    else if (c == '3') {
                        TileGrid[charIndex, lineIndex]= new Tile(TileTypes.Open, true, true, new Point(charIndex, lineIndex));
                    }
                    if (c != ' ') {
                        charIndex++;
                    }
                }
                charIndex = 0;
                lineIndex++;
                line = tr.ReadLine();
            }

            tr.Close();
			
			// Now, actually a few open tiles do not contain a crump; such as 
			// the tunnels and around the ghosts' home. 
			for (int i = 0; i < 28; i++) {
				if (i != 6 && i != 21) {
					TileGrid[i, 14].HasCrump = false;
				}
			}
			
			for (int j = 11; j < 20; j++) {
				TileGrid[9, j].HasCrump = false;
				TileGrid[18, j].HasCrump = false;
			}
			
			for (int i = 10; i < 18; i++) {
				TileGrid[i, 11].HasCrump = false;
				TileGrid[i, 17].HasCrump = false;
			}
			
			TileGrid[12, 9].HasCrump = false;
			TileGrid[15, 9].HasCrump = false;
			TileGrid[12, 10].HasCrump = false;
			TileGrid[15, 10].HasCrump = false;
            TileGrid[13, 23].HasCrump = false;
            TileGrid[14, 23].HasCrump = false;
        }

        

        static Tile[,] tileGrid_ = new Tile[28, 31];

        public static Tile[,] TileGrid {
            get { return tileGrid_; }
        }

        public static int Width {
            get { return 28; }
        }
        public static int Height {
            get { return 31; }
        }
        public static int NumCrumps { get; set; }

        public static void Reset() {
            NumCrumps = 0;
            initializeFromFile();
        }
    }

    /// <summary>
    /// A square of the maze
    /// </summary>
    public struct Tile {

        TileTypes type_;
        /// <summary>
        /// The type of the tile
        /// </summary>
        public TileTypes Type {
            get { return type_; }
            set { type_ = value; }
        }

        bool hasCrump_;
        /// <summary>
        /// Whether the tile has a crump
        /// </summary>
        public bool HasCrump {
            get { return hasCrump_; }
            set {
                if (value != hasCrump_) {
                    Grid.NumCrumps += value ? 1 : -1;
                } 
                hasCrump_ = value;
            }
        }

        bool hasPowerPill_;
        /// <summary>
        /// Whether the tile has a power pill
        /// </summary>
        public bool HasPowerPill {
            get { return hasPowerPill_; }
            set { hasPowerPill_ = value; }
        }

        public bool IsOpen {
            get { return type_ == TileTypes.Open; }
        }

        Point position_;
        public Point ToPoint { get { return position_; } }

        /// <summary>
        /// Sets the different attributes
        /// </summary>
        /// <param name="type">The type of tile</param>
        /// <param name="hasCrump">Whether the tile has a crump</param>
        /// <param name="hasPowerPill">Whether the tile has a power pill</param>
        public Tile(TileTypes type, bool hasCrump, bool hasPowerPill, Point position) {
            type_ = type;
            hasCrump_ = hasCrump;
            if (hasCrump) {
                Grid.NumCrumps++;
            }
            hasPowerPill_ = hasPowerPill;
            position_ = position;
        }
    }


}
