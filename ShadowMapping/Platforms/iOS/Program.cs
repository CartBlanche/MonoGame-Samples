using System;
using Microsoft.Xna.Framework;
using UIKit;

namespace ShadowMapping.iOS
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }

    public class AppDelegate : UIApplicationDelegate
    {
        private ShadowMappingGame _game;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            _game = new ShadowMappingGame();
            _game.Run();
            return true;
        }
    }
}
