using Foundation;
using UIKit;

namespace Audio3D.iOS
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private Audio3DGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new Audio3DGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}