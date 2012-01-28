using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Microsoft.Xna.Framework;
using AdSense;
using Android.Content.PM;

namespace MonoGame.Samples.AdMob
{
	[Activity (Label = "MonoGame.Samples.AdMob", MainLauncher = true
	          , Icon = "@drawable/icon", Theme = "@style/Theme.Splash",ConfigurationChanges=ConfigChanges.Orientation|ConfigChanges.Keyboard|ConfigChanges.KeyboardHidden)]	
	public class Activity1 : AndroidGameActivity
	{
		View adView;
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			Game1.Activity = this;
			var g = new Game1();
			FrameLayout fl = new FrameLayout(this);
			fl.AddView(g.Window);                 
			adView = AdMobHelper.CreateAdView(this,"publisherid");
			//AdMobHelper.AddTestDevice(adView,"deviceid");
			fl.AddView(adView);                        
			AdMobHelper.RequestFreshAd(adView);
			SetContentView (fl);            
			g.Run();  
			
		}
	}
}


