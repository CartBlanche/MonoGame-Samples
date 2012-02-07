using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace CatapultGame
{
	[Register ("AppDelegate")]
	class Program : UIApplicationDelegate 
	{
		private CatapultGame game;

		public override void FinishedLaunching (UIApplication app)
		{
			// Fun begins..
			game = new CatapultGame();
			game.Run();
		}
		
		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// if you want to use a different Application Delegate class from "AppDelegate"
			// you can specify it here.
			UIApplication.Main (args, null, "AppDelegate");
		}
	}
}
