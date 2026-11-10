using LibUsbDotNet;
using LibUsbDotNet.Main;
using PspConnect.Host.Protocol;

namespace PspConnect.Host.Transport;

public sealed class UsbMetricsSink : IMetricsSink
{
    public const int Vid = 0x054C;
    public const int Pid = 0xC001;

    private UsbDevice? _device;
    private UsbEndpointWriter? _writer;
    private readonly object _gate = new();
    private int _failStreak;
    private uint _sentOk;

    public string Name => "USB";
    public uint SentOk => _sentOk;
    public string? LastError { get; private set; }

    public bool IsConnected
    {
        get
        {
            lock (_gate)
                return _device is { IsOpen: true } && _writer is not null;
        }
    }

    public bool TryConnect(out string status)
    {
        lock (_gate)
        {
            DisconnectUnlocked();
            LastError = null;
            _failStreak = 0;

            var finder = new UsbDeviceFinder(Vid, Pid);
            _device = UsbDevice.OpenUsbDevice(finder);
            if (_device is null)
            {
                status = $"PSP introuvable (VID={Vid:X4} PID={Pid:X4}). Homebrew + Zadig WinUSB.";
                LastError = status;
                return false;
            }

            if (_device is IUsbDevice whole)
            {
                whole.SetConfiguration(1);
                whole.ClaimInterface(0);
            }

            _writer = _device.OpenEndpointWriter(WriteEndpointID.Ep02, EndpointType.Bulk);
            status = $"USB OK · envoi vers {Vid:X4}:{Pid:X4}";
            return true;
        }
    }

    public bool Send(in MetricsPacket packet)
    {
        lock (_gate)
        {
            if (_writer is null)
                return false;

            var bytes = packet.ToBytes();
            var ec = _writer.Write(bytes, 2000, out var written);
            if (ec == ErrorCode.Ok && written == bytes.Length)
            {
                _failStreak = 0;
                _sentOk++;
                LastError = null;
                return true;
            }

            _failStreak++;
            LastError = $"Write {ec} ({written}/{bytes.Length})";
            if (_failStreak >= 8)
                DisconnectUnlocked();
            return false;
        }
    }

    public void Dispose()
    {
        lock (_gate)
            DisconnectUnlocked();
        UsbDevice.Exit();
    }

    private void DisconnectUnlocked()
    {
        try
        {
            if (_device is IUsbDevice whole)
                whole.ReleaseInterface(0);
        }
        catch
        {
        }

        _writer = null;
        _device?.Close();
        _device = null;
    }
}
