using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.MasterData;

public class SupplierForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private List<Supplier> _data = [];

    public SupplierForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "Suppliers";
        BackColor = UIHelper.LightBg;

        Controls.Add(UIHelper.CreateHeader("Suppliers", "Manage medication and supply vendors"));

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
        try { _data = DataStore.GetSuppliers(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        var list = string.IsNullOrEmpty(q)
            ? _data
            : _data.Where(x => x.CompanyName.ToLower().Contains(q)
                             || x.ContactPerson.ToLower().Contains(q)
                             || x.Phone.ToLower().Contains(q)
                             || x.Email.ToLower().Contains(q)).ToList();

        dgv.DataSource = list.Select(x => new
        {
            x.Id,
            Company = x.CompanyName,
            Contact = x.ContactPerson,
            x.Phone,
            x.Email,
            Status  = x.IsActive ? "Active" : "Inactive",
            x.Address                                   // last → fills remaining space
        }).ToList();

        if (dgv.Columns.Count == 0) return;
        if (dgv.Columns["Id"]      is { } cId)      cId.Visible = false;
        if (dgv.Columns["Company"] is { } cCompany) cCompany.Width = 180;
        if (dgv.Columns["Contact"] is { } cContact) cContact.Width = 150;
        if (dgv.Columns["Phone"]   is { } cPhone)   cPhone.Width   = 120;
        if (dgv.Columns["Email"]   is { } cEmail)   cEmail.Width   = 180;
        if (dgv.Columns["Status"]  is { } cSt)      cSt.Width      = 80;
        if (dgv.Columns["Address"] is { } cAddr)    cAddr.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new SupplierDialog();
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

        using var dlg = new SupplierDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        item.CompanyName   = dlg.Result.CompanyName;
        item.ContactPerson = dlg.Result.ContactPerson;
        item.Phone         = dlg.Result.Phone;
        item.Email         = dlg.Result.Email;
        item.Address       = dlg.Result.Address;
        item.IsActive      = dlg.Result.IsActive;

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

        if (MessageBox.Show($"Delete supplier \"{item.CompanyName}\"?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try { DataStore.Delete(item); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        LoadData();
    }
}

// ── Supplier dialog ───────────────────────────────────────────────────────────
public class SupplierDialog : Form
{
    private readonly TextBox txtCompany, txtContact, txtPhone, txtEmail, txtAddress;
    private readonly CheckBox chkActive;
    public Supplier Result { get; private set; } = new();

    public SupplierDialog(Supplier? existing = null)
    {
        Text = existing is null ? "Add Supplier" : "Edit Supplier";
        Size = new Size(460, 430);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20, 14, 20, 0), AutoScroll = true };

        flow.Controls.Add(UIHelper.CreateFormLabel("Company Name *"));
        txtCompany = UIHelper.CreateTextBox(390);
        flow.Controls.Add(txtCompany);

        flow.Controls.Add(UIHelper.CreateFormLabel("Contact Person"));
        txtContact = UIHelper.CreateTextBox(390);
        flow.Controls.Add(txtContact);

        var pnlRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0) };
        var colPhone = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, Margin = new Padding(0, 0, 14, 0) };
        colPhone.Controls.Add(UIHelper.CreateFormLabel("Phone"));
        txtPhone = UIHelper.CreateTextBox(180);
        colPhone.Controls.Add(txtPhone);
        var colEmail = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown };
        colEmail.Controls.Add(UIHelper.CreateFormLabel("Email"));
        txtEmail = UIHelper.CreateTextBox(196);
        colEmail.Controls.Add(txtEmail);
        pnlRow.Controls.AddRange(new Control[] { colPhone, colEmail });
        flow.Controls.Add(pnlRow);

        flow.Controls.Add(UIHelper.CreateFormLabel("Address"));
        txtAddress = new TextBox { Width = 390, Height = 56, Multiline = true, Font = new Font("Segoe UI", 9.5f), ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 0, 6) };
        flow.Controls.Add(txtAddress);

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
            txtCompany.Text   = existing.CompanyName;
            txtContact.Text   = existing.ContactPerson;
            txtPhone.Text     = existing.Phone;
            txtEmail.Text     = existing.Email;
            txtAddress.Text   = existing.Address;
            chkActive.Checked = existing.IsActive;
            Result.Id         = existing.Id;
        }
    }

    private void BtnSave_Click(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCompany.Text))
        { MessageBox.Show("Company name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); txtCompany.Focus(); return; }

        Result = new Supplier
        {
            Id            = Result.Id,
            CompanyName   = txtCompany.Text.Trim(),
            ContactPerson = txtContact.Text.Trim(),
            Phone         = txtPhone.Text.Trim(),
            Email         = txtEmail.Text.Trim(),
            Address       = txtAddress.Text.Trim(),
            IsActive      = chkActive.Checked
        };
        DialogResult = DialogResult.OK;
    }
}
