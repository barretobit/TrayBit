using System.Management;
using System.Runtime.InteropServices;
using TrayBit.Helpers;

namespace TrayBit.Core;

internal sealed record PowerPlan(Guid Id, string Name);

internal sealed class PowerPlanManager
{
    public IReadOnlyList<PowerPlan> GetPlans()
    {
        var result = new List<PowerPlan>();

        using var searcher = new ManagementObjectSearcher(
            @"root\cimv2\power",
            "SELECT InstanceID, ElementName FROM Win32_PowerPlan");

        foreach (ManagementObject plan in searcher.Get())
        {
            using (plan)
            {
                string? instanceId = plan["InstanceID"]?.ToString();
                string? name = plan["ElementName"]?.ToString();

                if (instanceId is null || TryParsePlanId(instanceId) is not { } id)
                    continue;

                result.Add(new PowerPlan(id, name ?? id.ToString()));
            }
        }

        return result
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public bool SetActive(Guid planId)
    {
        Guid scheme = planId;
        return NativeMethods.PowerSetActiveScheme(IntPtr.Zero, ref scheme) == 0;
    }

    public Guid? GetActivePlan()
    {
        if (NativeMethods.PowerGetActiveScheme(IntPtr.Zero, out IntPtr ptr) != 0 || ptr == IntPtr.Zero)
            return null;

        try
        {
            return Marshal.PtrToStructure<Guid>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private static Guid? TryParsePlanId(string instanceId)
    {
        int start = instanceId.IndexOf('{');
        int end = instanceId.IndexOf('}');

        if (start < 0 || end <= start + 1)
            return null;

        return Guid.TryParse(instanceId[(start + 1)..end], out Guid id) ? id : null;
    }
}
