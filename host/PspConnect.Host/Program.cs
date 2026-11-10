using PspConnect.Host.Sensors;
using PspConnect.Host.Ui;

namespace PspConnect.Host;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
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
        Application.Run(new ControlPanelForm(sampler, enableUsb: !noUsb, intervalMs: interval));
    }
}
