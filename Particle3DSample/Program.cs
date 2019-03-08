#if __MACOS__
using AppKit;
using Foundation;
#endif

namespace Particle3DSample
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
		{
#if __MACOS__
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate ();
				NSApplication.Main (args);
			}
#else
			using (var game = new Particle3DSampleGame()) {
				game.Run();
			}
#endif

		}
	}
#if __MACOS__
	class AppDelegate : NSApplicationDelegate
	{
        Particle3DSampleGame game;
		public override void DidFinishLaunching (NSNotification notification)
		{
			game = new Particle3DSampleGame();
			game.Run();
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}		
#endif
}

