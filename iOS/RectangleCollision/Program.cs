using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Microsoft.Xna;

namespace RectangleCollision
{
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
        RectangleCollisionGame game;
		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new RectangleCollisionGame();
			game.Run();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
}
