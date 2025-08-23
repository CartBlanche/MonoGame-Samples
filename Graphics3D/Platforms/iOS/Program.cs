using Foundation;
using UIKit;

namespace Graphics3DSample
{
    [Register("AppDelegate")]
    public class Program : UIApplicationDelegate
    {
        private static Graphics3DSampleGame game;

        internal static void RunGame()
        {
            game = new Graphics3DSampleGame();
            game.Run();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(Program));
        }

        public override void FinishedLaunching(UIApplication app)
        {
            RunGame();
        }
    }
}
