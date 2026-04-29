using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class PetForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<Pet> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public PetForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "Patients (Pets)"; BackColor = UIHelper.LightBg;
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
        lblNoData = UIHelper.CreateEmptyDataLabel("No pets registered yet.");

        gridContainer.Controls.Add(lblNoData);
        gridContainer.Controls.Add(dgv);
        gridContainer.Controls.Add(pag);
        lblNoData.BringToFront();

        contentPanel.Controls.Add(gridContainer);
        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("Patients", "Manage animal records, owners, and patient profiles"));
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
        txtSearch = new TextBox { Left = 46, Top = 20, Width = 240, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search pet, owner, species..." };
        txtSearch.TextChanged += (_, _) => FilterData();

        cboStatus = new ComboBox { Left = txtSearch.Right + 10, Top = 20, Width = 130, Font = new Font("Segoe UI", 10f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboStatus.Items.AddRange(["Active Only", "Inactive Only", "All Patients"]);
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
        try { _data = DataStore.GetPets() ?? []; }
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
             x.Name?.ToLower().Contains(q) == true ||
             x.CustomerName?.ToLower().Contains(q) == true ||
             x.SpeciesName?.ToLower().Contains(q) == true ||
             x.BreedName?.ToLower().Contains(q) == true)
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
            .Select(x => new { x.Id, x.Name, Owner = x.CustomerName, x.SpeciesName, x.BreedName, x.Gender, DOB = x.DateOfBirth?.ToString("MMM dd, yyyy") ?? "-", Status = x.IsActive ? "Active" : "Inactive", CreatedAt = x.CreatedAt.ToString("MMM dd, yyyy") }).ToList();
        dgv.DataSource = page;
        if (dgv.Columns["Id"]          != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Name"]        is { } c1) c1.HeaderText = "Patient Name";
        if (dgv.Columns["Owner"]       is { } c2) c2.HeaderText = "Owner";
        if (dgv.Columns["SpeciesName"] is { } c3) c3.HeaderText = "Species";
        if (dgv.Columns["BreedName"]   is { } c4) c4.HeaderText = "Breed";
        if (dgv.Columns["Gender"]      is { } c5) c5.HeaderText = "Gender";
        if (dgv.Columns["DOB"]         is { } c6) c6.HeaderText = "Date of Birth";
        if (dgv.Columns["Status"]      is { } c7) c7.HeaderText = "Status";
        if (dgv.Columns["CreatedAt"]   is { } c8) c8.HeaderText = "Created At";
        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "", ReadOnly = true });
        // Set notes tooltip on every cell in each row
        var pageData = _filtered.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
        for (int i = 0; i < dgv.Rows.Count && i < pageData.Count; i++)
        {
            var notes = pageData[i].Notes?.Trim();
            if (!string.IsNullOrEmpty(notes))
                foreach (DataGridViewCell cell in dgv.Rows[i].Cells)
                    cell.ToolTipText = notes;
        }
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
        const int totalWeight = 900; // 130+140+90+110+70+90+90+180
        int available = dgv.ClientSize.Width - actionW - 2;
        if (available <= 0) return;
        if (dgv.Columns["ColAction"]   is { } ca) { ca.Width = actionW; ca.DisplayIndex = dgv.Columns.Count - 1; }
        if (dgv.Columns["Name"]        is { } c1) c1.Width = available * 140 / totalWeight;
        if (dgv.Columns["Owner"]       is { } c2) c2.Width = available * 130 / totalWeight;
        if (dgv.Columns["SpeciesName"] is { } c3) c3.Width = available *  90 / totalWeight;
        if (dgv.Columns["BreedName"]   is { } c4) c4.Width = available * 110 / totalWeight;
        if (dgv.Columns["Gender"]      is { } c5) c5.Width = available *  70 / totalWeight;
        if (dgv.Columns["DOB"]         is { } c6) c6.Width = available *  90 / totalWeight;
        if (dgv.Columns["Status"]      is { } c7) c7.Width = available *  90 / totalWeight;
        if (dgv.Columns["CreatedAt"]   is { } c8) c8.Width = available * 180 / totalWeight;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new PetDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Pet registered!"); LoadData();
    }

    private void EditRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new PetDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var r = dlg.Result;
        item.CustomerId = r.CustomerId; item.CustomerName = r.CustomerName; item.SpeciesId = r.SpeciesId;
        item.SpeciesName = r.SpeciesName; item.BreedId = r.BreedId; item.BreedName = r.BreedName;
        item.Name = r.Name; item.Gender = r.Gender; item.DateOfBirth = r.DateOfBirth;
        item.Weight = r.Weight; item.Color = r.Color; item.MicrochipNo = r.MicrochipNo;
        item.Notes = r.Notes; item.IsActive = r.IsActive; item.ProfilePicture = r.ProfilePicture;
        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Pet updated!"); LoadData();
    }
    private void ViewRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); 
        if (item is null) return;
        
        // Load PetDetailsForm as a page
        MainForm.Instance.LoadForm(new PetDetailsForm(item, () => MainForm.Instance.LoadForm(new PetForm())));
    }
}

