using System.Drawing.Drawing2D;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms.Operations;

public class CbcDialog : Form
{
    private readonly ComboBox cboPet;
    private readonly DateTimePicker dtpTestDate;
    private readonly NumericUpDown nudRbc, nudHgb, nudHct, nudMcv, nudMch, nudMchc;
    private readonly NumericUpDown nudPlt, nudWbc;
    private readonly NumericUpDown nudNeu, nudLym, nudMon, nudEos, nudBas;
    private readonly TextBox txtRemarks;
    private readonly List<Pet> _pets;
    private bool _petFiltering;

    public CbcRecord Result { get; private set; } = new();

    public CbcDialog(CbcRecord? existing = null)
    {
        bool isEdit = existing is not null;
        Text = isEdit ? "Edit CBC Record" : "New CBC Record";
        Size = new Size(850, 900);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        _pets = DataStore.GetPets().Where(p => p.IsActive).ToList();

        // ── Header ────────────────────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = UIHelper.Primary };
        var lblTitle = new Label
        {
            Text = isEdit ? "Edit CBC Record" : "New CBC Record",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White, AutoSize = true
        };
        var lblSub = new Label
        {
            Text = "Complete Blood Count — fill in laboratory values",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(180, 210, 240), AutoSize = true
        };
        header.Controls.AddRange(new Control[] { lblTitle, lblSub });
        header.Resize += (_, _) =>
        {
            lblTitle.Left = 20; lblTitle.Top = 10;
            lblSub.Left = 20; lblSub.Top = lblTitle.Bottom + 2;
        };

        // ── Body ──────────────────────────────────────────────────────────────
        const int lm  = 24;
        const int cw  = 752;   // 800 - 2*24
        const int gap = 12;
        int colW3 = (cw - 2 * gap) / 3;
        int colW2 = (cw - gap) / 2;
        int y = 20;
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };

        // Pet selection
        body.Controls.Add(FieldLabel("Pet *", lm, y)); y += 22;
        cboPet = new ComboBox
        {
            Left = lm, Top = y, Width = cw,
            Font = new Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDown,
            FormattingEnabled = true,
            BackColor = Color.FromArgb(250, 251, 253)
        };
        cboPet.Format += (_, fe) =>
        {
            if (fe.ListItem is Pet p) fe.Value = $"{p.Name}  —  {p.CustomerName}  ·  {p.SpeciesName}";
        };
        foreach (var p in _pets) cboPet.Items.Add(p);
        cboPet.TextChanged += CboPet_TextChanged;
        body.Controls.Add(cboPet); y += 42;

        // Test date
        body.Controls.Add(FieldLabel("Test Date", lm, y)); y += 22;
        dtpTestDate = new DateTimePicker
        {
            Left = lm, Top = y, Width = 220,
            Font = new Font("Segoe UI", 10f),
            Format = DateTimePickerFormat.Short
        };
        body.Controls.Add(dtpTestDate); y += 48;

        // ── Erythrogram ───────────────────────────────────────────────────────
        y = SectionHeader(body, "Erythrogram — Red Blood Cells", lm, cw, y);
        nudRbc  = NudField(body, "RBC (10¹²/L)", lm,                     y, colW3, 15);
        nudHgb  = NudField(body, "HGB (g/dL)",   lm + colW3 + gap,       y, colW3, 25);
        nudHct  = NudField(body, "HCT (%)",       lm + (colW3 + gap) * 2, y, colW3, 70);
        y += 62;
        nudMcv  = NudField(body, "MCV (fL)",      lm,                     y, colW3, 100);
        nudMch  = NudField(body, "MCH (pg)",      lm + colW3 + gap,       y, colW3, 40);
        nudMchc = NudField(body, "MCHC (g/dL)",  lm + (colW3 + gap) * 2, y, colW3, 40);
        y += 70;

        // ── Platelets & WBC ───────────────────────────────────────────────────
        y = SectionHeader(body, "Platelets & WBC Count", lm, cw, y);
        nudPlt = NudField(body, "PLT (10⁹/L)", lm,              y, colW2, 2000);
        nudWbc = NudField(body, "WBC (10⁹/L)", lm + colW2 + gap, y, colW2, 100);
        y += 70;

        // ── WBC Differential ──────────────────────────────────────────────────
        y = SectionHeader(body, "WBC Differential (%)", lm, cw, y);
        nudNeu = NudField(body, "Neutrophils (%)",  lm,                     y, colW3, 100);
        nudLym = NudField(body, "Lymphocytes (%)",  lm + colW3 + gap,       y, colW3, 100);
        nudMon = NudField(body, "Monocytes (%)",    lm + (colW3 + gap) * 2, y, colW3, 100);
        y += 62;
        nudEos = NudField(body, "Eosinophils (%)", lm,                     y, colW3, 100);
        nudBas = NudField(body, "Basophils (%)",   lm + colW3 + gap,       y, colW3, 100);
        y += 70;

