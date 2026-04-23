using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class CustomerForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<Customer> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public CustomerForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "Customers"; BackColor = UIHelper.LightBg;
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        var grid = new Panel { Dock = DockStyle.Top, Height = 420, BackColor = Color.White };
        dgv = new DataGridView
        {
            Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, BackgroundColor = Color.White, Cursor = Cursors.Hand
        };
        UIHelper.StyleGrid(dgv);
        dgv.CellPainting   += (_, e) => UIHelper.PaintActionColumn(dgv, e, "View", "Edit");
        dgv.CellMouseClick += (_, e) => UIHelper.HandleActionColumnClick(dgv, e, ViewRow, EditRow, "View", "Edit");
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") ViewRow(e.RowIndex); };
        var pag = BuildPaginationBar(); pag.Dock = DockStyle.Bottom;
        lblNoData = UIHelper.CreateEmptyDataLabel("No customers yet. Add your first pet owner!");
        grid.Controls.Add(lblNoData); grid.Controls.Add(dgv); grid.Controls.Add(pag);
        lblNoData.BringToFront(); dgv.BringToFront();
        content.Controls.Add(grid);
        Controls.Add(content); Controls.Add(BuildStatusBar()); Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("Customers", "Manage pet owners and client contact details"));
    }

    private Panel BuildStatusBar()
    {
        var p = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White };
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12,0,0,0), ForeColor = Color.FromArgb(90,100,115), Font = new Font("Segoe UI", 8.5f) };
        var line = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230,232,235) };
        p.Controls.Add(lblStatus); p.Controls.Add(line); return p;
    }

    private Panel BuildSearchBar()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(0,10,0,10) };
        var ico = new Label { Text = "🔍", Width = 24, Height = 26, Left = 4, Top = 13, TextAlign = ContentAlignment.MiddleCenter };
        txtSearch = new TextBox { Left = 28, Top = 13, Width = 300, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search customers..." };
        txtSearch.TextChanged += (_, _) => FilterData();
        var btnAdd = UIHelper.CreateButton("Add", UIHelper.Success, 70, 31); btnAdd.Left = txtSearch.Right + 12; btnAdd.Top = 12; btnAdd.Click += BtnAdd_Click;
        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray, 70, 31); btnReset.Left = btnAdd.Right + 8; btnReset.Top = 12;
        btnReset.Click += (_, _) => { txtSearch.Clear(); LoadData(); };
        p.Controls.AddRange(new Control[] { ico, txtSearch, btnAdd, btnReset }); return p;
    }

    private Panel BuildPaginationBar()
    {
        var p = new Panel { Height = 48, BackColor = Color.White };
        p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230,235,240) });
        btnPrev = UIHelper.CreateButton("Prev", Color.FromArgb(108,117,125), 60, 26);
        btnNext = UIHelper.CreateButton("Next", Color.FromArgb(108,117,125), 60, 26);
        lblPage = new Label { AutoSize = false, Width = 100, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(64,64,64) };
        p.Resize += (_, _) => { btnNext.Left = p.Width - btnNext.Width - 16; btnNext.Top = 11; lblPage.Left = btnNext.Left - lblPage.Width - 8; lblPage.Top = 15; btnPrev.Left = lblPage.Left - btnPrev.Width - 8; btnPrev.Top = 11; };
        btnPrev.Click += (_, _) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
        btnNext.Click += (_, _) => { if (_currentPage < GetTotalPages()) { _currentPage++; RefreshGrid(); } };
        p.Controls.AddRange(new Control[] { btnPrev, lblPage, btnNext }); return p;
    }

    private int GetTotalPages() => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));

    private void LoadData()
    {
        try { _data = DataStore.GetCustomers() ?? []; }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        _filtered = string.IsNullOrWhiteSpace(q) ? _data
            : _data.Where(x => (x.FullName?.ToLower().Contains(q) == true) || (x.Phone?.ToLower().Contains(q) == true) || (x.Email?.ToLower().Contains(q) == true)).ToList();
        _currentPage = 1; RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (_filtered.Count == 0)
        {
            dgv.DataSource = null; lblStatus.Text = "0 records"; lblPage.Text = "Page 1 / 1";
            btnPrev.Enabled = btnNext.Enabled = false; btnPrev.Visible = btnNext.Visible = lblPage.Visible = false;
            lblNoData.Visible = true; dgv.Visible = false; return;
        }
        lblNoData.Visible = false; dgv.Visible = true;
        var page = _filtered.Skip((_currentPage-1)*_pageSize).Take(_pageSize)
            .Select(x => new { x.Id, x.FullName, x.Phone, x.Email, Status = x.IsActive ? "Active" : "Inactive", x.Address }).ToList();
        dgv.DataSource = page;
        if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["FullName"] is { } c1) { c1.HeaderText = "Full Name"; c1.Width = 180; }
        if (dgv.Columns["Phone"]    is { } c2) c2.Width = 130;
        if (dgv.Columns["Email"]    is { } c3) c3.Width = 180;
        if (dgv.Columns["Status"]   is { } c4) c4.Width = 80;
        if (dgv.Columns["Address"]  is { } c5) c5.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "Action", ReadOnly = true, FillWeight = 20 });
        int tp = GetTotalPages();
        lblStatus.Text = $"{_filtered.Count} records"; lblPage.Text = $"Page {_currentPage} / {tp}";
        btnPrev.Enabled = _currentPage > 1; btnNext.Enabled = _currentPage < tp;
        btnPrev.Visible = btnNext.Visible = lblPage.Visible = tp > 1;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new CustomerDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Customer saved!"); LoadData();
    }

    private void ViewRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new CustomerDialog(item, true);
        dlg.ShowDialog(this);
    }

    private void EditRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new CustomerDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        item.FullName = dlg.Result.FullName; item.Phone = dlg.Result.Phone; item.Email = dlg.Result.Email;
        item.Address = dlg.Result.Address; item.Notes = dlg.Result.Notes; item.IsActive = dlg.Result.IsActive;
        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Customer updated!"); LoadData();
    }
}

