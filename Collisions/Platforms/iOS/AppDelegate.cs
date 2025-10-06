using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace CollisionSample
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game _game;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            _game = new CollisionGame();
            _game.Run();
            return true;
        }
    }
}
