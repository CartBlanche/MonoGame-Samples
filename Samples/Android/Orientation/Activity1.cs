using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;

namespace Orientation
{
	[Activity (Label = "Orientation", MainLauncher = true)]
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


