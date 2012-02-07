using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace OrientationSample
{
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
		private OrientationSample game;

		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new OrientationSample();
			game.Run();
		}

		static void Main (string [] args)
		{
			UIApplication.Main (args,null,"AppDelegate");
		}
	}
}
