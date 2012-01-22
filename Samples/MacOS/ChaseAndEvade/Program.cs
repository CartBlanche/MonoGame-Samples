using MonoMac.AppKit;
using MonoMac.Foundation;

namespace ChaseAndEvade
{
	class Program
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {

				NSApplication.SharedApplication.Delegate = new AppDelegate ();

				// Set our Application Icon
				NSImage appIcon = NSImage.ImageNamed ("GameThumbnail.png");
				NSApplication.SharedApplication.ApplicationIconImage = appIcon;

				NSApplication.Main (args);
			}
		}
	}

	class AppDelegate : NSApplicationDelegate
	{
        ChaseAndEvadeGame game;
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new ChaseAndEvadeGame();
		    game.Run();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}	
}
