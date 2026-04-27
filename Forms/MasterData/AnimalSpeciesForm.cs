using System.Drawing.Drawing2D;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.MasterData;

public class AnimalSpeciesForm : Form
{
    private DataGridView dgv        = null!;
    private TextBox      txtSearch  = null!;
    private Label        lblTotal   = null!;
    private Label        lblPageInfo = null!;
    private Label        lblRppCount = null!;
    private Button       btnPrev    = null!;
    private Button       btnNext    = null!;
    private ComboBox     cboPerPage = null!;
    private ComboBox     cboStatus  = null!;
    private Label        lblNoData  = null!;

    private int _currentPage = 1;
    private int _pageSize    = 10;
    private List<AnimalSpecies> _data     = [];
    private List<AnimalSpecies> _filtered = [];

    public AnimalSpeciesForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text      = "Animal Species";
        BackColor = Color.FromArgb(245, 247, 250);

        Controls.Add(BuildGridCard());
        Controls.Add(BuildFooter());
        Controls.Add(BuildToolbar());
        Controls.Add(UIHelper.CreateHeader("Animal Species", "Manage animal species / types"));
    }

    // ── Toolbar ──────────────────────────────────────────────────────────────
    private Panel BuildToolbar()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Top, Height = 60,
            BackColor = Color.White,
            Padding   = new Padding(16, 12, 16, 12)
        };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 228, 235));
            e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        };

        // Search box — left=16 to align with table
        var searchPanel = new Panel { Top = 12, Left = 16, Width = 240, Height = 36, BackColor = Color.White };
        searchPanel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(Color.FromArgb(210, 215, 225));
            using var path = RoundRect(new Rectangle(0, 0, searchPanel.Width - 1, searchPanel.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
        };
        var icoSearch = new Label
        {
            Text = "🔍", Width = 28, Left = 6, Top = 5,
            TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent
        };
        txtSearch = new TextBox
        {
            Left = 32, Top = 7, Width = 198,
            Font = new Font("Segoe UI", 10f), BorderStyle = BorderStyle.None,
            BackColor = Color.White, PlaceholderText = "Search species..."
        };
        txtSearch.TextChanged += (_, _) => FilterData();
        searchPanel.Controls.AddRange(new Control[] { icoSearch, txtSearch });

        // Status filter dropdown
        cboStatus = new ComboBox
        {
            Top = 14, Left = searchPanel.Right + 8, Width = 110,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5f)
        };
        cboStatus.Items.AddRange(new object[] { "Active", "Inactive", "All" });
        cboStatus.SelectedIndex = 0;
        cboStatus.SelectedIndexChanged += (_, _) => FilterData();

        // Add button — right next to status filter
        var btnAdd = MakeButton("+ Add", UIHelper.Success, 90, 36);
        btnAdd.Top  = 12;
        btnAdd.Left = cboStatus.Right + 8;
        btnAdd.Click += BtnAdd_Click;

        bar.Controls.Add(searchPanel);
        bar.Controls.Add(cboStatus);
        bar.Controls.Add(btnAdd);
        return bar;
    }

    // ── Grid card ─────────────────────────────────────────────────────────────
    private Panel BuildGridCard()
    {
        var outer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding   = new Padding(16, 12, 16, 12)
        };

        // Bordered table container
        var tableBox = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };
        tableBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(220, 223, 230));
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, tableBox.Width - 1, tableBox.Height - 1));
        };

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            Margin               = new Padding(1),
            BorderStyle          = BorderStyle.None,
            BackgroundColor      = Color.White,
            AutoSizeColumnsMode  = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode        = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect          = false,
            ReadOnly             = true,
            RowHeadersVisible    = false,
            AllowUserToAddRows    = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
            Cursor          = Cursors.Hand,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor       = Color.FromArgb(235, 238, 242)
        };

        dgv.ColumnHeadersHeight = 42;
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(60, 70, 90),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0),
            SelectionBackColor = Color.White,
            SelectionForeColor = Color.FromArgb(60, 70, 90)
        };
        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(30, 40, 60),
            BackColor = Color.White,
            SelectionBackColor = Color.FromArgb(235, 230, 255),
            SelectionForeColor = Color.FromArgb(30, 40, 60),
            Padding   = new Padding(8, 0, 0, 0)
        };
        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            SelectionBackColor = Color.FromArgb(235, 230, 255),
            SelectionForeColor = Color.FromArgb(30, 40, 60)
        };
        dgv.RowTemplate.Height = 40;

        dgv.CellPainting    += DgvCellPainting;
        dgv.CellMouseClick  += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Button != MouseButtons.Left) return;
            if (dgv.Columns[e.ColumnIndex].Name != "ColAction") return;
            bool isActive = dgv.Rows[e.RowIndex].Cells["Status"]?.Value?.ToString() == "Active";
            if (isActive)
                UIHelper.HandleDynamicActionColumnClick(dgv, e, ("Edit", EditRow), ("Delete", DeleteRow));
            else
                UIHelper.HandleDynamicActionColumnClick(dgv, e, ("Edit", EditRow), ("Recover", RecoverRow));
        };
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") EditRow(e.RowIndex); };

        lblNoData = UIHelper.CreateEmptyDataLabel("No species found.");

        tableBox.Controls.Add(lblNoData);
        tableBox.Controls.Add(dgv);
        lblNoData.BringToFront();

        outer.Controls.Add(tableBox);
        return outer;
    }

    // ── Footer ────────────────────────────────────────────────────────────────
    private Panel BuildFooter()
    {
        var bar = new Panel
        {
            Dock = DockStyle.Bottom, Height = 52,
            BackColor = Color.White
        };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 228, 235));
            e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
        };

        lblTotal = new Label
        {
            Text = "Total records: 0",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = UIHelper.Success,
            AutoSize = true, Top = 16
        };

        var lblShow = new Label
        {
            Text = "Show", Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(80, 90, 110),
            AutoSize = true, Top = 18
        };

        cboPerPage = new ComboBox
        {
            Width = 65, Top = 13, Height = 28,
            Font = new Font("Segoe UI", 9f),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboPerPage.Items.AddRange(new object[] { 10, 25, 50, 100 });
        cboPerPage.SelectedIndex = 0;
        cboPerPage.SelectedIndexChanged += (_, _) =>
        {
            _pageSize = (int)cboPerPage.SelectedItem!;
            _currentPage = 1;
            RefreshGrid();
        };

        lblRppCount = new Label
        {
            Text = "(0) Records per page",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(80, 90, 110),
            AutoSize = true, Top = 18
        };

        lblPageInfo = new Label
        {
            Text = "Showing page 1 of 1 Pages",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(80, 90, 110),
            AutoSize = true, Top = 18
        };

        btnPrev = MakeButton("Prev", UIHelper.Accent,  70, 32);
        btnNext = MakeButton("Next", UIHelper.Success,  70, 32);
        btnPrev.Top = btnNext.Top = 10;

        btnPrev.Click += (_, _) => { if (_currentPage > 1) { _currentPage--; RefreshGrid(); } };
        btnNext.Click += (_, _) => { if (_currentPage < GetTotalPages()) { _currentPage++; RefreshGrid(); } };

        bar.Controls.AddRange(new Control[] { lblTotal, lblShow, cboPerPage, lblRppCount, lblPageInfo, btnPrev, btnNext });

        bar.Resize += (_, _) =>
        {
            lblTotal.Left = 16;

            int groupW = lblShow.Width + 4 + cboPerPage.Width + 4 + lblRppCount.Width;
            int cx = (bar.Width - groupW) / 2;
            lblShow.Left     = cx;
            cboPerPage.Left  = lblShow.Right + 4;
            lblRppCount.Left = cboPerPage.Right + 4;

            btnNext.Left     = bar.Width - btnNext.Width - 16;
            btnPrev.Left     = btnNext.Left - btnPrev.Width - 8;
            lblPageInfo.Left = btnPrev.Left - lblPageInfo.Width - 16;
            lblPageInfo.Top  = 18;
        };

        return bar;
    }

    // ── Cell painting ─────────────────────────────────────────────────────────
    private void DgvCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        // Accent underline on header row
        if (e.RowIndex == -1)
        {
            e.PaintBackground(e.CellBounds, false);
            e.PaintContent(e.CellBounds);
            using var pen = new Pen(UIHelper.Accent, 2f);
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 2,
                                     e.CellBounds.Right, e.CellBounds.Bottom - 2);
            e.Handled = true;
            return;
        }

        // Status badge
        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "Status")
        {
            e.PaintBackground(e.CellBounds, true);
            string val    = e.Value?.ToString() ?? "";
            bool   active = val == "Active";
            var fg = active ? Color.FromArgb(0, 150, 80) : Color.FromArgb(140, 148, 160);
            var bg = active ? Color.FromArgb(20, 0, 180, 80) : Color.FromArgb(20, 140, 148, 160);
            var badgeRect = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 10, 64, e.CellBounds.Height - 20);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var bgBrush = new SolidBrush(bg);
            using var fgBrush = new SolidBrush(fg);
            using var path    = RoundRect(badgeRect, 10);
            e.Graphics.FillPath(bgBrush, path);
            using var sf   = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            e.Graphics.DrawString(val, font, fgBrush, badgeRect, sf);
            e.Handled = true;
            return;
        }

        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "ColAction")
        {
            bool isActive = dgv.Rows[e.RowIndex].Cells["Status"]?.Value?.ToString() == "Active";
            if (isActive)
                UIHelper.PaintDynamicActionColumn(dgv, e, "Edit", "Delete");
            else
                UIHelper.PaintDynamicActionColumn(dgv, e, "Edit", "Recover");
            return;
        }
    }

    // ── Data ──────────────────────────────────────────────────────────────────
    private void LoadData()
    {
        try { _data = DataStore.GetAnimalSpecies() ?? []; }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        var statusIndex = cboStatus?.SelectedIndex ?? 0; // 0=Active, 1=Inactive, 2=All

        _filtered = _data.Where(x =>
        {
            bool statusMatch = statusIndex == 2 || (statusIndex == 0 ? x.IsActive : !x.IsActive);
            bool searchMatch = string.IsNullOrWhiteSpace(q) ||
                x.Name?.ToLower().Contains(q) == true ||
                x.Description?.ToLower().Contains(q) == true;
            return statusMatch && searchMatch;
        }).ToList();
        _currentPage = 1;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        int total = _filtered.Count;
        bool empty = total == 0;
        lblNoData.Visible = empty;
        dgv.Visible       = !empty;

        lblTotal.Text = $"Total records: {total}";

        int totalPages = GetTotalPages();
        btnPrev.Enabled = _currentPage > 1;
        btnNext.Enabled = _currentPage < totalPages;

        if (empty)
        {
            lblPageInfo.Text  = "Showing page 1 of 1 Pages";
            lblRppCount.Text  = "(0) Records per page";
            return;
        }

        int skip = (_currentPage - 1) * _pageSize;
        var page = _filtered.Skip(skip).Take(_pageSize)
            .Select((x, i) => new
            {
                No          = skip + i + 1,
                x.Id,
                x.Name,
                x.Description,
                Status      = x.IsActive ? "Active" : "Inactive",
                CreatedAt   = x.CreatedAt.ToString("MMM dd, yyyy"),
                UpdatedAt   = x.UpdatedAt?.ToString("MMM dd, yyyy") ?? "—"
            }).ToList();

        dgv.DataSource = page;

        if (dgv.Columns["Id"]          is { } cId)  cId.Visible     = false;
        if (dgv.Columns["No"]          is { } cNo)  { cNo.HeaderText = "#";            cNo.FillWeight = 6; }
        if (dgv.Columns["Name"]        is { } cNm)  { cNm.HeaderText = "Species Name"; cNm.FillWeight = 22; }
        if (dgv.Columns["Description"] is { } cDs)  { cDs.HeaderText = "Description";  cDs.FillWeight = 28; }
        if (dgv.Columns["Status"]      is { } cSt)  { cSt.HeaderText = "Status";       cSt.FillWeight = 12; }
        if (dgv.Columns["CreatedAt"]   is { } cCr)  { cCr.HeaderText = "Created At";   cCr.FillWeight = 16; }
        if (dgv.Columns["UpdatedAt"]   is { } cUp)  { cUp.HeaderText = "Updated At";   cUp.FillWeight = 16; }

        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColAction", HeaderText = "Action",
                ReadOnly = true, FillWeight = 14
            });

        lblPageInfo.Text = $"Showing page {_currentPage} of {totalPages} Pages";
        lblRppCount.Text = $"({Math.Min(_pageSize, total - skip)}) Records per page";

        // Re-trigger resize to reflow footer labels
        BuildFooterLayout();
    }

    private void BuildFooterLayout()
    {
        if (lblTotal.Parent is not Panel bar) return;
        int groupW = 0;
        // measure show label width
        using var g = bar.CreateGraphics();
        int showW   = (int)g.MeasureString("Show", lblTotal.Font).Width;
        int rppW    = (int)g.MeasureString(lblRppCount.Text, lblRppCount.Font).Width + 10;
        groupW = showW + 4 + cboPerPage.Width + 4 + rppW;
        int cx = (bar.Width - groupW) / 2;
        var lblShow = bar.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Show");
        if (lblShow != null) { lblShow.Left = cx; cboPerPage.Left = lblShow.Right + 4; lblRppCount.Left = cboPerPage.Right + 4; }
        btnNext.Left     = bar.Width - btnNext.Width - 16;
        btnPrev.Left     = btnNext.Left - btnPrev.Width - 8;
        lblPageInfo.Left = btnPrev.Left - lblPageInfo.Width - 16;
    }

    private int GetTotalPages() => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));

    // ── Actions ───────────────────────────────────────────────────────────────
    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new AnimalSpeciesDialog();
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Species saved!");
        LoadData();
    }

    private void EditRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        int id = (int)dgv.Rows[rowIndex].Cells["Id"].Value;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null) return;
        using var dlg = new AnimalSpeciesDialog(item);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        item.Name = dlg.Result.Name; item.Description = dlg.Result.Description; item.IsActive = dlg.Result.IsActive;
        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Species updated!");
        LoadData();
    }

    private void DeleteRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        int id = (int)dgv.Rows[rowIndex].Cells["Id"].Value;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null) return;
        if (VetMS.Forms.CustomMessageBox.Show($"Deactivate {item.Name}?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        item.IsActive = false;
        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Species deactivated!");
        LoadData();
    }

    private void RecoverRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        int id = (int)dgv.Rows[rowIndex].Cells["Id"].Value;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item == null) return;
        item.IsActive = true;
        try { DataStore.Update(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Species recovered!");
        LoadData();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Button MakeButton(string text, Color back, int w, int h)
    {
        var btn = new Button
        {
            Text = text, Width = w, Height = h,
            BackColor = back, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(back, 0.1f);
        ApplyRound(btn, 6);
        btn.SizeChanged += (_, _) => ApplyRound(btn, 6);
        return btn;
    }

    private static void ApplyRound(Control c, int r)
    {
        if (c.Width <= 0 || c.Height <= 0) return;
        c.Region = new Region(RoundRect(new Rectangle(0, 0, c.Width, c.Height), r));
    }

    private static GraphicsPath RoundRect(Rectangle rc, int r)
    {
        int d = r * 2;
        var p = new GraphicsPath();
        p.AddArc(rc.Left,      rc.Top,        d, d, 180, 90);
        p.AddArc(rc.Right - d, rc.Top,        d, d, 270, 90);
        p.AddArc(rc.Right - d, rc.Bottom - d, d, d,   0, 90);
        p.AddArc(rc.Left,      rc.Bottom - d, d, d,  90, 90);
        p.CloseFigure();
        return p;
    }
}

// ── Add / Edit Dialog ─────────────────────────────────────────────────────────
public class AnimalSpeciesDialog : Form
{
    private readonly TextBox   txtName;
    private readonly TextBox   txtDesc;
    private readonly CheckBox? chkActive;

    public AnimalSpecies Result { get; private set; } = new();

    public AnimalSpeciesDialog(AnimalSpecies? existing = null)
    {
        bool isEdit   = existing != null;
        Text          = isEdit ? "Edit Species" : "Add Species";
        Size          = new Size(500, isEdit ? 384 : 370);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor   = Color.White;

        // ── Dialog header strip ───────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = UIHelper.Primary };
        var lblTitle = new Label
        {
            Text = isEdit ? "Edit Species" : "Add New Species",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White, AutoSize = true
        };
        var lblSub = new Label
        {
            Text = isEdit ? "Update species information" : "Fill in the details below",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(180, 210, 240), AutoSize = true
        };
        header.Controls.AddRange(new Control[] { lblTitle, lblSub });
        header.Resize += (_, _) =>
        {
            lblTitle.Left = 20; lblTitle.Top = 10;
            lblSub.Left   = 20; lblSub.Top   = lblTitle.Bottom + 2;
        };

        // ── Body ──────────────────────────────────────────────────────────────
        const int lm = 24; // left margin
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        int y = 20;

        body.Controls.Add(FieldLabel("Species Name *", lm, y));  y += 22;
        txtName = StyledTextBox(lm, y); body.Controls.Add(txtName); y += 42;

        body.Controls.Add(FieldLabel("Description", lm, y));     y += 22;
        txtDesc = StyledTextBox(lm, y, multiline: true); body.Controls.Add(txtDesc); y += 90;

        if (!isEdit)
        {
            chkActive = new CheckBox
            {
                Text = "Active", Checked = true, Left = lm, Top = y,
                Font = new Font("Segoe UI", 9.5f), AutoSize = true
            };
            body.Controls.Add(chkActive);
        }

        if (isEdit)
        {
            y += 36;
            var sep = new Panel { Left = lm, Top = y, Width = 430, Height = 1, BackColor = Color.FromArgb(225, 230, 240) };
            body.Controls.Add(sep); y += 14;

            body.Controls.Add(TsLabel("Created At",  existing!.CreatedAt.ToString("MMM dd, yyyy  HH:mm"),  lm,       y));
            body.Controls.Add(TsLabel("Updated At",  existing.UpdatedAt?.ToString("MMM dd, yyyy  HH:mm") ?? "—", lm + 220, y));
        }

        // ── Footer ────────────────────────────────────────────────────────────
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(248, 249, 251) };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 230, 240));
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };
        var btnSave   = DialogButton("Save",   UIHelper.Success, 100);
        var btnCancel = DialogButton("Cancel", Color.FromArgb(108, 117, 125), 100);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnSave.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) { VetMS.Forms.CustomMessageBox.Show("Species name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            Result = new AnimalSpecies { Id = existing?.Id ?? 0, Name = txtName.Text.Trim(), Description = txtDesc.Text.Trim(), IsActive = chkActive?.Checked ?? existing?.IsActive ?? true };
            DialogResult = DialogResult.OK;
        };
        footer.Controls.AddRange(new Control[] { btnSave, btnCancel });
        footer.Resize += (_, _) =>
        {
            btnCancel.Left = footer.Width - btnCancel.Width - 20; btnCancel.Top = 13;
            btnSave.Left   = btnCancel.Left - btnSave.Width - 10; btnSave.Top   = 13;
        };

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        AcceptButton = btnSave; CancelButton = btnCancel;

        if (isEdit) { txtName.Text = existing!.Name; txtDesc.Text = existing.Description; }
    }

    private static Label FieldLabel(string text, int x, int y) => new()
    {
        Text = text, Left = x, Top = y, AutoSize = true,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(60, 75, 95)
    };

    private static TextBox StyledTextBox(int x, int y, bool multiline = false) => new()
    {
        Left = x, Top = y, Width = 432,
        Height = multiline ? 72 : 28,
        Font = new Font("Segoe UI", 10f),
        Multiline = multiline,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.FromArgb(250, 251, 253)
    };

    private static Panel TsLabel(string lbl, string val, int x, int y)
    {
        var p = new Panel { Left = x, Top = y, Width = 210, Height = 40, BackColor = Color.FromArgb(248, 249, 251) };
        p.Controls.Add(new Label { Text = lbl, Left = 0, Top = 0, AutoSize = true, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(130, 140, 155) });
        p.Controls.Add(new Label { Text = val, Left = 0, Top = 16, AutoSize = true, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(40, 50, 70) });
        return p;
    }

    private static Button DialogButton(string text, Color back, int w)
    {
        var btn = new Button
        {
            Text = text, Width = w, Height = 32,
            BackColor = back, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(back, 0.1f);
        return btn;
    }
}
