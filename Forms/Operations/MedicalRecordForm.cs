using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class MedicalRecordForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Label lblTotal = null!;
    private Label lblPageInfo = null!;
    private Label lblRppCount = null!;
    private Button btnPrev = null!;
    private Button btnNext = null!;
    private ComboBox cboPerPage = null!;
    private Label lblNoData = null!;

    private int _currentPage = 1;
    private int _pageSize = 10;
    private List<MedicalRecord> _data = [];
    private List<MedicalRecord> _filtered = [];

    public MedicalRecordForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Medical Records";
        BackColor = Color.FromArgb(245, 247, 250);

        Controls.Add(BuildGridCard());
        Controls.Add(BuildFooter());
        Controls.Add(BuildToolbar());
        Controls.Add(UIHelper.CreateHeader("Medical Records", "Clinical notes, diagnoses and prescriptions"));
    }

    // ── Toolbar ──────────────────────────────────────────────────────────────
    private Panel BuildToolbar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12)
        };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 228, 235));
            e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        };

        var searchPanel = new Panel { Top = 12, Left = 16, Width = 300, Height = 36, BackColor = Color.White };
        searchPanel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(210, 215, 225));
            using var path = RoundRect(new Rectangle(0, 0, searchPanel.Width - 1, searchPanel.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
        };
        var icoSearch = new Label { Text = "🔍", Width = 28, Left = 6, Top = 5, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
        txtSearch = new TextBox
        {
            Left = 32, Top = 7, Width = 258,
            Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.None,
            BackColor = Color.White, PlaceholderText = "Search pet, owner, vet, diagnosis..."
        };
        txtSearch.TextChanged += (_, _) => FilterData();
        searchPanel.Controls.AddRange(new Control[] { icoSearch, txtSearch });

        var btnAdd = MakeButton("+ New Record", UIHelper.Success, 130, 36);
        btnAdd.Top = 12; btnAdd.Left = searchPanel.Right + 8;
        btnAdd.Click += BtnAdd_Click;

        bar.Controls.Add(searchPanel);
        bar.Controls.Add(btnAdd);
        return bar;
    }

    // ── Grid card ─────────────────────────────────────────────────────────────
    private Panel BuildGridCard()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(16, 12, 16, 12) };
        var tableBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        tableBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(220, 223, 230));
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, tableBox.Width - 1, tableBox.Height - 1));
        };

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill, Margin = new Padding(1), BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            ReadOnly = true, RowHeadersVisible = false, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
            Cursor = Cursors.Hand, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(235, 238, 242)
        };

        dgv.ColumnHeadersHeight = 42;
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White, ForeColor = Color.FromArgb(60, 70, 90),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0), SelectionBackColor = Color.White,
            SelectionForeColor = Color.FromArgb(60, 70, 90)
        };
        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(30, 40, 60),
            BackColor = Color.White, SelectionBackColor = Color.FromArgb(235, 230, 255),
            SelectionForeColor = Color.FromArgb(30, 40, 60), Padding = new Padding(8, 0, 0, 0)
        };
        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White, SelectionBackColor = Color.FromArgb(235, 230, 255),
            SelectionForeColor = Color.FromArgb(30, 40, 60)
        };
        dgv.RowTemplate.Height = 40;

        dgv.CellPainting += DgvCellPainting;
        dgv.CellMouseClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Button != MouseButtons.Left) return;
            if (dgv.Columns[e.ColumnIndex].Name != "ColAction") return;
            UIHelper.HandleDynamicActionColumnClick(dgv, e, ("View", ViewRow), ("Edit", EditRow));
        };
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") ViewRow(e.RowIndex); };

        lblNoData = UIHelper.CreateEmptyDataLabel("No medical records yet.");
        tableBox.Controls.Add(lblNoData);
        tableBox.Controls.Add(dgv);
        lblNoData.BringToFront();
        outer.Controls.Add(tableBox);
        return outer;
    }

    // ── Footer ────────────────────────────────────────────────────────────────
    private Panel BuildFooter()
    {
        var bar = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 228, 235));
            e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
        };

        lblTotal = new Label { Text = "Total records: 0", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = UIHelper.Success, AutoSize = true, Top = 16 };
        var lblShow = new Label { Text = "Show", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(80, 90, 110), AutoSize = true, Top = 18 };
        cboPerPage = new ComboBox { Width = 65, Top = 13, Font = new Font("Segoe UI", 9f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboPerPage.Items.AddRange(new object[] { 10, 25, 50, 100 });
        cboPerPage.SelectedIndex = 0;
        cboPerPage.SelectedIndexChanged += (_, _) => { _pageSize = (int)cboPerPage.SelectedItem!; _currentPage = 1; RefreshGrid(); };

        lblRppCount = new Label { Text = "(0) Records per page", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(80, 90, 110), AutoSize = true, Top = 18 };
        lblPageInfo = new Label { Text = "Showing page 1 of 1 Pages", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(80, 90, 110), AutoSize = true, Top = 18 };

        btnPrev = MakeButton("Prev", UIHelper.Accent, 70, 32); btnPrev.Top = 10;
        btnNext = MakeButton("Next", UIHelper.Success, 70, 32); btnNext.Top = 10;
        btnPrev.Click += (_, _) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
        btnNext.Click += (_, _) => { if (_currentPage < GetTotalPages()) { _currentPage++; RefreshGrid(); } };

        bar.Controls.AddRange(new Control[] { lblTotal, lblShow, cboPerPage, lblRppCount, lblPageInfo, btnPrev, btnNext });
        bar.Resize += (_, _) =>
        {
            lblTotal.Left = 16;
            int gw = lblShow.Width + 4 + cboPerPage.Width + 4 + lblRppCount.Width;
            int cx = (bar.Width - gw) / 2;
            lblShow.Left = cx; cboPerPage.Left = lblShow.Right + 4; lblRppCount.Left = cboPerPage.Right + 4;
            btnNext.Left = bar.Width - btnNext.Width - 16; btnPrev.Left = btnNext.Left - btnPrev.Width - 8;
            lblPageInfo.Left = btnPrev.Left - lblPageInfo.Width - 16; lblPageInfo.Top = 18;
        };
        return bar;
    }

    // ── Cell painting ─────────────────────────────────────────────────────────
    private void DgvCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex == -1)
        {
            e.PaintBackground(e.CellBounds, false); e.PaintContent(e.CellBounds);
            using var pen = new Pen(UIHelper.Accent, 2f);
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 2, e.CellBounds.Right, e.CellBounds.Bottom - 2);
            e.Handled = true; return;
        }
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "ColAction")
        {
            UIHelper.PaintDynamicActionColumn(dgv, e, "View", "Edit");
            return;
        }
    }

    // ── Data ──────────────────────────────────────────────────────────────────
    private void LoadData()
    {
        try { _data = DataStore.GetMedicalRecords() ?? []; }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        _filtered = string.IsNullOrWhiteSpace(q) ? _data
            : _data.Where(x =>
                x.PetName?.ToLower().Contains(q)      == true ||
                x.CustomerName?.ToLower().Contains(q) == true ||
                x.Diagnosis?.ToLower().Contains(q)    == true ||
                x.VetName?.ToLower().Contains(q)      == true).ToList();
        _currentPage = 1; RefreshGrid();
    }

    private void RefreshGrid()
    {
        int total = _filtered.Count;
        bool empty = total == 0;
        lblNoData.Visible = empty; dgv.Visible = !empty;
        lblTotal.Text = $"Total records: {total}";
        int totalPages = GetTotalPages();
        btnPrev.Enabled = _currentPage > 1; btnNext.Enabled = _currentPage < totalPages;

        if (empty) { lblPageInfo.Text = "Showing page 1 of 1 Pages"; lblRppCount.Text = "(0) Records per page"; return; }

        int skip = (_currentPage - 1) * _pageSize;
        var page = _filtered.Skip(skip).Take(_pageSize)
            .Select((x, i) => new
            {
                No = skip + i + 1,
                x.Id,
                Date      = x.CreatedAt.ToString("yyyy-MM-dd"),
                Patient   = x.PetName,
                Owner     = x.CustomerName,
                Vet       = x.VetName,
                Diagnosis = x.Diagnosis.Length > 60 ? x.Diagnosis[..57] + "..." : x.Diagnosis,
                FollowUp  = x.FollowUpDate?.ToString("yyyy-MM-dd") ?? "-"
            }).ToList();

        dgv.DataSource = page;
        if (dgv.Columns["Id"] is { } cId) cId.Visible = false;
        if (dgv.Columns["No"] is { } cNo) { cNo.HeaderText = "#"; cNo.FillWeight = 5; }
        if (dgv.Columns["Date"] is { } cDate) { cDate.HeaderText = "Date"; cDate.FillWeight = 12; }
        if (dgv.Columns["Patient"] is { } cPat) { cPat.HeaderText = "Patient"; cPat.FillWeight = 15; }
        if (dgv.Columns["Owner"] is { } cOwn) { cOwn.HeaderText = "Owner"; cOwn.FillWeight = 15; }
        if (dgv.Columns["Vet"] is { } cVet) { cVet.HeaderText = "Veterinarian"; cVet.FillWeight = 15; }
        if (dgv.Columns["Diagnosis"] is { } cDiag) { cDiag.HeaderText = "Diagnosis"; cDiag.FillWeight = 25; }
        if (dgv.Columns["FollowUp"] is { } cFol) { cFol.HeaderText = "Follow-up"; cFol.FillWeight = 13; }

        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "Action", ReadOnly = true, FillWeight = 15 });

        lblPageInfo.Text = $"Showing page {_currentPage} of {totalPages} Pages";
        lblRppCount.Text = $"({Math.Min(_pageSize, total - skip)}) Records per page";
    }

    private int GetTotalPages() => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new MedicalRecordDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Insert(dlg.Result); DataStore.SaveRecordMedications(dlg.Result.Id, dlg.Prescriptions); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Medical record saved!"); LoadData();
    }

    private void ViewRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); 
        if (item is null) return;

        MainForm.Instance.LoadForm(new MedicalRecordDetailForm(item, () => MainForm.Instance.LoadForm(new MedicalRecordForm())));
    }

    private void EditRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new MedicalRecordDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var r = dlg.Result;
        item.AppointmentId = r.AppointmentId; item.PetId = r.PetId; item.PetName = r.PetName;
        item.CustomerId = r.CustomerId; item.CustomerName = r.CustomerName;
        item.VetId = r.VetId; item.VetName = r.VetName;
        item.Diagnosis = r.Diagnosis; item.Treatment = r.Treatment; item.Notes = r.Notes; item.FollowUpDate = r.FollowUpDate;
        try { DataStore.Update(item); DataStore.SaveRecordMedications(item.Id, dlg.Prescriptions); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Medical record updated!"); LoadData();
    }

    private static Button MakeButton(string text, Color back, int w, int h)
    {
        var btn = new Button
        {
            Text = text, Width = w, Height = h, BackColor = back, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btn.FlatAppearance.BorderSize = 0; btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(back, 0.1f);
        ApplyRound(btn, 6); btn.SizeChanged += (_, _) => ApplyRound(btn, 6); return btn;
    }

    private static void ApplyRound(Control c, int r)
    {
        if (c.Width <= 0 || c.Height <= 0) return;
        c.Region = new Region(RoundRect(new Rectangle(0, 0, c.Width, c.Height), r));
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundRect(Rectangle rc, int r)
    {
        int d = r * 2;
        var p = new System.Drawing.Drawing2D.GraphicsPath();
        p.AddArc(rc.Left, rc.Top, d, d, 180, 90);
        p.AddArc(rc.Right - d, rc.Top, d, d, 270, 90);
        p.AddArc(rc.Right - d, rc.Bottom - d, d, d, 0, 90);
        p.AddArc(rc.Left, rc.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}

public class MedicalRecordDialog : Form
{
    private readonly ComboBox cboVet;
    private readonly TextBox txtDiagnosis, txtTreatment, txtNotes;
    private readonly DateTimePicker dtpFollowUp;
    private readonly CheckBox chkFollowUp;
    private readonly DataGridView dgvMeds, dgvAppt;
    private readonly TextBox txtApptSearch;
    private readonly Label lblSecondary, lblApptSelected;
    private readonly Panel pnlApptSelected;
    private readonly List<Appointment> _appointments;
    private readonly List<User> _vets;
    private readonly List<Medication> _meds;
    private readonly bool _readOnly;
    private Appointment? _selectedAppointment;

    public MedicalRecord Result { get; private set; } = new();
    public List<(int MedId, string Dosage, string Notes)> Prescriptions { get; private set; } = [];

    public MedicalRecordDialog(MedicalRecord? existing = null, bool readOnly = false)
    {
        _readOnly = readOnly;
        Text = existing is null ? "New Clinical Note" : (_readOnly ? "Clinical Note — View" : $"Edit Clinical Note");

        var screen = Screen.FromPoint(MousePosition);
        int formWidth  = Math.Min(1200, (int)(screen.WorkingArea.Width  * 0.88));
        int formHeight = Math.Min(980,  (int)(screen.WorkingArea.Height * 0.94));
        Size = new Size(formWidth, formHeight);
        MinimumSize = new Size(960, 820);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        _appointments = DataStore.GetAppointments()
            .Where(a => a.Status == "Scheduled" || a.Status == "In Progress" || a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDate).ToList();
        _vets = DataStore.GetUsers()
            .Where(u => u.IsActive && (u.Role == "Veterinarian" || u.Role == "Administrator")).ToList();
        _meds = DataStore.GetMedications().Where(m => m.IsActive).ToList();

        int cw = formWidth - 100;

        // ── Header band ──────────────────────────────────────────────────
        var hdr = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = UIHelper.Primary };

        var iconCol = new Panel { Width = 90, Dock = DockStyle.Left, BackColor = UIHelper.Primary };
        iconCol.Controls.Add(new Label
        {
            Text = "🩺", Left = 14, Top = 28, Width = 62, Height = 62,
            Font = new Font("Segoe UI", 22f), ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
        });

        var lblTitle = new Label
        {
            Text = existing is null ? "New Clinical Entry" : "Medical History Record",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.White,
            AutoSize = true, Margin = new Padding(0, 0, 0, 5)
        };
        lblSecondary = new Label
        {
            Text = "Select an appointment below to link this clinical entry",
            Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(195, 215, 240),
            AutoSize = true, Margin = new Padding(0, 0, 0, 0)
        };
        var textFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            BackColor = UIHelper.Primary, WrapContents = false,
            Padding = new Padding(0, 18, 16, 10)
        };
        textFlow.Controls.AddRange(new Control[] { lblTitle, lblSecondary });
        hdr.Controls.Add(textFlow);
        hdr.Controls.Add(iconCol);

        // ── Footer ───────────────────────────────────────────────────────
        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
        pnlBtn.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(225, 228, 235) });
        var btnSave   = UIHelper.CreateButton(_readOnly ? "Done" : "Save Record", UIHelper.Success, 120);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.FromArgb(108, 117, 125), 100);
        btnSave.Top = btnCancel.Top = 11;
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pnlBtn.Resize += (_, _) => { btnSave.Left = pnlBtn.Width - btnSave.Width - 12; btnCancel.Left = btnSave.Left - 108; };
        btnCancel.Visible = !_readOnly;
        if (_readOnly) btnSave.Click += (_, _) => DialogResult = DialogResult.OK;
        else btnSave.Click += Save;
        btnCancel.DialogResult = DialogResult.Cancel;
        pnlBtn.Controls.AddRange(new Control[] { btnSave, btnCancel });

        // ── Scrollable flow ───────────────────────────────────────────────
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(40, 18, 40, 20), AutoScroll = true
        };

        // ── SECTION 1 : APPOINTMENT PICKER ───────────────────────────────
        flow.Controls.Add(UIHelper.CreateSectionLabel("LINKED APPOINTMENT *"));

        var pnlApptSearch = new Panel { Width = cw, Height = 36, Margin = new Padding(0, 0, 0, 6) };
        var icoSearch = new Label { Text = "🔍", Left = 0, Top = 6, Width = 24, Height = 24, TextAlign = ContentAlignment.MiddleCenter };
        txtApptSearch = new TextBox
        {
            Left = 26, Top = 4, Width = cw - 26,
            Font = new Font("Segoe UI", 10f),
            PlaceholderText = "Search by pet name, owner, or service type..."
        };
        txtApptSearch.TextChanged += (_, _) => RefreshApptGrid();
        pnlApptSearch.Controls.AddRange(new Control[] { icoSearch, txtApptSearch });
        pnlApptSearch.Visible = !_readOnly;
        flow.Controls.Add(pnlApptSearch);

        dgvAppt = new DataGridView
        {
            Width = cw, Height = 170, ReadOnly = true, MultiSelect = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BorderStyle = BorderStyle.FixedSingle, BackgroundColor = Color.White,
            Font = new Font("Segoe UI", 9.5f), Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 8)
        };
        UIHelper.StyleGrid(dgvAppt);
        dgvAppt.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "ColApptId",  Visible = false },
            new DataGridViewTextBoxColumn { Name = "ColDate",    HeaderText = "Date & Time",  Width = 140 },
            new DataGridViewTextBoxColumn { Name = "ColPet",     HeaderText = "Pet",          Width = 130 },
            new DataGridViewTextBoxColumn { Name = "ColOwner",   HeaderText = "Owner",        Width = 160 },
            new DataGridViewTextBoxColumn { Name = "ColService", HeaderText = "Service",      FillWeight = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { Name = "ColStatus",  HeaderText = "Status",       Width = 100 }
        );
        dgvAppt.SelectionChanged += DgvAppt_SelectionChanged;
        dgvAppt.Visible = !_readOnly;
        flow.Controls.Add(dgvAppt);

        pnlApptSelected = new Panel
        {
            Width = cw, Height = 42, BackColor = Color.FromArgb(237, 247, 240),
            Visible = false, Margin = new Padding(0, 0, 0, 12)
        };
        pnlApptSelected.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 5, BackColor = UIHelper.Success });
        lblApptSelected = new Label
        {
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 100, 50)
        };
        pnlApptSelected.Controls.Add(lblApptSelected);
        flow.Controls.Add(pnlApptSelected);

        // ── SECTION 2 : VET ───────────────────────────────────────────────
        flow.Controls.Add(UIHelper.CreateSectionLabel("ATTENDING VET"));
        var vetList = new List<object> { new User { Id = 0, FullName = "(Not assigned)" } };
        vetList.AddRange(_vets.Cast<object>());
        cboVet = new ComboBox
        {
            Width = (int)(cw * 0.42), Font = new Font("Segoe UI", 10.5f),
            DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 16)
        };
        cboVet.DataSource = vetList; cboVet.DisplayMember = "FullName"; cboVet.ValueMember = "Id";
        flow.Controls.Add(UIHelper.WrapControl("Veterinarian", cboVet));

        // ── SECTION 3 : DIAGNOSIS & TREATMENT ────────────────────────────
        flow.Controls.Add(UIHelper.CreateSectionLabel("DIAGNOSIS & TREATMENT"));
        int halfW = (cw - 20) / 2;
        var tblDT = new TableLayoutPanel { Width = cw, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 4) };
        tblDT.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, halfW + 20));
        tblDT.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, halfW + 20));
        txtDiagnosis = new TextBox { Width = halfW, Height = 90, Multiline = true, Font = new Font("Segoe UI", 10.5f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 12) };
        txtTreatment = new TextBox { Width = halfW, Height = 90, Multiline = true, Font = new Font("Segoe UI", 10.5f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 12) };
        tblDT.Controls.Add(UIHelper.WrapControl("Primary Diagnosis *",          txtDiagnosis), 0, 0);
        tblDT.Controls.Add(UIHelper.WrapControl("Treatment Plan & Summary",     txtTreatment), 1, 0);
        flow.Controls.Add(tblDT);

        flow.Controls.Add(UIHelper.CreateFormLabel("Internal Clinical Notes"));
        txtNotes = new TextBox { Width = cw, Height = 58, Multiline = true, Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 16) };
        flow.Controls.Add(txtNotes);

        // ── SECTION 4 : PHARMACEUTICALS ──────────────────────────────────
        flow.Controls.Add(UIHelper.CreateSectionLabel("PHARMACEUTICALS"));
        dgvMeds = new DataGridView
        {
            Width = cw, Height = 150,
            AllowUserToAddRows = !_readOnly, AllowUserToDeleteRows = !_readOnly,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9.5f), RowHeadersVisible = false,
            BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 16),
            BackgroundColor = Color.White
        };
        dgvMeds.Columns.AddRange(
            new DataGridViewComboBoxColumn { Name = "Medication", HeaderText = "Medication", DataSource = _meds, DisplayMember = "Name", ValueMember = "Id" },
            new DataGridViewTextBoxColumn  { Name = "Dosage",     HeaderText = "Dosage / Instructions", FillWeight = 50 },
            new DataGridViewTextBoxColumn  { Name = "PNotes",     HeaderText = "Pharmacy Notes",        FillWeight = 40 }
        );
        UIHelper.StyleGrid(dgvMeds);
        flow.Controls.Add(dgvMeds);

        // ── FOLLOW-UP ─────────────────────────────────────────────────────
        flow.Controls.Add(UIHelper.CreateSectionLabel("FOLLOW-UP"));
        chkFollowUp = new CheckBox { Text = "Requires Follow-up Visit", Font = new Font("Segoe UI", 10f), AutoSize = false, Width = 240, Height = 26 };
        dtpFollowUp = new DateTimePicker { Width = 200, Font = new Font("Segoe UI", 10f), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7), Enabled = false };
        chkFollowUp.CheckedChanged += (_, _) => dtpFollowUp.Enabled = chkFollowUp.Checked;
        var tblFU = new TableLayoutPanel { Width = cw, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 24) };
        tblFU.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        tblFU.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        tblFU.Controls.Add(UIHelper.WrapControl("Schedule Follow-up", chkFollowUp), 0, 0);
        tblFU.Controls.Add(UIHelper.WrapControl("Follow-up Date",     dtpFollowUp), 1, 0);
        flow.Controls.Add(tblFU);
        flow.Controls.Add(new Panel { Width = cw, Height = 10 });

        Controls.Add(flow); Controls.Add(pnlBtn); Controls.Add(hdr);
        AcceptButton = btnSave; CancelButton = btnCancel;

        RefreshApptGrid();

        // ── POPULATE EXISTING RECORD ──────────────────────────────────────
        if (existing is not null)
        {
            var apt = _appointments.FirstOrDefault(a => a.Id == existing.AppointmentId);
            if (apt != null) SelectAppointmentInGrid(apt);

            var vet = vetList.OfType<User>().FirstOrDefault(v => v.Id == (existing.VetId ?? 0));
            if (vet != null) cboVet.SelectedItem = vet;

            txtDiagnosis.Text = existing.Diagnosis;
            txtTreatment.Text = existing.Treatment;
            txtNotes.Text     = existing.Notes;

            if (existing.FollowUpDate.HasValue) { chkFollowUp.Checked = true; dtpFollowUp.Value = existing.FollowUpDate.Value; }

            Result.Id = existing.Id;

            var rxList = DataStore.GetRecordMedications(existing.Id);
            foreach (var rx in rxList)
            {
                var r = dgvMeds.Rows.Add();
                dgvMeds.Rows[r].Cells["Medication"].Value = rx.MedicationId;
                dgvMeds.Rows[r].Cells["Dosage"].Value     = rx.Dosage;
                dgvMeds.Rows[r].Cells["PNotes"].Value     = rx.Notes;
            }

            if (_readOnly)
            {
                txtApptSearch.Enabled = dgvAppt.Enabled = false;
                cboVet.Enabled = chkFollowUp.Enabled = dtpFollowUp.Enabled = false;
                txtDiagnosis.ReadOnly = txtTreatment.ReadOnly = txtNotes.ReadOnly = true;
                txtDiagnosis.BackColor = txtTreatment.BackColor = txtNotes.BackColor = Color.White;
                dgvMeds.ReadOnly = true; dgvMeds.Enabled = false;
            }
        }
    }

    // ── APPOINTMENT GRID HELPERS ──────────────────────────────────────────────

    private void RefreshApptGrid()
    {
        var q = txtApptSearch.Text.Trim().ToLower();
        var src = string.IsNullOrWhiteSpace(q) ? _appointments
            : _appointments.Where(a =>
                a.PetName?.ToLower().Contains(q)          == true ||
                a.CustomerName?.ToLower().Contains(q)     == true ||
                a.ServiceTypeName?.ToLower().Contains(q)  == true).ToList();
        dgvAppt.Rows.Clear();
        foreach (var a in src)
        {
            int idx = dgvAppt.Rows.Add();
            var row = dgvAppt.Rows[idx];
            row.Cells["ColApptId"].Value  = a.Id;
            row.Cells["ColDate"].Value    = a.AppointmentDate.ToString("MMM dd, yyyy  HH:mm");
            row.Cells["ColPet"].Value     = a.PetName;
            row.Cells["ColOwner"].Value   = a.CustomerName;
            row.Cells["ColService"].Value = a.ServiceTypeName ?? "—";
            row.Cells["ColStatus"].Value  = a.Status;
            row.Cells["ColStatus"].Style.ForeColor = a.Status == "Completed" ? Color.FromArgb(30, 130, 76) : Color.FromArgb(180, 100, 0);
            row.Cells["ColStatus"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            if (_selectedAppointment?.Id == a.Id) row.Selected = true;
        }
    }

    private void SelectAppointmentInGrid(Appointment apt)
    {
        _selectedAppointment = apt;
        foreach (DataGridViewRow row in dgvAppt.Rows)
        {
            if (row.Cells["ColApptId"].Value is int id && id == apt.Id)
            {
                row.Selected = true;
                dgvAppt.FirstDisplayedScrollingRowIndex = row.Index;
                break;
            }
        }
        UpdateSelectedBadge();
    }

    private void DgvAppt_SelectionChanged(object? s, EventArgs e)
    {
        if (dgvAppt.SelectedRows.Count == 0) return;
        var row = dgvAppt.SelectedRows[0];
        if (row.Cells["ColApptId"].Value is int id)
        {
            _selectedAppointment = _appointments.FirstOrDefault(a => a.Id == id);
            UpdateSelectedBadge();
        }
    }

    private void UpdateSelectedBadge()
    {
        if (_selectedAppointment is not Appointment a)
        {
            pnlApptSelected.Visible = false;
            lblSecondary.Text = "Select an appointment below to link this clinical entry";
            return;
        }
        pnlApptSelected.Visible = true;
        lblApptSelected.Text  = $"✔  Selected: {a.PetName} — {a.CustomerName}  |  {a.AppointmentDate:MMM dd, yyyy  HH:mm}  |  {a.ServiceTypeName ?? "—"}";
        lblSecondary.Text     = $"Patient: {a.PetName}  ·  Owner: {a.CustomerName}  ·  Visit: {a.AppointmentDate:MMM dd, yyyy}";
    }

    // ── SAVE ──────────────────────────────────────────────────────────────────

    private void Save(object? s, EventArgs e)
    {
        if (_selectedAppointment is not Appointment apt)
        {
            VetMS.Forms.CustomMessageBox.Show("Please select a linked appointment from the list.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
        }
        if (string.IsNullOrWhiteSpace(txtDiagnosis.Text))
        {
            VetMS.Forms.CustomMessageBox.Show("Diagnosis is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtDiagnosis.Focus(); return;
        }
        var vet = cboVet.SelectedItem as User;
        Result = new MedicalRecord
        {
            Id           = Result.Id,
            AppointmentId = apt.Id,
            PetId        = apt.PetId,        PetName      = apt.PetName,
            CustomerId   = apt.CustomerId,   CustomerName = apt.CustomerName,
            VetId        = (vet?.Id ?? 0) == 0 ? null : vet?.Id,
            VetName      = vet?.FullName ?? "",
            Diagnosis    = txtDiagnosis.Text.Trim(),
            Treatment    = txtTreatment.Text.Trim(),
            Notes        = txtNotes.Text.Trim(),
            FollowUpDate = chkFollowUp.Checked ? dtpFollowUp.Value : null
        };
        Prescriptions = [];
        foreach (DataGridViewRow r in dgvMeds.Rows)
        {
            if (r.IsNewRow) continue;
            if (r.Cells["Medication"].Value is int medId && medId > 0)
                Prescriptions.Add((medId, r.Cells["Dosage"].Value?.ToString() ?? "", r.Cells["PNotes"].Value?.ToString() ?? ""));
        }
        DialogResult = DialogResult.OK;
    }
}
