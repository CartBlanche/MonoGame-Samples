using Foundation;
using Microsoft.Xna.Framework;
using UIKit;

namespace ShatterEffect.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game _game;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            _game = new ShatterEffectGame();
            _game.Run();
            return true;
        }
    }
}
