namespace PspConnect.Host.Protocol;

[Flags]
public enum DisplayFlags : ushort
{
    None = 0,
    ShowCpu = 1 << 0,
    ShowGpu = 1 << 1,
    ShowRam = 1 << 2,
    ShowVram = 1 << 3,
    ShowDisk = 1 << 4,
    ShowFps = 1 << 5,
    ShowTemp = 1 << 6,
    Default = ShowCpu | ShowGpu | ShowRam | ShowVram | ShowTemp
}
