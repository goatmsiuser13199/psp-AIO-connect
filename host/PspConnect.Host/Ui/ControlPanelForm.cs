using PspConnect.Host.Protocol;
using PspConnect.Host.Sensors;
using PspConnect.Host.Transport;

namespace PspConnect.Host.Ui;

public sealed class ControlPanelForm : Form
{
    private readonly HardwareSampler _sampler;
    private readonly UsbMetricsSink? _usb;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly PictureBox _preview;
    private readonly Bitmap _frame;
    private readonly Label _statusDot;
    private readonly Label _statusLabel;
    private readonly DisplayTheme _theme;
    private readonly bool _enableUsb;
    private readonly Dictionary<DisplayFlags, Button> _metricChips = new();
    private readonly Dictionary<string, Panel> _swatches = new();
    private readonly int _intervalMs;
    private MetricsPacket _latest = MetricsPacket.CreateEmpty(0);
    private string _transportStatus = "USB en attente";
    private int _usbRetryTicks;
    private readonly NotifyIcon _tray;
    private bool _exitRequested;
    private bool _hiddenToTray;
    private Thread? _showWatcher;
    private volatile bool _watchShow;

    public ControlPanelForm(HardwareSampler sampler, bool enableUsb, int intervalMs)
    {
        _sampler = sampler;
        _enableUsb = enableUsb;
        _intervalMs = Math.Clamp(intervalMs, 500, 5000);
        _theme = DisplayTheme.LoadOrDefault();
        _theme.Flags &= ~(DisplayFlags.ShowDisk | DisplayFlags.ShowFps);
        if (_enableUsb)
            _usb = new UsbMetricsSink();

        Text = "psp-connect";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(10, 12, 16);
        ForeColor = Color.FromArgb(236, 242, 250);
        Font = new Font("Segoe UI", 9f);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        Icon = LoadAppIcon();
        _tray = CreateTrayIcon();

        var side = new Panel
        {
            Left = 16,
            Top = 16,
            Width = 248,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.FromArgb(16, 18, 24),
            Padding = new Padding(0, 0, 0, 14)
        };
        Controls.Add(side);

        var y = 14;
        side.Controls.Add(SectionLabel("Ecran", 14, y, 220));
        y += 26;
        y = AddMetricChip(side, y, "CPU", DisplayFlags.ShowCpu);
        y = AddMetricChip(side, y, "GPU", DisplayFlags.ShowGpu);
        y = AddMetricChip(side, y, "RAM", DisplayFlags.ShowRam);
        y = AddMetricChip(side, y, "VRAM", DisplayFlags.ShowVram);
        y += 16;

        side.Controls.Add(SectionLabel("Theme", 14, y, 220));
        y += 26;
        y = AddSwatch(side, y, "Fond", () => _theme.Background, c => _theme.Background = c);
        y = AddSwatch(side, y, "Accent", () => _theme.Accent, c => _theme.Accent = c);
        y = AddSwatch(side, y, "Texte", () => _theme.Text, c => _theme.Text = c);
        y = AddSwatch(side, y, "Mute", () => _theme.Muted, c => _theme.Muted = c);
        y = AddSwatch(side, y, "CPU", () => _theme.CpuBar, c => _theme.CpuBar = c);
        y = AddSwatch(side, y, "GPU", () => _theme.GpuBar, c => _theme.GpuBar = c);
        y = AddSwatch(side, y, "VRAM", () => _theme.VramBar, c => _theme.VramBar = c);
        y += 14;

        var saveBtn = QuietButton(14, y, 106, "Sauver");
        saveBtn.Click += (_, _) =>
        {
            _theme.Save();
            _statusLabel!.Text = "Theme enregistre";
        };
        side.Controls.Add(saveBtn);

        var presetBtn = QuietButton(128, y, 106, "Studio");
        presetBtn.Click += (_, _) => ApplyStudioPreset();
        side.Controls.Add(presetBtn);

        // Force layout so AutoSize side reports final height.
        side.PerformLayout();
        var sideHeight = Math.Max(side.PreferredSize.Height, y + 42);
        side.AutoSize = false;
        side.Height = sideHeight;

        var previewFrame = new Panel
        {
            Left = 280,
            Top = 16,
            Width = DashboardPainter.Width + 8,
            Height = DashboardPainter.Height + 8,
            BackColor = Color.FromArgb(28, 32, 40)
        };
        Controls.Add(previewFrame);

        _frame = new Bitmap(DashboardPainter.Width, DashboardPainter.Height);
        _preview = new PictureBox
        {
            Left = 4,
            Top = 4,
            Width = DashboardPainter.Width,
            Height = DashboardPainter.Height,
            BorderStyle = BorderStyle.None,
            Image = _frame,
            SizeMode = PictureBoxSizeMode.Normal
        };
        previewFrame.Controls.Add(_preview);

        var statusTop = Math.Max(previewFrame.Bottom, side.Bottom) + 12;
        _statusDot = new Label
        {
            Left = 280,
            Top = statusTop + 4,
            Width = 10,
            Height = 10,
            BackColor = Color.FromArgb(120, 132, 148),
            Text = ""
        };
        Controls.Add(_statusDot);

        _statusLabel = new Label
        {
            Left = 298,
            Top = statusTop,
            Width = 460,
            Height = 22,
            Text = "USB en attente",
            ForeColor = Color.FromArgb(150, 160, 175),
            Font = new Font("Segoe UI", 8.5f)
        };
        Controls.Add(_statusLabel);

        ClientSize = new Size(
            previewFrame.Right + 16,
            Math.Max(statusTop + 36, side.Bottom + 16));

        MigrateLegacyTheme();
        RefreshSwatches();

        _timer = new System.Windows.Forms.Timer { Interval = _intervalMs };
        _timer.Tick += OnTick;
        _timer.Start();

        Shown += (_, _) => TryUsb();
        FormClosing += OnFormClosing;
        FormClosed += (_, _) => Cleanup();
        Resize += OnResizeToTray;
    }

