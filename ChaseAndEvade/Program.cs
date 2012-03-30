using System;
using System.Collections.Generic;
using System.Linq;
#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace ChaseAndEvade
{
#if MONOMAC
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
#elif IPHONE
	[Register("AppDelegate")]
	class Program : UIApplicationDelegate
	{
		ChaseAndEvadeGame game;
		public override void FinishedLaunching(UIApplication app)
		{
			// Fun begins..
			game = new ChaseAndEvadeGame();
			game.Run();
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			UIApplication.Main(args, null, "AppDelegate");
		}
	}	
#else
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	static class Program
	{
		static void Main()
		{
			using (ChaseAndEvadeGame game = new ChaseAndEvadeGame())
			{
				game.Run();
			}
		}
	}	
#endif
}
