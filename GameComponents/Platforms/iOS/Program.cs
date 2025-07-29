using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace GameComponents.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
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
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