    private static Label SectionLabel(string text, int x, int y, int w) => new()
    {
        Left = x,
        Top = y,
        Width = w,
        Height = 18,
        Text = text.ToUpperInvariant(),
        ForeColor = Color.FromArgb(110, 120, 135),
        Font = new Font("Segoe UI", 8f, FontStyle.Bold)
    };

    private static Button QuietButton(int x, int y, int w, string text)
    {
        var btn = new Button
        {
            Left = x,
            Top = y,
            Width = w,
            Height = 28,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(28, 32, 40),
            ForeColor = Color.FromArgb(220, 228, 236),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private int AddMetricChip(Control parent, int y, string label, DisplayFlags flag)
    {
        var on = _theme.Flags.HasFlag(flag);
        var chip = new Button
        {
            Left = 14,
            Top = y,
            Width = 220,
            Height = 28,
            Text = label,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Cursor = Cursors.Hand,
            Tag = flag
        };
        StyleChip(chip, on);
        chip.Click += (_, _) =>
        {
            if (_theme.Flags.HasFlag(flag))
                _theme.Flags &= ~flag;
            else
                _theme.Flags |= flag;
            StyleChip(chip, _theme.Flags.HasFlag(flag));
        };
        _metricChips[flag] = chip;
        parent.Controls.Add(chip);
        return y + 32;
    }

    private static void StyleChip(Button chip, bool on)
    {
        chip.FlatAppearance.BorderSize = 1;
        chip.FlatAppearance.BorderColor = on ? Color.FromArgb(64, 210, 190) : Color.FromArgb(40, 46, 56);
        chip.BackColor = on ? Color.FromArgb(24, 36, 40) : Color.FromArgb(20, 24, 30);
        chip.ForeColor = on ? Color.FromArgb(220, 240, 235) : Color.FromArgb(130, 140, 155);
    }

    private int AddSwatch(Control parent, int y, string key, Func<Color> get, Action<Color> set)
    {
        var row = new Panel { Left = 14, Top = y, Width = 220, Height = 24, BackColor = Color.Transparent };
        var label = new Label
        {
            Left = 0,
            Top = 3,
            Width = 70,
            Height = 18,
            Text = key,
            ForeColor = Color.FromArgb(160, 170, 185)
        };
        var swatch = new Panel
        {
            Left = 78,
            Top = 2,
            Width = 142,
            Height = 20,
            BackColor = get(),
            Cursor = Cursors.Hand,
            Tag = key
        };
        swatch.Click += (_, _) =>
        {
            using var dlg = new ColorDialog { Color = get(), FullOpen = true };
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;
            set(dlg.Color);
            swatch.BackColor = dlg.Color;
        };
        row.Controls.Add(label);
        row.Controls.Add(swatch);
        parent.Controls.Add(row);
        _swatches[key] = swatch;
        return y + 28;
    }

    private void RefreshSwatches()
    {
        SetSwatch("Fond", _theme.Background);
        SetSwatch("Accent", _theme.Accent);
        SetSwatch("Texte", _theme.Text);
        SetSwatch("Mute", _theme.Muted);
        SetSwatch("CPU", _theme.CpuBar);
        SetSwatch("GPU", _theme.GpuBar);
        SetSwatch("VRAM", _theme.VramBar);
    }

    private void SetSwatch(string key, Color color)
    {
        if (_swatches.TryGetValue(key, out var p))
            p.BackColor = color;
    }

    private static Icon LoadAppIcon()
    {
        var ico = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        return File.Exists(ico) ? new Icon(ico) : SystemIcons.Application;
    }

    private NotifyIcon CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Ouvrir", null, (_, _) => RestoreFromTray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => ExitFromTray());

        var tray = new NotifyIcon
        {
            Text = "psp-connect",
            Icon = Icon,
            Visible = true,
            ContextMenuStrip = menu
        };
        tray.DoubleClick += (_, _) => RestoreFromTray();
        return tray;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_exitRequested || e.CloseReason != CloseReason.UserClosing)
            return;
        e.Cancel = true;
        HideToTray();
    }

