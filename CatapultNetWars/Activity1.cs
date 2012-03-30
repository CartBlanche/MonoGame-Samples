using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;
using Android.Content.PM;

namespace CatapultWarsNet
{
	
	[Activity (Label = "CatapultWarsNet"
	           , MainLauncher = true
	           ,Icon = "@drawable/icon"
	           , Theme = "@style/Theme.Splash"
	           ,ScreenOrientation=ScreenOrientation.Landscape
	           ,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
	public class Activity1 : AndroidGameActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			CatapultGame.CatapultGame.Activity = this;
			var g = new CatapultGame.CatapultGame();
			SetContentView(g.Window);
			g.Run();
		}
	}
}


