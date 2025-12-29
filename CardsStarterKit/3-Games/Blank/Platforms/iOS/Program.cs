using Foundation;
using UIKit;

namespace Blank.iOS
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private BlankGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new BlankGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}