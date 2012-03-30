using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace AdSense
{
	public static class AdMobHelper
	{
		private static IntPtr _helperClass = JNIEnv.FindClass("MonoGame/AdMobHelper");
			
		public static void AddTestDevice(View view,string deviceid)
		{
			var s = new Java.Lang.String(deviceid);	
			IntPtr methodId = JNIEnv.GetStaticMethodID(_helperClass, "addTestDevice", "(Landroid/view/View;Ljava/lang/String;)V");
			JNIEnv.CallStaticVoidMethod(_helperClass, methodId, new JValue[2] { new JValue(view), new JValue(s) });
		}
		
		public static View CreateAdView(Activity context, string id)
		{				
			var s = new Java.Lang.String(id);			
			IntPtr methodId = JNIEnv.GetStaticMethodID(_helperClass, "createAdView", "(Landroid/app/Activity;Ljava/lang/String;)Landroid/view/View;");
			IntPtr view = JNIEnv.CallStaticObjectMethod(_helperClass, methodId, new JValue[2] { new JValue(context), new JValue(s) });
			return new Java.Lang.Object(view, JniHandleOwnership.TransferLocalRef).JavaCast<View>();
		}
		
		public static void RequestFreshAd(View view)
		{
			IntPtr methodId = JNIEnv.GetStaticMethodID(_helperClass, "requestFreshAd", "(Landroid/view/View;)V");
			JNIEnv.CallStaticVoidMethod(_helperClass, methodId, new JValue(view));
		}
	}
}


