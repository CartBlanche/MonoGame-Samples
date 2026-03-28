using System;

namespace MatchemPoker
{
    /// <summary>
    /// A container class for flag-defines to be used with a Grid item
    /// </summary>
    
    [Flags]
    public enum GridFlags
    {
        GRIDFLAG_SELECTED = 1,				// selected by user. 
        GRIDFLAG_CALCULATION_TEMP = 2,		// temporarely used for calculations.
        GRIDFLAG_MARKED = 4,  				// marked
        GRIDFLAG_HIDDEN = 8,  				// is the tile visible at all 
        GRIDFLAG_PLUS_STRAIGHT = 16,	    // calculation flag for straights
        GRIDFLAG_MINUS_STRAIGHT = 32,       // calculation flag for straights
        GRIDFLAG_SAME_EARTH = 64,           // calculation flag for same earth
        GRIDFLAG_SAME_NUMBER = 128,         // calculation flag for same number

        GRIDFLAG_CHECK_DESTROY_FLAGS = GRIDFLAG_CALCULATION_TEMP | 
                                       GRIDFLAG_PLUS_STRAIGHT |
                                       GRIDFLAG_MINUS_STRAIGHT |
                                       GRIDFLAG_SAME_EARTH | 
                                       GRIDFLAG_SAME_NUMBER,

        GRIDFLAG_ALL = GRIDFLAG_CHECK_DESTROY_FLAGS |
                       GRIDFLAG_SELECTED |
                       GRIDFLAG_MARKED |
                       GRIDFLAG_HIDDEN ,   // All of the flags

        GRIDFLAG_ALL_BUT_SELECTED = GRIDFLAG_ALL ^ GRIDFLAG_SELECTED // all of the flags except the selectedflag
    }

}