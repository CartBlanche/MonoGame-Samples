using MonoMac.AppKit;
using MonoMac.Foundation;

namespace Platformer
{
	class Program
	{
		static void Main (string [] args)
		{
			NSApplication.Init ();
			
			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate();
				NSApplication.Main(args);
			}
		}
	}
	
	class AppDelegate : NSApplicationDelegate
	{
		PlatformerGame game;
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{			
			game = new PlatformerGame();
			game.Run ();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}	
}
