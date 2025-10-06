using Foundation;
using UIKit;

namespace RenderTarget2DSample.iOS
{

    [Register("AppDelegate")]
    class AppDelegate : UIApplicationDelegate
    {
        private static RenderTarget2DSampleGame game;

        internal static void RunGame()
        {
            game = new RenderTarget2DSampleGame();
            game.Run();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }

        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }
    }
}

