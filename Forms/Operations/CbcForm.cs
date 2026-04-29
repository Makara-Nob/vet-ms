using System.Drawing.Drawing2D;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class CbcForm : Form
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
    private List<CbcRecord> _data = [];
    private List<CbcRecord> _filtered = [];

    public CbcForm()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        Text = "CBC Management";
        BackColor = Color.FromArgb(245, 247, 250);

        Controls.Add(BuildGridCard());
        Controls.Add(BuildFooter());
        Controls.Add(BuildToolbar());
        Controls.Add(UIHelper.CreateHeader("CBC Management", "Complete Blood Count lab results tracking"));
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

        var searchPanel = new Panel { Top = 12, Left = 16, Width = 240, Height = 36, BackColor = Color.White };
        searchPanel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(210, 215, 225));
            using var path = RoundRect(new Rectangle(0, 0, searchPanel.Width - 1, searchPanel.Height - 1), 6);
            e.Graphics.DrawPath(pen, path);
        };
        var icoSearch = new Label
        {
            Text = "🔍",
            Width = 28,
            Left = 6,
            Top = 5,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        txtSearch = new TextBox
        {
            Left = 32,
            Top = 7,
            Width = 198,
            Font = new Font("Segoe UI", 10f),
            BorderStyle = BorderStyle.None,
            BackColor = Color.White,
            PlaceholderText = "Search pet or owner..."
        };
        txtSearch.TextChanged += (_, _) => FilterData();
        searchPanel.Controls.AddRange(new Control[] { icoSearch, txtSearch });

        var btnAdd = MakeButton("+ New CBC Test", UIHelper.Success, 130, 36);
        btnAdd.Top = 12;
        btnAdd.Left = searchPanel.Right + 8;
        btnAdd.Click += BtnAdd_Click;

        bar.Controls.Add(searchPanel);
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
            Padding = new Padding(16, 12, 16, 12)
        };

        var tableBox = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
        tableBox.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(220, 223, 230));
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, tableBox.Width - 1, tableBox.Height - 1));
        };

        dgv = new DataGridView
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(1),
            BorderStyle = BorderStyle.None,
            BackgroundColor = Color.White,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing,
            Cursor = Cursors.Hand,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(235, 238, 242)
        };

        dgv.ColumnHeadersHeight = 42;
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            ForeColor = Color.FromArgb(60, 70, 90),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            SelectionBackColor = Color.White,
            SelectionForeColor = Color.FromArgb(60, 70, 90)
        };
        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(30, 40, 60),
            BackColor = Color.White,
            SelectionBackColor = Color.FromArgb(235, 230, 255),
            SelectionForeColor = Color.FromArgb(30, 40, 60),
            Padding = new Padding(8, 0, 0, 0)
        };
        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.White,
            SelectionBackColor = Color.FromArgb(235, 230, 255),
            SelectionForeColor = Color.FromArgb(30, 40, 60)
        };
        dgv.RowTemplate.Height = 40;

        dgv.CellPainting += DgvCellPainting;
        dgv.CellMouseClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Button != MouseButtons.Left) return;
            if (dgv.Columns[e.ColumnIndex].Name != "ColAction") return;
            UIHelper.HandleDynamicActionColumnClick(dgv, e, ("View", ViewRow), ("Edit", EditRow), ("Export", ExportRow), ("Delete", DeleteRow));
        };
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") ViewRow(e.RowIndex); };
        dgv.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "ColAction")
                e.ToolTipText = "View | Edit | Export PDF | Delete";
        };

        lblNoData = UIHelper.CreateEmptyDataLabel("No CBC records yet.");
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

        lblTotal = new Label
        {
            Text = "Total records: 0",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = UIHelper.Success,
            AutoSize = true,
            Top = 16
        };
        var lblShow = new Label
        {
            Text = "Show",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(80, 90, 110),
            AutoSize = true,
            Top = 18
        };
        cboPerPage = new ComboBox
        {
            Width = 65,
            Top = 13,
            Font = new Font("Segoe UI", 9f),
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboPerPage.Items.AddRange(new object[] { 10, 25, 50, 100 });
        cboPerPage.SelectedIndex = 0;
        cboPerPage.SelectedIndexChanged += (_, _) => { _pageSize = (int)cboPerPage.SelectedItem!; _currentPage = 1; RefreshGrid(); };

        lblRppCount = new Label
        {
            Text = "(0) Records per page",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(80, 90, 110),
            AutoSize = true,
            Top = 18
        };
        lblPageInfo = new Label
        {
            Text = "Showing page 1 of 1 Pages",
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(80, 90, 110),
            AutoSize = true,
            Top = 18
        };

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
            lblShow.Left = cx;
            cboPerPage.Left = lblShow.Right + 4;
            lblRppCount.Left = cboPerPage.Right + 4;
            btnNext.Left = bar.Width - btnNext.Width - 16;
            btnPrev.Left = btnNext.Left - btnPrev.Width - 8;
            lblPageInfo.Left = btnPrev.Left - lblPageInfo.Width - 16;
            lblPageInfo.Top = 18;
        };
        return bar;
    }

    // ── Cell painting ─────────────────────────────────────────────────────────
    private void DgvCellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex == -1)
        {
            e.PaintBackground(e.CellBounds, false);
            e.PaintContent(e.CellBounds);
            using var pen = new Pen(UIHelper.Accent, 2f);
            e.Graphics.DrawLine(pen, e.CellBounds.Left, e.CellBounds.Bottom - 2, e.CellBounds.Right, e.CellBounds.Bottom - 2);
            e.Handled = true;
            return;
        }

        if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgv.Columns[e.ColumnIndex].Name == "ColAction")
        {
            UIHelper.PaintDynamicActionColumn(dgv, e, "View", "Edit", "Export", "Delete");
            return;
        }
    }

    // ── Data ──────────────────────────────────────────────────────────────────
    private void LoadData()
    {
        try { _data = DataStore.GetCbcRecords() ?? []; }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        _filtered = string.IsNullOrWhiteSpace(q) ? _data
            : _data.Where(x =>
                x.PetName?.ToLower().Contains(q) == true ||
                x.CustomerName?.ToLower().Contains(q) == true).ToList();
        _currentPage = 1;
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        int total = _filtered.Count;
        bool empty = total == 0;
        lblNoData.Visible = empty;
        dgv.Visible = !empty;

        lblTotal.Text = $"Total records: {total}";
        int totalPages = GetTotalPages();
        btnPrev.Enabled = _currentPage > 1;
        btnNext.Enabled = _currentPage < totalPages;

        if (empty) { lblPageInfo.Text = "Showing page 1 of 1 Pages"; lblRppCount.Text = "(0) Records per page"; return; }

        int skip = (_currentPage - 1) * _pageSize;
        var page = _filtered.Skip(skip).Take(_pageSize)
            .Select((x, i) => new
            {
                No = skip + i + 1,
                x.Id,
                Date = x.TestDate.ToString("yyyy-MM-dd"),
                Pet = x.PetName,
                Owner = x.CustomerName,
                WBC = x.Wbc.ToString("F2"),
                RBC = x.Rbc.ToString("F2"),
                HGB = x.Hgb.ToString("F2"),
                HCT = x.Hct.ToString("F2") + "%",
                PLT = x.Plt.ToString("F0")
            }).ToList();

        dgv.DataSource = page;

        if (dgv.Columns["Id"] is { } cId) cId.Visible = false;
        if (dgv.Columns["No"] is { } cNo) { cNo.HeaderText = "#"; cNo.FillWeight = 5; }
        if (dgv.Columns["Date"] is { } cDate) { cDate.HeaderText = "Date"; cDate.FillWeight = 13; }
        if (dgv.Columns["Pet"] is { } cPet) { cPet.HeaderText = "Pet"; cPet.FillWeight = 16; }
        if (dgv.Columns["Owner"] is { } cOwn) { cOwn.HeaderText = "Owner"; cOwn.FillWeight = 16; }
        if (dgv.Columns["WBC"] is { } cWbc) { cWbc.HeaderText = "WBC"; cWbc.FillWeight = 10; }
        if (dgv.Columns["RBC"] is { } cRbc) { cRbc.HeaderText = "RBC"; cRbc.FillWeight = 10; }
        if (dgv.Columns["HGB"] is { } cHgb) { cHgb.HeaderText = "HGB"; cHgb.FillWeight = 10; }
        if (dgv.Columns["HCT"] is { } cHct) { cHct.HeaderText = "HCT"; cHct.FillWeight = 10; }
        if (dgv.Columns["PLT"] is { } cPlt) { cPlt.HeaderText = "PLT"; cPlt.FillWeight = 10; }

        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "Action", ReadOnly = true, FillWeight = 16 });

        lblPageInfo.Text = $"Showing page {_currentPage} of {totalPages} Pages";
        lblRppCount.Text = $"({Math.Min(_pageSize, total - skip)}) Records per page";
    }

    private int GetTotalPages() => Math.Max(1, (int)Math.Ceiling(_filtered.Count / (double)_pageSize));

    // ── Actions ───────────────────────────────────────────────────────────────
    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new CbcDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("CBC results saved!");
        LoadData();
    }

    private void ViewRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;

        var pet = DataStore.GetPets().FirstOrDefault(p => p.Id == item.PetId);
        if (pet != null)
        {
            // Load PetDetailsForm on tab 2 (CBC Results)
            MainForm.Instance.LoadForm(new PetDetailsForm(pet, () => MainForm.Instance.LoadForm(new CbcForm()), 2));
        }
        else
        {
            // Fallback to dialog if pet not found
            using var dlg = new CbcViewDialog(item);
            if (dlg.ShowDialog(this) == DialogResult.Yes)
                EditRow(rowIndex);
        }
    }

    private void EditRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;
        using var dlg = new CbcDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Update(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("CBC results updated!");
        LoadData();
    }

    private void DeleteRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;
        if (VetMS.Forms.CustomMessageBox.Show($"Delete CBC record for {item.PetName}?", "Confirm Delete",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { DataStore.Delete(item); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("Record deleted.");
        LoadData();
    }

    private void ExportRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return;
        if (dgv.Rows[rowIndex].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id);
        if (item is null) return;
        VetMS.Helpers.CbcPdfExporter.ShowExportDialog(item);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Button MakeButton(string text, Color back, int w, int h)
    {
        var btn = new Button
        {
            Text = text,
            Width = w,
            Height = h,
            BackColor = back,   
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
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
        p.AddArc(rc.Left, rc.Top, d, d, 180, 90);
        p.AddArc(rc.Right - d, rc.Top, d, d, 270, 90);
        p.AddArc(rc.Right - d, rc.Bottom - d, d, d, 0, 90);
        p.AddArc(rc.Left, rc.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}