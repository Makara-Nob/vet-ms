using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class AppointmentForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatusFilter = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<Appointment> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public AppointmentForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "Appointments"; BackColor = UIHelper.LightBg;
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
        dgv.RowPrePaint += PaintStatusRow;
        var pag = BuildPaginationBar(); pag.Dock = DockStyle.Bottom;
        lblNoData = UIHelper.CreateEmptyDataLabel("No appointments scheduled yet.");
        grid.Controls.Add(lblNoData); grid.Controls.Add(dgv); grid.Controls.Add(pag);
        lblNoData.BringToFront(); dgv.BringToFront();
        content.Controls.Add(grid);
        Controls.Add(content); Controls.Add(BuildStatusBar()); Controls.Add(BuildSearchBar());
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
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12,0,0,0), ForeColor = Color.FromArgb(90,100,115), Font = new Font("Segoe UI", 8.5f) };
        p.Controls.Add(lblStatus); p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230,232,235) }); return p;
    }

    private Panel BuildSearchBar()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(0,10,0,10) };
        var ico = new Label { Text = "🔍", Width = 24, Height = 26, Left = 4, Top = 13, TextAlign = ContentAlignment.MiddleCenter };
        txtSearch = new TextBox { Left = 28, Top = 13, Width = 220, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search pet, owner, vet..." };
        txtSearch.TextChanged += (_, _) => FilterData();

        cboStatusFilter = new ComboBox { Left = txtSearch.Right + 8, Top = 14, Width = 130, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboStatusFilter.Items.AddRange(["Upcoming", "Scheduled", "In Progress", "Completed", "Cancelled", "All Statuses"]);
        cboStatusFilter.SelectedIndex = 0; cboStatusFilter.SelectedIndexChanged += (_, _) => FilterData();

        var btnAdd = UIHelper.CreateButton("Add", UIHelper.Success, 70, 31); btnAdd.Left = cboStatusFilter.Right + 12; btnAdd.Top = 12; btnAdd.Click += BtnAdd_Click;
        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray, 70, 31); btnReset.Left = btnAdd.Right + 8; btnReset.Top = 12;
        btnReset.Click += (_, _) => { txtSearch.Clear(); cboStatusFilter.SelectedIndex = 0; LoadData(); };
        p.Controls.AddRange(new Control[] { ico, txtSearch, cboStatusFilter, btnAdd, btnReset }); return p;
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
        try { _data = DataStore.GetAppointments() ?? []; }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        var st = cboStatusFilter.SelectedItem?.ToString() ?? "Upcoming";
        _filtered = _data.Where(x =>
            (st == "All Statuses" || (st == "Upcoming" ? (x.Status == "Scheduled" || x.Status == "In Progress") : x.Status == st)) &&
            (string.IsNullOrWhiteSpace(q) || (x.PetName?.ToLower().Contains(q) == true) || (x.CustomerName?.ToLower().Contains(q) == true) || (x.VetName?.ToLower().Contains(q) == true))
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
            .Select(x => new { x.Id, Date = x.AppointmentDate.ToString("yyyy-MM-dd HH:mm"), x.PetName, Owner = x.CustomerName, Vet = x.VetName, Service = x.ServiceTypeName, x.Status }).ToList();
        dgv.DataSource = page;
        if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Date"]    is { } c1) { c1.HeaderText = "Date & Time"; c1.Width = 135; }
        if (dgv.Columns["PetName"] is { } c2) { c2.HeaderText = "Pet"; c2.Width = 120; }
        if (dgv.Columns["Owner"]   is { } c3) { c3.HeaderText = "Owner"; c3.Width = 140; }
        if (dgv.Columns["Vet"]     is { } c4) { c4.HeaderText = "Vet"; c4.Width = 140; }
        if (dgv.Columns["Service"] is { } c5) { c5.HeaderText = "Service"; c5.Width = 130; }
        if (dgv.Columns["Status"]  is { } c6) c6.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "Action", ReadOnly = true, FillWeight = 20 });
        int tp = GetTotalPages();
        lblStatus.Text = $"{_filtered.Count} records"; lblPage.Text = $"Page {_currentPage} / {tp}";
        btnPrev.Enabled = _currentPage > 1; btnNext.Enabled = _currentPage < tp;
        btnPrev.Visible = btnNext.Visible = lblPage.Visible = tp > 1;
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
        Size = new Size(850, 780); StartPosition = FormStartPosition.CenterParent;
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

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(40, 20, 40, 25), AutoScroll = true };

        var gridLayout = new TableLayoutPanel { Width = 740, AutoSize = true, ColumnCount = 2, Margin = new Padding(0,0,0,10) };
        gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        cboPet = new ComboBox { Width = 350, Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboPet.DataSource = _pets; cboPet.DisplayMember = "Name"; cboPet.ValueMember = "Id";
        cboPet.SelectedIndexChanged += (_, _) => UpdateHeaderSubtext();

        cboVet = new ComboBox { Width = 350, Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        var vetList = new List<object> { new User { Id = 0, FullName = "(Not assigned)" } };
        vetList.AddRange(_vets.Cast<object>()); cboVet.DataSource = vetList; cboVet.DisplayMember = "FullName"; cboVet.ValueMember = "Id";

        cboService = new ComboBox { Width = 350, Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        var svcList = new List<object> { new ServiceType { Id = 0, Name = "(Not specified)" } };
        svcList.AddRange(_services.Cast<object>()); cboService.DataSource = svcList; cboService.DisplayMember = "Name"; cboService.ValueMember = "Id";

        cboStatus = new ComboBox { Width = 350, Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboStatus.Items.AddRange(["Scheduled", "In Progress", "Completed", "Cancelled"]); cboStatus.SelectedIndex = 0;

        gridLayout.Controls.Add(UIHelper.WrapControl("Patient (Pet) *", cboPet), 0, 0);
        gridLayout.Controls.Add(UIHelper.WrapControl("Assigned Veterinarian", cboVet), 1, 0);
        gridLayout.Controls.Add(UIHelper.WrapControl("Service Category", cboService), 0, 1);
        gridLayout.Controls.Add(UIHelper.WrapControl("Appointment Status", cboStatus), 1, 1);

        flow.Controls.Add(UIHelper.CreateSectionLabel("GENERAL INFORMATION"));
        flow.Controls.Add(gridLayout);

        flow.Controls.Add(UIHelper.CreateSectionLabel("SCHEDULE & TIMING"));
        
        var gridSched = new TableLayoutPanel { Width = 740, AutoSize = true, ColumnCount = 3 };
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245));
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 245));
        gridSched.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        dtpDate = new DateTimePicker { Width = 235, Font = new Font("Segoe UI", 10.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        dtpTime = new DateTimePicker { Width = 235, Font = new Font("Segoe UI", 10.5f), Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(9) };
        numDuration = new NumericUpDown { Width = 235, Font = new Font("Segoe UI", 10.5f), Minimum = 5, Maximum = 480, Increment = 5, Value = 30 };

        gridSched.Controls.Add(UIHelper.WrapControl("Visit Date *", dtpDate), 0, 0);
        gridSched.Controls.Add(UIHelper.WrapControl("Start Time *", dtpTime), 1, 0);
        gridSched.Controls.Add(UIHelper.WrapControl("Duration (min)", numDuration), 2, 0);
        flow.Controls.Add(gridSched);

        flow.Controls.Add(UIHelper.CreateFormLabel("Internal Appointment Notes / History"));
        txtNotes = new TextBox { Width = 735, Height = 100, Multiline = true, Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0,0,0,15) };
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

        UpdateHeaderSubtext();

        if (existing is not null)
        {
            var pet = _pets.FirstOrDefault(p => p.Id == existing.PetId); if (pet != null) cboPet.SelectedItem = pet;
            var vet = vetList.OfType<User>().FirstOrDefault(v => v.Id == (existing.AssignedVetId ?? 0)); if (vet != null) cboVet.SelectedItem = vet;
            var svc = svcList.OfType<ServiceType>().FirstOrDefault(s => s.Id == (existing.ServiceTypeId ?? 0)); if (svc != null) cboService.SelectedItem = svc;
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
        }
    }

    private void UpdateHeaderSubtext()
    {
        if (cboPet.SelectedItem is Pet p)
            lblSecondary.Text = $"Scheduling visit for {p.Name} • Owner: {p.CustomerName}";
    }

    private void Save(object? s, EventArgs e)
    {
        if (cboPet.SelectedItem is not Pet pet) { VetMS.Forms.CustomMessageBox.Show("Please select a pet.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var vet = cboVet.SelectedItem as User;
        var svc = cboService.SelectedItem as ServiceType;
        var dt  = dtpDate.Value.Date + dtpTime.Value.TimeOfDay;
        Result = new Appointment
        {
            Id = Result.Id, PetId = pet.Id, PetName = pet.Name, CustomerId = pet.CustomerId, CustomerName = pet.CustomerName,
            AssignedVetId = (vet?.Id ?? 0) == 0 ? null : vet?.Id, VetName = vet?.FullName ?? "",
            ServiceTypeId = (svc?.Id ?? 0) == 0 ? null : svc?.Id, ServiceTypeName = svc?.Name ?? "",
            AppointmentDate = dt, Duration = (int)numDuration.Value,
            Status = cboStatus.SelectedItem?.ToString() ?? "Scheduled", Notes = txtNotes.Text.Trim()
        };
        DialogResult = DialogResult.OK;
    }
}
