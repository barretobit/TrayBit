using System.Diagnostics;

namespace TrayBit.Core;

internal sealed class CpuUsageMonitor : IDisposable
{
    private readonly PerformanceCounter _counter =
        new("Processor", "% Processor Time", "_Total", readOnly: true);

    public CpuUsageMonitor()
    {
        _counter.NextValue();
    }

    public float GetUsage() => _counter.NextValue();

    public void Dispose() => _counter.Dispose();
}
