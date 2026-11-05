using TrayBit.Helpers;

namespace TrayBit.UI;

internal sealed class TaskbarStatusPanel : Form
{
    private static readonly Color KeyColor = Color.FromArgb(1, 2, 3);
    private readonly Label _statusLabel;

    public TaskbarStatusPanel()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = KeyColor;
        TransparencyKey = KeyColor;
        Font = new Font("Segoe UI", 10f, FontStyle.Bold);

        _statusLabel = new Label
        {
            AutoSize = true,
            BackColor = KeyColor,
            ForeColor = Color.White,
            Text = "N/A | Unknown"
        };

        Controls.Add(_statusLabel);
    }

    public void UpdateStatus(string status)
    {
        _statusLabel.Text = status;
        PositionOnTaskbar();

        if (!Visible)
            Show();
    }

    private void PositionOnTaskbar()
    {
        IntPtr taskbar = NativeMethods.FindWindow("Shell_TrayWnd", null);

        if (taskbar == IntPtr.Zero || !NativeMethods.GetWindowRect(taskbar, out NativeMethods.RECT rect))
            return;

        int taskbarHeight = rect.Bottom - rect.Top;
        Location = new Point(rect.Left + 64, rect.Top + Math.Max((taskbarHeight - Height) / 2, 0));
    }

    protected override bool ShowWithoutActivation => true;

    protected override void WndProc(ref Message m)
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_NOACTIVATE = 0x0003;

        if (m.Msg == WM_MOUSEACTIVATE)
        {
            m.Result = (IntPtr)MA_NOACTIVATE;
            return;
        }

        base.WndProc(ref m);
    }
}
