#if __MACOS__
using AppKit;
using Foundation;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace MonoGame.Samples.VideoPlayer
{
#if __MACOS__
	class Program
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate ();

				// Set our Application Icon
				NSImage appIcon = NSImage.ImageNamed ("monogameicon.png");
				NSApplication.SharedApplication.ApplicationIconImage = appIcon;
				
				NSApplication.Main (args);
			}
		}
	}

	class AppDelegate : NSApplicationDelegate
	{
		private Game1 game;

		public override void DidFinishLaunching (NSNotification notification)
		{
			game = new Game1();
			game.Run();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}	
#elif IPHONE
	[Register("AppDelegate")]
	class Program : UIApplicationDelegate
	{
		private Game1 game;

		public override void FinishedLaunching (UIApplication app)
		{
			game = new Game1 ();
			game.Run ();
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			UIApplication.Main (args, null, "AppDelegate");
		}
	}
#endif
}
