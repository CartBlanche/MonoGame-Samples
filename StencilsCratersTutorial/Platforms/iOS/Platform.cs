using Foundation;
using UIKit;

namespace StencilCraters.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }
        StencilCratersGame game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new StencilCratersGame();
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
