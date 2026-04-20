using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.MasterData;

public class SupplierForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Button btnPrev = null!;
    private Button btnNext = null!;
    private Label lblPage = null!;
    private Label lblStatus = null!;
    private Label lblNoData = null!;

    private List<Supplier> _data = [];
    private List<Supplier> _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public SupplierForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Suppliers";
        BackColor = UIHelper.LightBg;

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        var gridContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 420,
            BackColor = Color.White
        };

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            Cursor = Cursors.Hand
        };
        
        UIHelper.StyleGrid(dgv);

        dgv.CellPainting += (_, e) => UIHelper.PaintActionColumn(dgv, e);
        dgv.CellMouseClick += (_, e) => UIHelper.HandleActionColumnClick(dgv, e, EditRow, DeleteRow);
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") EditRow(e.RowIndex); };

        var paginationBar = BuildPaginationBar();
        paginationBar.Dock = DockStyle.Bottom;

        lblNoData = UIHelper.CreateEmptyDataLabel("No suppliers or records yet.");

        gridContainer.Controls.Add(lblNoData);
        gridContainer.Controls.Add(dgv);
        gridContainer.Controls.Add(paginationBar);
        lblNoData.BringToFront();
        dgv.BringToFront();

        contentPanel.Controls.Add(gridContainer);

        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("Suppliers", "Manage medication and supply vendors"));
    }

    private Panel BuildStatusBar()
    {
        var pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White };
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), ForeColor = Color.FromArgb(90, 100, 115), Font = new Font("Segoe UI", 8.5f) };
        var topLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 232, 235) };
        pnlStatus.Controls.Add(lblStatus);
        pnlStatus.Controls.Add(topLine);
        return pnlStatus;
    }

    private Panel BuildSearchBar()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(0, 10, 0, 10) };

        var ico = new Label
        {
            Text = "🔍",
            Width = 24,
            Height = 26,
            Left = 4,
            Top = 13,
            TextAlign = ContentAlignment.MiddleCenter
        };

        txtSearch = new TextBox
        {
            Left = 28,
            Top = 13,
            Width = 300,
            Font = new Font("Segoe UI", 11f),
            PlaceholderText = "Search suppliers..."
        };
        txtSearch.TextChanged += (_, _) => FilterData();

        var btnAdd = UIHelper.CreateButton("Add", UIHelper.Success, 70, 31);
        btnAdd.Left = txtSearch.Right + 12;
        btnAdd.Top = 12;
        btnAdd.Click += BtnAdd_Click;

        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray, 70, 31);
        btnReset.Left = btnAdd.Right + 8;
        btnReset.Top = 12;
        btnReset.Click += (_, _) => { txtSearch.Clear(); LoadData(); };

        pnl.Controls.AddRange(new Control[] { ico, txtSearch, btnAdd, btnReset });
        return pnl;
    }

    private Panel BuildPaginationBar()
    {
        var pnl = new Panel { Height = 48, BackColor = Color.White };
        var topBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 235, 240) };
        pnl.Controls.Add(topBorder);

        btnPrev = UIHelper.CreateButton("Prev", Color.FromArgb(108, 117, 125), 60, 26);
        btnNext = UIHelper.CreateButton("Next", Color.FromArgb(108, 117, 125), 60, 26);

        lblPage = new Label
        {
            Text = "Page 1 / 1",
            AutoSize = false,
            Width = 100,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(64, 64, 64)
        };

        pnl.Resize += (_, _) =>
        {
            btnNext.Left = pnl.Width - btnNext.Width - 16;
            btnNext.Top = 11;
            lblPage.Left = btnNext.Left - lblPage.Width - 8;
            lblPage.Top = 15;
            btnPrev.Left = lblPage.Left - btnPrev.Width - 8;
            btnPrev.Top = 11;
        };

        btnPrev.Click += (_, _) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
        btnNext.Click += (_, _) => { if (_currentPage < GetTotalPages()) { _currentPage++; RefreshGrid(); } };

        pnl.Controls.AddRange(new Control[] { btnPrev, lblPage, btnNext });
        return pnl;
    }

    private int GetTotalPages() => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));

    private void LoadData()
    {
        try { _data = DataStore.GetSuppliers() ?? []; }
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
                (x.CompanyName?.ToLower().Contains(q) == true) ||
                (x.ContactPerson?.ToLower().Contains(q) == true) ||
                (x.Phone?.ToLower().Contains(q) == true) ||
                (x.Email?.ToLower().Contains(q) == true))
            .ToList();

        if (_filtered == null) _filtered = [];

        _currentPage = 1;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (_filtered == null || _filtered.Count == 0)
        {
            dgv.DataSource = null;
            lblStatus.Text = "0 records";
            lblPage.Text = "Page 1 / 1";
            btnPrev.Enabled = false;
            btnNext.Enabled = false;
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
                Company = x.CompanyName,
                Contact = x.ContactPerson,
                x.Phone,
                x.Email,
                Status  = x.IsActive ? "Active" : "Inactive",
                x.Address
            })
            .ToList();

        dgv.DataSource = pageData;

        if (dgv.Columns["Id"]      != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Company"] is { } cCompany) cCompany.Width = 180;
        if (dgv.Columns["Contact"] is { } cContact) cContact.Width = 150;
        if (dgv.Columns["Phone"]   is { } cPhone)   cPhone.Width   = 120;
        if (dgv.Columns["Email"]   is { } cEmail)   cEmail.Width   = 180;
        if (dgv.Columns["Status"]  is { } cSt)      cSt.Width      = 80;
        if (dgv.Columns["Address"] is { } cAddr)    cAddr.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

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
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new SupplierDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadData();
    }

    private void EditRow(int rowIndex)
    {
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        using var dlg = new SupplierDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        item.CompanyName   = dlg.Result.CompanyName;
        item.ContactPerson = dlg.Result.ContactPerson;
        item.Phone         = dlg.Result.Phone;
        item.Email         = dlg.Result.Email;
        item.Address       = dlg.Result.Address;
        item.IsActive      = dlg.Result.IsActive;

        try { DataStore.Update(item); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadData();
    }

    private void DeleteRow(int rowIndex)
    {
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        if (MessageBox.Show($"Delete supplier \"{item.CompanyName}\"?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try { DataStore.Delete(item); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadData();
    }
}

// ── Supplier dialog ───────────────────────────────────────────────────────────
public class SupplierDialog : Form
{
    private readonly TextBox txtCompany, txtContact, txtPhone, txtEmail, txtAddress;
    private readonly CheckBox chkActive;
    public Supplier Result { get; private set; } = new();

    public SupplierDialog(Supplier? existing = null)
    {
        Text = existing is null ? "Add Supplier" : "Edit Supplier";
        Size = new Size(460, 430);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20, 14, 20, 0), AutoScroll = true };

        flow.Controls.Add(UIHelper.CreateFormLabel("Company Name *"));
        txtCompany = UIHelper.CreateTextBox(390);
        flow.Controls.Add(txtCompany);

        flow.Controls.Add(UIHelper.CreateFormLabel("Contact Person"));
        txtContact = UIHelper.CreateTextBox(390);
        flow.Controls.Add(txtContact);

        var pnlRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0) };
        var colPhone = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 0, 14, 0) };
        colPhone.Controls.Add(UIHelper.CreateFormLabel("Phone"));
        txtPhone = UIHelper.CreateTextBox(180);
        colPhone.Controls.Add(txtPhone);
        var colEmail = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown };
        colEmail.Controls.Add(UIHelper.CreateFormLabel("Email"));
        txtEmail = UIHelper.CreateTextBox(196);
        colEmail.Controls.Add(txtEmail);
        pnlRow.Controls.AddRange(new Control[] { colPhone, colEmail });
        flow.Controls.Add(pnlRow);

        flow.Controls.Add(UIHelper.CreateFormLabel("Address"));
        txtAddress = new TextBox { Width = 390, Height = 56, Multiline = true, Font = new Font("Segoe UI", 9.5f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 6) };
        flow.Controls.Add(txtAddress);

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
            txtCompany.Text   = existing.CompanyName;
            txtContact.Text   = existing.ContactPerson;
            txtPhone.Text     = existing.Phone;
            txtEmail.Text     = existing.Email;
            txtAddress.Text   = existing.Address;
            chkActive.Checked = existing.IsActive;
            Result.Id         = existing.Id;
        }
    }

    private void BtnSave_Click(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCompany.Text))
        { MessageBox.Show("Company name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtCompany.Focus(); return; }

        Result = new Supplier
        {
            Id            = Result.Id,
            CompanyName   = txtCompany.Text.Trim(),
            ContactPerson = txtContact.Text.Trim(),
            Phone         = txtPhone.Text.Trim(),
            Email         = txtEmail.Text.Trim(),
            Address       = txtAddress.Text.Trim(),
            IsActive      = chkActive.Checked
        };
        DialogResult = DialogResult.OK;
    }
}
