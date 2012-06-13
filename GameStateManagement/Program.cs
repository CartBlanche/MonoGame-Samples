#region Using Statements
using System;
#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#elif IPHONE
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using Microsoft.Xna;
using Microsoft.Xna.Framework.Media;
#else
#endif
#endregion

namespace GameStateManagement
{
    #region Entry Point
#if MONOMAC
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

		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
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
