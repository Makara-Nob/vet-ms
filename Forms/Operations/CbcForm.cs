using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class CbcForm : Form
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private Button btnPrev = null!, btnNext = null!;
    private Label lblPage = null!, lblStatus = null!, lblNoData = null!;
    private List<CbcRecord> _data = [], _filtered = [];
    private int _currentPage = 1;
    private readonly int _pageSize = 20;

    public CbcForm() { InitializeUI(); LoadData(); }

    private void InitializeUI()
    {
        Text = "CBC Management"; BackColor = UIHelper.LightBg;
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        var grid = new Panel { Dock = DockStyle.Top, Height = 450, BackColor = Color.White };
        dgv = new DataGridView
        {
            Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false, RowHeadersVisible = false,
            BorderStyle = BorderStyle.None, BackgroundColor = Color.White, Cursor = Cursors.Hand
        };
        UIHelper.StyleGrid(dgv);
        dgv.CellPainting   += (_, e) => UIHelper.PaintActionColumn(dgv, e);
        dgv.CellMouseClick += (_, e) => UIHelper.HandleActionColumnClick(dgv, e, EditRow, DeleteRow);
        dgv.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0 && dgv.Columns[e.ColumnIndex].Name != "ColAction") EditRow(e.RowIndex); };
        
        var pag = BuildPaginationBar(); pag.Dock = DockStyle.Bottom;
        lblNoData = UIHelper.CreateEmptyDataLabel("No CBC records yet.");
        grid.Controls.Add(lblNoData); grid.Controls.Add(dgv); grid.Controls.Add(pag);
        lblNoData.BringToFront(); dgv.BringToFront();
        
        content.Controls.Add(grid);
        Controls.Add(content); Controls.Add(BuildStatusBar()); Controls.Add(BuildSearchBar());
        Controls.Add(UIHelper.CreateHeader("CBC Management", "Complete Blood Count lab results tracking"));
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
        txtSearch = new TextBox { Left = 28, Top = 13, Width = 300, Font = new Font("Segoe UI", 11f), PlaceholderText = "Search pet or owner..." };
        txtSearch.TextChanged += (_, _) => FilterData();
        var btnAdd = UIHelper.CreateButton("New CBC Test", UIHelper.Success, 120, 31); btnAdd.Left = txtSearch.Right + 12; btnAdd.Top = 12; btnAdd.Click += BtnAdd_Click;
        p.Controls.AddRange(new Control[] { ico, txtSearch, btnAdd }); return p;
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
        try { _data = DataStore.GetCbcRecords() ?? []; }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); return; }
        FilterData();
    }

    private void FilterData()
    {
        var q = txtSearch.Text.Trim().ToLower();
        _filtered = string.IsNullOrWhiteSpace(q) ? _data
            : _data.Where(x => (x.PetName?.ToLower().Contains(q) == true) || (x.CustomerName?.ToLower().Contains(q) == true)).ToList();
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
            .Select(x => new { 
                x.Id, 
                Date = x.TestDate.ToString("yyyy-MM-dd"), 
                x.PetName, 
                Owner = x.CustomerName, 
                WBC = x.Wbc.ToString("F2"), 
                RBC = x.Rbc.ToString("F2"), 
                HGB = x.Hgb.ToString("F2"), 
                HCT = x.Hct.ToString("F2") + "%",
                PLT = x.Plt.ToString("F0")
            }).ToList();
        
        dgv.DataSource = page;
        if (dgv.Columns["Id"] != null) dgv.Columns["Id"].Visible = false;
        if (dgv.Columns["Date"]    is { } c1) { c1.HeaderText = "Date"; c1.Width = 100; }
        if (dgv.Columns["PetName"] is { } c2) { c2.HeaderText = "Pet"; c2.Width = 140; }
        if (dgv.Columns["Owner"]   is { } c3) { c3.HeaderText = "Owner"; c3.Width = 140; }
        if (dgv.Columns["WBC"]     is { } c4) { c4.HeaderText = "WBC"; c4.Width = 80; }
        if (dgv.Columns["RBC"]     is { } c5) { c5.HeaderText = "RBC"; c5.Width = 80; }
        if (dgv.Columns["HGB"]     is { } c6) { c6.HeaderText = "HGB"; c6.Width = 80; }
        if (dgv.Columns["HCT"]     is { } c7) { c7.HeaderText = "HCT"; c7.Width = 80; }
        if (dgv.Columns["PLT"]     is { } c8) { c8.HeaderText = "PLT"; c8.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; }
        
        if (!dgv.Columns.Contains("ColAction"))
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ColAction", HeaderText = "Action", ReadOnly = true, FillWeight = 20 });
            
        int tp = GetTotalPages();
        lblStatus.Text = $"{_filtered.Count} records"; lblPage.Text = $"Page {_currentPage} / {tp}";
        btnPrev.Enabled = _currentPage > 1; btnNext.Enabled = _currentPage < tp;
        btnPrev.Visible = btnNext.Visible = lblPage.Visible = tp > 1;
    }

    private void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new CbcDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Insert(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("CBC results saved!"); LoadData();
    }

    private void EditRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        using var dlg = new CbcDialog(item);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try { DataStore.Update(dlg.Result); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        VetMS.Forms.Toast.Success("CBC results updated!"); LoadData();
    }

    private void DeleteRow(int row)
    {
        if (dgv.Rows[row].Cells["Id"]?.Value is not int id) return;
        var item = _data.FirstOrDefault(x => x.Id == id); if (item is null) return;
        var res = VetMS.Forms.CustomMessageBox.Show($"Are you sure you want to delete this CBC record for {item.PetName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (res != DialogResult.Yes) return;
        try { DataStore.Delete(item); LoadData(); VetMS.Forms.Toast.Success("Record deleted."); }
        catch (Exception ex) { VetMS.Forms.CustomMessageBox.Show(ex.Message); }
    }
}