public class CustomerDialog : Form
{
    private readonly TextBox txtName, txtPhone, txtEmail, txtAddress, txtNotes;
    private readonly CheckBox chkActive;
    private readonly bool _readOnly;
    public Customer Result { get; private set; } = new();

    public CustomerDialog(Customer? existing = null, bool readOnly = false)
    {
        _readOnly = readOnly;
        Text = existing is null ? "Register New Customer" : (_readOnly ? "Customer Profile" : "Edit Customer Profile");
        Size = new Size(850, 720); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; BackColor = Color.White;

        var contentPnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        
        // --- HEADER ---
        var hdr = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = UIHelper.LightBg };
        var picAvatar = new PictureBox { Width = 100, Height = 100, Left = 30, Top = 20, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(235, 238, 242) };
        var path = new System.Drawing.Drawing2D.GraphicsPath(); path.AddEllipse(0, 0, 100, 100); picAvatar.Region = new Region(path);
        
        var name = existing?.FullName ?? "New Customer";
        picAvatar.Image = UIHelper.CreateAvatar(name, 100);

        var lblTitle = new Label { Text = name, Left = 145, Top = 35, AutoSize = true, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = UIHelper.Primary };
        var lblSubtitle = new Label { Text = existing is null ? "Enter primary contact details for our record" : $"Customer ID: #{existing.Id:D4} • Active Account", Left = 145, Top = 72, AutoSize = true, Font = new Font("Segoe UI", 10f), ForeColor = Color.Gray };
        hdr.Controls.AddRange(new Control[] { picAvatar, lblTitle, lblSubtitle });
        