        // ── Remarks ───────────────────────────────────────────────────────────
        body.Controls.Add(FieldLabel("Clinical Remarks / Interpretation", lm, y)); y += 22;
        txtRemarks = new TextBox
        {
            Left = lm, Top = y, Width = cw, Height = 28,
            Font = new Font("Segoe UI", 10f),
            Multiline = true, ScrollBars = ScrollBars.None, WordWrap = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 251, 253)
        };
        txtRemarks.TextChanged += (_, _) =>
        {
            int lineH = txtRemarks.Font.Height + 2;
            int lines = txtRemarks.GetLineFromCharIndex(txtRemarks.TextLength) + 1;
            txtRemarks.Height = Math.Max(28, lines * lineH + 8);
        };
        body.Controls.Add(txtRemarks);

        // ── Footer ────────────────────────────────────────────────────────────
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(248, 249, 251) };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 230, 240));
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };
        var btnSave   = DialogButton("Save Results", UIHelper.Success, 130);
        var btnCancel = DialogButton("Cancel", Color.FromArgb(108, 117, 125), 100);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnSave.Click += Save;
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

        if (existing is not null)
        {
            var pet = _pets.FirstOrDefault(p => p.Id == existing.PetId);
            if (pet != null) cboPet.SelectedItem = pet;
            dtpTestDate.Value = existing.TestDate;
            nudRbc.Value  = existing.Rbc;  nudHgb.Value  = existing.Hgb;  nudHct.Value  = existing.Hct;
            nudMcv.Value  = existing.Mcv;  nudMch.Value  = existing.Mch;  nudMchc.Value = existing.Mchc;
            nudPlt.Value  = existing.Plt;  nudWbc.Value  = existing.Wbc;
            nudNeu.Value  = existing.Neu;  nudLym.Value  = existing.Lym;  nudMon.Value  = existing.Mon;
            nudEos.Value  = existing.Eos;  nudBas.Value  = existing.Bas;
            txtRemarks.Text = existing.Remarks;
            Result.Id = existing.Id;
        }
    }

    // ── Pet search ────────────────────────────────────────────────────────────
    private void CboPet_TextChanged(object? sender, EventArgs e)
    {
        if (_petFiltering) return;
        if (cboPet.SelectedItem is Pet) return; // selection already made, don't re-filter

        var q = cboPet.Text.Trim().ToLower();
        var hits = string.IsNullOrEmpty(q)
            ? _pets
            : _pets.Where(p =>
                p.Name.ToLower().Contains(q) ||
                p.CustomerName.ToLower().Contains(q) ||
                (p.SpeciesName?.ToLower().Contains(q) ?? false)).ToList();

        _petFiltering = true;
        var txt = cboPet.Text;
        var sel = cboPet.SelectionStart;
        cboPet.BeginUpdate();
        cboPet.Items.Clear();
        foreach (var p in hits) cboPet.Items.Add(p);
        cboPet.Text = txt;
        cboPet.SelectionStart = sel;
        cboPet.EndUpdate();
        _petFiltering = false;

        if (hits.Count > 0 && !string.IsNullOrEmpty(q))
            cboPet.DroppedDown = true;
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    private void Save(object? s, EventArgs e)
    {
        if (cboPet.SelectedItem is not Pet pet)
        {
            VetMS.Forms.CustomMessageBox.Show("Please select a pet.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Result = new CbcRecord
        {
            Id           = Result.Id,
            PetId        = pet.Id,        PetName      = pet.Name,
            CustomerId   = pet.CustomerId, CustomerName = pet.CustomerName,
            TestDate     = dtpTestDate.Value,
            Rbc = nudRbc.Value, Hgb = nudHgb.Value, Hct = nudHct.Value,
            Mcv = nudMcv.Value, Mch = nudMch.Value, Mchc = nudMchc.Value,
            Plt = nudPlt.Value, Wbc = nudWbc.Value,
            Neu = nudNeu.Value, Lym = nudLym.Value, Mon = nudMon.Value,
            Eos = nudEos.Value, Bas = nudBas.Value,
            Remarks = txtRemarks.Text.Trim()
        };
        DialogResult = DialogResult.OK;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static int SectionHeader(Panel body, string title, int x, int width, int y)
    {
        body.Controls.Add(new Panel
        {
            Left = x, Top = y, Width = width, Height = 1,
            BackColor = Color.FromArgb(220, 225, 235)
        });
        y += 10;
        body.Controls.Add(new Label
        {
            Text = title.ToUpper(),
            Left = x, Top = y, AutoSize = true,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 120, 150)
        });
        return y + 24;
    }

    private static NumericUpDown NudField(Panel body, string label, int x, int y, int width, decimal max)
    {
        body.Controls.Add(new Label
        {
            Text = label, Left = x, Top = y, Width = width,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 75, 95)
        });
        var nud = new NumericUpDown
        {
            Left = x, Top = y + 20, Width = width,
            Font = new Font("Segoe UI", 10f),
            DecimalPlaces = 2, Maximum = max, Minimum = 0,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 251, 253)
        };
        body.Controls.Add(nud);
        return nud;
    }

    private static Label FieldLabel(string text, int x, int y) => new()
    {
        Text = text, Left = x, Top = y, AutoSize = true,
        Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
        ForeColor = Color.FromArgb(60, 75, 95)
    };

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

// ── CBC View Dialog ───────────────────────────────────────────────────────────
public class CbcViewDialog : Form
{
    public CbcViewDialog(CbcRecord record)
    {
        Text = "CBC Record — View";
        Size = new Size(870, 890);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        BackColor = Color.White;

        // ── Header ────────────────────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = UIHelper.Primary };
        var lblTitle = new Label
        {
            Text = "CBC Report",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White, AutoSize = true
        };
        var lblSub = new Label
        {
            Text = $"{record.PetName}  ·  {record.CustomerName}  ·  {record.TestDate:MMM dd, yyyy}",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(180, 210, 240), AutoSize = true
        };
        header.Controls.AddRange(new Control[] { lblTitle, lblSub });
        header.Resize += (_, _) =>
        {
            lblTitle.Left = 20; lblTitle.Top = 10;
            lblSub.Left = 20; lblSub.Top = lblTitle.Bottom + 2;
        };

        // ── Body ──────────────────────────────────────────────────────────────
        const int lm  = 24;
        const int cw  = 782;   // 830 - 2*24
        const int gap = 12;
        int colW3 = (cw - 2 * gap) / 3;
        int colW2 = (cw - gap) / 2;
        int y = 20;
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };

        // Patient info banner
        var banner = new Panel { Left = lm, Top = y, Width = cw, Height = 56, BackColor = Color.FromArgb(244, 247, 255) };
        banner.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(210, 220, 245));
            e.Graphics.DrawRectangle(pen, 0, 0, banner.Width - 1, banner.Height - 1);
            using var accent = new SolidBrush(UIHelper.Accent);
            e.Graphics.FillRectangle(accent, 0, 0, 4, banner.Height);
        };
        InfoItem(banner, "PET",   record.PetName,       14);
        InfoItem(banner, "OWNER", record.CustomerName,  cw / 3 + 14);
        InfoItem(banner, "DATE",  record.TestDate.ToString("MMM dd, yyyy"), cw * 2 / 3 + 14);
        body.Controls.Add(banner); y += 72;

        // ── Erythrogram ───────────────────────────────────────────────────────
        y = SectionHeader(body, "Erythrogram — Red Blood Cells", lm, cw, y);
        ValueCard(body, "RBC",  "10¹²/L", record.Rbc.ToString("F2"),  lm,                     y, colW3);
        ValueCard(body, "HGB",  "g/dL",   record.Hgb.ToString("F2"),  lm + colW3 + gap,       y, colW3);
        ValueCard(body, "HCT",  "%",       record.Hct.ToString("F2"),  lm + (colW3 + gap) * 2, y, colW3);
        y += 74;
        ValueCard(body, "MCV",  "fL",     record.Mcv.ToString("F2"),  lm,                     y, colW3);
        ValueCard(body, "MCH",  "pg",     record.Mch.ToString("F2"),  lm + colW3 + gap,       y, colW3);
        ValueCard(body, "MCHC", "g/dL",   record.Mchc.ToString("F2"), lm + (colW3 + gap) * 2, y, colW3);
        y += 82;

        // ── Platelets & WBC ───────────────────────────────────────────────────
        y = SectionHeader(body, "Platelets & WBC Count", lm, cw, y);
        ValueCard(body, "PLT", "10⁹/L", record.Plt.ToString("F0"),  lm,              y, colW2);
        ValueCard(body, "WBC", "10⁹/L", record.Wbc.ToString("F2"),  lm + colW2 + gap, y, colW2);
        y += 82;

        // ── WBC Differential ──────────────────────────────────────────────────
        y = SectionHeader(body, "WBC Differential (%)", lm, cw, y);
        ValueCard(body, "Neutrophils",  "%", record.Neu.ToString("F2"), lm,                     y, colW3);
        ValueCard(body, "Lymphocytes",  "%", record.Lym.ToString("F2"), lm + colW3 + gap,       y, colW3);
        ValueCard(body, "Monocytes",    "%", record.Mon.ToString("F2"), lm + (colW3 + gap) * 2, y, colW3);
        y += 74;
        ValueCard(body, "Eosinophils",  "%", record.Eos.ToString("F2"), lm,                     y, colW3);
        ValueCard(body, "Basophils",    "%", record.Bas.ToString("F2"), lm + colW3 + gap,       y, colW3);
        y += 82;

        // ── Remarks ───────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(record.Remarks))
        {
            y = SectionHeader(body, "Clinical Remarks / Interpretation", lm, cw, y);
            var remarksBox = new Panel { Left = lm, Top = y, Width = cw, BackColor = Color.FromArgb(248, 249, 252), Cursor = Cursors.Help };
            remarksBox.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(218, 225, 238));
                e.Graphics.DrawRectangle(pen, 0, 0, remarksBox.Width - 1, remarksBox.Height - 1);
            };
            var sz = TextRenderer.MeasureText(record.Remarks, new Font("Segoe UI", 10f),
                new Size(cw - 32, int.MaxValue), TextFormatFlags.WordBreak);
            remarksBox.Height = sz.Height + 28;
            var remarksLbl = new Label
            {
                Text = record.Remarks,
                Left = 14, Top = 12, Width = cw - 32,
                AutoSize = false, Height = sz.Height + 4,
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(40, 50, 70),
                BackColor = Color.Transparent,
                Cursor = Cursors.Help
            };
            remarksBox.Controls.Add(remarksLbl);
            body.Controls.Add(remarksBox);

            var tip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 400,
                ReshowDelay  = 200,
                IsBalloon    = false
            };
            tip.SetToolTip(remarksBox, record.Remarks);
            tip.SetToolTip(remarksLbl,  record.Remarks);
        }

        // ── Footer ────────────────────────────────────────────────────────────
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.FromArgb(248, 249, 251) };
        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(225, 230, 240));
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };
        var btnEdit  = DialogButton("Edit",  UIHelper.Accent, 100);
        var btnClose = DialogButton("Close", Color.FromArgb(108, 117, 125), 100);
        btnEdit.DialogResult  = DialogResult.Yes;   // caller checks this to open Edit
        btnClose.DialogResult = DialogResult.Cancel;
        footer.Controls.AddRange(new Control[] { btnEdit, btnClose });
        footer.Resize += (_, _) =>
        {
            btnClose.Left = footer.Width - btnClose.Width - 20; btnClose.Top = 13;
            btnEdit.Left  = btnClose.Left - btnEdit.Width - 10; btnEdit.Top  = 13;
        };

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);
        CancelButton = btnClose;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static void InfoItem(Panel banner, string label, string value, int x)
    {
        banner.Controls.Add(new Label
        {
            Text = label,
            Left = x, Top = 8, AutoSize = true,
            Font = new Font("Segoe UI", 7f, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 140, 175),
            BackColor = Color.Transparent
        });
        banner.Controls.Add(new Label
        {
            Text = value,
            Left = x, Top = 22, AutoSize = true,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(25, 45, 80),
            BackColor = Color.Transparent
        });
    }

    private static int SectionHeader(Panel body, string title, int x, int width, int y)
    {
        body.Controls.Add(new Panel
        {
            Left = x, Top = y, Width = width, Height = 1,
            BackColor = Color.FromArgb(220, 225, 235)
        });
        y += 10;
        body.Controls.Add(new Label
        {
            Text = title.ToUpper(),
            Left = x, Top = y, AutoSize = true,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 120, 150)
        });
        return y + 24;
    }

    private static void ValueCard(Panel body, string name, string unit, string value, int x, int y, int width)
    {
        var card = new Panel
        {
            Left = x, Top = y, Width = width, Height = 64,
            BackColor = Color.FromArgb(248, 249, 252)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(218, 225, 238));
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        card.Controls.Add(new Label
        {
            Text = unit.Length > 0 ? $"{name}  ({unit})" : name,
            Left = 12, Top = 10, AutoSize = true,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(110, 125, 150),
            BackColor = Color.Transparent
        });
        card.Controls.Add(new Label
        {
            Text = value,
            Left = 12, Top = 28, AutoSize = true,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 35, 60),
            BackColor = Color.Transparent
        });
        body.Controls.Add(card);
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
