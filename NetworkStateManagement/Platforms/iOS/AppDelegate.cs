using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace NetworkStateManagement.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game game;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            game = new NetworkStateManagementGame();
            game.Run();
            return true;
        }
    }
}
