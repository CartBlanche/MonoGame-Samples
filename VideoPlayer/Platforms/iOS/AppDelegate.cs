using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace VideoPlayer
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private VideoPlayerGame game;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new VideoPlayerGame();
            game.Run();
            return true;
        }
    }
}
