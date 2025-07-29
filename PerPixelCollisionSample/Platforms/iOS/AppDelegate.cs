using Foundation;
using UIKit;

namespace PerPixelCollision.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private PerPixelCollisionGame game;
        public override void FinishedLaunching(UIApplication app)
        {
            game = new PerPixelCollisionGame();
            game.Run();
        }
    }
}
