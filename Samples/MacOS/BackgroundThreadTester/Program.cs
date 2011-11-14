using MonoMac.AppKit;
using MonoMac.Foundation;


namespace BackgroundThreadTester
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
		Game1 game; 
		
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{			
			using (game = new Game1()) {
				game.Run ();
			}
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
		
		public override NSApplicationTerminateReply ApplicationShouldTerminate (NSApplication sender)
		{


				
				NSAlert alert = NSAlert.WithMessage("Warning", "Yes", "No", null, "Do you really want to close?");
				
				var button = alert.RunModal();
				
				if ( button == 0 )
				{
					return NSApplicationTerminateReply.Cancel;		
				}//if
				else
				{
					return NSApplicationTerminateReply.Now;
				}//else

		}
		
		




	}
	
	
	

}

