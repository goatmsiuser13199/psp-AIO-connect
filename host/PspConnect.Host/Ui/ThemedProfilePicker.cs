namespace PspConnect.Host.Ui;

internal sealed class ThemedProfilePicker : UserControl
{
    private readonly Label _value;
    private readonly Panel _menu;
    private readonly FlowLayoutPanel _list;
    private bool _open;
    private string _selectedId = "";
    private List<(string Id, string Name)> _items = new();

    public event EventHandler? SelectionChanged;

    public string SelectedId
    {
        get => _selectedId;
        set
        {
            _selectedId = value ?? "";
            var hit = _items.FirstOrDefault(i => i.Id == _selectedId);
            _value.Text = string.IsNullOrEmpty(hit.Name) ? "Aucun profil" : hit.Name;
        }
    }

    public ThemedProfilePicker()
    {
        Height = 30;
        BackColor = Color.FromArgb(28, 32, 40);
        Cursor = Cursors.Hand;

        _value = new Label
        {
            Left = 10,
            Top = 5,
            Height = 20,
            Text = "Aucun profil",
            ForeColor = Color.FromArgb(230, 236, 245),
            Font = new Font("Segoe UI", 9f),
            AutoSize = false
        };
        var chevron = new Label
        {
            Text = "▾",
            ForeColor = Color.FromArgb(140, 150, 165),
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter,
            Width = 22,
            Height = 30,
            Dock = DockStyle.Right
        };
        Controls.Add(_value);
        Controls.Add(chevron);

        _menu = new Panel
        {
            Visible = false,
            BackColor = Color.FromArgb(22, 26, 34),
            BorderStyle = BorderStyle.FixedSingle,
            Width = 220,
            Height = 120
        };
        _list = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(4),
            BackColor = Color.FromArgb(22, 26, 34)
        };
        _menu.Controls.Add(_list);

        Click += (_, _) => Toggle();
        _value.Click += (_, _) => Toggle();
        chevron.Click += (_, _) => Toggle();
        Resize += (_, _) => _value.Width = Math.Max(40, Width - 34);
    }

    public void SetItems(IEnumerable<(string Id, string Name)> items, string selectedId)
    {
        _items = items.ToList();
        SelectedId = selectedId;
        RebuildList();
    }

    private void Toggle()
    {
        if (_open)
            CloseMenu();
        else
            OpenMenu();
    }

    private void OpenMenu()
    {
        if (ParentForm is null)
            return;

        RebuildList();
        var screen = PointToScreen(new Point(0, Height));
        var local = ParentForm.PointToClient(screen);
        _menu.Parent = ParentForm;
        _menu.Left = local.X;
        _menu.Top = local.Y;
        _menu.Width = Width;
        _menu.Height = Math.Min(148, Math.Max(36, 8 + Math.Max(1, _items.Count) * 28));
        _menu.BringToFront();
        _menu.Visible = true;
        _open = true;
        ParentForm.MouseDown -= ParentFormOnMouseDown;
        ParentForm.MouseDown += ParentFormOnMouseDown;
        ParentForm.Deactivate -= ParentFormOnDeactivate;
        ParentForm.Deactivate += ParentFormOnDeactivate;
    }

    private void CloseMenu()
    {
        _open = false;
        _menu.Visible = false;
        if (ParentForm is not null)
        {
            ParentForm.MouseDown -= ParentFormOnMouseDown;
            ParentForm.Deactivate -= ParentFormOnDeactivate;
        }
        if (_menu.Parent is not null)
            _menu.Parent = null;
    }

    private void ParentFormOnMouseDown(object? sender, MouseEventArgs e)
    {
        if (!_open || ParentForm is null)
            return;
        var p = ParentForm.PointToClient(Cursor.Position);
        var inPicker = ClientRectangle.Contains(PointToClient(Cursor.Position));
        var inMenu = _menu.Bounds.Contains(p);
        if (!inPicker && !inMenu)
            CloseMenu();
    }

    private void ParentFormOnDeactivate(object? sender, EventArgs e) => CloseMenu();

    private void RebuildList()
    {
        _list.Controls.Clear();
        foreach (var item in _items)
        {
            var row = new Button
            {
                Text = item.Name,
                Width = Math.Max(180, Width - 20),
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = item.Id == _selectedId
                    ? Color.FromArgb(32, 48, 44)
                    : Color.FromArgb(22, 26, 34),
                ForeColor = Color.FromArgb(230, 236, 245),
                Cursor = Cursors.Hand,
                Tag = item.Id,
                Margin = new Padding(0, 0, 0, 2)
            };
            row.FlatAppearance.BorderSize = 0;
            row.Click += (_, _) =>
            {
                SelectedId = (string)row.Tag!;
                CloseMenu();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            };
            _list.Controls.Add(row);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CloseMenu();
            _menu.Dispose();
        }
        base.Dispose(disposing);
    }
}
