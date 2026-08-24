using TrayBit.UI;

namespace TrayBit;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using (new Mutex(true, @"Local\TrayBit.SingleInstance", out bool createdNew))
        {
            if (!createdNew)
                return;

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayAppContext());
        }
    }
}
