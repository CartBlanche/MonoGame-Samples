using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace PeerToPeer.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game game;

        public override UIWindow Window { get; set; }

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            game = new PeerToPeerGame();
            game.Run();
            return true;
        }
    }
}
