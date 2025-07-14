using Foundation;
using UIKit;
using Gemstone_Hunter;

namespace GemstoneHunter.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var game = new Game1();
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
