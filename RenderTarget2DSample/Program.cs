#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace RenderTarget2DSample
{
	#region Entry Point
#if MONOMAC
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main (string[] args)
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
			game = new Game1 ();
			game.Run ();

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
        Game1 game;
        public override void FinishedLaunching(UIApplication app)
        {
            // Fun begins..
            game = new Game1();
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
#endif
    #endregion
}

