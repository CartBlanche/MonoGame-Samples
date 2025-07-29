using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace RobotRampage.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private RobotRampageGame game;

        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            game = new RobotRampageGame();
            game.Run();
            return true;
        }
    }
}
