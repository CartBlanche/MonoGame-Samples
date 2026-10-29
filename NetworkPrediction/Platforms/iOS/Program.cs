using Foundation;
using UIKit;

namespace NetworkPrediction.iOS
{
    [Register("AppDelegate")]
    internal class Program : UIApplicationDelegate
    {
        private static NetworkPredictionGame game;

        internal static void RunGame()
        {
            game = new NetworkPredictionGame();
            game.Run();
        }

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
