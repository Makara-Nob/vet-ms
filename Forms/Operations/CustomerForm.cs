using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class CustomerForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<Customer> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public CustomerForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "Customers"; BackColor = UIHelper.LightBg;
        var contentPanel  = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 8, 20, 0) };
        var gridContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ScrollBars = ScrollBars.Vertical,
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
        dgv.ColumnHeadersHeight = 42;
        dgv.RowTemplate.Height  = 38;
        dgv.ShowCellToolTips    = true;
        dgv.Resize             += (_, _) => DistributeColumns();
        dgv.CellPainting   += (_, e) => UIHelper.PaintActionColumn(dgv, e, "View", "Edit");
        dgv.CellMouseClick += (_, e) => UIHelper.HandleActionColumnClick(dgv, e, ViewRow, EditRow, "View", "Edit");
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") ViewRow(e.RowIndex); };

        var pag = BuildPaginationBar(); pag.Dock = DockStyle.Bottom;
        lblNoData = UIHelper.CreateEmptyDataLabel("No customers yet. Add your first pet owner!");

        gridContainer.Controls.Add(lblNoData);
        gridContainer.Controls.Add(dgv);
        gridContainer.Controls.Add(pag);
        lblNoData.BringToFront();

        contentPanel.Controls.Add(gridContainer);
        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("Customers", "Manage pet owners and client contact details"));
    }

    private Panel BuildStatusBar()
    {
        var p = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White };
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20, 0, 0, 0), ForeColor = Color.FromArgb(90, 100, 115), Font = new Font("Segoe UI", 8.5f) };
        p.Controls.Add(lblStatus); p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 232, 235) }); return p;
    }

    private Panel BuildSearchBar()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 72 };
        var ico = new Label { Text = "🔍", Width = 26, Height = 38, Left = 20, Top = 17, TextAlign = ContentAlignment.MiddleCenter };
        txtSearch = new TextBox { Left = 46, Top = 20, Width = 240, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search name, phone, email..." };
        txtSearch.TextChanged += (_, _) => FilterData();

        cboStatus = new ComboBox { Left = txtSearch.Right + 10, Top = 20, Width = 130, Font = new Font("Segoe UI", 10f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboStatus.Items.AddRange(["Active Only", "Inactive Only", "All Customers"]);
        cboStatus.SelectedIndex = 0;
        cboStatus.SelectedIndexChanged += (_, _) => FilterData();

        var btnAdd   = UIHelper.CreateButton("+ Add", UIHelper.Success, 90, 38); btnAdd.Left   = cboStatus.Right + 14; btnAdd.Top   = 17; btnAdd.Click += BtnAdd_Click;
        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray,  80, 38); btnReset.Left = btnAdd.Right + 8;     btnReset.Top = 17;
        btnReset.Click += (_, _) => { txtSearch.Clear(); cboStatus.SelectedIndex = 0; LoadData(); };

        p.Controls.AddRange(new Control[] { ico, txtSearch, cboStatus, btnAdd, btnReset }); return p;
    }

    private Panel BuildPaginationBar()
    {
        var p = new Panel { Height = 60, BackColor = Color.White };
        p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 235, 240) });
        btnPrev = UIHelper.CreateButton("← Prev", Color.FromArgb(108, 117, 125), 80, 36);
        btnNext = UIHelper.CreateButton("Next →", Color.FromArgb(108, 117, 125), 80, 36);
        lblPage = new Label { AutoSize = false, Width = 110, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(64, 64, 64) };
        p.Resize += (_, _) => { btnNext.Left = p.Width - btnNext.Width - 16; btnNext.Top = 12; lblPage.Left = btnNext.Left - lblPage.Width - 10; lblPage.Top = 18; btnPrev.Left = lblPage.Left - btnPrev.Width - 10; btnPrev.Top = 12; };
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
        var q  = txtSearch.Text.Trim().ToLower();
        var st = cboStatus.SelectedIndex; // 0=Active, 1=Inactive, 2=All
        _filtered = _data.Where(x =>
            (st == 2 || (st == 0 ? x.IsActive : !x.IsActive)) &&
            (string.IsNullOrWhiteSpace(q) ||
             x.FullName?.ToLower().Contains(q) == true ||
             x.Phone?.ToLower().Contains(q)    == true ||
             x.Email?.ToLower().Contains(q)    == true)
        ).ToList();
        _currentPage = 1; RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (_filtered.Count == 0)
        {
            dgv.DataSource = null; lblStatus.Text = "0 records";
            btnPrev.Enabled = btnNext.Enabled = false; btnPrev.Visible = btnNext.Visible = lblPage.Visible = false;
            lblNoData.Visible = true; dgv.Visible = false; return;
        }
        lblNoData.Visible = false; dgv.Visible = true;
        var page = _filtered.Skip((_currentPage - 1) * _pageSize).Take(_pageSize)
            .Select(x => new { x.Id, x.FullName, x.Phone, x.Email, Status = x.IsActive ? "Active" : "Inactive", x.Address }).ToList();
        dgv.DataSource = page;
        if (dgv.Columns["Id"]       != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["FullName"] is { } c1) c1.HeaderText = "Full Name";
        if (dgv.Columns["Phone"]    is { } c2) c2.HeaderText = "Phone";
        if (dgv.Columns["Email"]    is { } c3) c3.HeaderText = "Email";
        if (dgv.Columns["Status"]   is { } c4) c4.HeaderText = "Status";
        if (dgv.Columns["Address"]  is { } c5) c5.HeaderText = "Address";
        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "", ReadOnly = true });
        DistributeColumns();
        int tp = GetTotalPages();
        lblStatus.Text = $"{_filtered.Count} records"; lblPage.Text = $"Page {_currentPage} / {tp}";
        btnPrev.Enabled = _currentPage > 1; btnNext.Enabled = _currentPage < tp;
        btnPrev.Visible = btnNext.Visible = lblPage.Visible = tp > 1;
    }

    private void DistributeColumns()
    {
        if (dgv.Columns.Count == 0) return;
        const int actionW     = 130;
        const int totalWeight = 730; // 180+120+180+80+170
        int available = dgv.ClientSize.Width - actionW - 2;
        if (available <= 0) return;
        if (dgv.Columns["ColAction"] is { } ca) { ca.Width = actionW; ca.DisplayIndex = dgv.Columns.Count - 1; }
        if (dgv.Columns["FullName"]  is { } c1) c1.Width = available * 180 / totalWeight;
        if (dgv.Columns["Phone"]     is { } c2) c2.Width = available * 120 / totalWeight;
        if (dgv.Columns["Email"]     is { } c3) c3.Width = available * 180 / totalWeight;
        if (dgv.Columns["Status"]    is { } c4) c4.Width = available *  80 / totalWeight;
        if (dgv.Columns["Address"]   is { } c5) c5.Width = available * 170 / totalWeight;
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
        using var dlg = new CustomerDialog(item, readOnly: true);
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
    private PictureBox picAvatar = null!;
    private Label lblHdrName = null!, lblHdrSub = null!, lblHdrStatus = null!;
    public Customer Result { get; private set; } = new();

    public CustomerDialog(Customer? existing = null, bool readOnly = false)
    {
        _readOnly = readOnly;
        Text = existing is null ? "Register New Customer"
             : (_readOnly ? $"Customer Profile — {existing.FullName}" : $"Edit Customer — {existing.FullName}");
        Size = new Size(900, 800);
        MinimumSize = new Size(820, 700);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        // ── Header band ──────────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = UIHelper.Primary };

        var avatarCol = new Panel { Width = 90, Dock = DockStyle.Left, BackColor = UIHelper.Primary };
        picAvatar = new PictureBox
        {
            Width = 62, Height = 62, Left = 14, Top = 16,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(255, 255, 255, 20),
            Image = UIHelper.CreateAvatar(existing?.FullName ?? "New", 62)
        };
        var ap = new System.Drawing.Drawing2D.GraphicsPath(); ap.AddEllipse(0, 0, 62, 62); picAvatar.Region = new Region(ap);
        avatarCol.Controls.Add(picAvatar);

        lblHdrName = new Label
        {
            Text = existing?.FullName ?? "New Customer",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.White,
            AutoSize = true, Margin = new Padding(0, 0, 0, 5)
        };
        lblHdrSub = new Label
        {
            Text = BuildSubText(existing),
            Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(195, 215, 240),
            AutoSize = true, Margin = new Padding(0, 0, 0, 6)
        };
        lblHdrStatus = new Label
        {
            Text = existing is null ? "Active" : (existing.IsActive ? "Active" : "Inactive"),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = (existing?.IsActive == false) ? Color.FromArgb(255, 160, 160) : Color.FromArgb(160, 255, 190),
            AutoSize = true
        };
        var textFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            BackColor = UIHelper.Primary, WrapContents = false,
            Padding = new Padding(0, 18, 16, 10)
        };
        textFlow.Controls.AddRange(new Control[] { lblHdrName, lblHdrSub, lblHdrStatus });
        header.Controls.Add(textFlow);
        header.Controls.Add(avatarCol);

        // ── Footer ───────────────────────────────────────────────────────
        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
        pnlBtn.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(225, 228, 235) });
        var btnSave   = UIHelper.CreateButton(_readOnly ? "Close" : "Save Customer", UIHelper.Success, _readOnly ? 100 : 130);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.FromArgb(108, 117, 125), 100);
        btnSave.Top = btnCancel.Top = 11;
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pnlBtn.Resize += (_, _) => { btnSave.Left = pnlBtn.Width - btnSave.Width - 12; btnCancel.Left = btnSave.Left - 108; };
        if (_readOnly) btnSave.Click += (_, _) => DialogResult = DialogResult.OK;
        else btnSave.Click += Save;
        btnCancel.Visible = !_readOnly;
        btnCancel.DialogResult = DialogResult.Cancel;
        pnlBtn.Controls.AddRange(new Control[] { btnSave, btnCancel });

        // ── Scrollable form body ──────────────────────────────────────────
        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true,
            Padding = new Padding(24, 10, 8, 24), BackColor = Color.White
        };

        void SyncWidths()
        {
            int w = body.ClientSize.Width - body.Padding.Left - body.Padding.Right;
            if (w <= 10) return;
            foreach (Control c in body.Controls)
                if (c is TableLayoutPanel || c is Label || c is TextBox)
                    c.Width = w;
        }
        body.ClientSizeChanged += (_, _) => SyncWidths();

        TableLayoutPanel MakeGrid(int rows)
        {
            var g = new TableLayoutPanel
            {
                Height = rows * 76, ColumnCount = 2, RowCount = rows,
                Margin = new Padding(0, 0, 0, 4), Padding = new Padding(0)
            };
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < rows; i++) g.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            return g;
        }

        Panel WrapCell(string caption, Control ctrl, bool rightCol = false, bool stretchCtrl = true)
        {
            var p = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 2, rightCol ? 0 : 14, 0) };
            var lbl = new Label
            {
                Text = caption, Top = 0, Left = 0, Height = 20, AutoSize = false,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 90, 105)
            };
            lbl.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            ctrl.Top = 22; ctrl.Left = 0;
            ctrl.Anchor = stretchCtrl
                ? AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                : AnchorStyles.Left | AnchorStyles.Top;
            p.Controls.AddRange(new Control[] { lbl, ctrl });
            return p;
        }

        Label MakeSection(string text) => new Label
        {
            Text = text, Height = 36, Margin = new Padding(0, 8, 0, 0),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(110, 125, 148),
            Padding = new Padding(0, 14, 0, 0), AutoSize = false
        };

        // ── PRIMARY CONTACT INFO ──────────────────────────────────────────
        txtName  = new TextBox { Font = new Font("Segoe UI", 10.5f) };
        txtPhone = new TextBox { Font = new Font("Segoe UI", 10.5f) };
        txtEmail = new TextBox { Font = new Font("Segoe UI", 10.5f) };
        txtName.TextChanged  += (_, _) => UpdateHeader();
        txtPhone.TextChanged += (_, _) => UpdateHeader();

        chkActive = new CheckBox { Text = "Active", Checked = true, Font = new Font("Segoe UI", 10f), AutoSize = false, Height = 26 };
        chkActive.CheckedChanged += (_, _) =>
        {
            chkActive.Text = chkActive.Checked ? "Active" : "Inactive";
            UpdateHeader();
        };

        var gridContact = MakeGrid(2);
        gridContact.Controls.Add(WrapCell("Full Name *",    txtName),              0, 0);
        gridContact.Controls.Add(WrapCell("Phone Number *", txtPhone, true),       1, 0);
        gridContact.Controls.Add(WrapCell("Email Address",  txtEmail),             0, 1);
        gridContact.Controls.Add(WrapCell("Status", chkActive, true, true),        1, 1);

        // ── LOCATION & BACKGROUND ─────────────────────────────────────────
        txtAddress = new TextBox
        {
            Multiline = true, Height = 100, Margin = new Padding(0),
            Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "Street, City, Province / State..."
        };
        txtNotes = new TextBox
        {
            Multiline = true, Height = 100, Margin = new Padding(0),
            Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "Allergies, payment notes, special instructions..."
        };

        body.Controls.Add(MakeSection("PRIMARY CONTACT INFO"));
        body.Controls.Add(gridContact);
        body.Controls.Add(MakeSection("PHYSICAL ADDRESS"));
        body.Controls.Add(txtAddress);
        body.Controls.Add(MakeSection("INTERNAL NOTES"));
        body.Controls.Add(txtNotes);

        Controls.Add(body); Controls.Add(pnlBtn); Controls.Add(header);
        AcceptButton = btnSave; CancelButton = btnCancel;

        this.Load += (_, _) =>
        {
            SyncWidths();
            if (existing is not null)
            {
                txtName.Text = existing.FullName; txtPhone.Text = existing.Phone;
                txtEmail.Text = existing.Email; txtAddress.Text = existing.Address;
                txtNotes.Text = existing.Notes; chkActive.Checked = existing.IsActive;
                chkActive.Text = existing.IsActive ? "Active" : "Inactive";
                Result.Id = existing.Id;
                if (_readOnly)
                {
                    txtName.ReadOnly = txtPhone.ReadOnly = txtEmail.ReadOnly =
                    txtAddress.ReadOnly = txtNotes.ReadOnly = true;
                    txtName.BackColor = txtPhone.BackColor = txtEmail.BackColor =
                    txtAddress.BackColor = txtNotes.BackColor = Color.White;
                    chkActive.Enabled = false;
                }
            }
            UpdateHeader();
            SyncWidths();
            body.AutoScrollPosition = new Point(0, 0);
            txtName.Select();
        };
    }

    private static string BuildSubText(Customer? c)
    {
        if (c is null) return "Phone: —  ·  Email: —";
        var phone = string.IsNullOrWhiteSpace(c.Phone) ? "—" : c.Phone;
        var email = string.IsNullOrWhiteSpace(c.Email) ? "—" : c.Email;
        return $"Phone: {phone}  ·  Email: {email}";
    }

    private void UpdateHeader()
    {
        lblHdrName.Text = string.IsNullOrWhiteSpace(txtName.Text) ? "New Customer" : txtName.Text.Trim();
        var phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? "—" : txtPhone.Text.Trim();
        var email = string.IsNullOrWhiteSpace(txtEmail.Text) ? "—" : txtEmail.Text.Trim();
        lblHdrSub.Text = $"Phone: {phone}  ·  Email: {email}";
        lblHdrStatus.Text = chkActive.Checked ? "Active" : "Inactive";
        lblHdrStatus.ForeColor = chkActive.Checked ? Color.FromArgb(160, 255, 190) : Color.FromArgb(255, 160, 160);
        picAvatar.Image = UIHelper.CreateAvatar(lblHdrName.Text, 62);
    }

    private void Save(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        { VetMS.Forms.CustomMessageBox.Show("Full name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtName.Focus(); return; }
        if (string.IsNullOrWhiteSpace(txtPhone.Text))
        { VetMS.Forms.CustomMessageBox.Show("Phone is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtPhone.Focus(); return; }
        Result = new Customer
        {
            Id = Result.Id, FullName = txtName.Text.Trim(), Phone = txtPhone.Text.Trim(),
            Email = txtEmail.Text.Trim(), Address = txtAddress.Text.Trim(),
            Notes = txtNotes.Text.Trim(), IsActive = chkActive.Checked
        };
        DialogResult = DialogResult.OK;
    }
}
