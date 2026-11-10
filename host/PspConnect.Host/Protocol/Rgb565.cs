namespace PspConnect.Host.Protocol;

public static class Rgb565
{
    public static ushort FromColor(Color c) =>
        (ushort)((((c.R >> 3) & 0x1F) << 11) | (((c.G >> 2) & 0x3F) << 5) | ((c.B >> 3) & 0x1F));

    public static Color ToColor(ushort value)
    {
        var r = (byte)(((value >> 11) & 0x1F) << 3);
        var g = (byte)(((value >> 5) & 0x3F) << 2);
        var b = (byte)((value & 0x1F) << 3);
        return Color.FromArgb(r, g, b);
    }
}
