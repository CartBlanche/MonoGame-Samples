using Foundation;
using UIKit;

namespace Asteroid_Belt_Assault
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        private Game1 game;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new Game1();
            game.Run();

            return true;
        }

        public override void OnActivated(UIApplication application)
        {
            // Handle when your app becomes active
        }

        public override void OnResignActivation(UIApplication application)
        {
            // Handle when your app moves from active to inactive state
        }

        public override void DidEnterBackground(UIApplication application)
        {
            // Handle when your app moves to background
        }

        public override void WillEnterForeground(UIApplication application)
        {
            // Handle when your app transitions from background to foreground
        }

        public override void WillTerminate(UIApplication application)
        {
            // Handle when your app is about to terminate
        }
    }
}
