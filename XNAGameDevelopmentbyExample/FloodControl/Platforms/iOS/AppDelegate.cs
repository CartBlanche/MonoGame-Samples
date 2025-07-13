using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace Flood_Control
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        Game1 game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new Game1();
            game.Run();
            return true;
        }
    }

    public class Application
    {
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
