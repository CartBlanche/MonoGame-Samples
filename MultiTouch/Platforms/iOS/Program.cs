// ...existing code from Program.iOS.cs will be moved here...
using System;
using Foundation;
using UIKit;

namespace Microsoft.Xna.Samples.MultiTouch
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game1 game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new Game1();
            game.Run();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
