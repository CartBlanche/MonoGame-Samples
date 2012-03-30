using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;
using PrimitivesSample;

namespace MonoGame.Samples.Primitives.Android
{
	[Activity (Label = "MonoGame.Samples.Primitives.Android", 
	           MainLauncher = true,
	           ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden
	           ,ScreenOrientation=ScreenOrientation.Landscape)]
	public class Activity1 : AndroidGameActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Create our OpenGL view, and display it
			PrimitivesSampleGame.Activity = this;
			var g = new PrimitivesSampleGame();
			SetContentView (g.Window);
			g.Run();
		}
	}
}


