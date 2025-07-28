using Foundation;
using UIKit;

namespace TouchGesture.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var game = new TouchGestureGame();
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
