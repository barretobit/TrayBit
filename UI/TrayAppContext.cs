namespace TrayBit.UI;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly SynchronizationContext _uiContext;
    private readonly TrayManager _tray = new();
    private readonly Core.CpuUsageMonitor _cpu = new();
    private readonly Core.HardwareMonitor _hardware = new();
    private readonly Core.PowerPlanManager _powerPlans = new();
    private readonly Core.AppSettings _settings = Core.AppSettings.Load();
    private readonly System.Timers.Timer _timer = new(interval: 1000);

    private readonly TaskbarInfoPanel _taskbar = new();

    private List<Core.PowerPlan>? _planCache;
    private Guid? _activePlanId;
    private bool _caffeineOn;
    private bool _showTaskbar;
    private PopupPanel? _popup;

    private float _lastCpu;
    private float? _lastTemp;
    private float? _lastGpuTemp;
    private Core.BatteryStatus? _lastBattery;

    public TrayAppContext()
    {
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException("No UI synchronization context on startup thread.");

        _tray.ExitRequested += (_, _) => ExitThread();
        _tray.PowerPlanActivated += OnPowerPlanActivated;
        _tray.CaffeineToggleRequested += OnCaffeineToggleRequested;
        _tray.StartupToggleChanged += OnStartupToggleChanged;
        _tray.TaskbarToggleChanged += OnTaskbarToggleChanged;
        _tray.OpenPanelRequested += OnOpenPanelRequested;

        _taskbar.PlanActivated += id => OnPowerPlanActivated(this, id);
        _taskbar.CaffeineToggleRequested += OnCaffeineToggleRequested;

        SyncStartupState();

        _showTaskbar = _settings.ShowTaskbarInfo;
        _tray.SetTaskbarVisible(_showTaskbar);

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        if (_settings.CaffeineOn && Core.CaffeineManager.SetEnabled(true))
        {
            _caffeineOn = true;
            _tray.SetCaffeine(true);
        }

        LoadPowerPlans();

        _timer.Elapsed += (_, _) => Poll();
        _timer.Start();
    }

    private void SyncStartupState()
    {
        try
        {
            bool enabled = Core.StartupManager.IsEnabled();
            _settings.StartWithWindows = enabled;
            _tray.SetStartup(enabled);
        }
        catch
        {
            _tray.SetStartup(false);
        }
    }

    private void ApplyTaskbarVisibility()
    {
        if (!_showTaskbar)
        {
            _taskbar.Hide();
            return;
        }

        try
        {
            _taskbar.PositionOnTaskbar();
            _taskbar.Show();
        }
        catch
        {
            _showTaskbar = false;
            _tray.SetTaskbarVisible(false);
        }
    }

    private void OnTaskbarToggleChanged(bool enabled)
    {
        _showTaskbar = enabled;
        _settings.ShowTaskbarInfo = enabled;
        _settings.Save();
        ApplyTaskbarVisibility();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (!_showTaskbar || !_taskbar.IsHandleCreated)
            return;

        _taskbar.BeginInvoke(_taskbar.PositionOnTaskbar);
    }

    private static bool IsForegroundFullscreen()
    {
        IntPtr foreground = Helpers.NativeMethods.GetForegroundWindow();

        if (foreground == IntPtr.Zero)
            return false;

        var className = new System.Text.StringBuilder(256);

        if (Helpers.NativeMethods.GetClassName(foreground, className, className.Capacity) > 0)
        {
            string name = className.ToString();

            if (name is "Progman" or "WorkerW")
                return false;
        }

        if (!Helpers.NativeMethods.GetWindowRect(foreground, out Helpers.NativeMethods.RECT rect))
            return false;

        Rectangle bounds = Screen.FromHandle(foreground).Bounds;

        return rect.Left <= bounds.Left && rect.Top <= bounds.Top
               && rect.Right >= bounds.Right && rect.Bottom >= bounds.Bottom;
    }

    private void LoadPowerPlans()
    {
        try
        {
            _planCache = [.. _powerPlans.GetPlans()];
            _activePlanId = _powerPlans.GetActivePlan();
        }
        catch
        {
            _planCache = null;
            _activePlanId = null;
        }

        PublishPowerPlans();
    }

    private List<(Core.PowerPlan Plan, bool Active)> GetPlanDisplay() =>
        _planCache?.Select(p => (p, p.Id == _activePlanId)).ToList()
            ?? [];

    private void PublishPowerPlans()
    {
        List<(Core.PowerPlan Plan, bool Active)> display = GetPlanDisplay();

        if (display.Count == 0)
        {
            _tray.SetPowerPlansUnavailable();
        }
        else
        {
            _tray.SetPowerPlans(display);
        }

        _popup?.SetPlans(display, _activePlanId);
        _taskbar.SetPlans(display, _activePlanId);
    }

    private void OnPowerPlanActivated(object? sender, Guid planId)
    {
        if (_powerPlans.SetActive(planId))
        {
            _activePlanId = planId;
            PublishPowerPlans();
        }
    }

    private void OnCaffeineToggleRequested(object? sender, EventArgs e)
    {
        bool desired = !_caffeineOn;

        if (!Core.CaffeineManager.SetEnabled(desired))
            return;

        _caffeineOn = desired;
        _settings.CaffeineOn = _caffeineOn;
        _settings.Save();

        _tray.SetCaffeine(_caffeineOn);
        _popup?.SetCaffeine(_caffeineOn);
        _taskbar.SetCaffeine(_caffeineOn);
    }

    private void OnStartupToggleChanged(bool enabled)
    {
        try
        {
            Core.StartupManager.SetEnabled(enabled);
            _settings.StartWithWindows = enabled;
            _settings.Save();
        }
        catch
        {
            SyncStartupState();
        }
    }

    private void OnOpenPanelRequested(Point anchor)
    {
        if (_popup is null)
        {
            _popup = new PopupPanel();
            _popup.PlanActivated += id => OnPowerPlanActivated(this, id);
            _popup.CaffeineToggleRequested += OnCaffeineToggleRequested;
        }

        PublishPowerPlans();
        _popup.SetCaffeine(_caffeineOn);
        _popup.UpdateMetrics(_lastCpu, _lastTemp, _lastGpuTemp, _lastBattery);
        _popup.ShowAt(anchor);
    }

    private void Poll()
    {
        float usage = _cpu.GetUsage();
        _hardware.Poll();

        DetectExternalPowerPlanChange();

        float? temp = _hardware.CpuTemperature ?? _hardware.SocTemperature;
        Core.BatteryStatus? battery = Core.BatteryMonitor.GetStatus();

        _lastCpu = usage;
        _lastTemp = temp;
        _lastGpuTemp = _hardware.DiscreteGpuTemperature;
        _lastBattery = battery;

        var parts = new List<string>();

        if (_caffeineOn)
            parts.Add("[Caffeine]");

        parts.Add($"CPU: {usage:F0}%");

        if (temp is { } value)
            parts.Add($"{value:F0}°C");

        if (_hardware.DiscreteGpuTemperature is { } gpuTemp)
            parts.Add($"GPU {gpuTemp:F0}°C");

        if (battery is { } b)
            parts.Add(b.Charging ? $"Bat {b.Percent}% (chg)" : $"Bat {b.Percent}%");

        string status = string.Join(" | ", parts);

        _uiContext.Post(_ =>
        {
            _tray.SetStatus(status);

            if (_popup is { Visible: true })
                _popup.UpdateMetrics(usage, temp, _lastGpuTemp, battery);

            if (_showTaskbar && !IsForegroundFullscreen())
            {
                _taskbar.UpdateMetrics(usage, temp, battery);
                _taskbar.Show();
            }
            else
            {
                _taskbar.Hide();
            }
        }, null);
    }

    private void DetectExternalPowerPlanChange()
    {
        Guid? active = _powerPlans.GetActivePlan();

        if (active == _activePlanId)
            return;

        _activePlanId = active;
        _uiContext.Post(_ => PublishPowerPlans(), null);
    }

    protected override void ExitThreadCore()
    {
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        _timer.Stop();
        _timer.Dispose();
        _cpu.Dispose();
        _hardware.Dispose();
        _settings.Save();
        _popup?.Dispose();
        _taskbar.Dispose();
        _tray.Dispose();
        base.ExitThreadCore();
    }
}
