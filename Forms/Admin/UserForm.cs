using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Admin;

public class UserForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Button btnPrev = null!;
    private Button btnNext = null!;
    private Label lblPage = null!;
    private Label lblStatus = null!;
    private Label lblNoData = null!;

    private List<User> _data = [];
    private List<User> _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public UserForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "User Management";
        BackColor = UIHelper.LightBg;

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12)
        };

        var gridContainer = new Panel
        {
            Dock = DockStyle.Top,
            Height = 420,
            BackColor = Color.White
        };

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

        dgv.CellPainting += (_, e) => UIHelper.PaintActionColumn(dgv, e);
        dgv.CellMouseClick += (_, e) => UIHelper.HandleActionColumnClick(dgv, e, EditRow, DeleteRow);
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") EditRow(e.RowIndex); };

        var paginationBar = BuildPaginationBar();
        paginationBar.Dock = DockStyle.Bottom;

        lblNoData = UIHelper.CreateEmptyDataLabel("No users found.");

        gridContainer.Controls.Add(lblNoData);
        gridContainer.Controls.Add(dgv);
        gridContainer.Controls.Add(paginationBar);
        lblNoData.BringToFront();
        dgv.BringToFront();

        contentPanel.Controls.Add(gridContainer);

        Controls.Add(contentPanel);
        Controls.Add(BuildStatusBar());
        Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("User Management", "Manage system accounts, roles, and access."));
    }

    private Panel BuildStatusBar()
    {
        var pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.White };
        lblStatus = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), ForeColor = Color.FromArgb(90, 100, 115), Font = new Font("Segoe UI", 8.5f) };
        var topLine = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 232, 235) };
        pnlStatus.Controls.Add(lblStatus);
        pnlStatus.Controls.Add(topLine);
        return pnlStatus;
    }

    private Panel BuildSearchBar()
    {
        var pnl = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(0, 10, 0, 10) };

        var ico = new Label
        {
            Text = "🔍",
            Width = 24,
            Height = 26,
            Left = 4,
            Top = 13,
            TextAlign = ContentAlignment.MiddleCenter
        };

        txtSearch = new TextBox
        {
            Left = 28,
            Top = 13,
            Width = 300,
            Font = new Font("Segoe UI", 11f),
            PlaceholderText = "Search users..."
        };
        txtSearch.TextChanged += (_, _) => FilterData();

        var btnAdd = UIHelper.CreateButton("Add", UIHelper.Success, 70, 31);
        btnAdd.Left = txtSearch.Right + 12;
        btnAdd.Top = 12;
        btnAdd.Click += BtnAdd_Click;

        var btnReset = UIHelper.CreateButton("Reset", Color.SlateGray, 70, 31);
        btnReset.Left = btnAdd.Right + 8;
        btnReset.Top = 12;
        btnReset.Click += (_, _) => { txtSearch.Clear(); LoadData(); };

        pnl.Controls.AddRange(new Control[] { ico, txtSearch, btnAdd, btnReset });
        return pnl;
    }

    private Panel BuildPaginationBar()
    {
        var pnl = new Panel { Height = 48, BackColor = Color.White };
        var topBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(230, 235, 240) };
        pnl.Controls.Add(topBorder);

        btnPrev = UIHelper.CreateButton("Prev", Color.FromArgb(108, 117, 125), 60, 26);
        btnNext = UIHelper.CreateButton("Next", Color.FromArgb(108, 117, 125), 60, 26);

        lblPage = new Label
        {
            Text = "Page 1 / 1",
            AutoSize = false,
            Width = 100,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(64, 64, 64)
        };

        pnl.Resize += (_, _) =>
        {
            btnNext.Left = pnl.Width - btnNext.Width - 16;
            btnNext.Top = 11;
            lblPage.Left = btnNext.Left - lblPage.Width - 8;
            lblPage.Top = 15;
            btnPrev.Left = lblPage.Left - btnPrev.Width - 8;
            btnPrev.Top = 11;
        };

        btnPrev.Click += (_, _) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
        btnNext.Click += (_, _) => { if (_currentPage < GetTotalPages()) { _currentPage++; RefreshGrid(); } };

        pnl.Controls.AddRange(new Control[] { btnPrev, lblPage, btnNext });
        return pnl;
    }

    private int GetTotalPages() => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));

    private void LoadData()
    {
        try { _data = DataStore.GetUsers() ?? []; }
        catch (Exception ex)
        {
            VetMS.Forms.CustomMessageBox.Show(ex.Message);
            return;
        }

        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();

        _filtered = string.IsNullOrWhiteSpace(q)
            ? _data
            : _data.Where(x =>
                (x.Username?.ToLower().Contains(q) == true) ||
                (x.FullName?.ToLower().Contains(q) == true) ||
                (x.Email?.ToLower().Contains(q) == true) ||
                (x.Role?.ToLower().Contains(q) == true))
            .ToList();

        if (_filtered == null) _filtered = [];

        _currentPage = 1;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        if (_filtered == null || _filtered.Count == 0)
        {
            dgv.DataSource = null;
            lblStatus.Text = "0 records";
            lblPage.Text = "Page 1 / 1";
            btnPrev.Enabled = false;
            btnNext.Enabled = false;
            btnPrev.Visible = btnNext.Visible = lblPage.Visible = false;
            lblNoData.Visible = true;
            dgv.Visible = false;
            return;
        }

        lblNoData.Visible = false;
        dgv.Visible = true;

        int skip = (_currentPage - 1) * _pageSize;

        var pageData = _filtered
            .Skip(skip)
            .Take(_pageSize)
            .Select(x => new
            {
                x.Id,
                x.Username,
                Name = x.FullName,
                x.Email,
                x.Role,
                Status  = x.IsActive ? "Active" : "Inactive"
            })
            .ToList();

        dgv.DataSource = pageData;

        if (dgv.Columns["Id"]       != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Username"] is { } cUsr) cUsr.Width = 140;
        if (dgv.Columns["Name"]     is { } cName) cName.Width = 180;
        if (dgv.Columns["Email"]    is { } cEmail) cEmail.Width = 180;
        if (dgv.Columns["Role"]     is { } cRole) cRole.Width = 120;
        if (dgv.Columns["Status"]   is { } cSt)    cSt.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

        if (!dgv.Columns.Contains("ColAction"))
        {
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColAction",
                HeaderText = "Action",
                ReadOnly = true,
                FillWeight = 20
            });
        }

        int totalPages = GetTotalPages();

        lblStatus.Text = $"{_filtered.Count} records";
        lblPage.Text = $"Page {_currentPage} / {totalPages}";

        btnPrev.Enabled = _currentPage > 1;
        btnNext.Enabled = _currentPage < totalPages;
        btnPrev.Visible = btnNext.Visible = lblPage.Visible = totalPages > 1;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new UserDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("User successfully saved!");
        LoadData();
    }

    private void EditRow(int rowIndex)
    {
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        using var dlg = new UserDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        item.Username = dlg.Result.Username;
        item.FullName = dlg.Result.FullName;
        item.Email    = dlg.Result.Email;
        item.Role     = dlg.Result.Role;
        item.IsActive = dlg.Result.IsActive;
        item.PasswordHash = dlg.Result.PasswordHash;

        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("User successfully updated!");
        LoadData();
    }

    private void DeleteRow(int rowIndex)
    {
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        if (VetMS.Forms.CustomMessageBox.Show($"Delete user \"{item.Username}\"?\nThis action cannot be undone.", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try { DataStore.Delete(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("User successfully deleted!");
        LoadData();
    }
}

// ── User Dialog ───────────────────────────────────────────────────────────────
public class UserDialog : Form
{
    private readonly TextBox txtUsername, txtName, txtEmail, txtPass;
    private readonly ComboBox cbRole;
    private readonly CheckBox chkActive;
    public User Result { get; private set; } = new();
    private bool isEditing;

    public UserDialog(User? existing = null)
    {
        isEditing = existing is not null;
        Text = isEditing ? "Edit User" : "Add Result";
        Size = new Size(500, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20, 14, 20, 0), AutoScroll = true };

        flow.Controls.Add(UIHelper.CreateFormLabel("Username *"));
        txtUsername = UIHelper.CreateTextBox(330);
        flow.Controls.Add(txtUsername);

        flow.Controls.Add(UIHelper.CreateFormLabel("Full Name *"));
        txtName = UIHelper.CreateTextBox(330);
        flow.Controls.Add(txtName);

        flow.Controls.Add(UIHelper.CreateFormLabel("Email"));
        txtEmail = UIHelper.CreateTextBox(330);
        flow.Controls.Add(txtEmail);

        flow.Controls.Add(UIHelper.CreateFormLabel("Role *"));
        cbRole = new ComboBox { Width = 330, Font = new Font("Segoe UI", 9.5f), DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 0, 0, 6) };
        cbRole.Items.AddRange(["Administrator", "Veterinarian", "Staff"]);
        cbRole.SelectedIndex = 2; // Default to staff
        flow.Controls.Add(cbRole);

        flow.Controls.Add(UIHelper.CreateFormLabel(isEditing ? "Reset Password (leave blank to keep current)" : "Password *"));
        txtPass = UIHelper.CreateTextBox(330);
        txtPass.PasswordChar = '•';
        flow.Controls.Add(txtPass);

        chkActive = new CheckBox { Text = "Active Account", Checked = true, Font = new Font("Segoe UI", 9f), Margin = new Padding(0, 10, 0, 0) };
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
            txtUsername.Text  = existing.Username;
            txtName.Text      = existing.FullName;
            txtEmail.Text     = existing.Email;
            cbRole.SelectedItem = existing.Role;
            chkActive.Checked = existing.IsActive;
            Result.Id         = existing.Id;
        }
    }

    private void BtnSave_Click(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtName.Text))
        { VetMS.Forms.CustomMessageBox.Show("Username and Full Name are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        if (!isEditing && string.IsNullOrWhiteSpace(txtPass.Text))
        { VetMS.Forms.CustomMessageBox.Show("Password is required for new users.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        Result = new User
        {
            Id            = Result.Id,
            Username      = txtUsername.Text.Trim(),
            FullName      = txtName.Text.Trim(),
            Email         = txtEmail.Text.Trim(),
            Role          = cbRole.SelectedItem?.ToString() ?? "Staff",
            PasswordHash  = txtPass.Text,
            IsActive      = chkActive.Checked
        };
        DialogResult = DialogResult.OK;
    }
}
