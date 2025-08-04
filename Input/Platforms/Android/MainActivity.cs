using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace InputSample.Android
{
	[Activity (Label = "InputSamples.Android"
	           , MainLauncher = true
	           ,Icon = "@drawable/icon"
	           , Theme = "@style/Theme.Splash"
	           ,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden
	           )]
	public class MainActivity : AndroidGameActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
            var game = new InputGame();
            game.Run();
		}
	}
}


