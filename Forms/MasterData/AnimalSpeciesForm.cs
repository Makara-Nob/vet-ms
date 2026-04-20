using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.MasterData;

public class AnimalSpeciesForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Label lblStatus = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private List<AnimalSpecies> _filtered = [];

    private Button btnPrev = null!;
    private Button btnNext = null!;
    private Label lblPage = null!;

    private List<AnimalSpecies> _data = [];

    public AnimalSpeciesForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Animal Species";
        BackColor = UIHelper.LightBg;

        dgv = BuildGrid();

        Controls.Add(dgv);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(BuildToolbar());
        Controls.Add(UIHelper.CreateHeader(
            "Animal Species",
            "Manage animal species / types"));
    }

    // ───────────────── TOOLBAR ─────────────────
    private Panel BuildToolbar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.White
        };

        var topLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 232, 235) };
        var botLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(230, 232, 235) };

        var btnAdd = MakeToolButton("＋ Add", UIHelper.Success, 100);
        btnEdit = MakeToolButton("✎ Edit", UIHelper.Accent, 120);
        btnDelete = MakeToolButton("✕ Delete", UIHelper.Danger, 120);
        var btnRefresh = MakeToolButton("↺ Refresh", Color.FromArgb(90, 100, 115), 120);

        btnEdit.Enabled = false;
        btnDelete.Enabled = false;

        int x = 12;

        foreach (var b in new[] { btnAdd, btnEdit, btnDelete })
        {
            b.Left = x;
            b.Top = 9;
            bar.Controls.Add(b);
            x += b.Width + 8;
        }

        var div = new Panel
        {
            Width = 1,
            Height = 28,
            Left = x,
            Top = 11,
            BackColor = Color.FromArgb(220, 225, 230)
        };
        bar.Controls.Add(div);

        btnRefresh.Left = x + 12;
        btnRefresh.Top = 9;
        bar.Controls.Add(btnRefresh);

        bar.Controls.Add(topLine);
        bar.Controls.Add(botLine);

        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;
        btnRefresh.Click += (_, _) => LoadData();

        return bar;
    }

    // ───────────────── SEARCH ─────────────────
    private Panel BuildSearchBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 46,
            BackColor = UIHelper.LightBg,
            Padding = new Padding(12, 8, 12, 6)
        };

        var ico = new Label
        {
            Text = "🔍",
            Width = 24,
            Height = 26,
            Left = 14,
            Top = 10,
            TextAlign = ContentAlignment.MiddleCenter
        };

        txtSearch = new TextBox
        {
            Left = 40,
            Top = 10,
            Width = 280,
            Font = new Font("Segoe UI", 9.5f),
            PlaceholderText = "Search species..."
        };

        txtSearch.TextChanged += (_, _) => FilterData();

        bar.Controls.Add(ico);
        bar.Controls.Add(txtSearch);

        return bar;
    }

    // ───────────────── GRID ─────────────────
    private DataGridView BuildGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AllowUserToAddRows = false
        };

        UIHelper.StyleGrid(grid);

        grid.SelectionChanged += (_, _) =>
        {
            bool has = grid.SelectedRows.Count > 0;
            btnEdit.Enabled = has;
            btnDelete.Enabled = has;
        };

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                BtnEdit_Click(null, EventArgs.Empty);
        };

        return grid;
    }

    // ───────────────── STATUS ─────────────────
    private Panel BuildStatusBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            BackColor = Color.White
        };

        lblStatus = new Label
        {
            Left = 12,
            Top = 11,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f)
        };

        btnPrev = MakeToolButton("◀ Prev", Color.FromArgb(108, 117, 125), 80);
        btnNext = MakeToolButton("Next ▶", Color.FromArgb(108, 117, 125), 80);

        lblPage = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnPrev.Top = btnNext.Top = 2;

        btnPrev.Click += (_, _) =>
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                RefreshGrid();
            }
        };

        btnNext.Click += (_, _) =>
        {
            int totalPages = GetTotalPages();
            if (_currentPage < totalPages)
            {
                _currentPage++;
                RefreshGrid();
            }
        };

        bar.Resize += (_, _) =>
        {
            btnNext.Left = bar.Width - btnNext.Width - 12;
            lblPage.Left = btnNext.Left - 90;
            lblPage.Top = 11;
            btnPrev.Left = lblPage.Left - btnPrev.Width - 8;
        };

        bar.Controls.Add(lblStatus);
        bar.Controls.Add(btnPrev);
        bar.Controls.Add(lblPage);
        bar.Controls.Add(btnNext);

        return bar;
    }

    // ───────────────── DATA ─────────────────
    private void LoadData()
    {
        try { _data = DataStore.GetAnimalSpecies(); }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            return;
        }

        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();

        _filtered = string.IsNullOrWhiteSpace(q)
            ? _data
            : _data.Where(x =>
                x.Name.ToLower().Contains(q) ||
                x.Description?.ToLower().Contains(q) == true)
            .ToList();

        _currentPage = 1;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        int skip = (_currentPage - 1) * _pageSize;

        var pageData = _filtered
            .Skip(skip)
            .Take(_pageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                Status = x.IsActive ? "Active" : "Inactive",
                x.Description
            })
            .ToList();

        dgv.DataSource = pageData;

        if (dgv.Columns.Count == 0) return;

        dgv.Columns["Id"].Visible = false;
        dgv.Columns["Name"].HeaderText = "Species Name";

        int totalPages = GetTotalPages();

        lblStatus.Text = $"{_filtered.Count} records";
        lblPage.Text = $"Page {_currentPage} / {totalPages}";

        btnPrev.Enabled = _currentPage > 1;
        btnNext.Enabled = _currentPage < totalPages;
    }

    private int GetTotalPages()
    {
        return Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));
    }

    // ───────────────── BUTTONS ─────────────────
    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new AnimalSpeciesDialog();
        if (dlg.ShowDialog() != DialogResult.OK) return;

        DataStore.Insert(dlg.Result);
        LoadData();
    }

    private void BtnEdit_Click(object? s, EventArgs e)
    {
        if (dgv.SelectedRows.Count == 0) return;

        int id = (int)dgv.SelectedRows[0].Cells["Id"].Value;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null) return;

        using var dlg = new AnimalSpeciesDialog(item);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        item.Name = dlg.Result.Name;
        item.Description = dlg.Result.Description;
        item.IsActive = dlg.Result.IsActive;

        DataStore.Update(item);
        LoadData();
    }

    private void BtnDelete_Click(object? s, EventArgs e)
    {
        if (dgv.SelectedRows.Count == 0) return;

        int id = (int)dgv.SelectedRows[0].Cells["Id"].Value;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null) return;

        if (MessageBox.Show($"Delete {item.Name}?", "Confirm",
            MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;

        DataStore.Delete(item);
        LoadData();
    }

    // ───────────────── BUTTON STYLE ─────────────────
    private static Button MakeToolButton(string text, Color back, int width)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 34,
            BackColor = back,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(back);
        btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(back);

        return btn;
    }
}

