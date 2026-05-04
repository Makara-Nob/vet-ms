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
    // ── Controls ───────────────────────────────────────────────────────────────
    private readonly ComboBox cboVet, cboApptFilter;
    private readonly TextBox txtDiagnosis, txtTreatment, txtNotes, txtApptSearch;
    private readonly DateTimePicker dtpFollowUp;
    private readonly CheckBox chkFollowUp;
    private readonly DataGridView dgvMeds, dgvAppt;
    private readonly Label lblHeaderSub, lblSelectedCard;
    private readonly Panel pnlSelectedCard, pnlRightContext;
    private readonly Label lblRightContext;

    // ── Data ───────────────────────────────────────────────────────────────────
    private readonly List<Appointment> _appointments;
    private readonly List<User> _vets;
    private readonly List<Medication> _meds;
    private readonly bool _readOnly;
    private Appointment? _selectedAppointment;
    private List<object> _vetList = [];
    private bool _medFilterBusy;

    public MedicalRecord Result { get; private set; } = new();
    public List<(int MedId, string Dosage, string Notes)> Prescriptions { get; private set; } = [];

    public MedicalRecordDialog(MedicalRecord? existing = null, bool readOnly = false)
    {
        _readOnly = readOnly;
        Text = existing is null ? "New Clinical Note" : (readOnly ? "Clinical Note — View" : "Edit Clinical Note");

        var screen = Screen.FromPoint(MousePosition);
        int formWidth  = Math.Min(1180, (int)(screen.WorkingArea.Width  * 0.88));
        int formHeight = Math.Min(900,  (int)(screen.WorkingArea.Height * 0.92));
        Size = new Size(formWidth, formHeight);
        MinimumSize = new Size(1220, 700);
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

        // ── Header ───────────────────────────────────────────────────────
        lblHeaderSub = new Label
        {
            Text = existing is null ? "Select an appointment to link this clinical entry"
                                    : (readOnly ? "Read-only view" : "Editing existing medical record"),
            Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(185, 210, 240),
            AutoSize = true, Left = 70, Top = 42
        };
        var hdr = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = UIHelper.Primary };
        hdr.Controls.Add(new Label
        {
            Text = "🩺", Left = 14, Top = 12, Width = 46, Height = 44,
            Font = new Font("Segoe UI", 16f), ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter, AutoSize = false
        });
        hdr.Controls.Add(new Label
        {
            Text = existing is null ? "New Clinical Entry" : (readOnly ? "Clinical Note — View" : "Edit Medical Record"),
            Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White,
            AutoSize = true, Left = 70, Top = 12
        });
        hdr.Controls.Add(lblHeaderSub);

        // ── Footer ───────────────────────────────────────────────────────
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
        footer.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(220, 224, 232) });
        var btnSave   = UIHelper.CreateButton(_readOnly ? "Close" : "Save Record", UIHelper.Success, 126, 34);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.FromArgb(108, 117, 125), 90, 34);
        btnSave.Top = btnCancel.Top = 10;
        btnSave.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        footer.Resize += (_, _) =>
        {
            btnSave.Left   = footer.Width - btnSave.Width - 16;
            btnCancel.Left = btnSave.Left - btnCancel.Width - 8;
        };
        btnCancel.Visible = !_readOnly;
        footer.Controls.AddRange(new Control[] { btnSave, btnCancel });

        // ── LEFT PANEL — Appointment picker ──────────────────────────────
        var leftPanel = new Panel { Dock = DockStyle.Left, Width = 316, BackColor = Color.FromArgb(247, 249, 252) };
        leftPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(215, 220, 230));
            e.Graphics.DrawLine(pen, leftPanel.Width - 1, 0, leftPanel.Width - 1, leftPanel.Height);
        };

        // Left title bar
        var leftTitleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(238, 241, 248) };
        leftTitleBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(215, 220, 232));
            e.Graphics.DrawLine(pen, 0, leftTitleBar.Height - 1, leftTitleBar.Width, leftTitleBar.Height - 1);
        };
        leftTitleBar.Controls.Add(new Label
        {
            Text = "📋  APPOINTMENT",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = UIHelper.Accent, AutoSize = true, Left = 14, Top = 13
        });
        if (!_readOnly)
        {
            var reqLabel = new Label
            {
                Text = "* required",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(190, 60, 60), AutoSize = true, Top = 14
            };
            leftTitleBar.Controls.Add(reqLabel);
            leftTitleBar.Resize += (_, _) => reqLabel.Left = leftTitleBar.Width - reqLabel.Width - 12;
        }

        // Left filter bar
        var filterWrap = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(247, 249, 252), Padding = new Padding(10, 6, 10, 0) };
        cboApptFilter = new ComboBox
        {
            Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 8.5f), BackColor = Color.White
        };
        cboApptFilter.Items.AddRange(new object[] { "Today & Upcoming", "All Records" });
        cboApptFilter.SelectedIndex = 0;
        cboApptFilter.SelectedIndexChanged += (_, _) => RefreshApptGrid();
        filterWrap.Controls.Add(cboApptFilter);

        // Left search box
        var searchWrap = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(247, 249, 252), Padding = new Padding(10, 6, 10, 4) };
        var searchBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        searchBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(205, 210, 222));
            using var path = DlgRound(new Rectangle(0, 0, searchBox.Width - 1, searchBox.Height - 1), 5);
            e.Graphics.DrawPath(pen, path);
        };
        searchBox.Controls.Add(new Label { Text = "🔍", Left = 5, Top = 5, Width = 22, Height = 22, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
        txtApptSearch = new TextBox
        {
            Left = 28, Top = 6, Font = new Font("Segoe UI", 9f),
            BorderStyle = BorderStyle.None, BackColor = Color.White,
            PlaceholderText = "Search pet, owner, service…"
        };
        txtApptSearch.TextChanged += (_, _) => RefreshApptGrid();
        searchBox.Controls.Add(txtApptSearch);
        searchBox.Resize += (_, _) => txtApptSearch.Width = searchBox.Width - 34;
        searchWrap.Controls.Add(searchBox);

        // Left appointment grid
        dgvAppt = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, MultiSelect = false,
            AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BorderStyle = BorderStyle.None, BackgroundColor = Color.FromArgb(247, 249, 252),
            Font = new Font("Segoe UI", 8.5f), Cursor = Cursors.Hand,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(228, 232, 240)
        };
        dgvAppt.ColumnHeadersHeight = 32;
        dgvAppt.EnableHeadersVisualStyles = false;
        dgvAppt.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(238, 241, 248), ForeColor = Color.FromArgb(70, 80, 105),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            Padding = new Padding(6, 0, 0, 0),
            SelectionBackColor = Color.FromArgb(238, 241, 248),
            SelectionForeColor = Color.FromArgb(70, 80, 105)
        };
        dgvAppt.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(40, 50, 72),
            BackColor = Color.FromArgb(247, 249, 252),
            SelectionBackColor = Color.FromArgb(224, 235, 255),
            SelectionForeColor = Color.FromArgb(25, 60, 145),
            Padding = new Padding(6, 0, 0, 0)
        };
        dgvAppt.RowTemplate.Height = 36;
        dgvAppt.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "ColApptId", Visible = false },
            new DataGridViewTextBoxColumn { Name = "ColDate",   HeaderText = "Date",    Width = 84 },
            new DataGridViewTextBoxColumn { Name = "ColPet",    HeaderText = "Patient", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { Name = "ColOwner",  HeaderText = "Owner",   AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill },
            new DataGridViewTextBoxColumn { Name = "ColStatus", HeaderText = "Status",  Width = 72 }
        );
        dgvAppt.SelectionChanged += DgvAppt_SelectionChanged;
        dgvAppt.Visible = !_readOnly;

        // Left: selected appointment card (shown at bottom)
        pnlSelectedCard = new Panel { Dock = DockStyle.Bottom, Height = 68, BackColor = Color.FromArgb(230, 245, 235), Visible = false };
        pnlSelectedCard.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(175, 215, 190));
            e.Graphics.DrawLine(pen, 0, 0, pnlSelectedCard.Width, 0);
            using var accent = new SolidBrush(Color.FromArgb(30, 140, 80));
            e.Graphics.FillRectangle(accent, new Rectangle(0, 0, 4, pnlSelectedCard.Height));
        };
        pnlSelectedCard.Controls.Add(new Label
        {
            Text = "✓", Left = 14, Top = 12, Width = 20, Height = 20,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 130, 70), AutoSize = false
        });
        lblSelectedCard = new Label
        {
            Left = 36, Top = 8, Width = 270, Height = 52,
            Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(18, 90, 50), AutoSize = false
        };
        pnlSelectedCard.Controls.Add(lblSelectedCard);
        pnlSelectedCard.Resize += (_, _) => lblSelectedCard.Width = pnlSelectedCard.Width - 44;

        leftPanel.Controls.Add(dgvAppt);         // Fill — first
        leftPanel.Controls.Add(pnlSelectedCard); // Bottom
        leftPanel.Controls.Add(searchWrap);      // Top (stacked after filter)
        leftPanel.Controls.Add(filterWrap);      // Top (stacked after title)
        leftPanel.Controls.Add(leftTitleBar);    // Top (appears above filter)

        // ── RIGHT PANEL — Clinical data ───────────────────────────────────
        var rightScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };

        var rightFlow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, Dock = DockStyle.Top,
            Padding = new Padding(24, 14, 24, 20), BackColor = Color.White
        };
        rightScroll.Controls.Add(rightFlow);

        // Dynamic content width
        int rw = formWidth - 316 - 52;

        // Patient context strip (shown after appointment selected)
        pnlRightContext = new Panel
        {
            Width = rw, Height = 46, BackColor = Color.FromArgb(240, 246, 255),
            Visible = false, Margin = new Padding(0, 0, 0, 14)
        };
        pnlRightContext.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(195, 215, 245));
            using var path = DlgRound(new Rectangle(0, 0, pnlRightContext.Width - 1, pnlRightContext.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
            using var brush = new SolidBrush(UIHelper.Accent);
            e.Graphics.FillRectangle(brush, new Rectangle(0, 0, 4, pnlRightContext.Height));
        };
        lblRightContext = new Label
        {
            Left = 16, Top = 0, Height = 46, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 140), AutoSize = false, TextAlign = ContentAlignment.MiddleLeft
        };
        pnlRightContext.Controls.Add(lblRightContext);
        pnlRightContext.Resize += (_, _) => lblRightContext.Width = pnlRightContext.Width - 20;
        rightFlow.Controls.Add(pnlRightContext);

        // ── Section: Attending Vet ────────────────────────────────────────
        rightFlow.Controls.Add(MakeSectionTitle("👨‍⚕️  ATTENDING VETERINARIAN", rw));
        _vetList = new List<object> { new User { Id = 0, FullName = "(Not assigned)" } };
        _vetList.AddRange(_vets.Cast<object>());
        cboVet = new ComboBox
        {
            Width = Math.Min(300, (int)(rw * 0.42)), Font = new Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 4)
        };
        cboVet.DataSource = _vetList; cboVet.DisplayMember = "FullName"; cboVet.ValueMember = "Id";
        rightFlow.Controls.Add(UIHelper.WrapControl("Veterinarian", cboVet));
        rightFlow.Controls.Add(MakeDivider(rw));

        // ── Section: Diagnosis & Treatment ───────────────────────────────
        rightFlow.Controls.Add(MakeSectionTitle("🔬  DIAGNOSIS & TREATMENT", rw));
        int halfW = (rw - 16) / 2;
        var tblDT = new TableLayoutPanel { Width = rw, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 4) };
        tblDT.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, halfW + 16));
        tblDT.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, halfW + 16));
        txtDiagnosis = new TextBox
        {
            Width = halfW, Height = 120, Multiline = true,
            Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(0, 0, 0, 8)
        };
        txtTreatment = new TextBox
        {
            Width = halfW, Height = 120, Multiline = true,
            Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(0, 0, 0, 8)
        };
        tblDT.Controls.Add(UIHelper.WrapControl("Primary Diagnosis  *", txtDiagnosis), 0, 0);
        tblDT.Controls.Add(UIHelper.WrapControl("Treatment Plan & Summary", txtTreatment), 1, 0);
        rightFlow.Controls.Add(tblDT);

        // ── Section: Clinical Notes ───────────────────────────────────────
        rightFlow.Controls.Add(MakeSectionSubLabel("Internal Clinical Notes", rw));
        txtNotes = new TextBox
        {
            Width = rw, Height = 80, Multiline = true,
            Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical,
            Margin = new Padding(0, 0, 0, 4)
        };
        rightFlow.Controls.Add(txtNotes);
        rightFlow.Controls.Add(MakeDivider(rw));

        // ── Section: Pharmaceuticals ──────────────────────────────────────
        rightFlow.Controls.Add(MakeSectionTitle("💊  PHARMACEUTICALS", rw));

        dgvMeds = new DataGridView
        {
            Width = rw, Height = 140,
            AllowUserToAddRows = !_readOnly, AllowUserToDeleteRows = !_readOnly,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9.5f), RowHeadersVisible = false,
            BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 6),
            BackgroundColor = Color.White
        };
        dgvMeds.Columns.AddRange(
            new DataGridViewComboBoxColumn { Name = "Medication", HeaderText = "Medication", DataSource = _meds, DisplayMember = "Name", ValueMember = "Id" },
            new DataGridViewTextBoxColumn  { Name = "Dosage",     HeaderText = "Dosage / Instructions", FillWeight = 50 },
            new DataGridViewTextBoxColumn  { Name = "PNotes",     HeaderText = "Pharmacy Notes",        FillWeight = 40 }
        );
        if (!_readOnly)
        {
            var colDel = new DataGridViewButtonColumn
            {
                Name = "ColMedDel", HeaderText = "", Width = 36,
                Text = "✕", UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            colDel.DefaultCellStyle.BackColor          = Color.FromArgb(255, 235, 235);
            colDel.DefaultCellStyle.ForeColor          = Color.FromArgb(190, 40, 40);
            colDel.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 210, 210);
            colDel.DefaultCellStyle.SelectionForeColor = Color.FromArgb(190, 40, 40);
            colDel.DefaultCellStyle.Font               = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgvMeds.Columns.Add(colDel);
            dgvMeds.CellClick += (_, e) =>
            {
                if (e.RowIndex < 0 || dgvMeds.Columns[e.ColumnIndex].Name != "ColMedDel") return;
                if (!dgvMeds.Rows[e.RowIndex].IsNewRow) dgvMeds.Rows.RemoveAt(e.RowIndex);
            };
        }
        UIHelper.StyleGrid(dgvMeds);
        dgvMeds.EditingControlShowing += MedGrid_EditingControlShowing;
        rightFlow.Controls.Add(dgvMeds);

        if (!_readOnly)
        {
            var btnAddMed = UIHelper.CreateButton("+ Add Medication", UIHelper.Accent, 140, 28);
            btnAddMed.Margin = new Padding(0, 0, 0, 4);
            btnAddMed.Click += (_, _) =>
            {
                dgvMeds.CommitEdit(DataGridViewDataErrorContexts.Commit);
                var rowIdx = dgvMeds.Rows.Add();
                dgvMeds.CurrentCell = dgvMeds.Rows[rowIdx].Cells["Medication"];
                dgvMeds.BeginEdit(true);
            };
            rightFlow.Controls.Add(btnAddMed);
        }
        rightFlow.Controls.Add(MakeDivider(rw));

        // ── Section: Follow-up ────────────────────────────────────────────
        rightFlow.Controls.Add(MakeSectionTitle("📅  FOLLOW-UP SCHEDULING", rw));
        chkFollowUp = new CheckBox
        {
            Text = "Schedule a follow-up visit", Font = new Font("Segoe UI", 10f),
            AutoSize = true, Height = 26, Cursor = Cursors.Hand
        };
        dtpFollowUp = new DateTimePicker
        {
            Width = 200, Font = new Font("Segoe UI", 10f),
            Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(7), Enabled = false
        };
        chkFollowUp.CheckedChanged += (_, _) => dtpFollowUp.Enabled = chkFollowUp.Checked;
        var tblFU = new TableLayoutPanel { Width = rw, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 20) };
        tblFU.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tblFU.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tblFU.Controls.Add(UIHelper.WrapControl("Requires Follow-up?", chkFollowUp), 0, 0);
        tblFU.Controls.Add(UIHelper.WrapControl("Follow-up Date", dtpFollowUp), 1, 0);
        rightFlow.Controls.Add(tblFU);
        rightFlow.Controls.Add(new Panel { Width = rw, Height = 4 });

        // ── Assemble form ─────────────────────────────────────────────────
        Controls.Add(rightScroll);
        Controls.Add(leftPanel);
        Controls.Add(footer);
        Controls.Add(hdr);

        AcceptButton = btnSave;
        CancelButton = btnCancel;

        if (_readOnly) btnSave.Click += (_, _) => DialogResult = DialogResult.OK;
        else btnSave.Click += Save;
        btnCancel.DialogResult = DialogResult.Cancel;

        RefreshApptGrid();

        // ── Populate existing record ──────────────────────────────────────
        if (existing is not null)
        {
            var apt = _appointments.FirstOrDefault(a => a.Id == existing.AppointmentId);
            if (apt != null) SelectAppointmentInGrid(apt);

            var vet = _vetList.OfType<User>().FirstOrDefault(v => v.Id == (existing.VetId ?? 0));
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
                cboVet.Enabled = chkFollowUp.Enabled = dtpFollowUp.Enabled = false;
                txtDiagnosis.ReadOnly = txtTreatment.ReadOnly = txtNotes.ReadOnly = true;
                txtDiagnosis.BackColor = txtTreatment.BackColor = txtNotes.BackColor = Color.White;
                dgvMeds.ReadOnly = true; dgvMeds.Enabled = false;
            }
        }
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    private static Label MakeSectionTitle(string text, int width) => new Label
    {
        Text = text,
        Font = new Font("Segoe UI", 8f, FontStyle.Bold),
        ForeColor = UIHelper.Accent,
        AutoSize = false, Width = width, Height = 22,
        Margin = new Padding(0, 14, 0, 8)
    };

    private static Label MakeSectionSubLabel(string text, int width) => new Label
    {
        Text = text,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(80, 92, 115),
        AutoSize = false, Width = width, Height = 20,
        Margin = new Padding(0, 8, 0, 4)
    };

    private static Panel MakeDivider(int width) => new Panel
    {
        Width = width, Height = 1,
        BackColor = Color.FromArgb(228, 232, 240),
        Margin = new Padding(0, 8, 0, 4)
    };

    private static System.Drawing.Drawing2D.GraphicsPath DlgRound(Rectangle rc, int r)
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

    // ── Medication search helpers ─────────────────────────────────────────────

    private void MedGrid_EditingControlShowing(object? s, DataGridViewEditingControlShowingEventArgs e)
    {
        if (dgvMeds.CurrentCell?.OwningColumn.Name != "Medication") return;
        if (e.Control is not ComboBox cbo) return;
        cbo.DropDownStyle = ComboBoxStyle.DropDown;
        cbo.MaxDropDownItems = 10;
        cbo.AutoCompleteMode = AutoCompleteMode.None;
        cbo.KeyUp -= MedCbo_KeyUp;
        cbo.KeyUp += MedCbo_KeyUp;
    }

    private void MedCbo_KeyUp(object? s, KeyEventArgs e)
    {
        if (_medFilterBusy || s is not ComboBox cbo) return;
        if (e.KeyCode is Keys.Down or Keys.Up or Keys.Enter or Keys.Escape or Keys.Tab) return;

        _medFilterBusy = true;
        try
        {
            var q = cbo.Text;
            var hits = string.IsNullOrWhiteSpace(q)
                ? _meds
                : _meds.Where(m => m.Name?.ToLower().Contains(q.ToLower()) == true).ToList();
            cbo.DataSource = hits;
            cbo.DisplayMember = "Name";
            cbo.ValueMember = "Id";
            cbo.Text = q;
            cbo.Select(q.Length, 0);
            if (hits.Count > 0) BeginInvoke(() => { if (!cbo.IsDisposed) cbo.DroppedDown = true; });
        }
        finally { _medFilterBusy = false; }
    }

    // ── Appointment grid helpers ──────────────────────────────────────────────

    private void RefreshApptGrid()
    {
        bool todayAndUpcoming = cboApptFilter.SelectedIndex == 0;
        IEnumerable<Appointment> dateFiltered = todayAndUpcoming
            ? _appointments.Where(a => a.AppointmentDate.Date >= DateTime.Today).OrderBy(a => a.AppointmentDate)
            : _appointments; // already ordered descending

        var q = txtApptSearch.Text.Trim().ToLower();
        var src = string.IsNullOrWhiteSpace(q) ? dateFiltered
            : dateFiltered.Where(a =>
                a.PetName?.ToLower().Contains(q)         == true ||
                a.CustomerName?.ToLower().Contains(q)    == true ||
                a.ServiceTypeName?.ToLower().Contains(q) == true).ToList();
        dgvAppt.Rows.Clear();
        foreach (var a in src)
        {
            int idx = dgvAppt.Rows.Add();
            var row = dgvAppt.Rows[idx];
            row.Cells["ColApptId"].Value = a.Id;
            row.Cells["ColDate"].Value   = a.AppointmentDate.ToString("MMM dd");
            row.Cells["ColPet"].Value    = a.PetName;
            row.Cells["ColOwner"].Value  = a.CustomerName;
            row.Cells["ColStatus"].Value = a.Status;
            row.Cells["ColStatus"].Style.ForeColor =
                a.Status == "Completed"    ? Color.FromArgb(30, 130, 76) :
                a.Status == "In Progress"  ? Color.FromArgb(20, 80, 180) :
                                             Color.FromArgb(160, 100, 10);
            row.Cells["ColStatus"].Style.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
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
        UpdateSelectionUI();
    }

    private void DgvAppt_SelectionChanged(object? s, EventArgs e)
    {
        if (dgvAppt.SelectedRows.Count == 0) return;
        var row = dgvAppt.SelectedRows[0];
        if (row.Cells["ColApptId"].Value is int id)
        {
            _selectedAppointment = _appointments.FirstOrDefault(a => a.Id == id);
            UpdateSelectionUI();

            // Auto-suggest vet if appointment has one assigned
            if (_selectedAppointment?.AssignedVetId is int vid && vid > 0)
            {
                var v = _vetList.OfType<User>().FirstOrDefault(u => u.Id == vid);
                if (v != null) cboVet.SelectedItem = v;
            }
        }
    }

    private void UpdateSelectionUI()
    {
        if (_selectedAppointment is not Appointment a)
        {
            pnlSelectedCard.Visible   = false;
            pnlRightContext.Visible   = false;
            lblHeaderSub.Text = "Select an appointment to link this clinical entry";
            return;
        }
        // Header subtitle
        lblHeaderSub.Text = $"Patient: {a.PetName}  ·  Owner: {a.CustomerName}  ·  {a.AppointmentDate:MMM dd, yyyy}";

        // Left card
        pnlSelectedCard.Visible = true;
        lblSelectedCard.Text = $"{a.PetName}\n{a.CustomerName}  ·  {a.AppointmentDate:MMM dd, yyyy  HH:mm}";

        // Right context banner
        pnlRightContext.Visible = true;
        lblRightContext.Text = $"🐾  {a.PetName}  —  {a.CustomerName}   |   {a.ServiceTypeName ?? "—"}   |   {a.AppointmentDate:MMM dd, yyyy  HH:mm}";
    }

    // ── Validation helpers ────────────────────────────────────────────────────

    private static void FlashError(Control ctrl)
    {
        ctrl.BackColor = Color.FromArgb(255, 230, 230);
        var t = new System.Windows.Forms.Timer { Interval = 1200 };
        t.Tick += (_, _) => { ctrl.BackColor = Color.White; t.Stop(); t.Dispose(); };
        t.Start();
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void Save(object? s, EventArgs e)
    {
        if (_selectedAppointment is not Appointment apt)
        {
            VetMS.Forms.CustomMessageBox.Show(
                "Please select a linked appointment before saving.", "Linked Appointment Required",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(txtDiagnosis.Text))
        {
            FlashError(txtDiagnosis); txtDiagnosis.Focus();
            VetMS.Forms.CustomMessageBox.Show(
                "Primary Diagnosis is required.", "Diagnosis Required",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var vet = cboVet.SelectedItem as User;
        Result = new MedicalRecord
        {
            Id            = Result.Id,
            AppointmentId = apt.Id,
            PetId         = apt.PetId,       PetName      = apt.PetName,
            CustomerId    = apt.CustomerId,  CustomerName  = apt.CustomerName,
            VetId         = (vet?.Id ?? 0) == 0 ? null : vet?.Id,
            VetName       = vet?.FullName ?? "",
            Diagnosis     = txtDiagnosis.Text.Trim(),
            Treatment     = txtTreatment.Text.Trim(),
            Notes         = txtNotes.Text.Trim(),
            FollowUpDate  = chkFollowUp.Checked ? dtpFollowUp.Value : null
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
