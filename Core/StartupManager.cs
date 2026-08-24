using Microsoft.Win32;

namespace TrayBit.Core;

internal static class StartupManager
{
    private const string RunKeyName = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TrayBit";

    public static bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyName);
        return key?.GetValue(AppName) is not null;
    }

    public static void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyName, writable: true);

        if (enabled)
        {
            string exePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Cannot determine executable path.");

            key.SetValue(AppName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
