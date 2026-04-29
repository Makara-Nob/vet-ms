using System.Drawing.Drawing2D;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Settings;

public class ClinicSettingsForm : Form
{
    private TextBox      txtName        = null!;
    private TextBox      txtNameKh      = null!;
    private TextBox      txtAddressEn   = null!;
    private TextBox      txtAddressKh   = null!;
    private TextBox      txtPhone       = null!;
    private TextBox      txtEmail       = null!;
    private DataGridView dgvSocial      = null!;
    private ClinicSettings _settings    = new();

    public ClinicSettingsForm()
    {
        InitializeUI();
        LoadData();
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    private void InitializeUI()
    {
        Text      = "Clinic Settings";
        BackColor = Color.FromArgb(245, 247, 250);
        Controls.Add(BuildBody());
        Controls.Add(BuildFooter());
        Controls.Add(BuildToolbar());
        Controls.Add(UIHelper.CreateHeader("Clinic Settings", "Manage clinic profile shown on PDF reports"));
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────
    private Panel BuildToolbar()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 228, 235));
            e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        };
        var lbl = new Label
        {
            Text      = "These details appear in the header of every exported PDF report.",
            Font      = new Font("Segoe UI", 9f, FontStyle.Italic),
            ForeColor = Color.FromArgb(120, 135, 160),
            AutoSize  = true, Top = 16, Left = 16
        };
        bar.Controls.Add(lbl);
        return bar;
    }

    // ── Body ──────────────────────────────────────────────────────────────────
    private Panel BuildBody()
    {
        var outer = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250), AutoScroll = true };
        outer.HorizontalScroll.Enabled = false;
        outer.HorizontalScroll.Visible = false;
        outer.AutoScroll = true;   // re-assert after disabling horizontal

        var card = new Panel
        {
            BackColor = Color.White,
            Padding   = new Padding(28, 20, 28, 20)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(220, 223, 230));
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        outer.Resize += (_, _) =>
        {
            card.Left  = 16;
            card.Top   = 16;
            card.Width = Math.Max(300, outer.ClientSize.Width - 32);
            // keep horizontal scroll suppressed
            if (outer.HorizontalScroll.Visible)
            {
                outer.HorizontalScroll.Enabled = false;
                outer.HorizontalScroll.Visible = false;
                outer.AutoScroll = true;
            }
        };

        int y = 20;
        const int lm  = 0;
        const int gap = 16;

        int ColW(int parts, int total, int gaps) => (total - gaps * gap) / parts;

        // ── Section: Clinic Identity ──────────────────────────────────────────
        y = AddSectionLabel(card, "Clinic Identity", lm, y);

        // Name row: English | Khmer (split 50/50)
        card.Controls.Add(FieldLabel("Clinic Name (English) *", lm, y));
        card.Controls.Add(FieldLabel("ឈ្មោះគ្លីនិក (ខ្មែរ)", lm + 2 /* updated on resize */, y));
        y += 22;
        txtName   = TextField(lm, y, 0, 0, false);
        txtNameKh = TextField(0,  y, 0, 0, false);
        txtNameKh.Font = new Font("Khmer UI", 10f);
        card.Controls.Add(txtName);
        card.Controls.Add(txtNameKh);
        y += 44;

        // Address row: English | Khmer  (split 50/50)
        card.Controls.Add(FieldLabel("Address (English)", lm, y));
        card.Controls.Add(FieldLabel("អាសយដ្ឋាន (ខ្មែរ)", lm + 2 /* updated on resize */, y));
        y += 22;
        txtAddressEn = TextField(lm, y, 0, 0, true);
        txtAddressKh = TextField(0,  y, 0, 0, true);
        txtAddressKh.Font = new Font("Khmer UI", 10f);   // Khmer-aware font
        card.Controls.Add(txtAddressEn);
        card.Controls.Add(txtAddressKh);
        y += 84;

        // ── Section: Contact ──────────────────────────────────────────────────
        y = AddSectionLabel(card, "Contact", lm, y);

        card.Controls.Add(FieldLabel("Phone / Mobile", lm, y));
        card.Controls.Add(FieldLabel("Email", 0, y));   // x set on resize
        y += 22;
        txtPhone = TextField(lm, y, 0, 0, false);
        txtEmail = TextField(0,  y, 0, 0, false);
        card.Controls.Add(txtPhone);
        card.Controls.Add(txtEmail);
        y += 44;

        // ── Section: Social Media ─────────────────────────────────────────────
        y = AddSectionLabel(card, "Social Media & Pages", lm, y);

        var note = new Label
        {
            Text      = "Add your Facebook page, Telegram channel, Instagram handle, etc.",
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(130, 145, 165),
            Left = lm, Top = y, AutoSize = true
        };
        card.Controls.Add(note); y += 22;

        // Social grid
        dgvSocial = BuildSocialGrid();
        dgvSocial.Left = lm; dgvSocial.Top = y; dgvSocial.Height = 160;
        card.Controls.Add(dgvSocial); y += 168;

        // Add / Remove buttons
        var btnAdd = SmallBtn("+ Add Row", UIHelper.Success);
        var btnDel = SmallBtn("− Remove", UIHelper.Danger);
        btnAdd.Left = lm; btnAdd.Top = y;
        btnDel.Left = lm + btnAdd.Width + 8; btnDel.Top = y;
        btnAdd.Click += (_, _) =>
        {
            dgvSocial.Rows.Add("Facebook", "");
            int last = dgvSocial.Rows.Count - 1;
            dgvSocial.CurrentCell = dgvSocial.Rows[last].Cells[1];
            dgvSocial.BeginEdit(true);   // jump straight into typing
        };
        btnDel.Click += (_, _) =>
        {
            if (dgvSocial.SelectedRows.Count > 0)
                foreach (DataGridViewRow row in dgvSocial.SelectedRows)
                    if (!row.IsNewRow) dgvSocial.Rows.Remove(row);
        };
        card.Controls.Add(btnAdd);
        card.Controls.Add(btnDel);
        y += 40;

        card.Height = y + 20;

        // ── Responsive layout ─────────────────────────────────────────────────
        card.Resize += (_, _) =>
        {
            int w  = card.ClientSize.Width - lm * 2 - 28 * 2;
            int hw = (w - gap) / 2;

            // Clinic name row
            txtName.Width   = hw; txtName.Left   = lm;
            txtNameKh.Width = hw; txtNameKh.Left = lm + hw + gap;

            // Address row
            txtAddressEn.Width = hw; txtAddressEn.Left = lm;
            txtAddressKh.Width = hw; txtAddressKh.Left = lm + hw + gap;

            // Phone | Email
            txtPhone.Width = hw; txtPhone.Left = lm;
            txtEmail.Width = hw; txtEmail.Left = lm + hw + gap;

            // Reposition right-column labels
            foreach (Control c in card.Controls)
            {
                if (c is Label lb && (lb.Text.StartsWith("ឈ្មោះគ្លីនិក") || lb.Text.StartsWith("អាសយដ្ឋាន") || lb.Text == "Email"))
                    lb.Left = lm + hw + gap;
            }

            dgvSocial.Width = w;
        };

        outer.Controls.Add(card);
        return outer;
    }

    // ── Footer ────────────────────────────────────────────────────────────────
    private Panel BuildFooter()
    {
        var bar = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 230, 240));
            e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
        };

        var btnSave = MakeButton("Save Settings", UIHelper.Success, 140, 34);
        btnSave.Click += (_, _) => Save();

        bar.Controls.Add(btnSave);
        bar.Resize += (_, _) => { btnSave.Left = bar.Width - btnSave.Width - 20; btnSave.Top = 12; };
        return bar;
    }

    // ── Social media DataGridView ─────────────────────────────────────────────
    private static DataGridView BuildSocialGrid()
    {
        var platforms = new DataGridViewComboBoxColumn
        {
            Name        = "Platform",
            HeaderText  = "Platform",
            FillWeight  = 30,
            FlatStyle   = FlatStyle.Flat,
        };
        platforms.Items.AddRange("Facebook", "Telegram", "Instagram", "TikTok", "YouTube", "Twitter/X", "Website", "Other");

        var nameCol = new DataGridViewTextBoxColumn
        {
            Name       = "PageName",
            HeaderText = "Page Name / Handle / URL",
            FillWeight = 70,
        };

        var dgv = new DataGridView
        {
            AutoSizeColumnsMode        = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode              = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect                = true,
            AllowUserToAddRows         = false,
            AllowUserToDeleteRows      = false,
            AllowUserToResizeRows      = false,
            RowHeadersVisible          = false,
            BorderStyle                = BorderStyle.FixedSingle,
            BackgroundColor            = Color.White,
            GridColor                  = Color.FromArgb(225, 230, 240),
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight        = 36
        };
        dgv.EnableHeadersVisualStyles = false;
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 249, 252),
            ForeColor = Color.FromArgb(60, 75, 95),
            Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
            Padding   = new Padding(8, 0, 0, 0),
            SelectionBackColor = Color.FromArgb(248, 249, 252),
            SelectionForeColor = Color.FromArgb(60, 75, 95)
        };
        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            Font     = new Font("Segoe UI", 9.5f),
            Padding  = new Padding(6, 0, 0, 0),
            SelectionBackColor = Color.FromArgb(235, 230, 255),
            SelectionForeColor = Color.FromArgb(30, 40, 60)
        };
        dgv.RowTemplate.Height = 32;
        dgv.Columns.AddRange(platforms, nameCol);
        return dgv;
    }

    // ── Data load / save ──────────────────────────────────────────────────────
    private void LoadData()
    {
        try { _settings = DataStore.GetClinicSettings(); }
        catch { _settings = new ClinicSettings(); }

        txtName.Text      = _settings.Name;
        txtNameKh.Text    = _settings.NameKhmer;
        txtAddressEn.Text = _settings.AddressEnglish;
        txtAddressKh.Text = _settings.AddressKhmer;
        txtPhone.Text     = _settings.Phone;
        txtEmail.Text     = _settings.Email;

        dgvSocial.Rows.Clear();
        foreach (var link in _settings.SocialLinks)
            dgvSocial.Rows.Add(link.Platform, link.Name);
    }

    private void Save()
    {
        // Commit any pending edit in the social grid
        dgvSocial.EndEdit();

        _settings.Name           = txtName.Text.Trim();
        _settings.NameKhmer      = txtNameKh.Text.Trim();
        _settings.AddressEnglish = txtAddressEn.Text.Trim();
        _settings.AddressKhmer   = txtAddressKh.Text.Trim();
        _settings.Phone          = txtPhone.Text.Trim();
        _settings.Email          = txtEmail.Text.Trim();
        _settings.SocialLinks    = [];

        foreach (DataGridViewRow row in dgvSocial.Rows)
        {
            var platform = row.Cells["Platform"].Value?.ToString()?.Trim() ?? "";
            var name     = row.Cells["PageName"].Value?.ToString()?.Trim() ?? "";
            if (!string.IsNullOrEmpty(name))
                _settings.SocialLinks.Add(new SocialLink { Platform = platform, Name = name });
        }

        try
        {
            DataStore.SaveClinicSettings(_settings);
            Toast.Success("Clinic settings saved!");
        }
        catch (Exception ex)
        {
            CustomMessageBox.Show($"Save failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static int AddSectionLabel(Panel parent, string text, int x, int y)
    {
        parent.Controls.Add(new Panel { Left = x, Top = y, Width = 9999, Height = 1, BackColor = Color.FromArgb(220, 225, 235), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top });
        y += 10;
        parent.Controls.Add(new Label
        {
            Text = text.ToUpper(), Left = x, Top = y, AutoSize = true,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 120, 150)
        });
        return y + 24;
    }

    private static Label FieldLabel(string text, int x, int y) => new()
    {
        Text = text, Left = x, Top = y, AutoSize = true,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(60, 75, 95)
    };

    private static TextBox TextField(int x, int y, int width, int maxWidth, bool multiline) => new()
    {
        Left          = x, Top = y, Width = width > 0 ? width : 200,
        Height        = multiline ? 68 : 30,
        Font          = new Font("Segoe UI", 10f),
        Multiline     = multiline, ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        BorderStyle   = BorderStyle.FixedSingle,
        BackColor     = Color.FromArgb(250, 251, 253),
        WordWrap      = true
    };

    private static Button SmallBtn(string text, Color back)
    {
        var btn = new Button
        {
            Text = text, Width = 110, Height = 30,
            BackColor = back, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(back, 0.1f);
        return btn;
    }

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
        using var path = new GraphicsPath();
        path.AddRoundedRectangle(new RectangleF(0, 0, w, h), 6);
        btn.Region = new Region(path);
        btn.SizeChanged += (_, _) =>
        {
            using var p2 = new GraphicsPath();
            p2.AddRoundedRectangle(new RectangleF(0, 0, btn.Width, btn.Height), 6);
            btn.Region = new Region(p2);
        };
        return btn;
    }
}

file static class GraphicsPathExtensions
{
    public static void AddRoundedRectangle(this GraphicsPath path, RectangleF rc, float r)
    {
        float d = r * 2;
        path.AddArc(rc.Left,          rc.Top,           d, d, 180, 90);
        path.AddArc(rc.Right - d,     rc.Top,           d, d, 270, 90);
        path.AddArc(rc.Right - d,     rc.Bottom - d,    d, d,   0, 90);
        path.AddArc(rc.Left,          rc.Bottom - d,    d, d,  90, 90);
        path.CloseFigure();
    }
}
