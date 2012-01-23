using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Microsoft.Xna;

namespace PerPixelCollision
{
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
        PerPixelCollisionGame game;
		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new PerPixelCollisionGame();
			game.Run ();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
}
