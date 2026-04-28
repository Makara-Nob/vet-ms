using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class MedicalRecordForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<MedicalRecord> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public MedicalRecordForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "Medical Records"; BackColor = UIHelper.LightBg;
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
        lblNoData = UIHelper.CreateEmptyDataLabel("No medical records yet.");

        gridContainer.Controls.Add(lblNoData);
        gridContainer.Controls.Add(dgv);
        gridContainer.Controls.Add(pag);
        lblNoData.BringToFront();

        contentPanel.Controls.Add(gridContainer);
        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("Medical Records", "Clinical notes, diagnoses and prescriptions"));
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
        txtSearch = new TextBox { Left = 46, Top = 20, Width = 240, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search pet, owner, vet, diagnosis..." };
        txtSearch.TextChanged += (_, _) => FilterData();

        var btnAdd   = UIHelper.CreateButton("+ New Record", UIHelper.Success, 110, 38); btnAdd.Left   = txtSearch.Right + 14; btnAdd.Top   = 17; btnAdd.Click += BtnAdd_Click;
        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray, 80, 38);          btnReset.Left = btnAdd.Right + 8;     btnReset.Top = 17;
        btnReset.Click += (_, _) => { txtSearch.Clear(); LoadData(); };

        p.Controls.AddRange(new Control[] { ico, txtSearch, btnAdd, btnReset }); return p;
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
        if (_filtered.Count == 0)
        {
            dgv.DataSource = null; lblStatus.Text = "0 records";
            btnPrev.Enabled = btnNext.Enabled = false; btnPrev.Visible = btnNext.Visible = lblPage.Visible = false;
            lblNoData.Visible = true; dgv.Visible = false; return;
        }
        lblNoData.Visible = false; dgv.Visible = true;
        var page = _filtered.Skip((_currentPage - 1) * _pageSize).Take(_pageSize)
            .Select(x => new
            {
                x.Id,
                Date      = x.CreatedAt.ToString("yyyy-MM-dd"),
                x.PetName,
                Owner     = x.CustomerName,
                Vet       = x.VetName,
                Diagnosis = x.Diagnosis.Length > 60 ? x.Diagnosis[..57] + "..." : x.Diagnosis,
                FollowUp  = x.FollowUpDate?.ToString("yyyy-MM-dd") ?? "-"
            }).ToList();
        dgv.DataSource = page;
        if (dgv.Columns["Id"]        != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Date"]      is { } c1) c1.HeaderText = "Date";
        if (dgv.Columns["PetName"]   is { } c2) c2.HeaderText = "Patient";
        if (dgv.Columns["Owner"]     is { } c3) c3.HeaderText = "Owner";
        if (dgv.Columns["Vet"]       is { } c4) c4.HeaderText = "Veterinarian";
        if (dgv.Columns["Diagnosis"] is { } c5) c5.HeaderText = "Diagnosis";
        if (dgv.Columns["FollowUp"]  is { } c6) c6.HeaderText = "Follow-up";
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
        const int totalWeight = 800; // 100+120+140+140+200+100
        int available = dgv.ClientSize.Width - actionW - 2;
        if (available <= 0) return;
        if (dgv.Columns["ColAction"] is { } ca) { ca.Width = actionW; ca.DisplayIndex = dgv.Columns.Count - 1; }
        if (dgv.Columns["Date"]      is { } c1) c1.Width = available * 100 / totalWeight;
        if (dgv.Columns["PetName"]   is { } c2) c2.Width = available * 120 / totalWeight;
        if (dgv.Columns["Owner"]     is { } c3) c3.Width = available * 140 / totalWeight;
        if (dgv.Columns["Vet"]       is { } c4) c4.Width = available * 140 / totalWeight;
        if (dgv.Columns["Diagnosis"] is { } c5) c5.Width = available * 200 / totalWeight;
        if (dgv.Columns["FollowUp"]  is { } c6) c6.Width = available * 100 / totalWeight;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new MedicalRecordDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            DataStore.Insert(dlg.Result);
            DataStore.SaveRecordMedications(dlg.Result.Id, dlg.Prescriptions);
        }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Medical record saved!"); LoadData();
    }

    private void ViewRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new MedicalRecordDialog(item, readOnly: true);
        dlg.ShowDialog(this);
    }

    private void EditRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new MedicalRecordDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var r = dlg.Result;
        item.AppointmentId = r.AppointmentId; item.PetId = r.PetId; item.PetName = r.PetName;
        item.CustomerId = r.CustomerId; item.CustomerName = r.CustomerName;
        item.VetId = r.VetId; item.VetName = r.VetName;
        item.Diagnosis = r.Diagnosis; item.Treatment = r.Treatment; item.Notes = r.Notes; item.FollowUpDate = r.FollowUpDate;
        try
        {
            DataStore.Update(item);
            DataStore.SaveRecordMedications(item.Id, dlg.Prescriptions);
        }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Medical record updated!"); LoadData();
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
