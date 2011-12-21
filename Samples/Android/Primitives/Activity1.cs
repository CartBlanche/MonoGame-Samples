using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace MonoGame.Samples.Primitives.Android
{
	[Activity (Label = "MonoGame.Samples.Primitives.Android", MainLauncher = true,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create our OpenGL view, and display it
			GLView1 view = new GLView1 (this);
			SetContentView (view);
		}
	}
}


