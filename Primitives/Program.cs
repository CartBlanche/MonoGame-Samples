using System;

namespace PrimitivesSample
{
	static class Program
	{


		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			AppKit.NSApplication.Init ();
			
			using (var p = new Foundation.NSAutoreleasePool ()) {
				AppKit.NSApplication.SharedApplication.Delegate = new AppDelegate ();
				AppKit.NSApplication.Main (args);
			}
		}

	
		class AppDelegate : AppKit.NSApplicationDelegate
		{

            PrimitivesSampleGame game;
			public override void DidFinishLaunching (NSNotification notification)
			{
				game = new PrimitivesSampleGame();
				game.Run();
			}
		
			public override bool ApplicationShouldTerminateAfterLastWindowClosed (AppKit.NSApplication sender)
			{
				return true;
			}
		}
	}
}



