using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace PspConnect.Host.Protocol;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MetricsPacket
{
    public const uint MagicValue = 0x43505350;
    public const ushort VersionValue = 3;
    public const int Size = 64;
    public const float Na = -1f;

    public uint Magic;
    public ushort Version;
    public ushort Flags;
    public uint Seq;
    public uint TimestampMs;
    public float CpuLoad;
    public float CpuTempC;
    public float GpuLoad;
    public float GpuTempC;
    public float RamUsedMb;
    public float RamTotalMb;
    public float VramUsedMb;
    public float VramTotalMb;
    public ushort ColorCpu;
    public ushort ColorGpu;
    public ushort ColorVram;
    public ushort Reserved;
    public ushort ColorBg;
    public ushort ColorAccent;
    public ushort ColorText;
    public ushort ColorMuted;

    public DisplayFlags DisplayFlags
    {
        get => (DisplayFlags)Flags;
        set => Flags = (ushort)value;
    }

    public static MetricsPacket CreateEmpty(uint seq)
    {
        return new MetricsPacket
        {
            Magic = MagicValue,
            Version = VersionValue,
            Flags = (ushort)DisplayFlags.Default,
            Seq = seq,
            TimestampMs = (uint)Environment.TickCount64,
            CpuLoad = Na,
            CpuTempC = Na,
            GpuLoad = Na,
            GpuTempC = Na,
            RamUsedMb = Na,
            RamTotalMb = Na,
            VramUsedMb = Na,
            VramTotalMb = Na,
            ColorCpu = Rgb565.FromColor(Color.FromArgb(94, 176, 255)),
            ColorGpu = Rgb565.FromColor(Color.FromArgb(72, 214, 168)),
            ColorVram = Rgb565.FromColor(Color.FromArgb(180, 140, 255)),
            ColorBg = Rgb565.FromColor(Color.FromArgb(10, 12, 16)),
            ColorAccent = Rgb565.FromColor(Color.FromArgb(64, 210, 190)),
            ColorText = Rgb565.FromColor(Color.FromArgb(236, 242, 250)),
            ColorMuted = Rgb565.FromColor(Color.FromArgb(120, 132, 148))
        };
    }

    public void ApplyTheme(DisplayTheme theme)
    {
        DisplayFlags = theme.Flags & ~(DisplayFlags.ShowDisk | DisplayFlags.ShowFps);
        ColorBg = Rgb565.FromColor(theme.Background);
        ColorAccent = Rgb565.FromColor(theme.Accent);
        ColorText = Rgb565.FromColor(theme.Text);
        ColorMuted = Rgb565.FromColor(theme.Muted);
        ColorCpu = Rgb565.FromColor(theme.CpuBar);
        ColorGpu = Rgb565.FromColor(theme.GpuBar);
        ColorVram = Rgb565.FromColor(theme.VramBar);
    }

    public byte[] ToBytes()
    {
        var buffer = new byte[Size];
        WriteTo(buffer);
        return buffer;
    }

    public void WriteTo(Span<byte> destination)
    {
        if (destination.Length < Size)
            throw new ArgumentException("Buffer too small.", nameof(destination));

        BinaryPrimitives.WriteUInt32LittleEndian(destination[0..], Magic);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[4..], Version);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[6..], Flags);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[8..], Seq);
        BinaryPrimitives.WriteUInt32LittleEndian(destination[12..], TimestampMs);
        BinaryPrimitives.WriteSingleLittleEndian(destination[16..], CpuLoad);
        BinaryPrimitives.WriteSingleLittleEndian(destination[20..], CpuTempC);
        BinaryPrimitives.WriteSingleLittleEndian(destination[24..], GpuLoad);
        BinaryPrimitives.WriteSingleLittleEndian(destination[28..], GpuTempC);
        BinaryPrimitives.WriteSingleLittleEndian(destination[32..], RamUsedMb);
        BinaryPrimitives.WriteSingleLittleEndian(destination[36..], RamTotalMb);
        BinaryPrimitives.WriteSingleLittleEndian(destination[40..], VramUsedMb);
        BinaryPrimitives.WriteSingleLittleEndian(destination[44..], VramTotalMb);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[48..], ColorCpu);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[50..], ColorGpu);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[52..], ColorVram);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[54..], Reserved);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[56..], ColorBg);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[58..], ColorAccent);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[60..], ColorText);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[62..], ColorMuted);
    }
}
