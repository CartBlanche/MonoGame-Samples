
using System.Windows.Forms;

namespace BatteryStatusDemo
{
    public static partial class PowerStatus
    {
        public static string BatteryChargeStatus => SystemInformation.PowerStatus.BatteryChargeStatus.ToString();
        public static string PowerLineStatus => SystemInformation.PowerStatus.PowerLineStatus.ToString();
        public static int BatteryLifePercent => (int)(SystemInformation.PowerStatus.BatteryLifePercent * 100);
    }
}