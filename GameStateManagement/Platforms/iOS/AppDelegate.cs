using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace GameStateManagement.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private GameStateManagementGame game;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            game = new GameStateManagementGame();
            game.Run();
            return true;
        }
    }
}
