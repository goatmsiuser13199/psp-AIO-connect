using System.Text.Json;

namespace PspConnect.Host.Protocol;

public sealed class ThemeProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Profil";
    public DisplayTheme Theme { get; set; } = new();
}

public sealed class ThemeProfileStore
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private static string Dir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "psp-connect");

    private static string StorePath => Path.Combine(Dir, "profiles.json");

    public string? ActiveId { get; private set; }
    public List<ThemeProfile> Profiles { get; } = new();

    public ThemeProfile? Active =>
        Profiles.FirstOrDefault(p => p.Id == ActiveId) ?? Profiles.FirstOrDefault();

    public static ThemeProfileStore Load()
    {
        var store = new ThemeProfileStore();
        try
        {
            if (File.Exists(StorePath))
            {
                var dto = JsonSerializer.Deserialize<StoreDto>(File.ReadAllText(StorePath));
                if (dto?.Profiles is { Count: > 0 })
                {
                    foreach (var p in dto.Profiles)
                    {
                        var theme = p.ToTheme();
                        theme.Flags &= ~(DisplayFlags.ShowDisk | DisplayFlags.ShowFps);
                        store.Profiles.Add(new ThemeProfile
                        {
                            Id = string.IsNullOrWhiteSpace(p.Id) ? Guid.NewGuid().ToString("N") : p.Id,
                            Name = string.IsNullOrWhiteSpace(p.Name) ? "Profil" : p.Name.Trim(),
                            Theme = theme
                        });
                    }
                    store.ActiveId = dto.ActiveId;
                    if (store.Active is null && store.Profiles.Count > 0)
                        store.ActiveId = store.Profiles[0].Id;
                    return store;
                }
            }
        }
        catch
        {
        }

        var legacy = DisplayTheme.LoadOrDefault();
        var first = new ThemeProfile { Name = "Defaut", Theme = legacy };
        store.Profiles.Add(first);
        store.ActiveId = first.Id;
        store.Persist();
        return store;
    }

    public DisplayTheme CurrentTheme => Active?.Theme.Clone() ?? new DisplayTheme();

    public ThemeProfile SaveAs(string name, DisplayTheme theme)
    {
        name = NormalizeName(name);
        var existing = Profiles.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Theme = Sanitize(theme);
            ActiveId = existing.Id;
            Persist();
            return existing;
        }

        var created = new ThemeProfile
        {
            Name = name,
            Theme = Sanitize(theme)
        };
        Profiles.Add(created);
        ActiveId = created.Id;
        Persist();
        return created;
    }

    public void UpdateActive(DisplayTheme theme)
    {
        var active = Active;
        if (active is null)
        {
            SaveAs("Defaut", theme);
            return;
        }

        active.Theme = Sanitize(theme);
        Persist();
    }

    public bool Select(string id)
    {
        var profile = Profiles.FirstOrDefault(p => p.Id == id);
        if (profile is null)
            return false;
        ActiveId = profile.Id;
        Persist();
        return true;
    }

    public bool Rename(string id, string newName)
    {
        var profile = Profiles.FirstOrDefault(p => p.Id == id);
        if (profile is null)
            return false;

        newName = NormalizeName(newName);
        if (Profiles.Any(p => p.Id != id &&
                              string.Equals(p.Name, newName, StringComparison.OrdinalIgnoreCase)))
            return false;

        profile.Name = newName;
        Persist();
        return true;
    }

    public bool Delete(string id)
    {
        var profile = Profiles.FirstOrDefault(p => p.Id == id);
        if (profile is null)
            return false;

        Profiles.Remove(profile);
        if (Profiles.Count == 0)
        {
            var fallback = new ThemeProfile { Name = "Defaut", Theme = new DisplayTheme() };
            Profiles.Add(fallback);
            ActiveId = fallback.Id;
        }
        else if (ActiveId == id)
        {
            ActiveId = Profiles[0].Id;
        }

        Persist();
        return true;
    }

    public void Persist()
    {
        Directory.CreateDirectory(Dir);
        var dto = new StoreDto
        {
            ActiveId = ActiveId,
            Profiles = Profiles.Select(ProfileDto.From).ToList()
        };
        File.WriteAllText(StorePath, JsonSerializer.Serialize(dto, JsonOpts));

        var active = Active;
        if (active is not null)
            active.Theme.Save();
    }

    private static DisplayTheme Sanitize(DisplayTheme theme)
    {
        var copy = theme.Clone();
        copy.Flags &= ~(DisplayFlags.ShowDisk | DisplayFlags.ShowFps);
        return copy;
    }

    private static string NormalizeName(string name)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            name = "Profil";
        if (name.Length > 32)
            name = name[..32];
        return name;
    }

    private sealed class StoreDto
    {
        public string? ActiveId { get; set; }
        public List<ProfileDto> Profiles { get; set; } = new();
    }

    private sealed class ProfileDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Flags { get; set; }
        public int Background { get; set; }
        public int Accent { get; set; }
        public int Text { get; set; }
        public int Muted { get; set; }
        public int CpuBar { get; set; }
        public int GpuBar { get; set; }
        public int VramBar { get; set; }

        public static ProfileDto From(ThemeProfile p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Flags = (int)p.Theme.Flags,
            Background = p.Theme.Background.ToArgb(),
            Accent = p.Theme.Accent.ToArgb(),
            Text = p.Theme.Text.ToArgb(),
            Muted = p.Theme.Muted.ToArgb(),
            CpuBar = p.Theme.CpuBar.ToArgb(),
            GpuBar = p.Theme.GpuBar.ToArgb(),
            VramBar = p.Theme.VramBar.ToArgb()
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
            VramBar = VramBar != 0 ? Color.FromArgb(VramBar) : Color.FromArgb(170, 130, 200)
        };
    }
}