        var lineH = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220,220,225) };
        hdr.Controls.Add(lineH);

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(40, 20, 40, 25), AutoScroll = true };

        var gridLayout = new TableLayoutPanel { Width = 740, AutoSize = true, ColumnCount = 2, Margin = new Padding(0,0,0,10) };
        gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        txtName = new TextBox { Width = 350, Font = new Font("Segoe UI", 10.5f) };
        txtPhone = new TextBox { Width = 350, Font = new Font("Segoe UI", 10.5f) };
        txtEmail = new TextBox { Width = 350, Font = new Font("Segoe UI", 10.5f) };
        chkActive = new CheckBox { Text = "Account Active", Checked = true, Font = new Font("Segoe UI", 10.5f) };
        var pnlActive = new Panel { Width = 350, Height = 65 };
        chkActive.Location = new Point(0, 28); pnlActive.Controls.Add(chkActive);

        gridLayout.Controls.Add(UIHelper.WrapControl("Full Name *", txtName), 0, 0);
        gridLayout.Controls.Add(UIHelper.WrapControl("Phone Number *", txtPhone), 1, 0);
        gridLayout.Controls.Add(UIHelper.WrapControl("Email Address", txtEmail), 0, 1);
        gridLayout.Controls.Add(pnlActive, 1, 1);

        flow.Controls.Add(UIHelper.CreateSectionLabel("PRIMARY CONTACT INFO"));
        flow.Controls.Add(gridLayout);

        flow.Controls.Add(UIHelper.CreateSectionLabel("LOCATION & BACKGROUND"));
        
        flow.Controls.Add(UIHelper.CreateFormLabel("Physical Address"));
        txtAddress = new TextBox { Width = 735, Height = 80, Multiline = true, Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0,0,0,15) };
        flow.Controls.Add(txtAddress);

        flow.Controls.Add(UIHelper.CreateFormLabel("Internal Notes (Confidential)"));
        txtNotes = new TextBox { Width = 735, Height = 80, Multiline = true, Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0,0,0,15) };
        flow.Controls.Add(txtNotes);

        contentPnl.Controls.Add(flow);
        contentPnl.Controls.Add(hdr);
        Controls.Add(contentPnl);

        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White };
        var btnSave = UIHelper.CreateButton(_readOnly ? "Done" : "Save", UIHelper.Success, 90);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.FromArgb(108,117,125), 90);
        
        btnSave.Top = btnCancel.Top = 9; btnSave.Left = pnlBtn.Width - 200; btnCancel.Left = pnlBtn.Width - 100;
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        
        btnCancel.Visible = !_readOnly;
        if (_readOnly) btnSave.Click += (s, e) => DialogResult = DialogResult.OK;
        else btnSave.Click += Save;

        btnCancel.DialogResult = DialogResult.Cancel;
        pnlBtn.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230,230,230) });
        pnlBtn.Controls.AddRange(new Control[] { btnSave, btnCancel });
        Controls.Add(pnlBtn); AcceptButton = btnSave; CancelButton = btnCancel;

        if (existing is not null)
        {
            txtName.Text = existing.FullName; txtPhone.Text = existing.Phone; 
            txtEmail.Text = existing.Email; txtAddress.Text = existing.Address; 
            txtNotes.Text = existing.Notes; chkActive.Checked = existing.IsActive;
            Result.Id = existing.Id;
            
            if (_readOnly)
            {
                txtName.ReadOnly = txtPhone.ReadOnly = txtEmail.ReadOnly = txtAddress.ReadOnly = txtNotes.ReadOnly = true;
                txtName.BackColor = txtPhone.BackColor = txtEmail.BackColor = txtAddress.BackColor = txtNotes.BackColor = Color.White;
                chkActive.Enabled = false;
            }
        }
    }

    private void Save(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        { VetMS.Forms.CustomMessageBox.Show("Full name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtName.Focus(); return; }
        if (string.IsNullOrWhiteSpace(txtPhone.Text))
        { VetMS.Forms.CustomMessageBox.Show("Phone is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtPhone.Focus(); return; }
        Result = new Customer { Id = Result.Id, FullName = txtName.Text.Trim(), Phone = txtPhone.Text.Trim(), Email = txtEmail.Text.Trim(), Address = txtAddress.Text.Trim(), Notes = txtNotes.Text.Trim(), IsActive = chkActive.Checked };
        DialogResult = DialogResult.OK;
    }
}
