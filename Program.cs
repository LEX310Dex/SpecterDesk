using System.Diagnostics;
using System.Windows.Forms;

namespace GhostCursorV6;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--watchdog" && int.TryParse(args[1], out int pid))
        {
            try
            {
                using var main = Process.GetProcessById(pid);
                main.WaitForExit();
            }
            catch (ArgumentException) { /* main process already gone */ }
            catch (InvalidOperationException) { /* main process already gone */ }
            Native.Restore();
            return;
        }

        using var mutex = new Mutex(true, "Local\\GhostCursorV6SingleInstance", out bool first);
        if (!first)
        {
            MessageBox.Show("GhostCursor v0.6 is already running. Check the system tray.");
            return;
        }
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            var p = new ProcessStartInfo(Environment.ProcessPath!, "--watchdog " + Environment.ProcessId)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(p)?.Dispose();
        }
        catch { /* emergency restore still available in README */ }
        try { Application.Run(new GhostHub()); }
        finally { Native.Restore(); }
    }
}
