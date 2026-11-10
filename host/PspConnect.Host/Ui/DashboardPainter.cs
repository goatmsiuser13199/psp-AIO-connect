using System.Drawing.Drawing2D;
using PspConnect.Host.Protocol;

namespace PspConnect.Host.Ui;

public static class DashboardPainter
{
    public const int Width = 480;
    public const int Height = 272;
    private const int SparkN = 56;

    private static readonly Dictionary<string, Queue<float>> History = new();

    public static void Paint(Graphics g, in MetricsPacket packet, DisplayTheme theme, string transportStatus)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.Clear(theme.Background);

        var flags = theme.Flags & ~(DisplayFlags.ShowDisk | DisplayFlags.ShowFps);
        const int gap = 10;
        const int margin = 12;
        var cellW = (Width - margin * 2 - gap) / 2;
        var cellH = (Height - margin * 2 - gap) / 2;

        var tiles = new (bool on, string tag, string value, string unit, string sub, float pct, Color fill, Rectangle bounds)[]
        {
            (flags.HasFlag(DisplayFlags.ShowCpu), "CPU",
                FormatLoad(packet.CpuLoad), "%",
                flags.HasFlag(DisplayFlags.ShowTemp) ? FormatTemp(packet.CpuTempC) : "",
                packet.CpuLoad, theme.CpuBar,
                new Rectangle(margin, margin, cellW, cellH)),

            (flags.HasFlag(DisplayFlags.ShowGpu), "GPU",
                FormatLoad(packet.GpuLoad), "%",
                flags.HasFlag(DisplayFlags.ShowTemp) ? FormatTemp(packet.GpuTempC) : "",
                packet.GpuLoad, theme.GpuBar,
                new Rectangle(margin + cellW + gap, margin, cellW, cellH)),

            (flags.HasFlag(DisplayFlags.ShowRam), "RAM",
                FormatGo(packet.RamUsedMb), "Go",
                packet.RamTotalMb >= 0 ? $"{packet.RamTotalMb / 1024f:0} Go" : "",
                packet.RamUsedMb >= 0 && packet.RamTotalMb > 0 ? packet.RamUsedMb / packet.RamTotalMb * 100f : MetricsPacket.Na,
                theme.Accent,
                new Rectangle(margin, margin + cellH + gap, cellW, cellH)),

            (flags.HasFlag(DisplayFlags.ShowVram), "VRAM",
                FormatGo(packet.VramUsedMb), "Go",
                packet.VramTotalMb >= 0 ? $"{packet.VramTotalMb / 1024f:0} Go" : "",
                packet.VramUsedMb >= 0 && packet.VramTotalMb > 0 ? packet.VramUsedMb / packet.VramTotalMb * 100f : MetricsPacket.Na,
                theme.VramBar,
                new Rectangle(margin + cellW + gap, margin + cellH + gap, cellW, cellH)),
        };

        using var tagFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        using var valueFont = new Font("Segoe UI", 28f, FontStyle.Bold);
        using var unitFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var subFont = new Font("Segoe UI", 11f, FontStyle.Bold);

        foreach (var tile in tiles)
        {
            DrawFrame(g, tile.bounds, tile.on ? tile.fill : theme.Muted, theme.Background);
            if (!tile.on)
                continue;

            PushHistory(tile.tag, tile.pct);
            DrawTile(g, tile.bounds, tile.tag, tile.value, tile.unit, tile.sub, tile.pct,
                tile.fill, theme.Background, theme.Text, theme.Muted,
                tagFont, valueFont, unitFont, subFont);
        }

