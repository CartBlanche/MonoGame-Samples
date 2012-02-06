using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace RenderTarget2DSample
{
    [Activity(Label = "RenderTarget2D", MainLauncher = true, Icon = "@drawable/icon"
        , Theme = "@style/Theme.Splash",ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
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


