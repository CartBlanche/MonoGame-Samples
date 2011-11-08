using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace MonoGame.Samples.Input
{
	[Activity (Label = "MonoGame.Samples.Input", MainLauncher = true,Icon = "@drawable/icon", Theme = "@style/Theme.Splash")]
	public class Activity1 :  Microsoft.Xna.Framework.AndroidGameActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
	        Game1.Activity = this;
            var g = new Game1();
            SetContentView(g.Window);
            g.Run();
		}
	}
}


