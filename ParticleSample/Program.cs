
using System;
using System.Collections.Generic;
using System.Linq;

#if IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace ParticleSample
{
	#region Entry Point
#if IPHONE
	[Register("AppDelegate")]
	class Program : UIApplicationDelegate
	{
		ParticleSampleGame game;
		public override void FinishedLaunching(UIApplication app)
		{
			// Fun begins..
			game = new ParticleSampleGame();
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
#elif __MACOS__
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
			AppKit.NSApplication.Init ();
			
			using (var p = new Foundation.NSAutoreleasePool ()) {
				AppKit.NSApplication.SharedApplication.Delegate = new AppDelegate();
				AppKit.NSApplication.Main(args);
			}
		}
	}
	
	class AppDelegate : AppKit.NSApplicationDelegate
	{
		ParticleSampleGame game;
		public override void DidFinishLaunching (NSNotification notification)
		{
	
			game = new ParticleSampleGame();
			game.Run ();
	
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (AppKit.NSApplication sender)
		{
			return true;
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
			using (ParticleSampleGame game = new ParticleSampleGame())
			{
				game.Run();
			}
		}
	}	
#endif
	#endregion
}

