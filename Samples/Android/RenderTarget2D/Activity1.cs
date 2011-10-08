using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace RenderTarget2DSample
{
	[Activity (Label = "RenderTarget2D", MainLauncher = true)]
	public class Activity1 : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			var g = new Game1(this);
            SetContentView(g.Window);
            g.Run();
		}
	}
}


