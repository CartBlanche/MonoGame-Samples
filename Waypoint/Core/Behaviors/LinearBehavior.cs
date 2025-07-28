//-----------------------------------------------------------------------------
// LinearBehavior.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------


using System;
#if IPHONE
using Microsoft.Xna.Framework;
#else
using Microsoft.Xna.Framework;
#endif


namespace Waypoint
{
    /// <summary>
    /// This Behavior makes the tank turn instantly and follow a direct 
    /// line to the current waypoint
    /// </summary>
    class LinearBehavior : Behavior
    {

        public LinearBehavior(Tank tank)
            : base(tank)
        {
        }



        /// <summary>
        /// This Update finds the direction vector that goes from a straight 
        /// line directly to the current waypoint
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime)
        {
            // This gives us a vector that points directly from the tank's
            // current location to the waypoint.
            Vector2 direction = -(tank.Location - tank.Waypoints.Peek());
            // This scales the vector to 1, we'll use move Speed and elapsed Time 
            // in the Tank's Update function to find the how far the tank moves
            direction.Normalize();
            tank.Direction = direction;
        }

    }
}
