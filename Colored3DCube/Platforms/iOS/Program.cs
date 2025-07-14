using Foundation;
using UIKit;

namespace Colored3DCube.iOS
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
        Game1 game;
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new Colored3DCube.Game1();
            game.Run();
            return true;
        }
    }
}
