using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class AppointmentForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatusFilter = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private CheckBox chkDateFilter = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<Appointment> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public AppointmentForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "Appointments"; BackColor = UIHelper.LightBg;
        var contentPanel   = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(20, 8, 20, 0) };
        var gridContainer  = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ScrollBars = ScrollBars.Vertical,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, BackgroundColor = Color.White, Cursor = Cursors.Hand
        };
        UIHelper.StyleGrid(dgv);
        dgv.ColumnHeadersHeight = 42;
        dgv.RowTemplate.Height  = 38;
        dgv.Resize             += (_, _) => DistributeColumns();
        dgv.CellPainting   += (_, e) => UIHelper.PaintActionColumn(dgv, e, "View", "Edit");
        dgv.CellMouseClick += (_, e) => UIHelper.HandleActionColumnClick(dgv, e, ViewRow, EditRow, "View", "Edit");
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") ViewRow(e.RowIndex); };
        dgv.RowPrePaint += PaintStatusRow;

        var pag = BuildPaginationBar(); pag.Dock = DockStyle.Bottom;
        lblNoData = UIHelper.CreateEmptyDataLabel("No appointments scheduled yet.");

        gridContainer.Controls.Add(lblNoData);
        gridContainer.Controls.Add(dgv);
        gridContainer.Controls.Add(pag);
        lblNoData.BringToFront();

        contentPanel.Controls.Add(gridContainer);

        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("Appointments", "Schedule and manage clinic appointments"));
    }

    private void PaintStatusRow(object? sender, DataGridViewRowPrePaintEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= dgv.Rows.Count) return;
        var row = dgv.Rows[e.RowIndex];
        var statusCell = row.Cells["Status"];
        if (statusCell?.Value is not string status) return;
        row.DefaultCellStyle.ForeColor = status switch
        {
            "Completed"   => Color.FromArgb(30,130,60),
            "Cancelled"   => Color.FromArgb(180,50,50),
            "In Progress" => Color.FromArgb(200,130,0),
            _             => Color.FromArgb(40,40,40)
        };
    }

    private Panel BuildStatusBar()
    {
        var p = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White };
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(20,0,0,0), ForeColor = Color.FromArgb(90,100,115), Font = new Font("Segoe UI", 8.5f) };
        p.Controls.Add(lblStatus); p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230,232,235) }); return p;
    }

    private Panel BuildSearchBar()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 112 };

        // ── Row 1: search + status + action buttons ──────────────────────────
        var ico = new Label { Text = "🔍", Width = 26, Height = 38, Left = 20, Top = 17, TextAlign = ContentAlignment.MiddleCenter };
        txtSearch = new TextBox { Left = 46, Top = 20, Width = 240, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search pet, owner, vet..." };
        txtSearch.TextChanged += (_, _) => FilterData();

        cboStatusFilter = new ComboBox { Left = txtSearch.Right + 10, Top = 20, Width = 145, Font = new Font("Segoe UI", 10f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboStatusFilter.Items.AddRange(["Upcoming", "Scheduled", "In Progress", "Completed", "Cancelled", "All Statuses"]);
        cboStatusFilter.SelectedIndex = 0; cboStatusFilter.SelectedIndexChanged += (_, _) => FilterData();

        var btnAdd   = UIHelper.CreateButton("+ Add", UIHelper.Success,  90, 38); btnAdd.Left   = cboStatusFilter.Right + 14; btnAdd.Top   = 17; btnAdd.Click += BtnAdd_Click;
        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray,   80, 38); btnReset.Left = btnAdd.Right + 8;           btnReset.Top = 17;
        btnReset.Click += (_, _) =>
        {
            txtSearch.Clear();
            cboStatusFilter.SelectedIndex = 0;
            chkDateFilter.Checked = false;
            dtpFrom.Value = DateTime.Today;
            dtpTo.Value   = DateTime.Today;
            LoadData();
        };

        // ── Row 2: date range filter ──────────────────────────────────────────
        const int row2Top = 68;   // top edge for all row-2 controls
        chkDateFilter = new CheckBox
        {
            Text = "Filter by date:", Left = 20, Top = row2Top + 2, Width = 118,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(60, 70, 85), Checked = false
        };
        chkDateFilter.CheckedChanged += (_, _) =>
        {
            dtpFrom.Enabled = dtpTo.Enabled = chkDateFilter.Checked;
            FilterData();
        };

        dtpFrom = new DateTimePicker { Left = 144, Top = row2Top, Width = 120, Font = new Font("Segoe UI", 9.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Enabled = false };
        dtpFrom.ValueChanged += (_, _) => { if (chkDateFilter.Checked) FilterData(); };

        var lblTo = new Label { Text = "—", Left = 270, Top = row2Top + 2, Width = 16, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.Gray };

        dtpTo = new DateTimePicker { Left = 292, Top = row2Top, Width = 120, Font = new Font("Segoe UI", 9.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Enabled = false };
        dtpTo.ValueChanged += (_, _) => { if (chkDateFilter.Checked) FilterData(); };

        var sep = new Panel { Left = 0, Top = 111, Width = 9999, Height = 1, BackColor = Color.FromArgb(230, 232, 235), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };

        p.Controls.AddRange(new Control[] { ico, txtSearch, cboStatusFilter, btnAdd, btnReset, chkDateFilter, dtpFrom, lblTo, dtpTo, sep });
        return p;
    }

    private Panel BuildPaginationBar()
    {
        var p = new Panel { Height = 60, BackColor = Color.White };
        p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230,235,240) });
        btnPrev = UIHelper.CreateButton("← Prev", Color.FromArgb(108,117,125), 80, 36);
        btnNext = UIHelper.CreateButton("Next →", Color.FromArgb(108,117,125), 80, 36);
        lblPage = new Label { AutoSize = false, Width = 110, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9.5f), ForeColor = Color.FromArgb(64,64,64) };
        p.Resize += (_, _) => { btnNext.Left = p.Width - btnNext.Width - 16; btnNext.Top = 12; lblPage.Left = btnNext.Left - lblPage.Width - 10; lblPage.Top = 18; btnPrev.Left = lblPage.Left - btnPrev.Width - 10; btnPrev.Top = 12; };
        btnPrev.Click += (_, _) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
        btnNext.Click += (_, _) => { if (_currentPage < GetTotalPages()) { _currentPage++; RefreshGrid(); } };
        p.Controls.AddRange(new Control[] { btnPrev, lblPage, btnNext }); return p;
    }

    private int GetTotalPages() => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));

    private void LoadData()
    {
        try { _data = DataStore.GetAppointments() ?? []; }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        var st = cboStatusFilter.SelectedItem?.ToString() ?? "Upcoming";
        var useDateFilter = chkDateFilter.Checked;
        var from = dtpFrom.Value.Date;
        var to   = dtpTo.Value.Date;
        _filtered = _data.Where(x =>
            (st == "All Statuses" || (st == "Upcoming" ? (x.Status == "Scheduled" || x.Status == "In Progress") : x.Status == st)) &&
            (string.IsNullOrWhiteSpace(q) || (x.PetName?.ToLower().Contains(q) == true) || (x.CustomerName?.ToLower().Contains(q) == true) || (x.VetName?.ToLower().Contains(q) == true)) &&
            (!useDateFilter || (x.AppointmentDate.Date >= from && x.AppointmentDate.Date <= to))
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
        var page = _filtered.Skip((_currentPage-1)*_pageSize).Take(_pageSize)
            .Select(x => new { x.Id, Date = x.AppointmentDate.ToString("MMM dd, yyyy HH:mm"), x.PetName, Owner = x.CustomerName, Vet = x.VetName, Service = x.ServiceTypeName, x.Status, CreatedAt = x.CreatedAt.ToString("MMM dd, yyyy") }).ToList();
        dgv.DataSource = page;
        if (dgv.Columns["Id"]        != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Date"]      is { } c1) c1.HeaderText = "Date & Time";
        if (dgv.Columns["PetName"]   is { } c2) c2.HeaderText = "Patient (Pet)";
        if (dgv.Columns["Owner"]     is { } c3) c3.HeaderText = "Owner";
        if (dgv.Columns["Vet"]       is { } c4) c4.HeaderText = "Assigned Vet";
        if (dgv.Columns["Service"]   is { } c5) c5.HeaderText = "Service";
        if (dgv.Columns["Status"]    is { } c6) c6.HeaderText = "Status";
        if (dgv.Columns["CreatedAt"] is { } c7) c7.HeaderText = "Created At";
        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "", ReadOnly = true });
        dgv.Columns["ColAction"]!.DisplayIndex = dgv.Columns.Count - 1;
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
        const int totalWeight = 870; // 140+110+120+120+110+80+190
        int available = dgv.ClientSize.Width - actionW - 2;
        if (available <= 0) return;

        if (dgv.Columns["ColAction"] is { } ca) ca.Width = actionW;
        if (dgv.Columns["Date"]      is { } c1) c1.Width = available * 140 / totalWeight;
        if (dgv.Columns["PetName"]   is { } c2) c2.Width = available * 110 / totalWeight;
        if (dgv.Columns["Owner"]     is { } c3) c3.Width = available * 120 / totalWeight;
        if (dgv.Columns["Vet"]       is { } c4) c4.Width = available * 120 / totalWeight;
        if (dgv.Columns["Service"]   is { } c5) c5.Width = available * 110 / totalWeight;
        if (dgv.Columns["Status"]    is { } c6) c6.Width = available *  80 / totalWeight;
        if (dgv.Columns["CreatedAt"] is { } c7) c7.Width = available * 190 / totalWeight;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new AppointmentDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Appointment scheduled!"); LoadData();
    }

    private void ViewRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new AppointmentDialog(item, true);
        dlg.ShowDialog(this);
    }

    private void EditRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new AppointmentDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var r = dlg.Result;
        item.PetId = r.PetId; item.PetName = r.PetName; item.CustomerId = r.CustomerId; item.CustomerName = r.CustomerName;
        item.AssignedVetId = r.AssignedVetId; item.VetName = r.VetName; item.ServiceTypeId = r.ServiceTypeId;
        item.ServiceTypeName = r.ServiceTypeName; item.AppointmentDate = r.AppointmentDate;
        item.Duration = r.Duration; item.Status = r.Status; item.Notes = r.Notes;
        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Appointment updated!"); LoadData();
    }
}

