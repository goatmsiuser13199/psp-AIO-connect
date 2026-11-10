using PspConnect.Host.Sensors;
using PspConnect.Host.Ui;

namespace PspConnect.Host;

static class Program
{
    private const string MutexName = @"Local\PspConnect.Host.SingleInstance";
    private const string ShowEventName = @"Local\PspConnect.Host.ShowWindow";

    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            try
            {
                using var show = EventWaitHandle.OpenExisting(ShowEventName);
                show.Set();
            }
            catch
            {
            }

            return;
        }

        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);

        var mock = args.Any(a => a.Equals("--mock", StringComparison.OrdinalIgnoreCase));
        var noUsb = args.Any(a => a.Equals("--no-usb", StringComparison.OrdinalIgnoreCase));
        var interval = 750;
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals("--interval", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(args[i + 1], out var ms))
                interval = Math.Clamp(ms, 500, 5000);
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        var sampler = new HardwareSampler(mock);
        var form = new ControlPanelForm(sampler, enableUsb: !noUsb, intervalMs: interval);
        form.StartShowSignalWatcher(showEvent);
        Application.Run(form);
        GC.KeepAlive(mutex);
    }
}