public class PetDialog : Form
{
    private readonly ComboBox cboCustomer, cboSpecies, cboBreed, cboGender;
    private readonly TextBox txtName, txtColor, txtMicrochip, txtNotes;
    private readonly NumericUpDown numWeight;
    private readonly DateTimePicker dtpDOB;
    private readonly CheckBox chkActive, chkNoDOB;
    private readonly PictureBox picAvatar;
    private Label lblHdrName = null!, lblHdrSub = null!, lblHdrStatus = null!;

    private byte[]? _profilePicture;
    private readonly List<Customer> _customers;
    private readonly List<AnimalSpecies> _species;
    private readonly List<Breed> _allBreeds;
    public Pet Result { get; private set; } = new();

    public PetDialog(Pet? existing = null)
    {
        Text = existing is null ? "Register New Patient" : $"Edit Patient — {existing.Name}";
        Size = new Size(980, 920);
        MinimumSize = new Size(900, 820);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        _customers = DataStore.GetCustomers().Where(c => c.IsActive || (existing != null && c.Id == existing.CustomerId)).ToList();
        _species = DataStore.GetAnimalSpecies().Where(s => s.IsActive || (existing != null && s.Id == existing.SpeciesId)).ToList();
        _allBreeds = DataStore.GetBreeds().Where(b => b.IsActive || (existing != null && b.Id == existing.BreedId)).ToList();

        // ── Header band ──────────────────────────────────────────────────
        // Avatar column (90 px) + FlowLayoutPanel fills the rest top-down.
        // All text labels use AutoSize=true so font metrics at any DPI determine
        // the height — nothing is ever clipped by a hard-coded pixel value.
        var header = new Panel { Dock = DockStyle.Top, Height = 120, BackColor = UIHelper.Primary };

        // left column: avatar + "Change Photo" hint
        var avatarCol = new Panel { Width = 90, Dock = DockStyle.Left, BackColor = UIHelper.Primary };
        picAvatar = new PictureBox
        {
            Width = 62, Height = 62, Left = 14, Top = 16,
            SizeMode = PictureBoxSizeMode.Zoom, Cursor = Cursors.Hand,
            BackColor = Color.FromArgb(255, 255, 255, 20),
            Image = UIHelper.CreateProfilePlaceholder(62)
        };
        var ap = new System.Drawing.Drawing2D.GraphicsPath(); ap.AddEllipse(0, 0, 62, 62); picAvatar.Region = new Region(ap);
        UIHelper.AttachImageViewer(picAvatar, () => picAvatar.Image);
        picAvatar.Click += (_, _) => HandleUpload();
        var lblCamHint = new Label
        {
            Text = "📷 Change Photo", Left = 14, Top = 82, Width = 62, Height = 16,
            Font = new Font("Segoe UI", 7f), ForeColor = Color.FromArgb(170, 200, 235),
            TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, AutoSize = false
        };
        lblCamHint.Click += (_, _) => HandleUpload();
        avatarCol.Controls.AddRange(new Control[] { picAvatar, lblCamHint });

        // right column: name / sub-info / status, stacked by FlowLayoutPanel
        lblHdrName = new Label
        {
            Text = existing?.Name ?? "New Patient",
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
            AutoSize = true, Margin = new Padding(0, 0, 0, 0)
        };
        var textFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            BackColor = UIHelper.Primary, WrapContents = false,
            Padding = new Padding(0, 18, 16, 10)
        };
        textFlow.Controls.AddRange(new Control[] { lblHdrName, lblHdrSub, lblHdrStatus });

