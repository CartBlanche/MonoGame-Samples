using System;
using Android.App;
using Android.Runtime;

namespace SamplesContentBuilder.Android
{
    [Application]
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }
        public override void OnCreate()
        {
            base.OnCreate();
            // Initialize your game here
        }
    }

    public static class Program
    {
        static void Main(string[] args)
        {
            // Entry point for Android
        }
    }
}
