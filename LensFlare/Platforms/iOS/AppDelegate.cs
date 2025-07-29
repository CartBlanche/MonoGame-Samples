using Foundation;
using UIKit;

namespace LensFlare
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private static LensFlareGame game;

        internal static void RunGame()
        {
            game = new LensFlareGame();
            game.Run();
        }

        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }
    }
}
