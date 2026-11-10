using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using PspConnect.Host.Protocol;

namespace PspConnect.Host.Sensors;

public sealed class HardwareSampler : IDisposable
{
    private readonly Computer? _computer;
    private readonly UpdateVisitor _visitor = new();
    private readonly bool _mock;
    private readonly Random _rng = new();
    private uint _seq;
    private int _cpuTempRank = -1;

    public HardwareSampler(bool mock)
    {
        _mock = mock;
        if (_mock)
            return;

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = false,
            IsStorageEnabled = false,
            IsMotherboardEnabled = false,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsBatteryEnabled = false,
            IsPsuEnabled = false
        };
        _computer.Open();
        _computer.Accept(_visitor);
        _computer.Accept(_visitor);
    }

    public MetricsPacket Sample()
    {
        var packet = MetricsPacket.CreateEmpty(++_seq);
        if (_mock)
        {
            packet.CpuLoad = 20f + (float)_rng.NextDouble() * 55f;
            packet.CpuTempC = 45f + (float)_rng.NextDouble() * 25f;
            packet.GpuLoad = 10f + (float)_rng.NextDouble() * 70f;
            packet.GpuTempC = 40f + (float)_rng.NextDouble() * 30f;
            packet.RamUsedMb = 12000f + (float)_rng.NextDouble() * 4000f;
            packet.RamTotalMb = 32768f;
            packet.VramUsedMb = 1800f + (float)_rng.NextDouble() * 2200f;
            packet.VramTotalMb = 8192f;
            return packet;
        }

        _computer!.Accept(_visitor);

        float? cpuLoad = null;
        float? cpuTemp = null;
        float? gpuLoad = null;
        float? gpuTemp = null;
        float? vramUsed = null;
        float? vramTotal = null;
        _cpuTempRank = -1;

        foreach (var hardware in _computer.Hardware)
            Walk(hardware);

        void Walk(IHardware hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (!sensor.Value.HasValue)
                    continue;

                var value = sensor.Value.Value;
                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                            cpuLoad ??= value;
                        if (sensor.SensorType == SensorType.Temperature)
                            PreferCpuTemp(ref cpuTemp, sensor.Name, value);
                        break;

                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        if (sensor.SensorType == SensorType.Load &&
                            (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                             sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase) ||
                             sensor.Name.Contains("D3D", StringComparison.OrdinalIgnoreCase)))
                            gpuLoad ??= value;
                        if (sensor.SensorType == SensorType.Temperature &&
                            (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                             sensor.Name.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase) ||
                             sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase)))
                            gpuTemp ??= value;
                        if (sensor.SensorType == SensorType.SmallData &&
                            sensor.Name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase))
                            vramUsed ??= value;
                        if (sensor.SensorType == SensorType.SmallData &&
                            sensor.Name.Contains("Memory Total", StringComparison.OrdinalIgnoreCase))
                            vramTotal ??= value;
                        break;
                }
            }

            foreach (var sub in hardware.SubHardware)
                Walk(sub);
        }

        ReadPhysicalRam(out var ramUsedMb, out var ramTotalMb);

        packet.CpuLoad = cpuLoad ?? MetricsPacket.Na;
        packet.CpuTempC = cpuTemp ?? MetricsPacket.Na;
        packet.GpuLoad = gpuLoad ?? MetricsPacket.Na;
        packet.GpuTempC = gpuTemp ?? MetricsPacket.Na;
        packet.RamUsedMb = ramUsedMb;
        packet.RamTotalMb = ramTotalMb;
        packet.VramUsedMb = vramUsed ?? MetricsPacket.Na;
        packet.VramTotalMb = vramTotal ?? MetricsPacket.Na;
        return packet;
    }

    private void PreferCpuTemp(ref float? cpuTemp, string name, float value)
    {
        if (value < 1f || value > 125f)
            return;

        var rank = name.Contains("Package", StringComparison.OrdinalIgnoreCase) ? 100
            : name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ? 95
            : name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ? 90
            : name.Contains("Core Average", StringComparison.OrdinalIgnoreCase) ? 80
            : name.Contains("Core Max", StringComparison.OrdinalIgnoreCase) ? 70
            : name.Contains("CCD", StringComparison.OrdinalIgnoreCase) ? 60
            : name.Contains("Distance to TjMax", StringComparison.OrdinalIgnoreCase) ? -1
            : name.Contains("Core #", StringComparison.OrdinalIgnoreCase) ? 40
            : -1;
        if (rank < 0)
            return;

        if (cpuTemp is null || rank > _cpuTempRank || (rank == _cpuTempRank && value > cpuTemp.Value))
        {
            cpuTemp = value;
            _cpuTempRank = rank;
        }
    }

    private static void ReadPhysicalRam(out float usedMb, out float totalMb)
    {
        usedMb = MetricsPacket.Na;
        totalMb = MetricsPacket.Na;

        var status = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref status) || status.TotalPhys == 0)
            return;

        totalMb = status.TotalPhys / (1024f * 1024f);
        usedMb = (status.TotalPhys - status.AvailPhys) / (1024f * 1024f);
    }

    public void Dispose()
    {
        _computer?.Close();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
                sub.Accept(this);
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
