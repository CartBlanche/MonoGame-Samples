using UIKit;

namespace FarseerPhysics.iOS
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
        public override UIWindow Window { get; set; }
        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            var game = new FarseerPhysicsGame();
            game.Run();
            return true;
        }
    }
}
