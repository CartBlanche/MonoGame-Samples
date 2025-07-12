
using Android.App;
using Android.Content;
using Android.OS;

namespace BatteryStatusDemo
{
    public static partial class PowerStatus
    {
        public static string BatteryChargeStatus
        {
            get
            {
                var filter = new IntentFilter(Intent.ActionBatteryChanged);
                var battery = Application.Context.RegisterReceiver(null, filter);
                if (battery != null)
                {
                    int status = battery.GetIntExtra(BatteryManager.ExtraStatus, -1);

                    switch ((Android.OS.BatteryStatus)status)
                    {
                        case Android.OS.BatteryStatus.Charging:
                            return "Charging";
                        case Android.OS.BatteryStatus.Full:
                            return "Full";
                        case Android.OS.BatteryStatus.Discharging:
                            return "Discharging";
                        case Android.OS.BatteryStatus.NotCharging:
                            return "Not Charging";
                        case Android.OS.BatteryStatus.Unknown:
                        default:
                            return "Unknown";
                    }
                }

				return "Unknown";
			}
        }

        public static string PowerLineStatus
        {
            get
            {
                var filter = new IntentFilter(Intent.ActionBatteryChanged);
                var battery = Application.Context.RegisterReceiver(null, filter);
                if (battery != null)
                {
                    int plugged = battery.GetIntExtra(Android.OS.BatteryManager.ExtraPlugged, -1);
                    if (plugged == (int)Android.OS.BatteryPlugged.Ac)
                        return "AC";
                    if (plugged == (int)Android.OS.BatteryPlugged.Usb)
                        return "USB";
                    if (plugged == (int)Android.OS.BatteryPlugged.Wireless)
                        return "Wireless";
                }
                return "Unplugged";
            }
        }

        public static int BatteryLifePercent
        {
            get
            {
                var filter = new IntentFilter(Intent.ActionBatteryChanged);
                var battery = Application.Context.RegisterReceiver(null, filter);
                if (battery != null)
                {
                    int level = battery.GetIntExtra(Android.OS.BatteryManager.ExtraLevel, -1);
                    int scale = battery.GetIntExtra(Android.OS.BatteryManager.ExtraScale, -1);
                    if (level >= 0 && scale > 0)
                        return (int)((level / (float)scale) * 100);
                }
                return -1;
            }
        }
    }
}
