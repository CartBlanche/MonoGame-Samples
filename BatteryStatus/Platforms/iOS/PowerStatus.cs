
using UIKit;

namespace BatteryStatusDemo
{
    public static partial class PowerStatus
    {
        static PowerStatus()
        {
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;
        }

        public static string BatteryChargeStatus
        {
            get
            {
                switch (UIDevice.CurrentDevice.BatteryState)
                {
                    case UIDeviceBatteryState.Charging:
                        return "Charging";
                    case UIDeviceBatteryState.Full:
                        return "Full";
                    case UIDeviceBatteryState.Unplugged:
                        return "Unplugged";
                    case UIDeviceBatteryState.Unknown:
                    default:
                        return "Unknown";
                }
            }
        }

        public static string PowerLineStatus
        {
            get
            {
                var state = UIDevice.CurrentDevice.BatteryState;
                if (state == UIDeviceBatteryState.Charging || state == UIDeviceBatteryState.Full)
                    return "Plugged";
                if (state == UIDeviceBatteryState.Unplugged)
                    return "Unplugged";
                return "Unknown";
            }
        }

        public static int BatteryLifePercent
        {
            get
            {
                float level = UIDevice.CurrentDevice.BatteryLevel;
                if (level < 0)
                    return -1;
                return (int)(level * 100);
            }
        }
    }
}
