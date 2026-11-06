namespace TrayBit.UI;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly TrayManager _tray = new();
    private readonly Core.HardwareMonitor _hardware = new();
    private readonly Core.PowerPlanManager _powerPlans = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };

    private List<Core.PowerPlan>? _planCache;
    private Guid? _activePlanId;
    private float? _lastTemp;

    public TrayAppContext()
    {
        _tray.ExitRequested += (_, _) => ExitThread();
        _tray.PowerPlanActivated += OnPowerPlanActivated;

        LoadPowerPlans();
        Poll();

        _timer.Tick += (_, _) => Poll();
        _timer.Start();
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
        _planCache?.Select(plan => (plan, plan.Id == _activePlanId)).ToList()
            ?? [];

    private void PublishPowerPlans()
    {
        List<(Core.PowerPlan Plan, bool Active)> display = GetPlanDisplay();

        if (display.Count == 0)
            _tray.SetPowerPlansUnavailable();
        else
            _tray.SetPowerPlans(display);
    }

    private void OnPowerPlanActivated(object? sender, Guid planId)
    {
        if (!_powerPlans.SetActive(planId))
            return;

        _activePlanId = planId;
        PublishPowerPlans();
        UpdateTrayStatus();
    }

    private void Poll()
    {
        _hardware.Poll();

        _lastTemp = _hardware.CpuTemperature ?? _hardware.SocTemperature;

        DetectExternalPowerPlanChange();
        UpdateTrayStatus();
    }

    private void UpdateTrayStatus()
    {
        string profile = _planCache?.FirstOrDefault(plan => plan.Id == _activePlanId)?.Name ?? "Unknown";
        string temperature = _lastTemp is { } value ? $"{value:F0}°C" : "N/A";
        string status = $"{temperature} | {profile}";
        _tray.SetStatus(status);
    }

    private void DetectExternalPowerPlanChange()
    {
        Guid? active = _powerPlans.GetActivePlan();

        if (active == _activePlanId)
            return;

        _activePlanId = active;
        PublishPowerPlans();
        UpdateTrayStatus();
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _timer.Dispose();
        _hardware.Dispose();
        _tray.Dispose();
        base.ExitThreadCore();
    }
}