        // avatarCol docks Left, textFlow fills the rest
        header.Controls.Add(textFlow);
        header.Controls.Add(avatarCol);

        // ── Footer ───────────────────────────────────────────────────────
        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.White };
        pnlBtn.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(225, 228, 235) });
        var btnSave = UIHelper.CreateButton("Save Patient", UIHelper.Success, 120);
        var btnCancel = UIHelper.CreateButton("Cancel", Color.FromArgb(108, 117, 125), 100);
        btnSave.Top = btnCancel.Top = 11;
        btnSave.Anchor = btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pnlBtn.Resize += (_, _) => { btnSave.Left = pnlBtn.Width - 132; btnCancel.Left = btnSave.Left - 108; };
        btnSave.Click += Save; btnCancel.DialogResult = DialogResult.Cancel;
        pnlBtn.Controls.AddRange(new Control[] { btnSave, btnCancel });

        // ── Scrollable form body (FlowLayoutPanel = predictable top-to-bottom order) ──
        var body = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true,
            Padding = new Padding(24, 10, 8, 24), BackColor = Color.White
        };

        // sync widths of grids / labels / notes whenever the panel resizes
        void SyncWidths()
        {
            int w = body.ClientSize.Width - body.Padding.Left - body.Padding.Right;
            if (w <= 10) return;
            foreach (Control c in body.Controls)
                if (c is TableLayoutPanel || c is Label || c is TextBox)
                    c.Width = w;
        }
        body.ClientSizeChanged += (_, _) => SyncWidths();

        // helper: 2-column TableLayoutPanel with 50%/50% columns, explicit height
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

        // helper: cell wrapper — label above control, control fills cell width
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

        // helper: section divider label (width synced by SyncWidths)
        Label MakeSection(string text) => new Label
        {
            Text = text, Height = 36, Margin = new Padding(0, 8, 0, 0),
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(110, 125, 148),
            Padding = new Padding(0, 14, 0, 0), AutoSize = false
        };

        // ── IDENTITY & OWNERSHIP ─────────────────────────────────────────
        cboCustomer = new ComboBox { Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDown, DisplayMember = "FullName", ValueMember = "Id", AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        cboCustomer.SelectedIndexChanged += (_, _) => UpdateHeader();
        // Commit typed text → SelectedItem when focus leaves (DropDown + AutoComplete doesn't do this automatically)
        cboCustomer.Leave += (_, _) =>
        {
            if (cboCustomer.SelectedItem is Customer) return;
            var match = _customers.FirstOrDefault(c => string.Equals(c.FullName, cboCustomer.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is not null) cboCustomer.SelectedItem = match;
        };

        txtName = new TextBox { Font = new Font("Segoe UI", 10.5f) };
        txtName.TextChanged += (_, _) => UpdateHeader();

        cboSpecies = new ComboBox { Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDown, DisplayMember = "Name", ValueMember = "Id", AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        cboSpecies.SelectedIndexChanged += (_, _) => { FilterBreeds(); UpdateHeader(); };
        cboSpecies.Leave += (_, _) =>
        {
            if (cboSpecies.SelectedItem is AnimalSpecies) return;
            var match = _species.FirstOrDefault(s => string.Equals(s.Name, cboSpecies.Text.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match is not null) cboSpecies.SelectedItem = match; // fires SelectedIndexChanged → FilterBreeds
        };

        cboBreed = new ComboBox { Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDown, AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems };
        cboBreed.SelectedIndexChanged += (_, _) => UpdateHeader();
        cboBreed.Leave += (_, _) =>
        {
            if (cboBreed.SelectedItem is Breed) return;
            var txt = cboBreed.Text.Trim();
            foreach (object item in cboBreed.Items)
                if (item is Breed b && string.Equals(b.Name, txt, StringComparison.OrdinalIgnoreCase))
                { cboBreed.SelectedItem = item; return; }
        };

        var gridId = MakeGrid(2);
        gridId.Controls.Add(WrapCell("Owner / Customer *", cboCustomer), 0, 0);
        gridId.Controls.Add(WrapCell("Patient Name *", txtName, true), 1, 0);
        gridId.Controls.Add(WrapCell("Animal Species *", cboSpecies), 0, 1);
        gridId.Controls.Add(WrapCell("Breed  (leave blank if unknown / mix)", cboBreed, true), 1, 1);

        // ── CLINICAL ATTRIBUTES ──────────────────────────────────────────
        cboGender = new ComboBox { Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboGender.Items.AddRange(["Male", "Female", "Unknown"]); cboGender.SelectedIndex = 2;
        cboGender.SelectedIndexChanged += (_, _) => UpdateHeader();
        numWeight = new NumericUpDown { DecimalPlaces = 2, Maximum = 999, Font = new Font("Segoe UI", 10.5f) };
        numWeight.ValueChanged += (_, _) => UpdateHeader();
        txtColor = new TextBox { Font = new Font("Segoe UI", 10.5f), PlaceholderText = "e.g. Black, white, Tabby..." };
        txtMicrochip = new TextBox { Font = new Font("Segoe UI", 10.5f), PlaceholderText = "15-digit chip number" };
        txtMicrochip.TextChanged += (_, _) => UpdateHeader();

        var gridCl = MakeGrid(2);
        gridCl.Controls.Add(WrapCell("Gender", cboGender), 0, 0);
        gridCl.Controls.Add(WrapCell("Weight (kg)", numWeight, true), 1, 0);
        gridCl.Controls.Add(WrapCell("Primary Color / Markings", txtColor), 0, 1);
        gridCl.Controls.Add(WrapCell("Microchip Number", txtMicrochip, true), 1, 1);

        // ── LIFECYCLE STATUS ─────────────────────────────────────────────
        dtpDOB = new DateTimePicker { Font = new Font("Segoe UI", 10.5f), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
        dtpDOB.ValueChanged += (_, _) => UpdateHeader();
        chkNoDOB = new CheckBox { Text = "Unknown", Font = new Font("Segoe UI", 9.5f), Width = 90, Height = 22 };
        chkNoDOB.CheckedChanged += (_, _) => { dtpDOB.Enabled = !chkNoDOB.Checked; UpdateHeader(); };
        var pnlDob = new FlowLayoutPanel { Height = 28, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        dtpDOB.Margin = new Padding(0, 0, 10, 0); chkNoDOB.Margin = new Padding(0, 4, 0, 0);
        pnlDob.Controls.AddRange(new Control[] { dtpDOB, chkNoDOB });

        chkActive = new CheckBox { Text = "Active", Checked = true, Font = new Font("Segoe UI", 10f), AutoSize = false, Height = 26 };
        chkActive.CheckedChanged += (_, _) =>
        {
            chkActive.Text = chkActive.Checked ? "Active" : "Inactive";
            UpdateHeader();
        };

        var gridLc = MakeGrid(1);
        gridLc.Controls.Add(WrapCell("Date of Birth",   pnlDob,    stretchCtrl: false), 0, 0);
        gridLc.Controls.Add(WrapCell("Patient Status",  chkActive, rightCol: true, stretchCtrl: true), 1, 0);

        // ── CLINICAL NOTES ───────────────────────────────────────────────
        txtNotes = new TextBox
        {
            Multiline = true, Height = 130, Margin = new Padding(0, 0, 0, 0),
            Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "Allergies, conditions, special instructions..."
        };
        var notesTooltip = new ToolTip { AutoPopDelay = 12000, InitialDelay = 400, ReshowDelay = 200, ShowAlways = true };
        txtNotes.TextChanged += (_, _) =>
        {
            var t = txtNotes.Text.Trim();
            notesTooltip.SetToolTip(txtNotes, string.IsNullOrEmpty(t) ? "" : t);
        };

        // add sections top-to-bottom (first added = topmost in FlowLayoutPanel)
        body.Controls.Add(MakeSection("IDENTITY & OWNERSHIP"));
        body.Controls.Add(gridId);
        body.Controls.Add(MakeSection("CLINICAL ATTRIBUTES"));
        body.Controls.Add(gridCl);
        body.Controls.Add(MakeSection("LIFECYCLE STATUS"));
        body.Controls.Add(gridLc);
        body.Controls.Add(MakeSection("CLINICAL NOTES"));
        body.Controls.Add(txtNotes);

        Controls.Add(body); Controls.Add(pnlBtn); Controls.Add(header);
        AcceptButton = btnSave; CancelButton = btnCancel;

        this.Load += (s, e) =>
        {
            SyncWidths();
            cboCustomer.DataSource = _customers;
            cboSpecies.DataSource  = _species;
            FilterBreeds(); // always populate breeds — SelectedIndexChanged timing is unreliable with AutoComplete+DataSource

            if (existing is not null)
            {
                cboCustomer.SelectedValue = existing.CustomerId; txtName.Text = existing.Name; cboSpecies.SelectedValue = existing.SpeciesId;
                FilterBreeds(); foreach (var itm in cboBreed.Items) if (itm is Breed b && b.Id == existing.BreedId) { cboBreed.SelectedItem = itm; break; }
                var gi = cboGender.Items.IndexOf(existing.Gender ?? "Unknown"); if (gi >= 0) cboGender.SelectedIndex = gi;
                numWeight.Value = existing.Weight;
                if (existing.DateOfBirth.HasValue) dtpDOB.Value = existing.DateOfBirth.Value; else chkNoDOB.Checked = true;
                txtColor.Text = existing.Color ?? ""; txtMicrochip.Text = existing.MicrochipNo ?? "";
                txtNotes.Text = existing.Notes ?? "";
                chkActive.Checked = existing.IsActive;
                chkActive.Text = existing.IsActive ? "Active" : "Inactive";
                _profilePicture = existing.ProfilePicture;
                if (_profilePicture is { Length: > 0 }) { using var ms = new System.IO.MemoryStream(_profilePicture); picAvatar.Image = Image.FromStream(ms); }
                Result.Id = existing.Id;
            }
            UpdateHeader();
            SyncWidths();
            body.AutoScrollPosition = new Point(0, 0);
            cboCustomer.Select();
        };
    }

    private static string BuildSubText(Pet? p)
    {
        if (p is null) return "Owner: —  ·  Species: —";
        var breed = string.IsNullOrWhiteSpace(p.BreedName) || p.BreedName.Contains("Unknown") ? "Mix" : p.BreedName;
        return $"Owner: {p.CustomerName}  ·  {p.SpeciesName} / {breed}";
    }

    private void UpdateHeader()
    {
        lblHdrName.Text = string.IsNullOrWhiteSpace(txtName.Text) ? "New Patient" : txtName.Text;
        var owner = cboCustomer.SelectedItem is Customer c ? c.FullName : "—";
        var species = cboSpecies.SelectedItem is AnimalSpecies sp ? sp.Name : "—";
        var breed = (cboBreed.SelectedItem as Breed)?.Name;
        if (string.IsNullOrWhiteSpace(breed) || breed.Contains("Unknown")) breed = "Mix";
        var gender = cboGender.SelectedItem?.ToString() ?? "—";
        var weight = numWeight.Value > 0 ? $"  ·  {numWeight.Value:F1} kg" : "";
        lblHdrSub.Text = $"Owner: {owner}  ·  {species} / {breed}  ·  {gender}{weight}";
        lblHdrStatus.Text = chkActive.Checked ? "Active" : "Inactive";
        lblHdrStatus.ForeColor = chkActive.Checked ? Color.FromArgb(160, 255, 190) : Color.FromArgb(255, 160, 160);
    }

    private void HandleUpload()
    {
        using var ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp" };
        if (ofd.ShowDialog() != DialogResult.OK) return;
        try { _profilePicture = System.IO.File.ReadAllBytes(ofd.FileName); using var ms = new System.IO.MemoryStream(_profilePicture); picAvatar.Image = Image.FromStream(ms); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); }
    }

    private void FilterBreeds()
    {
        if (cboSpecies.SelectedItem is not AnimalSpecies sp) { cboBreed.Items.Clear(); return; }
        var breeds = _allBreeds.Where(b => b.SpeciesId == sp.Id).ToList();
        cboBreed.Items.Clear(); cboBreed.Items.Add(new Breed { Id = 0, Name = "(Unknown / Mix)" });
        cboBreed.Items.AddRange(breeds.ToArray()); cboBreed.DisplayMember = "Name"; cboBreed.ValueMember = "Id"; cboBreed.SelectedIndex = 0;
    }

    private void Save(object? s, EventArgs e)
    {
        // DropDown + AutoComplete: SelectedItem may be null when user typed without clicking a suggestion.
        // Fall back to matching the typed text against the source list.
        var cust = cboCustomer.SelectedItem as Customer
            ?? _customers.FirstOrDefault(c => string.Equals(c.FullName, cboCustomer.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (cust is null) { VetMS.Forms.CustomMessageBox.Show("Please select an owner.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cboCustomer.Focus(); return; }

        if (string.IsNullOrWhiteSpace(txtName.Text)) { VetMS.Forms.CustomMessageBox.Show("Patient name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtName.Focus(); return; }

        var sp = cboSpecies.SelectedItem as AnimalSpecies
            ?? _species.FirstOrDefault(s => string.Equals(s.Name, cboSpecies.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        if (sp is null) { VetMS.Forms.CustomMessageBox.Show("Please select a species.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); cboSpecies.Focus(); return; }

        var breed = cboBreed.SelectedItem as Breed
            ?? _allBreeds.FirstOrDefault(b => string.Equals(b.Name, cboBreed.Text.Trim(), StringComparison.OrdinalIgnoreCase));
        Result = new Pet
        {
            Id = Result.Id,
            CustomerId = cust.Id,
            CustomerName = cust.FullName,
            SpeciesId = sp.Id,
            SpeciesName = sp.Name,
            BreedId = (breed?.Id ?? 0) == 0 ? null : breed?.Id,
            BreedName = breed?.Name ?? "",
            Name = txtName.Text.Trim(),
            Gender = cboGender.SelectedItem?.ToString() ?? "Unknown",
            DateOfBirth = chkNoDOB.Checked ? null : dtpDOB.Value,
            Weight = numWeight.Value,
            Color = txtColor.Text.Trim(),
            MicrochipNo = txtMicrochip.Text.Trim(),
            Notes = txtNotes.Text.Trim(),
            IsActive = chkActive.Checked,
            ProfilePicture = _profilePicture
        };
        DialogResult = DialogResult.OK;
    }
}