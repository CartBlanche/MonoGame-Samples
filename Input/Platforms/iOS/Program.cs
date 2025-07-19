using Foundation;
using UIKit;

namespace MonoGame.Samples.Input.iOS
{
    [Register("AppDelegate")]
    class Program : UIApplicationDelegate
    {
        private Game1 game;

        public override void FinishedLaunching(UIApplication app)
        {
            game = new Game1();
            game.Run();
        }

        static void Main(string[] args)
        {
            UIApplication.Main(args, null, "AppDelegate");
        }
    }
}
