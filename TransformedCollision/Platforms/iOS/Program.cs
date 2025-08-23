using System;
using UIKit;
using Foundation;
using TransformedCollision;

namespace TransformedCollision.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        UIWindow window;
        TransformedCollisionGame game;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            window = new UIWindow(UIScreen.MainScreen.Bounds);
            game = new TransformedCollisionGame();
            window.RootViewController = game.Services.GetService(typeof(UIViewController)) as UIViewController;
            window.MakeKeyAndVisible();
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
