using TrayBit.Helpers;

namespace TrayBit.Core;

internal static class CaffeineManager
{
    public static bool SetEnabled(bool enabled)
    {
        var flags = NativeMethods.EXECUTION_STATE.EsContinuous;

        if (enabled)
        {
            flags |= NativeMethods.EXECUTION_STATE.EsDisplayRequired
                     | NativeMethods.EXECUTION_STATE.EsSystemRequired;
        }

        return NativeMethods.SetThreadExecutionState(flags) != 0;
    }
}
