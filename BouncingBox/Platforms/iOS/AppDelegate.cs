using Foundation;
using UIKit;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Samples.BouncingBox;

namespace Microsoft.Xna.Samples.BouncingBox
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game1 _game;

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            _game = new Game1();
            _game.Run();
            return true;
        }
    }
}
