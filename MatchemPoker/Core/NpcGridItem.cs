using Microsoft.Xna.Framework;

namespace MatchemPoker
{

    /// <summary>
    /// Each grid item is defined with this class. Holds all of the information related to it
    /// </summary>
    public class NpcGridItem
    {
        public GridFlags Flags { get; set; }            // Flags
        public int Index { get; set; } 				    // index to be rendered. 

        public Point GridPos { get; set; }

        public float Wobble { get; set; }               // "Wobbe" spring. This makes the card to wobble when dropped etc. 
        public float Wobbleinc { get; set; }

        public float GenCount { get; set; }             // Used according the levelstate.

        public float Dropping { get; set; }             // If below zero, the card is not dropping. Otherwise indicates the current dropping speed
        public float Destroying { get; set; } 		    // If below zero, the card is not destroying. 

        public float PosOffSetX { get; set; }           // Offset for rendering
        public float PosOffSetY { get; set; }

        public Vector2 LPos { get; set; } 				// original (and main) position of this grid item

        public float AnimationCount { get; set; }       // Usage changes according levelstate.
    };
}

