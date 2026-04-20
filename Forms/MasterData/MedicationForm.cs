using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.MasterData;

public class MedicationForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private List<Medication> _data = [];

    public MedicationForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Medications";
        BackColor = UIHelper.LightBg;

        Controls.Add(UIHelper.CreateHeader("Medications", "Manage drugs and medication inventory items"));

        var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.White, Padding = new Padding(12, 8, 12, 0) };
        var lblSearch = new Label { Text = "Search:", AutoSize = true, Left = 12, Top = 14, Font = new Font("Segoe UI", 9f) };
        txtSearch = new TextBox { Left = 68, Top = 11, Width = 240, Font = new Font("Segoe UI", 9.5f) };
        txtSearch.TextChanged += (_, _) => FilterData();
        pnlSearch.Controls.AddRange(new Control[] { lblSearch, txtSearch });

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false
        };
        UIHelper.StyleGrid(dgv);
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) BtnEdit_Click(null, EventArgs.Empty); };

        var pnlBtn = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White, Padding = new Padding(12, 9, 12, 9) };
        var btnAdd     = UIHelper.CreateButton("+ Add",     UIHelper.Success);
        var btnEdit    = UIHelper.CreateButton("✎ Edit",    UIHelper.Accent);
        var btnDelete  = UIHelper.CreateButton("✕ Delete",  UIHelper.Danger);
        var btnRefresh = UIHelper.CreateButton("↺ Refresh", Color.FromArgb(108, 117, 125));

        int x = 0;
        foreach (var b in new[] { btnAdd, btnEdit, btnDelete, btnRefresh })
        { b.Left = x; b.Top = 10; pnlBtn.Controls.Add(b); x += 98; }

        btnAdd.Click     += BtnAdd_Click;
        btnEdit.Click    += BtnEdit_Click;
        btnDelete.Click  += BtnDelete_Click;
        btnRefresh.Click += (_, _) => LoadData();

        var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(220, 225, 230) };

        Controls.Add(dgv);
        Controls.Add(pnlSearch);
        Controls.Add(sep);
        Controls.Add(pnlBtn);
    }

    private void LoadData()
    {
        try { _data = DataStore.GetMedications(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        var list = string.IsNullOrEmpty(q)
            ? _data
            : _data.Where(x => x.Name.ToLower().Contains(q)
                             || x.Category.ToLower().Contains(q)
                             || x.DosageForm.ToLower().Contains(q)).ToList();

        dgv.DataSource = list.Select(x => new
        {
            x.Id,
            x.Name,
            x.Category,
            x.DosageForm,
            x.Unit,
            Status = x.IsActive ? "Active" : "Inactive",
            x.Description                               // last → fills remaining space
        }).ToList();

        if (dgv.Columns.Count == 0) return;
        if (dgv.Columns["Id"]          is { } cId)   cId.Visible = false;
        if (dgv.Columns["Name"]        is { } cName) cName.Width     = 180;
        if (dgv.Columns["Category"]    is { } cCat)  cCat.Width      = 130;
        if (dgv.Columns["DosageForm"]  is { } cDose) { cDose.HeaderText = "Dosage Form"; cDose.Width = 110; }
        if (dgv.Columns["Unit"]        is { } cUnit) cUnit.Width     = 70;
        if (dgv.Columns["Status"]      is { } cSt)   cSt.Width       = 80;
        if (dgv.Columns["Description"] is { } cDesc) cDesc.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new MedicationDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadData();
    }

    private void BtnEdit_Click(object? s, EventArgs e)
    {
        if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Select a record to edit.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (dgv.SelectedRows[0].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        using var dlg = new MedicationDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        item.Name        = dlg.Result.Name;
        item.Category    = dlg.Result.Category;
        item.DosageForm  = dlg.Result.DosageForm;
        item.Unit        = dlg.Result.Unit;
        item.Description = dlg.Result.Description;
        item.IsActive    = dlg.Result.IsActive;

        try { DataStore.Update(item); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadData();
    }

    private void BtnDelete_Click(object? s, EventArgs e)
    {
        if (dgv.SelectedRows.Count == 0) { MessageBox.Show("Select a record to delete.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (dgv.SelectedRows[0].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        if (MessageBox.Show($"Delete medication \"{item.Name}\"?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try { DataStore.Delete(item); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadData();
    }
}

// ── Medication dialog ─────────────────────────────────────────────────────────
public class MedicationDialog : Form
{
    private readonly TextBox txtName, txtUnit, txtDesc;
    private readonly ComboBox cboCategory, cboDosageForm;
    private readonly CheckBox chkActive;
    public Medication Result { get; private set; } = new();

    private static readonly string[] Categories  = ["Antibiotic", "Antiparasitic", "Analgesic", "Anti-inflammatory", "Vaccine", "Vitamin", "Antifungal", "Sedative", "Other"];
    private static readonly string[] DosageForms = ["Tablet", "Capsule", "Injection", "Syrup", "Powder", "Cream", "Drops", "Spray", "Other"];

    public MedicationDialog(Medication? existing = null)
    {
        Text = existing is null ? "Add Medication" : "Edit Medication";
        Size = new Size(450, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20, 14, 20, 0), AutoScroll = true };

        flow.Controls.Add(UIHelper.CreateFormLabel("Medication Name *"));
        txtName = UIHelper.CreateTextBox(380);
        flow.Controls.Add(txtName);

        flow.Controls.Add(UIHelper.CreateFormLabel("Category *"));
        cboCategory = new ComboBox { Width = 380, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 8) };
        cboCategory.Items.AddRange(Categories);
        flow.Controls.Add(cboCategory);

        flow.Controls.Add(UIHelper.CreateFormLabel("Dosage Form *"));
        cboDosageForm = new ComboBox { Width = 380, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 8) };
        cboDosageForm.Items.AddRange(DosageForms);
        flow.Controls.Add(cboDosageForm);

        flow.Controls.Add(UIHelper.CreateFormLabel("Unit (e.g. mg, ml, tablets)"));
        txtUnit = UIHelper.CreateTextBox(180);
        flow.Controls.Add(txtUnit);

        flow.Controls.Add(UIHelper.CreateFormLabel("Description"));
        txtDesc = new TextBox { Width = 380, Height = 56, Multiline = true, Font = new Font("Segoe UI", 9.5f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 6) };
        flow.Controls.Add(txtDesc);

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
            txtName.Text = existing.Name;
            var catIdx  = Array.IndexOf(Categories,  existing.Category);
            if (catIdx >= 0)  cboCategory.SelectedIndex  = catIdx;
            var doseIdx = Array.IndexOf(DosageForms, existing.DosageForm);
            if (doseIdx >= 0) cboDosageForm.SelectedIndex = doseIdx;
            txtUnit.Text      = existing.Unit;
            txtDesc.Text      = existing.Description;
            chkActive.Checked = existing.IsActive;
            Result.Id         = existing.Id;
        }
    }

    private void BtnSave_Click(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtName.Text))
        { MessageBox.Show("Medication name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtName.Focus(); return; }
        if (cboCategory.SelectedItem is null)
        { MessageBox.Show("Category is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if (cboDosageForm.SelectedItem is null)
        { MessageBox.Show("Dosage form is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        Result = new Medication
        {
            Id          = Result.Id,
            Name        = txtName.Text.Trim(),
            Category    = cboCategory.SelectedItem.ToString()!,
            DosageForm  = cboDosageForm.SelectedItem.ToString()!,
            Unit        = txtUnit.Text.Trim(),
            Description = txtDesc.Text.Trim(),
            IsActive    = chkActive.Checked
        };
        DialogResult = DialogResult.OK;
    }
}
