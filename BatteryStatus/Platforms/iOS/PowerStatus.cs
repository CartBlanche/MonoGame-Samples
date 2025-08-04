using BatteryStatus;
using UIKit;

namespace BatteryStatus.iOS
{
    public class PowerStatus : IPowerStatus
    {
        public PowerStatus()
        {
            UIDevice.CurrentDevice.BatteryMonitoringEnabled = true;
        }

        public string BatteryChargeStatus
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

        public string PowerLineStatus
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

        public int BatteryLifePercent
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
