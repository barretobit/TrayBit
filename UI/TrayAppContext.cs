namespace TrayBit.UI;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly SynchronizationContext _uiContext;
    private readonly TrayManager _tray = new();
    private readonly Core.CpuUsageMonitor _cpu = new();
    private readonly System.Timers.Timer _timer = new(interval: 1000);

    public TrayAppContext()
    {
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("No UI synchronization context on startup thread.");

        _tray.ExitRequested += (_, _) => ExitThread();

        _timer.Elapsed += (_, _) => Poll();
        _timer.Start();
    }

    private void Poll()
    {
        float usage = _cpu.GetUsage();
        _uiContext.Post(_ => _tray.SetStatus($"CPU: {usage:F0}%"), null);
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();
        _cpu.Dispose();
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
