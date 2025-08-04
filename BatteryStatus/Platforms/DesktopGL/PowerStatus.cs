
using BatteryStatus;

namespace BatteryStatus.DesktopGL
{
    public class PowerStatus : IPowerStatus
    {
        public string BatteryChargeStatus => "N/A";
        public string PowerLineStatus => "N/A";
        public int BatteryLifePercent => 100;
    }
}
