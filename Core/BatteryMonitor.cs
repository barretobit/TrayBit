using TrayBit.Helpers;

namespace TrayBit.Core;

internal static class BatteryMonitor
{
    private const byte FlagNoBattery = 0x80;
    private const byte FlagCharging = 0x08;

    public static BatteryStatus? GetStatus()
    {
        if (!NativeMethods.GetSystemPowerStatus(out NativeMethods.SYSTEM_POWER_STATUS status))
            return null;

        if (status.BatteryLifePercent == 255 || (status.BatteryFlag & FlagNoBattery) != 0)
            return null;

        return new BatteryStatus(status.BatteryLifePercent, (status.BatteryFlag & FlagCharging) != 0);
    }
}

internal readonly record struct BatteryStatus(int Percent, bool Charging);
