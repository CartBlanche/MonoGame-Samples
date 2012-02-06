#region Using Statements
using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Microsoft.Xna;
using Microsoft.Xna.Framework.Media;
#endregion

namespace Microsoft.Xna.Samples.MultiTouch
{
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    {
        Game1 game;
        public override void FinishedLaunching(UIApplication app)
        {
            // Fun begins..
            game = new Game1();
            game.Run();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "AppDelegate");
        }
    }    
}
