using Foundation;
using UIKit;

namespace SpriteFontSample.iOS
{
    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private SpriteFontGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new SpriteFontGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
