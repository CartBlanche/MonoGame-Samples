using Foundation;
using UIKit;

namespace Blackjack.iOS
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private BlackjackGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new BlackjackGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}