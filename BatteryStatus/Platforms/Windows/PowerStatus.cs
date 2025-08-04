
using System.Windows.Forms;
using BatteryStatus;

namespace BatteryStatus.Windows
{
    public class PowerStatus : IPowerStatus
    {
        public string BatteryChargeStatus => SystemInformation.PowerStatus.BatteryChargeStatus.ToString();
        public string PowerLineStatus => SystemInformation.PowerStatus.PowerLineStatus.ToString();
        public int BatteryLifePercent => (int)(SystemInformation.PowerStatus.BatteryLifePercent * 100);
    }
}