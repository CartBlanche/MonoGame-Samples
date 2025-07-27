// Entry point for iOS platform
using Foundation;
using UIKit;

namespace StarWarrior.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
        StarWarriorGame game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new StarWarriorGame();
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
