using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Microsoft.Xna.Framework;

namespace Orientation
{
	[Activity (Label = "Orientation", MainLauncher = true
	           ,Icon = "@drawable/icon"
	           , Theme = "@style/Theme.Splash"
	           ,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
	public class Activity1 : AndroidGameActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
						
			OrientationSample.OrientationSample.Activity = this;		
			OrientationSample.OrientationSample g = new OrientationSample.OrientationSample();						
			SetContentView (g.Window);
			g.Run();						
		}
	}
}