public class AppointmentDialog : Form
{
    private sealed class PetItem
    {
        public Pet Pet { get; }
        public PetItem(Pet pet) { Pet = pet; }
        public override string ToString() => $"{Pet.Name}  ({Pet.CustomerName})";
    }

    private readonly ComboBox cboPet, cboVet, cboService, cboStatus;
    private readonly DateTimePicker dtpDate, dtpTime;
    private readonly NumericUpDown numDuration;
    private readonly TextBox txtNotes;
    private readonly Label lblSecondary;
    private readonly List<Pet> _pets;
    private readonly List<User> _vets;
    private readonly List<ServiceType> _services;
    private readonly bool _readOnly;
    public Appointment Result { get; private set; } = new();

    public AppointmentDialog(Appointment? existing = null, bool readOnly = false)
    {
        _readOnly = readOnly;
        Text = existing is null ? "Schedule New Visit" : (_readOnly ? "Appointment Details" : "Edit Appointment");
        Size = new Size(900, 840); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false; BackColor = Color.White;

        _pets     = DataStore.GetPets().Where(p => p.IsActive).ToList();
        _vets     = DataStore.GetUsers().Where(u => u.IsActive && (u.Role == "Veterinarian" || u.Role == "Administrator")).ToList();
        _services = DataStore.GetServiceTypes().Where(s => s.IsActive).ToList();

        var contentPnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        
        // --- HEADER ---
        var hdr = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = UIHelper.LightBg };
        var lblTitle = new Label { Text = existing is null ? "Schedule New Visit" : $"Visit: {existing.PetName}", Left = 40, Top = 35, AutoSize = true, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = UIHelper.Primary };
        lblSecondary = new Label { Text = existing is null ? "Fill in the details below to schedule a new appointment" : $"Owner: {existing.CustomerName} • Patient: {existing.PetName}", Left = 40, Top = 75, AutoSize = true, Font = new Font("Segoe UI", 10.5f), ForeColor = Color.Gray };
        hdr.Controls.AddRange(new Control[] { lblTitle, lblSecondary });
        var lineH = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220,220,225) };
        hdr.Controls.Add(lineH);

        const int Gap    = 24;   // column gap
        const int GW     = 820;  // usable grid width (900 - 40 padding each side)
        const int CboW   = (GW - Gap) / 2 - 4;  // ~396px per combo
        const int SchedW = (GW - Gap * 2) / 3;  // ~257px per timing field

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(40, 24, 40, 28), AutoScroll = true };

        // ── 2-column grid with explicit gap column ─────────────────────────────
        var gridLayout = new TableLayoutPanel { Width = GW, AutoSize = true, ColumnCount = 3, Margin = new Padding(0, 0, 0, 8) };
        gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Gap));
        gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        cboPet = new ComboBox { Width = CboW, Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDown };
        cboPet.Items.AddRange(_pets.Select(p => (object)new PetItem(p)).ToArray());
        cboPet.AutoCompleteMode   = AutoCompleteMode.SuggestAppend;
        cboPet.AutoCompleteSource = AutoCompleteSource.ListItems;
        cboPet.SelectedIndexChanged += (_, _) => UpdateHeaderSubtext();

        cboVet = new ComboBox
        {
            Width = CboW,
            Font = new Font("Segoe UI", 10.5f),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        var vetList = new List<User>
{
    new User { Id = 0, Username = "(Not assigned)" }
};

        vetList.AddRange(_vets);

        cboVet.DataSource = vetList;
        cboVet.DisplayMember = "Username";
        cboVet.ValueMember = "Id";

        cboService = new ComboBox { Width = CboW, Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        var svcList = new List<ServiceType> { new ServiceType { Id = 0, Name = "(Not specified)" } };
        svcList.AddRange(_services); cboService.DataSource = svcList; cboService.DisplayMember = "Name"; cboService.ValueMember = "Id";

        cboStatus = new ComboBox { Width = CboW, Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboStatus.Items.AddRange(["Scheduled", "In Progress", "Completed", "Cancelled"]); cboStatus.SelectedIndex = 0;

        gridLayout.Controls.Add(UIHelper.WrapControl("Patient (Pet) *",       cboPet),    0, 0);
        gridLayout.Controls.Add(UIHelper.WrapControl("Assigned Veterinarian", cboVet),    2, 0);
        gridLayout.Controls.Add(UIHelper.WrapControl("Service Category",      cboService), 0, 1);
        gridLayout.Controls.Add(UIHelper.WrapControl("Appointment Status",    cboStatus), 2, 1);

        flow.Controls.Add(UIHelper.CreateSectionLabel("GENERAL INFORMATION"));
        flow.Controls.Add(gridLayout);

        flow.Controls.Add(UIHelper.CreateSectionLabel("SCHEDULE & TIMING"));

        // ── 3-column schedule grid with gap columns ────────────────────────────
        var gridSched = new TableLayoutPanel { Width = GW, AutoSize = true, ColumnCount = 5, Margin = new Padding(0, 0, 0, 8) };
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SchedW));
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Gap));
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SchedW));
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Gap));
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        dtpDate    = new DateTimePicker   { Width = SchedW - 8, Font = new Font("Segoe UI", 10.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        dtpTime    = new DateTimePicker   { Width = SchedW - 8, Font = new Font("Segoe UI", 10.5f), Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(9) };
        numDuration = new NumericUpDown   { Width = SchedW - 8, Font = new Font("Segoe UI", 10.5f), Minimum = 5, Maximum = 480, Increment = 5, Value = 30 };

        gridSched.Controls.Add(UIHelper.WrapControl("Visit Date *",    dtpDate),     0, 0);
        gridSched.Controls.Add(UIHelper.WrapControl("Start Time *",    dtpTime),     2, 0);
        gridSched.Controls.Add(UIHelper.WrapControl("Duration (min)",  numDuration), 4, 0);
        flow.Controls.Add(gridSched);

        flow.Controls.Add(UIHelper.CreateFormLabel("Internal Appointment Notes / History"));
        txtNotes = new TextBox { Width = GW, Height = 100, Multiline = true, Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 15) };
        flow.Controls.Add(txtNotes);

        contentPnl.Controls.Add(flow);
        contentPnl.Controls.Add(hdr);
        Controls.Add(contentPnl);

        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 72, BackColor = Color.White };
        var btnSave   = UIHelper.CreateButton(_readOnly ? "Back" : "Save", UIHelper.Success,              110, 40);
        var btnCancel = UIHelper.CreateButton("Cancel",                   Color.FromArgb(108,117,125),   110, 40);

        btnSave.Top = btnCancel.Top = 16;
        btnSave.Left = _readOnly ? pnlBtn.Width - 122 : pnlBtn.Width - 238;
        btnCancel.Left = pnlBtn.Width - 122;
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        
        btnCancel.Visible = !_readOnly;
        if (_readOnly) btnSave.Click += (s, e) => DialogResult = DialogResult.OK;
        else btnSave.Click += Save;

        btnCancel.DialogResult = DialogResult.Cancel;
        pnlBtn.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230,230,230) });
        pnlBtn.Controls.AddRange(new Control[] { btnSave, btnCancel });
        Controls.Add(pnlBtn); AcceptButton = btnSave; CancelButton = btnCancel;

        UpdateHeaderSubtext();

        if (existing is not null)
        {
            var petItem = cboPet.Items.OfType<PetItem>().FirstOrDefault(pi => pi.Pet.Id == existing.PetId);
            if (petItem != null) cboPet.SelectedItem = petItem;
            dtpDate.Value = existing.AppointmentDate.Date;
            dtpTime.Value = DateTime.Today.AddHours(existing.AppointmentDate.Hour).AddMinutes(existing.AppointmentDate.Minute);
            numDuration.Value = existing.Duration;
            var si = cboStatus.Items.IndexOf(existing.Status); if (si >= 0) cboStatus.SelectedIndex = si;
            txtNotes.Text = existing.Notes; Result.Id = existing.Id;

            if (_readOnly)
            {
                cboPet.Enabled = cboVet.Enabled = cboService.Enabled = cboStatus.Enabled = false;
                dtpDate.Enabled = dtpTime.Enabled = numDuration.Enabled = false;
                txtNotes.ReadOnly = true;
                txtNotes.BackColor = Color.White;
            }

            Load += (_, _) =>
            {
                var vi = vetList.FindIndex(u => u.Id == (existing.AssignedVetId ?? 0));
                if (vi >= 0) cboVet.SelectedIndex = vi;
                var si2 = svcList.FindIndex(s => s.Id == (existing.ServiceTypeId ?? 0));
                if (si2 >= 0) cboService.SelectedIndex = si2;
            };
        }
    }

    private void UpdateHeaderSubtext()
    {
        if (cboPet.SelectedItem is PetItem pi)
            lblSecondary.Text = $"Scheduling visit for {pi.Pet.Name} • Owner: {pi.Pet.CustomerName}";
    }

    private void Save(object? s, EventArgs e)
    {
        if (cboPet.SelectedItem is not PetItem petItem) { VetMS.Forms.CustomMessageBox.Show("Please select a patient. Start typing the pet or owner name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var pet = petItem.Pet;
        var vet = cboVet.SelectedItem as User;
        var svc = cboService.SelectedItem as ServiceType;
        var dt  = DateTime.SpecifyKind(dtpDate.Value.Date + dtpTime.Value.TimeOfDay, DateTimeKind.Unspecified);
        Result = new Appointment
        {
            Id = Result.Id, PetId = pet.Id, PetName = pet.Name, CustomerId = pet.CustomerId, CustomerName = pet.CustomerName,
            AssignedVetId = (vet?.Id ?? 0) == 0 ? null : vet?.Id, VetName = (vet?.Id ?? 0) == 0 ? "" : vet!.FullName,
            ServiceTypeId = (svc?.Id ?? 0) == 0 ? null : svc?.Id, ServiceTypeName = (svc?.Id ?? 0) == 0 ? "" : svc!.Name,
            AppointmentDate = dt, Duration = (int)numDuration.Value,
            Status = cboStatus.SelectedItem?.ToString() ?? "Scheduled", Notes = txtNotes.Text.Trim()
        };
        DialogResult = DialogResult.OK;
    }
}
