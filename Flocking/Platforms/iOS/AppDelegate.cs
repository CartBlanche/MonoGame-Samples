using Foundation;
using UIKit;

namespace Flocking.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : UIApplicationDelegate
    {
        private FlockingSample game;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new FlockingSample();
            game.Run();

            return true;
        }

        public override void OnActivated(UIApplication application)
        {
            // Handle when your app is activated
        }

        public override void WillEnterForeground(UIApplication application)
        {
            // Handle when your app returns to the foreground
        }

        public override void DidEnterBackground(UIApplication application)
        {
            // Handle when your app goes to background
        }
    }
}