        _ = transportStatus;
    }

    private static void DrawFrame(Graphics g, Rectangle bounds, Color fill, Color bg)
    {
        var panel = Color.FromArgb(255, Mix(bg, Color.White, 0.06f));
        using (var brush = new SolidBrush(panel))
            g.FillRectangle(brush, bounds);
        using (var accent = new SolidBrush(fill))
            g.FillRectangle(accent, bounds.X, bounds.Y, 3, bounds.Height);
    }

    private static void DrawTile(
        Graphics g, Rectangle bounds, string tag, string value, string unit, string sub, float pct,
        Color fill, Color bg, Color text, Color muted,
        Font tagFont, Font valueFont, Font unitFont, Font subFont)
    {
        using var mutedBrush = new SolidBrush(Color.FromArgb(220, muted));
        using var textBrush = new SolidBrush(text);
        using var softBrush = new SolidBrush(Darken(fill, 0.2f));

        g.DrawString(tag, tagFont, mutedBrush, bounds.X + 14, bounds.Y + 10);
        if (!string.IsNullOrEmpty(sub))
        {
            var subSize = g.MeasureString(sub, subFont);
            g.DrawString(sub, subFont, softBrush, bounds.Right - 14 - subSize.Width, bounds.Y + 10);
        }

        var valuePos = new PointF(bounds.X + 14, bounds.Y + 36);
        g.DrawString(value, valueFont, textBrush, valuePos);
        var valueSize = g.MeasureString(value, valueFont);
        g.DrawString(unit, unitFont, mutedBrush,
            valuePos.X + valueSize.Width - 2,
            valuePos.Y + valueSize.Height - 24);

        var spark = new Rectangle(bounds.X + 12, bounds.Bottom - 40, bounds.Width - 24, 16);
        DrawSpark(g, spark, tag, Darken(fill, 0.15f));

        var meter = new Rectangle(bounds.X + 12, bounds.Bottom - 12, bounds.Width - 24, 4);
        using (var track = new SolidBrush(Color.FromArgb(255, Mix(bg, Color.White, 0.1f))))
            g.FillRectangle(track, meter);
        if (pct >= 0)
        {
            var w = (int)Math.Round(meter.Width * (Math.Clamp(pct, 0f, 100f) / 100f));
            if (w > 0)
            {
                using var fillBrush = new SolidBrush(fill);
                g.FillRectangle(fillBrush, meter.X, meter.Y, w, meter.Height);
            }
        }
    }

    private static void DrawSpark(Graphics g, Rectangle bounds, string key, Color color)
    {
        if (!History.TryGetValue(key, out var q) || q.Count < 2)
            return;

        var values = q.ToArray();
        using var pen = new Pen(Color.FromArgb(180, color), 1.5f)
        {
            LineJoin = LineJoin.Round,
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };

        var points = new PointF[values.Length];
        for (var i = 0; i < values.Length; i++)
        {
            var t = values.Length == 1 ? 0f : i / (float)(values.Length - 1);
            var x = bounds.X + t * (bounds.Width - 1);
            var y = bounds.Bottom - 1 - (Math.Clamp(values[i], 0f, 100f) / 100f) * (bounds.Height - 1);
            points[i] = new PointF(x, y);
        }

        g.DrawLines(pen, points);
    }

    private static void PushHistory(string key, float pct)
    {
        if (!History.TryGetValue(key, out var q))
        {
            q = new Queue<float>(SparkN);
            History[key] = q;
        }

        q.Enqueue(pct < 0 ? 0 : Math.Clamp(pct, 0f, 100f));
        while (q.Count > SparkN)
            q.Dequeue();
    }

    private static Color Mix(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return Color.FromArgb(
            (int)(a.A + (b.A - a.A) * t),
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    private static Color Darken(Color c, float amount) => Mix(c, Color.Black, amount);

    private static string FormatLoad(float value) => value < 0 ? "--" : $"{value:0}";
    private static string FormatTemp(float value) => value < 0 ? "" : $"{value:0}C";
    private static string FormatGo(float usedMb) => usedMb < 0 ? "--" : usedMb >= 10240f ? $"{usedMb / 1024f:0}" : $"{usedMb / 1024f:0.0}";
}
