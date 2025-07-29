using Foundation;
using UIKit;
using SpriteFontSample.Core;

namespace SpriteFontSample.iOS
{
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    {
        private SpriteFontGame game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new SpriteFontGame();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
