namespace TrayBit.UI;

internal sealed class TrayManager : IDisposable
{
    private const int TooltipLimit = 63;

    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _powerPlansItem;
    private readonly ToolStripMenuItem _caffeineItem;
    private readonly ToolStripMenuItem _startupItem;

    public event EventHandler? ExitRequested;
    public event EventHandler<Guid>? PowerPlanActivated;
    public event EventHandler? CaffeineToggleRequested;
    public event Action<Point>? OpenPanelRequested;
    public event Action<bool>? StartupToggleChanged;
    public event Action<bool>? TaskbarToggleChanged;

    private readonly ToolStripMenuItem _taskbarItem;

    public TrayManager()
    {
        var openPanelItem = new ToolStripMenuItem("Open Panel");
        openPanelItem.Click += (_, _) => OpenPanelRequested?.Invoke(Cursor.Position);

        _powerPlansItem = new ToolStripMenuItem("Power Plans")
        {
            Enabled = false
        };

        _caffeineItem = new ToolStripMenuItem("Caffeine: OFF");
        _caffeineItem.Click += (_, _) => CaffeineToggleRequested?.Invoke(this, EventArgs.Empty);

        _startupItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true
        };
        _startupItem.CheckedChanged += (_, _) => StartupToggleChanged?.Invoke(_startupItem.Checked);

        _taskbarItem = new ToolStripMenuItem("Show Taskbar Info")
        {
            CheckOnClick = true,
            Checked = true
        };
        _taskbarItem.CheckedChanged += (_, _) => TaskbarToggleChanged?.Invoke(_taskbarItem.Checked);

        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += OnAboutClicked;

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var menu = new ContextMenuStrip();
        menu.Items.Add(openPanelItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_powerPlansItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_caffeineItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_taskbarItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(aboutItem);
        menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Icon = LoadIcon("icon_normal"),
            Text = "TrayBit",
            ContextMenuStrip = menu,
            Visible = true
        };

        _trayIcon.MouseClick += OnTrayIconMouseClick;
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

    public void SetPowerPlansUnavailable() =>
        _powerPlansItem.Enabled = false;

    public void SetCaffeine(bool on)
    {
        _caffeineItem.Text = on ? "Caffeine: ON" : "Caffeine: OFF";
        ReplaceIcon(LoadIcon(on ? "icon_caffeine" : "icon_normal"));
    }

    public void SetStartup(bool enabled)
    {
        if (_startupItem.Checked != enabled)
            _startupItem.Checked = enabled;
    }

    public void SetTaskbarVisible(bool enabled)
    {
        if (_taskbarItem.Checked != enabled)
            _taskbarItem.Checked = enabled;
    }

    private void OnTrayIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            OpenPanelRequested?.Invoke(e.Location);
    }

    private void OnAboutClicked(object? sender, EventArgs e)
    {
        string version = typeof(TrayManager).Assembly.GetName().Version?.ToString(3) ?? "?";

        MessageBox.Show(
            $"TrayBit v{version}\n\nSystem monitor tray utility.\nCPU usage and temperatures, battery status, power plans and a keep-awake toggle.",
            "About TrayBit",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void ReplaceIcon(Icon icon)
    {
        Icon? old = _trayIcon.Icon;
        _trayIcon.Icon = icon;
        old?.Dispose();
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
