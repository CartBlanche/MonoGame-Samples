using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace PrimitivesSample.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        Game? game;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            game = new PrimitivesSampleGame();
            game.Run();
            return true;
        }
    }
}
