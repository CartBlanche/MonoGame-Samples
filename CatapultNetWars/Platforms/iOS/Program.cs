using Foundation;
using UIKit;

namespace CatapultGame.iOS
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private CatapultGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new CatapultGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}