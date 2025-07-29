using Foundation;
using UIKit;
using Microsoft.Xna.Framework;
using ChaseAndEvade;

namespace ChaseAndEvade.iOS
{
    [Register("AppDelegate")]
    public class AppDelegate : UIApplicationDelegate
    {
        private Game game;

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            game = new ChaseAndEvadeGame();
            game.Run();
            return true;
        }
    }
}
