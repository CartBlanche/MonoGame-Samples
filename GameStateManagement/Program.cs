#region Using Statements
using System;
#if __MACOS__
using AppKit;
using Foundation;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Microsoft.Xna;
using Microsoft.Xna.Framework.Media;
#elif __MACOS__

#endif
#endregion

namespace GameStateManagement
{
    #region Entry Point
#if __MACOS__
	class Program
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();

			using (var p = new NSAutoreleasePool ()) {
				NSApplication.SharedApplication.Delegate = new AppDelegate ();			

				NSApplication.Main (args);
			}
		}
	}

	class AppDelegate : NSApplicationDelegate
	{
		private GameStateManagementGame game;

		public override void DidFinishLaunching (NSNotification notification)
		{
			game = new GameStateManagementGame ();
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
        GameStateManagementGame game;
        public override void FinishedLaunching(UIApplication app)
        {
            // Fun begins..
            game = new GameStateManagementGame();
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
		public override void DidFinishLaunching (NSNotification notification)
		{
			var game = new GameStateManagementGame();
			game.Run();
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
            using (GameStateManagementGame game = new GameStateManagementGame())
            {
                game.Run();
            }
        }
    }    
#endif
    #endregion
}
