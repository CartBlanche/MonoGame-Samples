using System;
using System.Collections.Generic;
using System.Linq;

#if MONOMAC
using MonoMac.AppKit;
using MonoMac.Foundation;
#endif

namespace SpriteEffects
{
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
		SpriteEffectsGame game;
		public override void FinishedLaunching (MonoMac.Foundation.NSObject notification)
		{
			game = new SpriteEffectsGame();
			game.Run();
		}
		
		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}
	}		
#else
    static class Program
    {
        private static SpriteEffectsGame game;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            game = new SpriteEffectsGame();
            game.Run();
        }
    }
#endif
}

