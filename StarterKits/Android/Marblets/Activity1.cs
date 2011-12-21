using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Marblets
{
	[Activity (Label = "Marblets", MainLauncher = true,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			MarbletsGame g = new MarbletsGame(this);
            SetContentView(g.Window);
            g.Run();
		}
	}
}


