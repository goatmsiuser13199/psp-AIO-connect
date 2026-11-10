namespace PspConnect.Host.Ui;

internal static class ThemedDialog
{
    private static readonly Color Bg = Color.FromArgb(18, 22, 28);
    private static readonly Color Panel = Color.FromArgb(24, 28, 36);
    private static readonly Color Text = Color.FromArgb(230, 236, 245);
    private static readonly Color Muted = Color.FromArgb(140, 150, 165);
    private static readonly Color Accent = Color.FromArgb(70, 180, 140);
    private static readonly Color Danger = Color.FromArgb(200, 80, 70);
    private static readonly Color Button = Color.FromArgb(32, 38, 48);

    public static bool Confirm(IWin32Window owner, string title, string message)
    {
        using var form = CreateShell(title, 380, 168);
        form.Controls.Add(new Label
        {
            Left = 20,
            Top = 20,
            Width = 340,
            Height = 56,
            Text = message,
            ForeColor = Text,
            Font = new Font("Segoe UI", 9.5f)
        });

        var cancel = MakeButton(20, 100, 150, "Annuler", Button, Text);
        var ok = MakeButton(210, 100, 150, "Supprimer", Danger, Color.White);
        form.Controls.Add(cancel);
        form.Controls.Add(ok);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        var result = false;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        ok.Click += (_, _) =>
        {
            result = true;
            form.DialogResult = DialogResult.OK;
        };

        form.ShowDialog(owner);
        return result;
    }

    public static string? PromptName(IWin32Window owner, string title, string initial)
    {
        using var form = CreateShell(title, 380, 168);
        form.Controls.Add(new Label
        {
            Left = 20,
            Top = 18,
            Width = 340,
            Height = 20,
            Text = "Nom du profil",
            ForeColor = Muted,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        });

        var box = new TextBox
        {
            Left = 20,
            Top = 42,
            Width = 340,
            Height = 28,
            Text = initial,
            BackColor = Panel,
            ForeColor = Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10f),
            MaxLength = 32
        };
        form.Controls.Add(box);

        var cancel = MakeButton(20, 100, 150, "Annuler", Button, Text);
        var ok = MakeButton(210, 100, 150, "OK", Accent, Color.FromArgb(12, 20, 18));
        form.Controls.Add(cancel);
        form.Controls.Add(ok);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        string? value = null;
        cancel.Click += (_, _) => form.DialogResult = DialogResult.Cancel;
        ok.Click += (_, _) =>
        {
            value = box.Text.Trim();
            if (string.IsNullOrWhiteSpace(value))
                return;
            form.DialogResult = DialogResult.OK;
        };

        form.Shown += (_, _) =>
        {
            box.Focus();
            box.SelectAll();
        };

        return form.ShowDialog(owner) == DialogResult.OK ? value : null;
    }

    private static Form CreateShell(string title, int w, int h) => new()
    {
        Text = title,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false,
        MinimizeBox = false,
        ShowInTaskbar = false,
        StartPosition = FormStartPosition.CenterParent,
        ClientSize = new Size(w, h),
        BackColor = Bg,
        ForeColor = Text,
        Font = new Font("Segoe UI", 9f)
    };

    private static Button MakeButton(int x, int y, int w, string text, Color back, Color fore)
    {
        var btn = new Button
        {
            Left = x,
            Top = y,
            Width = w,
            Height = 32,
            Text = text,
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}
