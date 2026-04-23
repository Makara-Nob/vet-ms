using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.MasterData;

public class ServiceTypeForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Label lblStatus = null!;
    
    private int _currentPage = 1;
    private int _pageSize = 10;
    private List<ServiceType> _filtered = [];

    private Button btnPrev = null!;
    private Button btnNext = null!;
    private Label lblPage = null!;
    private Label lblNoData = null!;

    private List<ServiceType> _data = [];

    public ServiceTypeForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Service Types";
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
            Height = 420,
            Dock = DockStyle.Top
        };

        var paginationBar = BuildPaginationBar();
        paginationBar.Dock = DockStyle.Bottom;

        dgv.Dock = DockStyle.Fill;
        
        lblNoData = UIHelper.CreateEmptyDataLabel("No services or records yet.");

        gridContainer.Controls.Add(lblNoData);
        gridContainer.Controls.Add(dgv);
        gridContainer.Controls.Add(paginationBar);
        lblNoData.BringToFront();
        dgv.BringToFront();

        contentPanel.Controls.Add(gridContainer);

        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader(
            "Service Types",
            "Manage clinic services and their pricing"));
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
            Top = 8,
            Width = 280,
            Font = new Font("Segoe UI", 11f),
            PlaceholderText = "Search services..."
        };

        txtSearch.TextChanged += (_, _) => FilterData();

        var btnAdd = MakeToolButton("Add", UIHelper.Success, 80);
        btnAdd.Left = txtSearch.Right + 16;
        btnAdd.Top = 8;
        btnAdd.Height = 31;
        btnAdd.Click += BtnAdd_Click;

        var btnRefresh = MakeToolButton("Reset", Color.FromArgb(108, 117, 125), 80);
        btnRefresh.Left = btnAdd.Right + 8;
        btnRefresh.Top = 8;
        btnRefresh.Height = 31;
        btnRefresh.Click += (_, _) => { txtSearch.Clear(); LoadData(); };

        bar.Controls.AddRange(new Control[] { ico, txtSearch, btnAdd, btnRefresh });
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
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,

            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            Cursor = Cursors.Hand
        };

        UIHelper.StyleGrid(grid);

        grid.CellPainting += (_, e) => UIHelper.PaintActionColumn(grid, e);
        grid.CellMouseClick += (_, e) => UIHelper.HandleActionColumnClick(grid, e, EditRow, DeleteRow);

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && (grid.Columns[e.ColumnIndex].Name != "ColAction"))
                EditRow(e.RowIndex);
        };

        return grid;
    }

    // ───────────────── PAGINATION ─────────────────
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

        btnPrev = MakeToolButton("Prev", Color.FromArgb(108, 117, 125), 80);
        btnNext = MakeToolButton("Next", Color.FromArgb(108, 117, 125), 80);

        lblPage = new Label
        {
            AutoSize = false,
            Width = 100,
            Height = 30,
            Font = new Font("Segoe UI", 9f),
            TextAlign = ContentAlignment.MiddleCenter
        };

        btnPrev.Top = btnNext.Top = 3;
        btnPrev.Height = btnNext.Height = 30;

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
            btnNext.Left = bar.Width - btnNext.Width - 16;
            lblPage.Left = btnNext.Left - lblPage.Width - 8;
            lblPage.Top = btnNext.Top;
            btnPrev.Left = lblPage.Left - btnPrev.Width - 8;
        };

        bar.Controls.Add(topLine);
        bar.Controls.Add(btnPrev);
        bar.Controls.Add(lblPage);
        bar.Controls.Add(btnNext);

        return bar;
    }

    // ───────────────── STATUS ─────────────────
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

    // ───────────────── DATA ─────────────────
    private void LoadData()
    {
        try { _data = DataStore.GetServiceTypes() ?? []; }
        catch (Exception ex)
        {
            VetMS.Forms.CustomMessageBox.Show(ex.Message);
            return;
        }

        FilterData();
    }

    private void FilterData()
    {
        if (txtSearch == null || _data == null) return;

        var q = txtSearch.Text.Trim().ToLower();

        _filtered = string.IsNullOrWhiteSpace(q)
            ? _data
            : _data.Where(x =>
                x != null &&
                ((x.Name?.ToLower().Contains(q) == true) ||
                 (x.Category?.ToLower().Contains(q) == true) ||
                 (x.Description?.ToLower().Contains(q) == true)))
            .ToList();

        if (_filtered == null) _filtered = [];

        _currentPage = 1;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (dgv == null || lblStatus == null || lblPage == null || btnPrev == null || btnNext == null || lblNoData == null) return;

        if (_filtered == null || _filtered.Count == 0)
        {
            dgv.DataSource = null;
            lblStatus.Text = "0 records";
            lblPage.Text = "Page 1 / 1";
            btnPrev.Enabled = false;
            btnNext.Enabled = false;
            btnPrev.Visible = btnNext.Visible = lblPage.Visible = false;
            lblNoData.Visible = true;
            dgv.Visible = false;
            return;
        }

        lblNoData.Visible = false;
        dgv.Visible = true;

        int skip = (_currentPage - 1) * _pageSize;

        var pageData = _filtered
            .Skip(skip)
            .Take(_pageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Category,
                Price  = x.Price.ToString("N2"),
                Status = x.IsActive ? "Active" : "Inactive",
                x.Description
            })
            .ToList();

        dgv.DataSource = pageData;

        if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Name"]        is { } cName)     cName.Width     = 180;
        if (dgv.Columns["Category"]    is { } cCategory) cCategory.Width = 140;
        if (dgv.Columns["Price"]       is { } cPrice)    cPrice.Width    = 100;
        if (dgv.Columns["Status"]      is { } cSt)       cSt.Width       = 90;
        if (dgv.Columns["Description"] is { } cDesc)     cDesc.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        if (!dgv.Columns.Contains("ColAction"))
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColAction",
                HeaderText = "Action",
                ReadOnly = true,
                FillWeight = 20
            });
        }

        int totalPages = GetTotalPages();

        lblStatus.Text = $"{_filtered.Count} records";
        lblPage.Text = $"Page {_currentPage} / {totalPages}";

        btnPrev.Enabled = _currentPage > 1;
        btnNext.Enabled = _currentPage < totalPages;
        btnPrev.Visible = btnNext.Visible = lblPage.Visible = totalPages > 1;
    }

    private int GetTotalPages()
    {
        if (_filtered == null || _filtered.Count == 0) return 1;
        return Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));
    }

    // ───────────────── BUTTONS ─────────────────
    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new ServiceTypeDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Service successfully saved!");
        LoadData();
    }

    private void EditRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        using var dlg = new ServiceTypeDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        item.Name        = dlg.Result.Name;
        item.Category    = dlg.Result.Category;
        item.Price       = dlg.Result.Price;
        item.Description = dlg.Result.Description;
        item.IsActive    = dlg.Result.IsActive;

        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Service successfully updated!");
        LoadData();
    }

    private void DeleteRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        if (VetMS.Forms.CustomMessageBox.Show($"Delete service \"{item.Name}\"?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try { DataStore.Delete(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Service successfully deleted!");
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

// ── Service type dialog ───────────────────────────────────────────────────────
public class ServiceTypeDialog : Form
{
    private readonly TextBox txtName, txtDesc;
    private readonly ComboBox cboCategory;
    private readonly NumericUpDown numPrice;
    private readonly CheckBox chkActive;
    public ServiceType Result { get; private set; } = new();

    private static readonly string[] Categories =
        ["Consultation", "Vaccination", "Surgery", "Grooming", "Dental", "Diagnostic", "Emergency", "Other"];

    public ServiceTypeDialog(ServiceType? existing = null)
    {
        Text = existing is null ? "Add Service Type" : "Edit Service Type";
        Size = new Size(600, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20, 14, 20, 0), AutoScroll = true };

        flow.Controls.Add(UIHelper.CreateFormLabel("Service Name *"));
        txtName = UIHelper.CreateTextBox(370);
        flow.Controls.Add(txtName);

        flow.Controls.Add(UIHelper.CreateFormLabel("Category *"));
        cboCategory = new ComboBox { Width = 370, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 8) };
        cboCategory.Items.AddRange(Categories);
        flow.Controls.Add(cboCategory);

        flow.Controls.Add(UIHelper.CreateFormLabel("Price"));
        numPrice = new NumericUpDown
        {
            Width = 180, Minimum = 0, Maximum = 999999, DecimalPlaces = 2, ThousandsSeparator = true,
            Font = new Font("Segoe UI", 9.5f), Margin = new Padding(0, 0, 0, 8)
        };
        flow.Controls.Add(numPrice);

        flow.Controls.Add(UIHelper.CreateFormLabel("Description"));
        txtDesc = new TextBox { Width = 370, Height = 56, Multiline = true, Font = new Font("Segoe UI", 9.5f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 6) };
        flow.Controls.Add(txtDesc);

        chkActive = new CheckBox { Text = "Active", Checked = true, Font = new Font("Segoe UI", 9f) };
        flow.Controls.Add(chkActive);

        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 50, BackColor = UIHelper.LightBg };
        var btnSave   = UIHelper.CreateButton("Save",   UIHelper.Success, 90);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.FromArgb(108, 117, 125), 90);
        btnSave.Top = btnCancel.Top = 9;
        btnSave.Left   = pnlBtn.Width - 200;
        btnCancel.Left = pnlBtn.Width - 100;
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancel.DialogResult = DialogResult.Cancel;
        btnSave.Click += BtnSave_Click;
        pnlBtn.Controls.AddRange(new Control[] { btnSave, btnCancel });

        Controls.Add(flow);
        Controls.Add(pnlBtn);
        AcceptButton = btnSave;
        CancelButton = btnCancel;

        if (existing is not null)
        {
            txtName.Text = existing.Name;
            var idx = Array.IndexOf(Categories, existing.Category);
            if (idx >= 0) cboCategory.SelectedIndex = idx;
            numPrice.Value    = existing.Price;
            txtDesc.Text      = existing.Description;
            chkActive.Checked = existing.IsActive;
            Result.Id         = existing.Id;
        }
    }

    private void BtnSave_Click(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        { VetMS.Forms.CustomMessageBox.Show("Service name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtName.Focus(); return; }
        if (cboCategory.SelectedItem is null)
        { VetMS.Forms.CustomMessageBox.Show("Category is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        Result = new ServiceType
        {
            Id          = Result.Id,
            Name        = txtName.Text.Trim(),
            Category    = cboCategory.SelectedItem.ToString()!,
            Price       = numPrice.Value,
            Description = txtDesc.Text.Trim(),
            IsActive    = chkActive.Checked
        };
        DialogResult = DialogResult.OK;
    }
}
