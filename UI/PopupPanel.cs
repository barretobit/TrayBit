namespace TrayBit.UI;

internal sealed class PopupPanel : Form
{
    private static readonly Color PanelBack = Color.FromArgb(30, 30, 46);
    private static readonly Color ControlBack = Color.FromArgb(49, 50, 68);
    private static readonly Color MainText = Color.FromArgb(205, 214, 244);
    private static readonly Color DimText = Color.FromArgb(148, 152, 175);

    private readonly Label _cpuValue;
    private readonly ProgressBar _cpuBar;
    private readonly Label _tempValue;
    private readonly Label _gpuName;
    private readonly Label _gpuValue;
    private readonly Label _batteryName;
    private readonly Label _batteryValue;
    private readonly ComboBox _planCombo;
    private readonly Button _caffeineButton;

    private readonly List<Guid> _planIds = [];
    private bool _syncing;

    public event Action<Guid>? PlanActivated;
    public event EventHandler? CaffeineToggleRequested;

    public PopupPanel()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = PanelBack;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(276, 236);

        _cpuValue = CreateValueLabel(new Point(118, 14));
        _tempValue = CreateValueLabel(new Point(118, 42));
        _gpuName = CreateNameLabel("GPU Temp:", new Point(16, 70));
        _gpuValue = CreateValueLabel(new Point(118, 70));
        _batteryName = CreateNameLabel("Battery:", new Point(16, 98));
        _batteryValue = CreateValueLabel(new Point(118, 98));

        _cpuBar = new ProgressBar
        {
            Location = new Point(16, 126),
            Size = new Size(244, 10)
        };

        var separator = new Label
        {
            Location = new Point(0, 148),
            Size = new Size(276, 1),
            BackColor = ControlBack
        };

        _planCombo = new ComboBox
        {
            Location = new Point(118, 158),
            Size = new Size(142, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = ControlBack,
            ForeColor = MainText
        };
        _planCombo.SelectedIndexChanged += OnPlanSelected;

        _caffeineButton = new Button
        {
            Location = new Point(118, 190),
            Size = new Size(142, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = ControlBack,
            ForeColor = MainText,
            Text = "OFF"
        };
        _caffeineButton.FlatAppearance.BorderSize = 0;
        _caffeineButton.Click += (_, _) => CaffeineToggleRequested?.Invoke(this, EventArgs.Empty);

        Controls.Add(CreateNameLabel("CPU Usage:", new Point(16, 14)));
        Controls.Add(_cpuValue);
        Controls.Add(CreateNameLabel("Temp:", new Point(16, 42)));
        Controls.Add(_tempValue);
        Controls.Add(_gpuName);
        Controls.Add(_gpuValue);
        Controls.Add(_batteryName);
        Controls.Add(_batteryValue);
        Controls.Add(_cpuBar);
        Controls.Add(separator);
        Controls.Add(CreateNameLabel("Power Plan:", new Point(16, 162)));
        Controls.Add(_planCombo);
        Controls.Add(CreateNameLabel("Caffeine:", new Point(16, 194)));
        Controls.Add(_caffeineButton);

        Deactivate += (_, _) => Hide();
    }

    public void ShowAt(Point anchor)
    {
        Rectangle workArea = Screen.FromPoint(anchor).WorkingArea;

        int x = Math.Min(Math.Max(anchor.X - Width + 16, workArea.Left + 4), workArea.Right - Width - 4);
        int y = workArea.Bottom - Height - 4;

        Location = new Point(x, y);
        Show();
        Activate();
    }

    public void SetPlans(IReadOnlyList<(Core.PowerPlan Plan, bool Active)> plans, Guid? activeId)
    {
        _syncing = true;

        _planCombo.BeginUpdate();

        try
        {
            _planCombo.Items.Clear();
            _planIds.Clear();

            foreach ((Core.PowerPlan plan, _) in plans)
            {
                _planIds.Add(plan.Id);
                _planCombo.Items.Add(plan.Name);
            }

            _planCombo.SelectedIndex = activeId is { } id ? _planIds.IndexOf(id) : -1;
        }
        finally
        {
            _planCombo.EndUpdate();
            _syncing = false;
        }
    }

    public void SetCaffeine(bool on)
    {
        _caffeineButton.Text = on ? "ON" : "OFF";
        _caffeineButton.BackColor = on ? Color.FromArgb(250, 179, 135) : ControlBack;
        _caffeineButton.ForeColor = on ? PanelBack : MainText;
    }

    public void UpdateMetrics(float cpuPercent, float? mainTemp, float? gpuTemp, Core.BatteryStatus? battery)
    {
        _cpuValue.Text = $"{cpuPercent:F0}%";
        _cpuBar.Value = Math.Clamp((int)MathF.Round(cpuPercent), 0, 100);
        _tempValue.Text = mainTemp is { } temp ? $"{temp:F0}°C" : "N/A";

        bool hasGpu = gpuTemp is not null;
        _gpuName.Visible = hasGpu;
        _gpuValue.Visible = hasGpu;
        if (gpuTemp is { } gpu)
            _gpuValue.Text = $"{gpu:F0}°C";

        bool hasBattery = battery is not null;
        _batteryName.Visible = hasBattery;
        _batteryValue.Visible = hasBattery;
        if (battery is { } b)
            _batteryValue.Text = b.Charging ? $"{b.Percent}% (Charging)" : $"{b.Percent}%";
    }

    private void OnPlanSelected(object? sender, EventArgs e)
    {
        if (!_syncing && _planCombo.SelectedIndex >= 0)
            PlanActivated?.Invoke(_planIds[_planCombo.SelectedIndex]);
    }

    private static Label CreateNameLabel(string text, Point location) =>
        new()
        {
            Text = text,
            Location = location,
            AutoSize = true,
            ForeColor = DimText
        };

    private static Label CreateValueLabel(Point location) =>
        new()
        {
            Location = location,
            AutoSize = true,
            ForeColor = MainText,
            Text = "-"
        };
}
