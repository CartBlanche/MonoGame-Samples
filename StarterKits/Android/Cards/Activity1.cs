using System;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Blackjack
{
	[Activity (Label = "Blackjack.Android", MainLauncher = true,ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]
	public class Activity1 : Microsoft.Xna.Framework.AndroidGameActivity
	{
		protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            BlackjackGame.Activity = this;
            var g = new BlackjackGame();
            SetContentView(g.Window);
            g.Run();
        }
	}
}


