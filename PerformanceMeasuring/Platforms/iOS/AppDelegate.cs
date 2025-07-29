using Foundation;
using UIKit;
using Microsoft.Xna.Framework;

namespace PerformanceMeasuring.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game _game;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            _game = new PerformanceMeasuringGame();
            _game.Run();
            return true;
        }
    }
}
