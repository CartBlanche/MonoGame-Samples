using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Microsoft.Xna.Framework;

namespace Gorillas.Android
{
	[Activity (
		Label = "@string/app_name",
		MainLauncher = true,
		Icon = "@drawable/icon",
		AlwaysRetainTaskState = true,
		LaunchMode = LaunchMode.SingleInstance,
		ScreenOrientation = ScreenOrientation.SensorLandscape,
		ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize
	)]
	public class MainActivity : AndroidGameActivity
	{
		private Engine _game;
		private View _view;
		private WifiManager.MulticastLock _multicastLock;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Print the full exception (type, message, inner exceptions, stack) to logcat and the network
			// log. The networking is async, so without this a failed task disappears without a trace.
			AndroidEnvironment.UnhandledExceptionRaiser += (s, e) => LogException ("UnhandledExceptionRaiser", e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, e) => LogException ("AppDomain.UnhandledException", e.ExceptionObject as Exception);
			TaskScheduler.UnobservedTaskException += (s, e) => LogException ("UnobservedTaskException", e.Exception);

			// Android drops broadcast packets unless a multicast lock is held; LAN game discovery needs them.
			var wifi = (WifiManager)GetSystemService (WifiService);
			_multicastLock = wifi?.CreateMulticastLock ("Gorillas.LanDiscovery");
			_multicastLock?.SetReferenceCounted (false);
			_multicastLock?.Acquire ();

			_game = new Engine ();
			_view = _game.Services.GetService (typeof (View)) as View;

			SetContentView (_view);
			_game.Run ();
		}

		private static void LogException (string source, Exception ex)
		{
			var text = $"[{source}] {ex}";
			Log.Error ("Gorillas", text);
			NetLog.Write (text);
		}

		protected override void OnDestroy ()
		{
			if (_multicastLock != null && _multicastLock.IsHeld)
				_multicastLock.Release ();
			base.OnDestroy ();
		}
	}
}
