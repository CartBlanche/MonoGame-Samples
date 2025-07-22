using System;
using System.Collections.Generic;
using System.Linq;

using Foundation;
using UIKit;

namespace OrientationSample.iOS
{
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
		private OrientationSampleGame game;

		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new OrientationSampleGame();
			game.Run();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
}
