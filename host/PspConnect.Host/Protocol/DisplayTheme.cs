using System.Text.Json;

namespace PspConnect.Host.Protocol;

public sealed class DisplayTheme
{
    public DisplayFlags Flags { get; set; } = DisplayFlags.Default;
    public Color Background { get; set; } = Color.FromArgb(10, 12, 16);
    public Color Accent { get; set; } = Color.FromArgb(64, 210, 190);
    public Color Text { get; set; } = Color.FromArgb(236, 242, 250);
    public Color Muted { get; set; } = Color.FromArgb(120, 132, 148);
    public Color CpuBar { get; set; } = Color.FromArgb(94, 176, 255);
    public Color GpuBar { get; set; } = Color.FromArgb(72, 214, 168);
    public Color VramBar { get; set; } = Color.FromArgb(180, 140, 255);

    private static string StorePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "psp-connect", "theme.json");

    public DisplayTheme Clone() => new()
    {
        Flags = Flags,
        Background = Background,
        Accent = Accent,
        Text = Text,
        Muted = Muted,
        CpuBar = CpuBar,
        GpuBar = GpuBar,
        VramBar = VramBar
    };

    public void Save()
    {
        Flags &= ~(DisplayFlags.ShowDisk | DisplayFlags.ShowFps);
        var dir = Path.GetDirectoryName(StorePath)!;
        Directory.CreateDirectory(dir);
        var dto = ThemeDto.From(this);
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(StorePath, json);
    }

    public static DisplayTheme LoadOrDefault()
    {
        try
        {
            if (!File.Exists(StorePath))
                return new DisplayTheme();
            var json = File.ReadAllText(StorePath);
            var dto = JsonSerializer.Deserialize<ThemeDto>(json);
            var theme = dto?.ToTheme() ?? new DisplayTheme();
            theme.Flags &= ~(DisplayFlags.ShowDisk | DisplayFlags.ShowFps);
            return theme;
        }
        catch
        {
            return new DisplayTheme();
        }
    }

    private sealed class ThemeDto
    {
        public int Flags { get; set; }
        public int Background { get; set; }
        public int Accent { get; set; }
        public int Text { get; set; }
        public int Muted { get; set; }
        public int CpuBar { get; set; }
        public int GpuBar { get; set; }
        public int VramBar { get; set; }

        public static ThemeDto From(DisplayTheme t) => new()
        {
            Flags = (int)t.Flags,
            Background = t.Background.ToArgb(),
            Accent = t.Accent.ToArgb(),
            Text = t.Text.ToArgb(),
            Muted = t.Muted.ToArgb(),
            CpuBar = t.CpuBar.ToArgb(),
            GpuBar = t.GpuBar.ToArgb(),
            VramBar = t.VramBar.ToArgb()
        };

        public DisplayTheme ToTheme() => new()
        {
            Flags = (DisplayFlags)Flags,
            Background = Color.FromArgb(Background),
            Accent = Color.FromArgb(Accent),
            Text = Color.FromArgb(Text),
            Muted = Color.FromArgb(Muted),
            CpuBar = Color.FromArgb(CpuBar),
            GpuBar = Color.FromArgb(GpuBar),
            VramBar = VramBar != 0 ? Color.FromArgb(VramBar) : Color.FromArgb(180, 140, 255)
        };
    }
}
