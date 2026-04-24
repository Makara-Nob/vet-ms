using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class PetForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<Pet> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public PetForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "Patients (Pets)"; BackColor = UIHelper.LightBg;
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        var grid = new Panel { Dock = DockStyle.Top, Height = 420, BackColor = Color.White };
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
        dgv.CellPainting += (_, e) => UIHelper.PaintDynamicActionColumn(dgv, e, "View", "Edit");
        dgv.CellMouseClick += (_, e) => UIHelper.HandleDynamicActionColumnClick(dgv, e, ("View", ViewRow), ("Edit", EditRow));
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") ViewRow(e.RowIndex); };
        var pag = BuildPaginationBar(); pag.Dock = DockStyle.Bottom;
        lblNoData = UIHelper.CreateEmptyDataLabel("No pets registered yet.");
        grid.Controls.Add(lblNoData); grid.Controls.Add(dgv); grid.Controls.Add(pag);
        lblNoData.BringToFront(); dgv.BringToFront();
        content.Controls.Add(grid);
        Controls.Add(content); Controls.Add(BuildStatusBar()); Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("Patients", "Manage registered pets and patient profiles"));
    }

    private Panel BuildStatusBar()
    {
        var p = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White };
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), ForeColor = Color.FromArgb(90, 100, 115), Font = new Font("Segoe UI", 8.5f) };
        p.Controls.Add(lblStatus); p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 232, 235) }); return p;
    }

    private Panel BuildSearchBar()
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(0, 10, 0, 10) };
        var ico = new Label { Text = "🔍", Width = 24, Height = 26, Left = 4, Top = 13, TextAlign = ContentAlignment.MiddleCenter };
        txtSearch = new TextBox { Left = 28, Top = 13, Width = 300, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search pets by name, owner, species..." };
        txtSearch.TextChanged += (_, _) => FilterData();
        var btnAdd = UIHelper.CreateButton("Add", UIHelper.Success, 70, 31); btnAdd.Left = txtSearch.Right + 12; btnAdd.Top = 12; btnAdd.Click += BtnAdd_Click;
        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray, 70, 31); btnReset.Left = btnAdd.Right + 8; btnReset.Top = 12;
        btnReset.Click += (_, _) => { txtSearch.Clear(); LoadData(); };
        p.Controls.AddRange(new Control[] { ico, txtSearch, btnAdd, btnReset }); return p;
    }

    private Panel BuildPaginationBar()
    {
        var p = new Panel { Height = 48, BackColor = Color.White };
        p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 235, 240) });
        btnPrev = UIHelper.CreateButton("Prev", Color.FromArgb(108, 117, 125), 60, 26);
        btnNext = UIHelper.CreateButton("Next", Color.FromArgb(108, 117, 125), 60, 26);
        lblPage = new Label { AutoSize = false, Width = 100, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(64, 64, 64) };
        p.Resize += (_, _) => { btnNext.Left = p.Width - btnNext.Width - 16; btnNext.Top = 11; lblPage.Left = btnNext.Left - lblPage.Width - 8; lblPage.Top = 15; btnPrev.Left = lblPage.Left - btnPrev.Width - 8; btnPrev.Top = 11; };
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
        var q = txtSearch.Text.Trim().ToLower();
        _filtered = string.IsNullOrWhiteSpace(q) ? _data
            : _data.Where(x => (x.Name?.ToLower().Contains(q) == true) || (x.CustomerName?.ToLower().Contains(q) == true) || (x.SpeciesName?.ToLower().Contains(q) == true) || (x.BreedName?.ToLower().Contains(q) == true)).ToList();
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
            .Select(x => new { x.Id, x.Name, Owner = x.CustomerName, x.SpeciesName, x.BreedName, x.Gender, DOB = x.DateOfBirth?.ToString("yyyy-MM-dd") ?? "-", Status = x.IsActive ? "Active" : "Inactive" }).ToList();
        dgv.DataSource = page;
        if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Name"] is { } c1) { c1.HeaderText = "Pet Name"; c1.Width = 140; }
        if (dgv.Columns["Owner"] is { } c2) { c2.HeaderText = "Owner"; c2.Width = 160; }
        if (dgv.Columns["SpeciesName"] is { } c3) { c3.HeaderText = "Species"; c3.Width = 100; }
        if (dgv.Columns["BreedName"] is { } c4) { c4.HeaderText = "Breed"; c4.Width = 120; }
        if (dgv.Columns["Gender"] is { } c5) c5.Width = 70;
        if (dgv.Columns["DOB"] is { } c6) { c6.HeaderText = "Date of Birth"; c6.Width = 110; }
        if (dgv.Columns["Status"] is { } c7) c7.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "Action", ReadOnly = true, FillWeight = 20 });
        int tp = GetTotalPages();
        lblStatus.Text = $"{_filtered.Count} records"; lblPage.Text = $"Page {_currentPage} / {tp}";
        btnPrev.Enabled = _currentPage > 1; btnNext.Enabled = _currentPage < tp;
        btnPrev.Visible = btnNext.Visible = lblPage.Visible = tp > 1;
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
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new PetViewDialog(item);
        dlg.ShowDialog(this);
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
        Size = new Size(840, 680);
        MinimumSize = new Size(720, 580);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        _customers = DataStore.GetCustomers().Where(c => c.IsActive || (existing != null && c.Id == existing.CustomerId)).ToList();
        _species = DataStore.GetAnimalSpecies().Where(s => s.IsActive || (existing != null && s.Id == existing.SpeciesId)).ToList();
        _allBreeds = DataStore.GetBreeds().Where(b => b.IsActive || (existing != null && b.Id == existing.BreedId)).ToList();

        // ── Header band ──────────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = UIHelper.Primary };
        picAvatar = new PictureBox
        {
            Width = 62, Height = 62, Left = 20, Top = 12,
            SizeMode = PictureBoxSizeMode.Zoom, Cursor = Cursors.Hand,
            BackColor = Color.FromArgb(255, 255, 255, 20),
            Image = UIHelper.CreateProfilePlaceholder(62)
        };
        var ap = new System.Drawing.Drawing2D.GraphicsPath(); ap.AddEllipse(0, 0, 62, 62); picAvatar.Region = new Region(ap);
        UIHelper.AttachImageViewer(picAvatar, () => picAvatar.Image);
        picAvatar.Click += (_, _) => HandleUpload();

        lblHdrName = new Label
        {
            Text = existing?.Name ?? "New Patient", Left = 96, Top = 12, Width = 620, Height = 28,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.White, AutoSize = false
        };
        lblHdrSub = new Label
        {
            Text = BuildSubText(existing), Left = 96, Top = 40, Width = 620, Height = 20,
            Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(195, 215, 240), AutoSize = false
        };
        lblHdrStatus = new Label
        {
            Text = existing is null ? "New Patient" : (existing.IsActive ? "● Active" : "○ Inactive"),
            Left = 96, Top = 60, Width = 200, Height = 18,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = (existing?.IsActive == false) ? Color.FromArgb(255, 160, 160) : Color.FromArgb(160, 255, 190),
            AutoSize = false
        };
        var lblCamHint = new Label
        {
            Text = "📷 Change Photo", Left = 20, Top = 76, Width = 62, Height = 14,
            Font = new Font("Segoe UI", 7f), ForeColor = Color.FromArgb(170, 200, 235),
            TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand, AutoSize = false
        };
        lblCamHint.Click += (_, _) => HandleUpload();
        header.Controls.AddRange(new Control[] { picAvatar, lblHdrName, lblHdrSub, lblHdrStatus, lblCamHint });

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
                Height = rows * 68, ColumnCount = 2, RowCount = rows,
                Margin = new Padding(0, 0, 0, 4), Padding = new Padding(0)
            };
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            g.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < rows; i++) g.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
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
        cboCustomer = new ComboBox { Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "FullName", ValueMember = "Id" };
        cboCustomer.SelectedIndexChanged += (_, _) => UpdateHeader();
        txtName = new TextBox { Font = new Font("Segoe UI", 10.5f) };
        txtName.TextChanged += (_, _) => UpdateHeader();
        cboSpecies = new ComboBox { Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = "Name", ValueMember = "Id" };
        cboSpecies.SelectedIndexChanged += (_, _) => { FilterBreeds(); UpdateHeader(); };
        cboBreed = new ComboBox { Font = new Font("Segoe UI", 10.5f), DropDownStyle = ComboBoxStyle.DropDownList };
        cboBreed.SelectedIndexChanged += (_, _) => UpdateHeader();

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

        chkActive = new CheckBox { Text = "Patient is currently active", Checked = true, Font = new Font("Segoe UI", 10f), AutoSize = true };
        chkActive.CheckedChanged += (_, _) => UpdateHeader();
        var pnlStatus = new Panel { Height = 28 };
        chkActive.Top = 4; chkActive.Left = 0; pnlStatus.Controls.Add(chkActive);

        var gridLc = MakeGrid(1);
        gridLc.Controls.Add(WrapCell("Date of Birth", pnlDob, stretchCtrl: false), 0, 0);
        gridLc.Controls.Add(WrapCell("Patient Status", pnlStatus, rightCol: true, stretchCtrl: false), 1, 0);

        // ── CLINICAL NOTES ───────────────────────────────────────────────
        txtNotes = new TextBox
        {
            Multiline = true, Height = 96, Margin = new Padding(0, 0, 0, 0),
            Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "Allergies, conditions, special instructions..."
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
            cboSpecies.DataSource = _species;

            if (existing is not null)
            {
                cboCustomer.SelectedValue = existing.CustomerId; txtName.Text = existing.Name; cboSpecies.SelectedValue = existing.SpeciesId;
                FilterBreeds(); foreach (var itm in cboBreed.Items) if (itm is Breed b && b.Id == existing.BreedId) { cboBreed.SelectedItem = itm; break; }
                var gi = cboGender.Items.IndexOf(existing.Gender ?? "Unknown"); if (gi >= 0) cboGender.SelectedIndex = gi;
                numWeight.Value = existing.Weight;
                if (existing.DateOfBirth.HasValue) dtpDOB.Value = existing.DateOfBirth.Value; else chkNoDOB.Checked = true;
                txtColor.Text = existing.Color ?? ""; txtMicrochip.Text = existing.MicrochipNo ?? "";
                txtNotes.Text = existing.Notes ?? ""; chkActive.Checked = existing.IsActive;
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
        lblHdrStatus.Text = chkActive.Checked ? "● Active" : "○ Inactive";
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
        if (cboCustomer.SelectedItem is not Customer cust) { VetMS.Forms.CustomMessageBox.Show("Please select an owner.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (string.IsNullOrWhiteSpace(txtName.Text)) { VetMS.Forms.CustomMessageBox.Show("Patient name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtName.Focus(); return; }
        if (cboSpecies.SelectedItem is not AnimalSpecies sp) { VetMS.Forms.CustomMessageBox.Show("Please select a species.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        var breed = cboBreed.SelectedItem as Breed;
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

public class PetViewDialog : Form
{
    private readonly Pet _pet;

    public PetViewDialog(Pet pet)
    {
        _pet = pet;
        Text = $"Patient Profile — {pet.Name}";
        // Calculate form size based on available screen size
        var screen = Screen.FromPoint(MousePosition);
        int formWidth = Math.Min(980, (int)(screen.WorkingArea.Width * 0.80));
        int formHeight = Math.Min(900, (int)(screen.WorkingArea.Height * 0.90));
        Size = new Size(formWidth, formHeight);
        MinimumSize = new Size(900, 780);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        var masterPanel = new Panel { Dock = DockStyle.Fill };

        var sidebar = new Panel { Width = 300, Dock = DockStyle.Left, BackColor = Color.FromArgb(248, 249, 252), Padding = new Padding(0) };
        sidebar.Controls.Add(new Panel { Width = 1, Dock = DockStyle.Right, BackColor = Color.FromArgb(220, 222, 228) });

        var sbAvatarPanel = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = UIHelper.Primary };
        var picAvatar = new PictureBox { Width = 110, Height = 110, Left = 95, Top = 30, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(240, 242, 245), Cursor = Cursors.Hand, Image = UIHelper.CreateProfilePlaceholder(110) };
        var avatarPath = new System.Drawing.Drawing2D.GraphicsPath(); avatarPath.AddEllipse(0, 0, 110, 110); picAvatar.Region = new Region(avatarPath);
        if (_pet.ProfilePicture is { Length: > 0 }) { using var ms = new System.IO.MemoryStream(_pet.ProfilePicture); picAvatar.Image = Image.FromStream(ms); }
        UIHelper.AttachImageViewer(picAvatar, () => picAvatar.Image);
        sbAvatarPanel.Controls.Add(picAvatar);

        var lblUpload = new Label { Text = "🔍  View Photo", Left = 0, Top = 158, Width = 300, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(210, 218, 230), Cursor = Cursors.Default };
        sbAvatarPanel.Controls.Add(lblUpload);

        var sbVitals = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 249, 252), Padding = new Padding(22, 18, 22, 18), AutoScroll = true };
        int sbY = 18, sbX = 22, sbW = 256;
        void AddVitalRow(string caption, string value, bool large = false)
        {
            sbVitals.Controls.Add(new Label { Text = caption.ToUpperInvariant(), Left = sbX, Top = sbY, Width = sbW, Height = 18, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(140, 150, 165), AutoSize = false }); sbY += 19;
            sbVitals.Controls.Add(new Label { Text = value, Left = sbX, Top = sbY, Width = sbW, Height = large ? 34 : 28, Font = new Font("Segoe UI", large ? 11.5f : 10f, large ? FontStyle.Bold : FontStyle.Regular), ForeColor = large ? UIHelper.Primary : Color.FromArgb(25, 35, 50), AutoSize = false }); sbY += (large ? 34 : 28) + 18;
            sbVitals.Controls.Add(new Panel { Left = sbX, Top = sbY - 7, Width = sbW, Height = 1, BackColor = Color.FromArgb(225, 227, 232) });
        }
        void AddVitalPair(string cap1, string val1, string cap2, string val2)
        {
            int halfW = (sbW - 10) / 2;
            sbVitals.Controls.Add(new Label { Text = cap1.ToUpperInvariant(), Left = sbX, Top = sbY, Width = halfW, Height = 18, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(140, 150, 165), AutoSize = false });
            sbVitals.Controls.Add(new Label { Text = cap2.ToUpperInvariant(), Left = sbX + halfW + 10, Top = sbY, Width = halfW, Height = 18, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(140, 150, 165), AutoSize = false }); sbY += 19;
            sbVitals.Controls.Add(new Label { Text = val1, Left = sbX, Top = sbY, Width = halfW, Height = 28, Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(25, 35, 50), AutoSize = false });
            sbVitals.Controls.Add(new Label { Text = val2, Left = sbX + halfW + 10, Top = sbY, Width = halfW, Height = 28, Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(25, 35, 50), AutoSize = false }); sbY += 42;
            sbVitals.Controls.Add(new Panel { Left = sbX, Top = sbY - 7, Width = sbW, Height = 1, BackColor = Color.FromArgb(225, 227, 232) });
        }

        AddVitalRow("Patient Name", _pet.Name, large: true);
        AddVitalRow("Owner", _pet.CustomerName);
        string breedDisplay = string.IsNullOrWhiteSpace(_pet.BreedName) || _pet.BreedName.Contains("Unknown") ? "Mix / Unknown" : _pet.BreedName;
        AddVitalRow("Species / Breed", $"{_pet.SpeciesName} / {breedDisplay}");
        AddVitalRow("Date of Birth", _pet.DateOfBirth?.ToString("MMM dd, yyyy") ?? "Unknown");
        AddVitalPair("Gender", _pet.Gender, "Weight", $"{_pet.Weight:F2} kg");
        AddVitalPair("Status", _pet.IsActive ? "🟢 Active" : "🔴 Inactive", "Microchip", string.IsNullOrWhiteSpace(_pet.MicrochipNo) ? "None" : _pet.MicrochipNo);
        sidebar.Controls.Add(sbVitals); sidebar.Controls.Add(sbAvatarPanel);

        var contentArea = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10f), Padding = new Point(16, 8) };

        var tabProfile = new TabPage("  Clinical Profile  ") { BackColor = Color.White, Padding = new Padding(0) };
        var profileFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(20, 18, 10, 18), AutoScroll = true };

        int availW = formWidth - 300 - 50;
        int ctrlW = availW / 2 - 40;
        TableLayoutPanel MakeGrid2Col() { var g = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Margin = new Padding(0, 0, 0, 6) }; g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, availW / 2)); g.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, availW / 2)); return g; }
        Control MakeRO(string text) => new TextBox { Text = text, Width = ctrlW, Font = new Font("Segoe UI", 10.5f), ReadOnly = true, BackColor = Color.White };

        profileFlow.Controls.Add(new Panel { Width = 10, Height = 1, Margin = new Padding(0) });
        profileFlow.Controls.Add(UIHelper.CreateSectionLabel("IDENTITY & OWNERSHIP"));
        var gridId = MakeGrid2Col();
        gridId.Margin = new Padding(0, 0, 0, 20);
        gridId.Controls.Add(UIHelper.WrapControl("Owner / Customer", MakeRO(_pet.CustomerName)), 0, 0);
        gridId.Controls.Add(UIHelper.WrapControl("Patient Name", MakeRO(_pet.Name)), 1, 0);
        gridId.Controls.Add(UIHelper.WrapControl("Animal Species", MakeRO(_pet.SpeciesName)), 0, 1);
        gridId.Controls.Add(UIHelper.WrapControl("Breed (Specific)", MakeRO(breedDisplay)), 1, 1);
        profileFlow.Controls.Add(gridId);

        profileFlow.Controls.Add(UIHelper.CreateSectionLabel("CLINICAL ATTRIBUTES"));
        var gridCl = MakeGrid2Col();
        gridCl.Margin = new Padding(0, 0, 0, 20);
        gridCl.Controls.Add(UIHelper.WrapControl("Gender", MakeRO(_pet.Gender)), 0, 0);
        gridCl.Controls.Add(UIHelper.WrapControl("Weight (kg)", MakeRO($"{_pet.Weight:F2}")), 1, 0);
        gridCl.Controls.Add(UIHelper.WrapControl("Primary Color / Mix", MakeRO(_pet.Color)), 0, 1);
        gridCl.Controls.Add(UIHelper.WrapControl("Microchip Number", MakeRO(_pet.MicrochipNo ?? "")), 1, 1);
        profileFlow.Controls.Add(gridCl);

        profileFlow.Controls.Add(UIHelper.CreateSectionLabel("CLINICAL NOTES"));
        var txtNotes = new TextBox { Text = _pet.Notes, Width = availW, Height = 110, Multiline = true, Font = new Font("Segoe UI", 10f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 20), ReadOnly = true, BackColor = Color.White };
        profileFlow.Controls.Add(txtNotes);
        profileFlow.Controls.Add(new Panel { Width = availW, Height = 40, Margin = new Padding(0) }); // Bottom spacer to fix cutoff
        tabProfile.Controls.Add(profileFlow);

        var tabMedical = new TabPage("  Medical History  ") { BackColor = Color.White };
        var dgvMedical = BuildHistoryGrid();
        var lblNoMedical = UIHelper.CreateEmptyDataLabel("No medical records found for this patient.");
        tabMedical.Controls.Add(dgvMedical); tabMedical.Controls.Add(lblNoMedical); lblNoMedical.BringToFront();
        var medRecords = DataStore.GetMedicalRecords().Where(m => m.PetId == _pet.Id).OrderByDescending(m => m.CreatedAt)
            .Select(m => new { m.Id, Date = m.CreatedAt.ToString("yyyy-MM-dd"), Vet = m.VetName, Diagnosis = m.Diagnosis.Length > 60 ? m.Diagnosis[..57] + "..." : m.Diagnosis, FollowUp = m.FollowUpDate?.ToString("yyyy-MM-dd") ?? "-" }).ToList();
        if (medRecords.Count > 0)
        {
            dgvMedical.DataSource = medRecords;
            if (dgvMedical.Columns["Id"] != null) dgvMedical.Columns["Id"].Visible = false;
            if (dgvMedical.Columns["Date"] is { } mc1) { mc1.HeaderText = "Date"; mc1.Width = 100; }
            if (dgvMedical.Columns["Vet"] is { } mc2) { mc2.HeaderText = "Attending Vet"; mc2.Width = 170; }
            if (dgvMedical.Columns["Diagnosis"] is { } mc3) { mc3.HeaderText = "Diagnosis"; mc3.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
            if (dgvMedical.Columns["FollowUp"] is { } mc4) { mc4.HeaderText = "Follow-Up"; mc4.Width = 110; }
            lblNoMedical.Visible = false; dgvMedical.Visible = true;
        }
        else { lblNoMedical.Visible = true; dgvMedical.Visible = false; }
        dgvMedical.CellDoubleClick += (_, e) => {
            if (e.RowIndex < 0 || dgvMedical.Rows[e.RowIndex].Cells["Id"]?.Value is not int rid) return;
            var rec = DataStore.GetMedicalRecords().FirstOrDefault(m => m.Id == rid); if (rec is null) return;
            using var dlg = new MedicalRecordDialog(rec, true); dlg.ShowDialog(this);
        };

        var tabAppt = new TabPage("  Appointments  ") { BackColor = Color.White };
        var dgvAppt = BuildHistoryGrid();
        var lblNoAppt = UIHelper.CreateEmptyDataLabel("No appointments found for this patient.");
        tabAppt.Controls.Add(dgvAppt); tabAppt.Controls.Add(lblNoAppt); lblNoAppt.BringToFront();
        var appts = DataStore.GetAppointments().Where(a => a.PetId == _pet.Id).OrderByDescending(a => a.AppointmentDate)
            .Select(a => new { a.Id, Date = a.AppointmentDate.ToString("yyyy-MM-dd HH:mm"), Service = a.ServiceTypeName, Vet = a.VetName, a.Status, Duration = $"{a.Duration} min" }).ToList();
        if (appts.Count > 0)
        {
            dgvAppt.DataSource = appts;
            if (dgvAppt.Columns["Id"] != null) dgvAppt.Columns["Id"].Visible = false;
            if (dgvAppt.Columns["Date"] is { } ac1) { ac1.HeaderText = "Date & Time"; ac1.Width = 140; }
            if (dgvAppt.Columns["Service"] is { } ac2) { ac2.HeaderText = "Service"; ac2.Width = 170; }
            if (dgvAppt.Columns["Vet"] is { } ac3) { ac3.HeaderText = "Veterinarian"; ac3.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
            if (dgvAppt.Columns["Status"] is { } ac4) { ac4.HeaderText = "Status"; ac4.Width = 110; }
            if (dgvAppt.Columns["Duration"] is { } ac5) { ac5.HeaderText = "Duration"; ac5.Width = 80; }
            lblNoAppt.Visible = false; dgvAppt.Visible = true;
        }
        else { lblNoAppt.Visible = true; dgvAppt.Visible = false; }

        tabs.TabPages.Add(tabProfile); tabs.TabPages.Add(tabMedical); tabs.TabPages.Add(tabAppt);
        contentArea.Controls.Add(tabs);

        masterPanel.Controls.Add(contentArea); masterPanel.Controls.Add(sidebar);

        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
        pnlBtn.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(225, 228, 235) });
        var btnClose = UIHelper.CreateButton("  Close  ", Color.FromArgb(108, 117, 125), 110);
        btnClose.Top = 11; btnClose.Left = pnlBtn.Width - 122; btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClose.Click += (s, e) => DialogResult = DialogResult.OK;
        pnlBtn.Controls.Add(btnClose);
        Controls.Add(pnlBtn); Controls.Add(masterPanel); AcceptButton = btnClose; CancelButton = btnClose;
    }

    private static DataGridView BuildHistoryGrid()
    {
        var g = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, BorderStyle = BorderStyle.None, BackgroundColor = Color.White, Font = new Font("Segoe UI", 10f), Cursor = Cursors.Hand };
        UIHelper.StyleGrid(g); return g;
    }
}