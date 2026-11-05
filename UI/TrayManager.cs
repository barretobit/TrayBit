namespace TrayBit.UI;

internal sealed class TrayManager : IDisposable
{
    private const int TooltipLimit = 63;

    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _powerPlansItem;

    public event EventHandler? ExitRequested;
    public event EventHandler<Guid>? PowerPlanActivated;
    public event Action<Point>? OpenPanelRequested;

    public TrayManager()
    {
        var openPanelItem = new ToolStripMenuItem("Open Panel");
        openPanelItem.Click += (_, _) => OpenPanelRequested?.Invoke(Cursor.Position);

        _powerPlansItem = new ToolStripMenuItem("Energy Profile")
        {
            Enabled = false
        };

        var aboutItem = new ToolStripMenuItem("About");
        aboutItem.Click += OnAboutClicked;

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var menu = new ContextMenuStrip();
        menu.Items.Add(openPanelItem);
        menu.Items.Add(_powerPlansItem);
        menu.Items.Add(new ToolStripSeparator());
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

    public void SetPowerPlansUnavailable()
    {
        _powerPlansItem.Enabled = false;
        _powerPlansItem.DropDownItems.Clear();
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
            $"TrayBit v{version}\n\nDisplays system temperature and lets you switch energy profiles.",
            "About TrayBit",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
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
