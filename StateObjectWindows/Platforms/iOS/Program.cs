using Foundation;
using UIKit;

namespace StateObject.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            var game = new StateObjectGame();
            game.Run();
            return true;
        }
    }

    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
