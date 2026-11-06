namespace TrayBit.UI;

internal sealed class TrayManager : IDisposable
{
    private const int TooltipLimit = 63;

    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _powerPlansItem;

    public event EventHandler? ExitRequested;
    public event EventHandler<Guid>? PowerPlanActivated;

    public TrayManager()
    {
        _powerPlansItem = new ToolStripMenuItem("Energy Profile")
        {
            Enabled = false
        };

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var menu = new ContextMenuStrip();
        menu.Items.Add(_powerPlansItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon("icon_normal"),
            Text = "TrayBit",
            ContextMenuStrip = menu,
            Visible = true
        };
    }

    public void SetStatus(string text) =>
        _trayIcon.Text = text.Length <= TooltipLimit ? text : text[..TooltipLimit];

    public void SetPowerPlans(IReadOnlyList<(Core.PowerPlan Plan, bool Active)> plans)
    {
        _powerPlansItem.Enabled = plans.Count > 0;

        ToolStripItem[] stale = _powerPlansItem.DropDownItems.Cast<ToolStripItem>().ToArray();
        _powerPlansItem.DropDownItems.Clear();

        foreach (ToolStripItem item in stale)
            item.Dispose();

        foreach ((Core.PowerPlan plan, bool active) in plans)
        {
            Guid id = plan.Id;

            var item = new ToolStripMenuItem(plan.Name)
            {
                Checked = active,
                CheckOnClick = false
            };

            item.Click += (_, _) => PowerPlanActivated?.Invoke(this, id);
            _powerPlansItem.DropDownItems.Add(item);
        }
    }

    public void SetPowerPlansUnavailable()
    {
        _powerPlansItem.Enabled = false;
        _powerPlansItem.DropDownItems.Clear();
    }

    private static Icon LoadIcon(string name)
    {
        using Stream stream =
            typeof(TrayManager).Assembly.GetManifestResourceStream($"TrayBit.Resources.{name}.ico")
            ?? throw new InvalidOperationException($"Embedded icon '{name}' not found.");
        return new Icon(stream);
    }

    public void Dispose()
    {
        _trayIcon.Visible = false;
        _trayIcon.Icon?.Dispose();
        _trayIcon.Dispose();
    }
}