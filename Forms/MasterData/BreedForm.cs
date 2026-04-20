using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.MasterData;

public class BreedForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Label lblStatus = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private List<Breed> _filtered = [];

    private Button btnPrev = null!;
    private Button btnNext = null!;
    private Label lblPage = null!;

    private List<Breed> _data = [];

    public BreedForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Breeds";
        BackColor = UIHelper.LightBg;

        dgv = BuildGrid();

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        var gridContainer = new Panel
        {
            Width = 1000,          // control width
            Height = 420,          // ✅ FIXED HEIGHT (important)
            Anchor = AnchorStyles.Top, // stay near top
        };

        var paginationBar = BuildPaginationBar();
        paginationBar.Dock = DockStyle.Bottom;

        dgv.Dock = DockStyle.Fill;

        contentPanel.Controls.Add(dgv);
        contentPanel.Controls.Add(paginationBar);


        // center horizontally
        contentPanel.Resize += (_, _) =>
        {
            gridContainer.Left = (contentPanel.Width - gridContainer.Width) / 2;
            gridContainer.Top = 10;
        };


        // ✅ Add in correct order (top → bottom layout)
        contentPanel.Controls.Add(gridContainer);
        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());   // stays at very bottom
        Controls.Add(BuildSearchBar());
        Controls.Add(BuildToolbar());
        Controls.Add(UIHelper.CreateHeader(
            "Breeds",
            "Manage animal breeds linked to each species"
        ));
    }

    // ── Toolbar ─────────────────────────────

    private Panel BuildPaginationBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 36,
            BackColor = Color.White
        };

        var topLine = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(230, 232, 235)
        };

        btnPrev = MakeToolButton("◀ Prev", Color.FromArgb(108, 117, 125), 80);
        btnNext = MakeToolButton("Next ▶", Color.FromArgb(108, 117, 125), 80);

        lblPage = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnPrev.Top = btnNext.Top = 4;

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
            if (_currentPage < GetTotalPages())
            {
                _currentPage++;
                RefreshGrid();
            }
        };

        bar.Resize += (_, _) =>
        {
            btnNext.Left = bar.Width - btnNext.Width - 12;
            lblPage.Left = btnNext.Left - 90;
            lblPage.Top = 12;
            btnPrev.Left = lblPage.Left - btnPrev.Width - 8;
        };

        bar.Controls.Add(topLine);
        bar.Controls.Add(btnPrev);
        bar.Controls.Add(lblPage);
        bar.Controls.Add(btnNext);

        return bar;
    }

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
            x += b.Width + 6;
        }

        var div = new Panel
        {
            Width = 1,
            Height = 28,
            Left = x + 4,
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

    // ── Search ─────────────────────────────

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
            PlaceholderText = "Search breeds..."
        };
        txtSearch.TextChanged += (_, _) => FilterData();

        bar.Controls.AddRange(new Control[] { ico, txtSearch });
        return bar;
    }

    // ── Grid ─────────────────────────────

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
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            Cursor = Cursors.Hand
        };

        UIHelper.StyleGrid(grid);

        grid.SelectionChanged += (_, _) =>
        {
            bool hasRow = grid.SelectedRows.Count > 0;
            btnEdit.Enabled = hasRow;
            btnDelete.Enabled = hasRow;
        };

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
                BtnEdit_Click(null, EventArgs.Empty);
        };

        return grid;
    }

    // ── Status ─────────────────────────────

    private Panel BuildStatusBar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Color.White
        };

        lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(90, 100, 115)
        };

        var topLine = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = Color.FromArgb(230, 232, 235)
        };

        bar.Controls.Add(lblStatus);
        bar.Controls.Add(topLine);

        return bar;
    }

    // ── Data ─────────────────────────────

    private void LoadData()
    {
        _data = DataStore.GetBreeds();
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();

        _filtered = string.IsNullOrWhiteSpace(q)
            ? _data
            : _data.Where(x =>
                x.Name.ToLower().Contains(q) ||
                x.SpeciesName.ToLower().Contains(q) ||
                x.Description?.ToLower().Contains(q) == true)
            .ToList();

        _currentPage = 1;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        int totalPages = GetTotalPages();

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        var pageData = _filtered
            .Skip((_currentPage - 1) * _pageSize)
            .Take(_pageSize)
            .Select(x => new
            {
                x.Id,
                x.SpeciesName,
                x.Name,
                Status = x.IsActive ? "Active" : "Inactive",
                x.Description
            })
            .ToList();

        dgv.DataSource = pageData;

        if (dgv.Columns.Count == 0)
            return;

        dgv.Columns["Id"].Visible = false;
        dgv.Columns["SpeciesName"].HeaderText = "Species";
        dgv.Columns["Name"].HeaderText = "Breed Name";

        dgv.Columns["SpeciesName"].FillWeight = 25;
        dgv.Columns["Name"].FillWeight = 25;
        dgv.Columns["Status"].FillWeight = 15;
        dgv.Columns["Description"].FillWeight = 35;

        lblPage.Text = $"Page {_currentPage} of {totalPages}";
        btnPrev.Enabled = _currentPage > 1;
        btnNext.Enabled = _currentPage < totalPages;

        UpdateStatusBar(pageData.Count);
    }


    private void UpdateStatusBar(int showing)
    {
        if (_filtered.Count == 0)
        {
            lblStatus.Text = "No breeds found";
            return;
        }

        int start = ((_currentPage - 1) * _pageSize) + 1;
        int end = start + showing - 1;

        lblStatus.Text = $"Showing {start} - {end} of {_filtered.Count} breeds";
    }
    private int GetTotalPages()
    {
        return Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));
    }

    // ── Buttons ─────────────────────────────

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

    // ── CRUD (wire to dialog) ─────────────────────────────

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new BreedDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        DataStore.Insert(dlg.Result);
        LoadData();
    }

    private void BtnEdit_Click(object? s, EventArgs e)
    {
        if (dgv.SelectedRows.Count == 0) return;

        int id = (int)dgv.SelectedRows[0].Cells["Id"].Value!;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null) return;

        using var dlg = new BreedDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        DataStore.Update(dlg.Result);
        LoadData();
    }

    private void BtnDelete_Click(object? s, EventArgs e)
    {
        if (dgv.SelectedRows.Count == 0) return;

        int id = (int)dgv.SelectedRows[0].Cells["Id"].Value!;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null) return;

        if (MessageBox.Show($"Delete {item.Name}?", "Confirm",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        DataStore.Delete(item);
        LoadData();
    }
}
public class BreedDialog : Form
{
    private readonly ComboBox cboSpecies;
    private readonly TextBox txtName;
    private readonly TextBox txtDesc;
    private readonly CheckBox chkActive;

