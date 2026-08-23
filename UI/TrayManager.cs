namespace TrayBit.UI;

internal sealed class TrayManager : IDisposable
{
    private const int TooltipLimit = 63;

    private readonly NotifyIcon _trayIcon;

    public event EventHandler? ExitRequested;

    public TrayManager()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Exit", null, OnExit);

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

    private void OnExit(object? sender, EventArgs e) =>
        ExitRequested?.Invoke(this, EventArgs.Empty);

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
