using TrayBit.Helpers;

namespace TrayBit.UI;

internal sealed class TaskbarInfoPanel : Form
{
    private static readonly Color KeyColor = Color.FromArgb(1, 2, 3);
    private static readonly Color ControlBack = Color.FromArgb(49, 50, 68);
    private static readonly Color AccentColor = Color.FromArgb(250, 179, 135);
    private static readonly Color MainText = Color.FromArgb(205, 214, 244);
    private static readonly Color DimText = Color.FromArgb(148, 152, 175);

    private readonly Label _cpuChip;
    private readonly Label _tempChip;
    private readonly Label _batteryChip;
    private readonly Button _planChip;
    private readonly Button _caffeineChip;
    private readonly ContextMenuStrip _planMenu;

    private readonly List<Guid> _planIds = [];

    public event Action<Guid>? PlanActivated;
    public event EventHandler? CaffeineToggleRequested;

    public TaskbarInfoPanel()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = KeyColor;
        TransparencyKey = KeyColor;
        Font = new Font("Segoe UI", 8.25f);

        _planMenu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = Font
        };

        _cpuChip = CreateChip();
        _cpuChip.Text = "-";
        _tempChip = CreateChip();
        _tempChip.Visible = false;
        _batteryChip = CreateChip();
        _batteryChip.Visible = false;

        _planChip = CreateTextButton(MainText);
        _planChip.Text = "-";
        _planChip.Click += (_, _) => _planMenu.Show(_planChip, new Point(0, -_planMenu.Height));

        _caffeineChip = CreateTextButton(DimText);
        _caffeineChip.Text = "Caffeine";
        _caffeineChip.Click += (_, _) => CaffeineToggleRequested?.Invoke(this, EventArgs.Empty);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0),
            BackColor = KeyColor
        };

        flow.Controls.Add(_cpuChip);
        flow.Controls.Add(_tempChip);
        flow.Controls.Add(_batteryChip);
        flow.Controls.Add(_planChip);
        flow.Controls.Add(_caffeineChip);

        Controls.Add(flow);
    }

    public void PositionOnTaskbar()
    {
        Rectangle workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1040);

        int leftOffset = 64;

        IntPtr tray = NativeMethods.FindWindow("Shell_TrayWnd", null);

        if (tray == IntPtr.Zero || !NativeMethods.GetWindowRect(tray, out NativeMethods.RECT trayRect))
        {
            Location = new Point(workArea.Left + leftOffset, workArea.Bottom - Height - 6);
            return;
        }

        int bandHeight = trayRect.Bottom - trayRect.Top;
        int y = trayRect.Top + Math.Max((bandHeight - Height) / 2, 0);

        Location = new Point(workArea.Left + leftOffset, y);
    }

    public void UpdateMetrics(float cpuPercent, float? mainTemp, Core.BatteryStatus? battery)
    {
        _cpuChip.Text = $"CPU {cpuPercent:F0}%";

        _tempChip.Visible = mainTemp is not null;
        if (mainTemp is { } temp)
            _tempChip.Text = $"{temp:F0}°C";

        _batteryChip.Visible = battery is not null;
        if (battery is { } b)
            _batteryChip.Text = b.Charging ? $"{b.Percent}% (chg)" : $"{b.Percent}%";
    }

    public void SetPlans(IReadOnlyList<(Core.PowerPlan Plan, bool Active)> plans, Guid? activeId)
    {
        _planIds.Clear();

        ToolStripItem[] stale = _planMenu.Items.Cast<ToolStripItem>().ToArray();
        _planMenu.Items.Clear();

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

            item.Click += (_, _) => PlanActivated?.Invoke(id);
            _planMenu.Items.Add(item);

            if (active)
                _planChip.Text = Truncate(plan.Name, 16);
        }
    }

    public void SetCaffeine(bool on)
    {
        _caffeineChip.ForeColor = on ? AccentColor : DimText;
        _caffeineChip.Text = on ? "Caffeine ON" : "Caffeine";
    }

    private static Label CreateChip() =>
        new()
        {
            AutoSize = true,
            BackColor = KeyColor,
            ForeColor = MainText,
            Margin = new Padding(0, 5, 12, 4),
            Text = "-"
        };

    private static Button CreateTextButton(Color textColor)
    {
        var button = new Button
        {
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = KeyColor,
            ForeColor = textColor,
            Margin = new Padding(0, 3, 12, 3),
            TextAlign = ContentAlignment.MiddleLeft
        };

        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = KeyColor;
        button.FlatAppearance.MouseDownBackColor = KeyColor;

        return button;
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";

    protected override bool ShowWithoutActivation => true;

    protected override void WndProc(ref Message m)
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_NOACTIVATE = 0x0003;

        if (m.Msg == WM_MOUSEACTIVATE)
        {
            m.Result = MA_NOACTIVATE;
            return;
        }

        base.WndProc(ref m);
    }
}
