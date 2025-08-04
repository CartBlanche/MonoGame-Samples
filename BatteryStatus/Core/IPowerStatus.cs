namespace BatteryStatus
{
    public interface IPowerStatus
    {
        string BatteryChargeStatus { get; }
        string PowerLineStatus { get; }
        int BatteryLifePercent { get; }
    }
}
