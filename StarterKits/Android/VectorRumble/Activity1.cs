using System;

using Android.App;
using Android.OS;
using Android.Content.PM;

namespace VectorRumble
{
	[Activity (Label = "VectorRumble", MainLauncher = true, 
		Icon = "@drawable/icon", 
		Theme = "@style/Theme.Splash"
	    ,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
	{
		protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            VectorRumbleGame.Activity = this;
            VectorRumbleGame g = new VectorRumbleGame();
            SetContentView(g.Window);
            g.Run();
        }
	}
}


