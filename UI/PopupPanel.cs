namespace TrayBit.UI;

internal sealed class PopupPanel : Form
{
    private static readonly Color PanelBack = Color.FromArgb(30, 30, 46);
    private static readonly Color ControlBack = Color.FromArgb(49, 50, 68);
    private static readonly Color MainText = Color.FromArgb(205, 214, 244);
    private static readonly Color DimText = Color.FromArgb(148, 152, 175);

    private readonly Label _temperatureValue;
    private readonly ComboBox _planCombo;
    private readonly List<Guid> _planIds = [];
    private bool _syncing;

    public event Action<Guid>? PlanActivated;

    public PopupPanel()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        BackColor = PanelBack;
        Font = new Font("Segoe UI", 9f);
        ClientSize = new Size(276, 112);

        _temperatureValue = new Label
        {
            Location = new Point(142, 18),
            AutoSize = true,
            ForeColor = MainText,
            Text = "N/A"
        };

        _planCombo = new ComboBox
        {
            Location = new Point(16, 62),
            Size = new Size(244, 26),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = ControlBack,
            ForeColor = MainText
        };
        _planCombo.SelectedIndexChanged += OnPlanSelected;

        Controls.Add(new Label
        {
            Text = "Temperature:",
            Location = new Point(16, 18),
            AutoSize = true,
            ForeColor = DimText
        });
        Controls.Add(_temperatureValue);
        Controls.Add(new Label
        {
            Text = "Energy profile:",
            Location = new Point(16, 42),
            AutoSize = true,
            ForeColor = DimText
        });
        Controls.Add(_planCombo);

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
            _planCombo.Enabled = plans.Count > 0;
        }
        finally
        {
            _planCombo.EndUpdate();
            _syncing = false;
        }
    }

    public void SetTemperature(float? temperature) =>
        _temperatureValue.Text = temperature is { } value ? $"{value:F0}°C" : "N/A";

    private void OnPlanSelected(object? sender, EventArgs e)
    {
        if (!_syncing && _planCombo.SelectedIndex >= 0)
            PlanActivated?.Invoke(_planIds[_planCombo.SelectedIndex]);
    }
}
