using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Microsoft.Xna;

namespace PrimitivesSample
{
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
        PrimitivesSampleGame game;
		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new PrimitivesSampleGame();
			game.Run ();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
}
