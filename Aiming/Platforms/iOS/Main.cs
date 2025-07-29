using Foundation;
using UIKit;

namespace Aiming.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        AimingGame game;
        public override void FinishedLaunching(UIApplication app)
        {
            game = new AimingGame();
            game.Run();
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