    private void OnResizeToTray(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            HideToTray();
    }

    private void HideToTray()
    {
        _hiddenToTray = true;
        ShowInTaskbar = false;
        Hide();
        _tray.Visible = true;
        _timer.Interval = Math.Max(_intervalMs, 1000);
    }

    public void StartShowSignalWatcher(EventWaitHandle showEvent)
    {
        _watchShow = true;
        _showWatcher = new Thread(() =>
        {
            while (_watchShow)
            {
                if (!showEvent.WaitOne(500))
                    continue;
                if (!_watchShow)
                    break;
                try
                {
                    BeginInvoke(RestoreFromTray);
                }
                catch
                {
                }
            }
        })
        {
            IsBackground = true,
            Name = "psp-connect-show"
        };
        _showWatcher.Start();
    }

    private void RestoreFromTray()
    {
        _hiddenToTray = false;
        _timer.Interval = _intervalMs;
        ShowInTaskbar = true;
        if (!Visible)
            Show();
        WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    private void ExitFromTray()
    {
        _exitRequested = true;
        Close();
    }

    private void ApplyStudioPreset()
    {
        _theme.Background = Color.FromArgb(10, 12, 16);
        _theme.Accent = Color.FromArgb(64, 210, 190);
        _theme.Text = Color.FromArgb(236, 242, 250);
        _theme.Muted = Color.FromArgb(120, 132, 148);
        _theme.CpuBar = Color.FromArgb(94, 176, 255);
        _theme.GpuBar = Color.FromArgb(72, 214, 168);
        _theme.VramBar = Color.FromArgb(180, 140, 255);
        RefreshSwatches();
        _statusLabel.Text = "Preset studio";
    }

    private void MigrateLegacyTheme()
    {
        if (_theme.Background.ToArgb() != Color.FromArgb(18, 22, 28).ToArgb())
            return;
        ApplyStudioPreset();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _latest = _sampler.Sample();
        _latest.ApplyTheme(_theme);

        if (_enableUsb)
        {
            if (_usb is { IsConnected: true })
            {
                if (_usb.Send(in _latest))
                    _transportStatus = $"USB live  #{_usb.SentOk}";
                else
                    _transportStatus = $"USB erreur  {_usb.LastError}";
            }
            else
            {
                _usbRetryTicks++;
                if (_usbRetryTicks % 8 == 0)
                    TryUsb();
                else
                    _transportStatus = "USB attente";
            }
        }
        else
        {
            _transportStatus = "Preview local";
        }

        if (!_hiddenToTray)
        {
            using var g = Graphics.FromImage(_frame);
            DashboardPainter.Paint(g, in _latest, _theme, _transportStatus);
            _preview.Invalidate();
        }

        var live = _usb is { IsConnected: true } && _transportStatus.StartsWith("USB live", StringComparison.Ordinal);
        _statusDot.BackColor = live ? Color.FromArgb(64, 210, 190) : Color.FromArgb(200, 80, 70);
        _statusLabel.Text = _transportStatus;
        if (_tray.Visible)
            _tray.Text = $"psp-connect · {_transportStatus}";
    }

    private void TryUsb()
    {
        if (_usb is null)
            return;
        _usb.TryConnect(out var status);
        _transportStatus = status;
        _statusLabel.Text = status;
    }

    private void Cleanup()
    {
        _watchShow = false;
        _timer.Stop();
        _timer.Dispose();
        _theme.Save();
        _usb?.Dispose();
        _sampler.Dispose();
        _frame.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
    }
}