    private readonly List<AnimalSpecies> _species;

    public Breed Result { get; private set; } = new();

    private const int FormWidth = 540;
    private const int FieldWidth = 460;

    public BreedDialog(Breed? existing = null)
    {
        Text = existing is null ? "Add Breed" : "Edit Breed";
        Size = new Size(FormWidth, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        _species = DataStore.GetAnimalSpecies()
            .Where(x => x.IsActive)
            .ToList();

        // ── MAIN LAYOUT (NO FlowLayoutPanel → stable UI) ────────────────
        var main = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 16, 20, 10),
            AutoScroll = true
        };

        int y = 10;

        // ── Species ─────────────────────────────────────────────
        main.Controls.Add(MakeLabel("Species *", 10, y));
        y += 22;

        cboSpecies = new ComboBox
        {
            Width = FieldWidth,
            Left = 10,
            Top = y,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboSpecies.DataSource = _species;
        cboSpecies.DisplayMember = "Name";
        cboSpecies.ValueMember = "Id";

        main.Controls.Add(cboSpecies);
        y += 42;

        // ── Name ───────────────────────────────────────────────
        main.Controls.Add(MakeLabel("Breed Name *", 10, y));
        y += 22;

        txtName = new TextBox
        {
            Width = FieldWidth,
            Left = 10,
            Top = y
        };

        main.Controls.Add(txtName);
        y += 42;

        // ── Description ────────────────────────────────────────
        main.Controls.Add(MakeLabel("Description", 10, y));
        y += 22;

        txtDesc = new TextBox
        {
            Width = FieldWidth,
            Height = 70,
            Left = 10,
            Top = y,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };

        main.Controls.Add(txtDesc);
        y += 80;

        // ── Status (FIXED ALIGNMENT) ───────────────────────────
        main.Controls.Add(MakeLabel("Status", 10, y));
        y += 22;

        chkActive = new CheckBox
        {
            Text = "Active",
            Checked = true,
            Width = FieldWidth,
            Left = 10,
            Top = y,
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };

        main.Controls.Add(chkActive);

        // ── FOOTER ─────────────────────────────────────────────
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = UIHelper.LightBg
        };

        var btnSave = UIHelper.CreateButton("Save", UIHelper.Success, 90);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.Gray, 90);

        footer.Controls.Add(btnSave);
        footer.Controls.Add(btnCancel);

        footer.Resize += (_, _) =>
        {
            btnCancel.Left = footer.Width - btnCancel.Width - 18;
            btnSave.Left = btnCancel.Left - btnSave.Width - 10;
            btnSave.Top = btnCancel.Top = 12;
        };

        btnCancel.DialogResult = DialogResult.Cancel;
        btnSave.Click += BtnSave_Click;

        Controls.Add(main);
        Controls.Add(footer);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        // ── EDIT MODE ───────────────────────────────────────────
        if (existing is not null)
        {
            var sp = _species.FirstOrDefault(x => x.Id == existing.SpeciesId);
            if (sp != null) cboSpecies.SelectedItem = sp;

            txtName.Text = existing.Name;
            txtDesc.Text = existing.Description;
            chkActive.Checked = existing.IsActive;

            Result.Id = existing.Id;
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (cboSpecies.SelectedItem is not AnimalSpecies sp)
        {
            MessageBox.Show("Select species");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Name required");
            return;
        }

        Result = new Breed
        {
            Id = Result.Id,
            SpeciesId = sp.Id,
            SpeciesName = sp.Name,
            Name = txtName.Text.Trim(),
            Description = txtDesc.Text.Trim(),
            IsActive = chkActive.Checked
        };

        DialogResult = DialogResult.OK;
    }

    private static Label MakeLabel(string text, int x, int y) => new Label
    {
        Text = text,
        AutoSize = true,
        Left = x,
        Top = y,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(70, 80, 95)
    };
}