// ── Add / Edit Dialog ─────────────────────────────────────────────────────────
public class AnimalSpeciesDialog : Form
{
    private readonly TextBox txtName;
    private readonly TextBox txtDesc;
    private readonly CheckBox chkActive;

    public AnimalSpecies Result { get; private set; } = new();

    public AnimalSpeciesDialog(AnimalSpecies? existing = null)
    {
        Text = existing == null ? "Add Species" : "Edit Species";
        Size = new Size(560, 360);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            FlowDirection = FlowDirection.TopDown
        };

        flow.Controls.Add(MakeLabel("Species Name"));
        txtName = new TextBox { Width = 480 };
        flow.Controls.Add(txtName);

        flow.Controls.Add(MakeLabel("Description"));
        txtDesc = new TextBox
        {
            Width = 480,
            Height = 80,
            Multiline = true
        };
        flow.Controls.Add(txtDesc);

        chkActive = new CheckBox
        {
            Text = "Active",
            Checked = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        flow.Controls.Add(chkActive);

        var btnSave = new Button
        {
            Text = "Save",
            Width = 100,
            Height = 32,
            BackColor = UIHelper.Success,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };

        btnSave.Click += (_, _) =>
        {
            Result = new AnimalSpecies
            {
                Id = existing?.Id ?? 0,
                Name = txtName.Text.Trim(),
                Description = txtDesc.Text.Trim(),
                IsActive = chkActive.Checked
            };

            DialogResult = DialogResult.OK;
        };

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50
        };

        btnSave.Left = 420;
        btnSave.Top = 10;

        footer.Controls.Add(btnSave);

        Controls.Add(flow);
        Controls.Add(footer);

        if (existing != null)
        {
            txtName.Text = existing.Name;
            txtDesc.Text = existing.Description;
            chkActive.Checked = existing.IsActive;
        }
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 9f, FontStyle.Bold),
        Margin = new Padding(0, 10, 0, 4)
    };
}