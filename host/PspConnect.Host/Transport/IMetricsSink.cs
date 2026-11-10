using PspConnect.Host.Protocol;

namespace PspConnect.Host.Transport;

public interface IMetricsSink : IDisposable
{
    string Name { get; }
    bool IsConnected { get; }
    bool Send(in MetricsPacket packet);
}
