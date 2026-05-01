using VetMS.Data;
using VetMS.Models;
using System.Drawing.Drawing2D;

namespace VetMS.Forms.Operations;

public class MedicalRecordDetailForm : Form
{
    private MedicalRecord _record;
    private Action _onBack;

    public MedicalRecordDetailForm(MedicalRecord record, Action onBack)
    {
        _record = record;
        _onBack = onBack;
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text = $"Medical Record — {_record.PetName}";
        BackColor = Color.FromArgb(245, 248, 252);
        Controls.Add(BuildScrollContent());
        Controls.Add(BuildHero());
        Controls.Add(BuildTopBar());
    }

    // ── Top bar ───────────────────────────────────────────────────────────────
    private Panel BuildTopBar()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(228, 232, 240));
            e.Graphics.DrawLine(pen, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        };

        var btnBack = new Button
        {
            Text = "← Back to Records",
            Font = new Font("Segoe UI", 9f), ForeColor = Theme.AppTheme.Primary,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            AutoSize = true, Location = new Point(20, 14), BackColor = Color.Transparent
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.FlatAppearance.MouseOverBackColor = Color.Transparent;
        btnBack.Click += (_, _) => _onBack?.Invoke();
        bar.Controls.Add(btnBack);

        var btnEdit = UIHelper.CreateButton("✎  Edit Record", Theme.AppTheme.Primary, 132, 34);
        btnEdit.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        btnEdit.Click += OnEditClicked;
        bar.Controls.Add(btnEdit);
        bar.Resize += (_, _) => btnEdit.Location = new Point(bar.Width - btnEdit.Width - 20, 9);
        return bar;
    }

    // ── Hero banner ───────────────────────────────────────────────────────────
    private Panel BuildHero()
    {
        var hero = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Theme.AppTheme.BrandDark };
        hero.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(
                new Point(0, 0), new Point(hero.Width, 0),
                Color.FromArgb(0, 0, 0, 0), Color.FromArgb(50, 0, 0, 0));
            e.Graphics.FillRectangle(brush, new Rectangle(0, 0, hero.Width, hero.Height));
        };

        hero.Controls.Add(new Label
        {
            Text = "🩺", Left = 22, Top = 18, Width = 50, Height = 50,
            Font = new Font("Segoe UI", 20f), ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter, AutoSize = false, BackColor = Color.Transparent
        });
        hero.Controls.Add(new Label
        {
            Text = $"Clinical Record — {_record.PetName}",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = Color.White,
            AutoSize = true, Left = 84, Top = 20, BackColor = Color.Transparent
        });

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_record.CustomerName)) parts.Add($"Owner: {_record.CustomerName}");
        if (!string.IsNullOrWhiteSpace(_record.VetName))      parts.Add($"Dr. {_record.VetName}");
        parts.Add(_record.CreatedAt.ToString("MMM d, yyyy"));
        hero.Controls.Add(new Label
        {
            Text = string.Join("   ·   ", parts),
            Font = new Font("Segoe UI", 9f), ForeColor = Theme.AppTheme.SubtitleText,
            AutoSize = true, Left = 84, Top = 48, BackColor = Color.Transparent
        });
        return hero;
    }

    // ── Scrollable content ────────────────────────────────────────────────────
    private Control BuildScrollContent()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(245, 248, 252) };

        var content = new Panel { Location = Point.Empty, AutoSize = true, BackColor = Color.Transparent };
        scroll.Controls.Add(content);
        scroll.Resize       += (_, _) => content.Width = scroll.ClientSize.Width;
        scroll.HandleCreated += (_, _) => content.Width = scroll.ClientSize.Width;

        var meds         = DataStore.GetRecordMedications(_record.Id);
        bool hasTreatment = !string.IsNullOrWhiteSpace(_record.Treatment);
        bool hasNotes     = !string.IsNullOrWhiteSpace(_record.Notes);

        // Added in REVERSE visual order — Dock.Top stacks last-added at top
        content.Controls.Add(Pad(28, 0, 28, 28));                              // bottom spacer
        if (meds.Any())
            content.Controls.Add(Section(BuildMedsCard(meds),    content, 14));
        if (hasTreatment || hasNotes)
            content.Controls.Add(Section(BuildTreatmentRow(hasTreatment, hasNotes), content, 14));
        content.Controls.Add(Section(BuildDiagnosisCard(),        content, 14));
        content.Controls.Add(Section(BuildChipsRow(),             content, 16));
        content.Controls.Add(Pad(28, 20, 28, 0));                              // top spacer

        return scroll;
    }

    // Wraps an inner panel with horizontal padding = 28 on each side
    private static Panel Section(Panel inner, Panel content, int bottomGap)
    {
        var wrap = new Panel { Dock = DockStyle.Top, BackColor = Color.Transparent };
        inner.Left = 28;
        inner.Top  = 0;
        wrap.Controls.Add(inner);

        void Sync()
        {
            inner.Width = Math.Max(1, content.Width - 56);
            wrap.Height = inner.Height + bottomGap;
        }
        content.Resize       += (_, _) => Sync();
        content.HandleCreated += (_, _) => Sync();
        inner.Resize         += (_, _) => wrap.Height = inner.Height + bottomGap;
        Sync();
        return wrap;
    }

    private static Panel Pad(int l, int t, int r, int b)
        => new Panel { Dock = DockStyle.Top, Height = t + b, BackColor = Color.Transparent };

    // ── Info chips row ────────────────────────────────────────────────────────
    private Panel BuildChipsRow()
    {
        var row = new Panel { Height = 80, BackColor = Color.Transparent };

        var fuText  = _record.FollowUpDate?.ToString("MMM d, yyyy") ?? "Not scheduled";
        var fuColor = _record.FollowUpDate.HasValue ? Theme.AppTheme.Success : Color.FromArgb(158, 170, 188);
        var vetText = string.IsNullOrWhiteSpace(_record.VetName) ? "Not assigned" : $"Dr. {_record.VetName}";

        var chips = new[]
        {
            Chip("👨‍⚕️", "VETERINARIAN",  vetText,                                          Theme.AppTheme.Accent),
            Chip("📅", "DATE OF VISIT", _record.CreatedAt.ToString("MMM d, yyyy · h:mm tt"), Theme.AppTheme.Primary),
            Chip("🔄", "FOLLOW-UP DATE", fuText,                                             fuColor),
        };

        row.Resize += (_, _) =>
        {
            int gap = 10;
            int w   = (row.Width - gap * (chips.Length - 1)) / chips.Length;
            for (int i = 0; i < chips.Length; i++)
            {
                chips[i].Width  = w;
                chips[i].Height = row.Height;
                chips[i].Left   = i * (w + gap);
                chips[i].Top    = 0;
            }
        };
        row.Controls.AddRange(chips);
        return row;
    }

    private static Panel Chip(string icon, string label, string value, Color accent)
    {
        var p = new Panel { BackColor = Color.White };
        p.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(Color.FromArgb(220, 226, 238));
            using var path = Round(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
            using var bar  = new SolidBrush(accent);
            e.Graphics.FillRectangle(bar, new Rectangle(0, 0, 4, p.Height));
        };
        p.Controls.Add(new Label { Text = icon, Left = 14, Top = 14, Width = 26, Height = 26, Font = new Font("Segoe UI", 11f), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, AutoSize = false });
        p.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(152, 164, 184), AutoSize = true, Left = 46, Top = 16, BackColor = Color.Transparent });
        p.Controls.Add(new Label { Text = value, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = Color.FromArgb(28, 38, 58), AutoSize = true, Left = 46, Top = 36, BackColor = Color.Transparent });
        return p;
    }

    // ── Diagnosis card ────────────────────────────────────────────────────────
    private Panel BuildDiagnosisCard()
    {
        var card = new Panel { BackColor = Color.White, Height = 76 };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(Color.FromArgb(220, 226, 238));
            using var path = Round(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
            using var acc  = new SolidBrush(Theme.AppTheme.Danger);
            e.Graphics.FillRectangle(acc, new Rectangle(0, 0, 5, card.Height));
        };
        card.Controls.Add(new Label
        {
            Text = "🔬  PRIMARY DIAGNOSIS",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(152, 164, 184),
            AutoSize = true, Left = 20, Top = 14, BackColor = Color.Transparent
        });
        var lblDiag = new Label
        {
            Text = _record.Diagnosis, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 32, 52), AutoSize = false,
            Left = 20, Top = 36, BackColor = Color.Transparent
        };
        card.Controls.Add(lblDiag);
        card.Resize += (_, _) =>
        {
            lblDiag.Width = Math.Max(1, card.Width - 40);
            lblDiag.MaximumSize = new Size(lblDiag.Width, 0);
            lblDiag.AutoSize = true;
            card.Height = lblDiag.Bottom + 18;
        };
        return card;
    }

    // ── Treatment + Notes row ─────────────────────────────────────────────────
    private Panel BuildTreatmentRow(bool hasTreatment, bool hasNotes)
    {
        var row  = new Panel { BackColor = Color.Transparent, Height = 120 };
        int cols = (hasTreatment && hasNotes) ? 2 : 1;

        Panel? left  = hasTreatment ? TextCard("💊  TREATMENT PLAN",  _record.Treatment, Theme.AppTheme.Primary) : null;
        Panel? right = hasNotes     ? TextCard("📝  CLINICAL NOTES",   _record.Notes,     Theme.AppTheme.Accent)  : null;

        if (left  != null) row.Controls.Add(left);
        if (right != null) row.Controls.Add(right);

        row.Resize += (_, _) =>
        {
            int gap = 12;
            int w   = cols == 2 ? (row.Width - gap) / 2 : row.Width;
            if (left  != null) { left.SetBounds(0,       0, w, left.Height);  }
            if (right != null) { right.SetBounds(cols == 2 ? w + gap : 0, 0, w, right.Height); }
            row.Height = Math.Max(left?.Height ?? 0, right?.Height ?? 0);
        };
        return row;
    }

    private static Panel TextCard(string heading, string body, Color accent)
    {
        var card = new Panel { BackColor = Color.White, Height = 100 };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(Color.FromArgb(220, 226, 238));
            using var path = Round(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
            using var acc  = new SolidBrush(accent);
            e.Graphics.FillRectangle(acc, new Rectangle(0, 0, 4, card.Height));
        };
        card.Controls.Add(new Label
        {
            Text = heading, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(152, 164, 184), AutoSize = true,
            Left = 20, Top = 14, BackColor = Color.Transparent
        });
        var lblBody = new Label
        {
            Text = body, Font = new Font("Segoe UI", 10f), ForeColor = Color.FromArgb(46, 58, 78),
            AutoSize = false, Left = 20, Top = 36, BackColor = Color.Transparent
        };
        card.Controls.Add(lblBody);
        card.Resize += (_, _) =>
        {
            lblBody.Width = Math.Max(1, card.Width - 40);
            lblBody.MaximumSize = new Size(lblBody.Width, 0);
            lblBody.AutoSize = true;
            card.Height = lblBody.Bottom + 18;
        };
        return card;
    }

    // ── Medications card ──────────────────────────────────────────────────────
    private Panel BuildMedsCard(List<RecordMedication> meds)
    {
        var card = new Panel { BackColor = Color.White, Height = 60 };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(Color.FromArgb(220, 226, 238));
            using var path = Round(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.DrawPath(pen, path);
        };

        // Card header bar
        var hdr = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(244, 249, 255) };
        hdr.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(Color.FromArgb(220, 226, 238));
            using var path = Round(new Rectangle(0, 0, hdr.Width - 1, hdr.Height + 10), 8);
            e.Graphics.DrawPath(pen, path);
            using var sep  = new Pen(Color.FromArgb(224, 230, 244));
            e.Graphics.DrawLine(sep, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
        };
        hdr.Controls.Add(new Label
        {
            Text = $"💊  PRESCRIBED MEDICATIONS  ({meds.Count})",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(50, 72, 118),
            AutoSize = true, Left = 20, Top = 13, BackColor = Color.Transparent
        });
        card.Controls.Add(hdr);

        // Items list
        var itemsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoSize = true,
            Padding = new Padding(16, 10, 16, 12), BackColor = Color.White
        };

        foreach (var med in meds)
        {
            bool hasNote = !string.IsNullOrWhiteSpace(med.Notes);
            var item = new Panel { Height = hasNote ? 64 : 52, BackColor = Color.FromArgb(247, 251, 255), Margin = new Padding(0, 0, 0, 8) };
            item.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen  = new Pen(Color.FromArgb(210, 224, 246));
                using var path = Round(new Rectangle(0, 0, item.Width - 1, item.Height - 1), 6);
                e.Graphics.DrawPath(pen, path);
                using var acc  = new SolidBrush(Theme.AppTheme.Accent);
                e.Graphics.FillRectangle(acc, new Rectangle(0, 0, 4, item.Height));
            };
            item.Controls.Add(new Label { Text = "💊", Left = 12, Top = hasNote ? 8 : 12, Width = 26, Height = 26, Font = new Font("Segoe UI", 11f), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, AutoSize = false });
            item.Controls.Add(new Label { Text = med.MedicationName, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(22, 36, 58), AutoSize = true, Left = 46, Top = hasNote ? 6 : 14, BackColor = Color.Transparent });
            item.Controls.Add(new Label { Text = med.Dosage, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(88, 106, 132), AutoSize = true, Left = 46, Top = hasNote ? 26 : 30, BackColor = Color.Transparent });
            if (hasNote)
                item.Controls.Add(new Label { Text = $"Note: {med.Notes}", Font = new Font("Segoe UI", 8.5f, FontStyle.Italic), ForeColor = Color.FromArgb(122, 140, 164), AutoSize = true, Left = 46, Top = 44, BackColor = Color.Transparent });

            itemsFlow.Controls.Add(item);
            itemsFlow.Resize += (_, _) => item.Width = Math.Max(1, itemsFlow.ClientSize.Width - 32);
        }

        card.Controls.Add(itemsFlow);
        card.Resize += (_, _) => card.Height = hdr.Height + itemsFlow.Height;
        return card;
    }

    // ── Edit handler ──────────────────────────────────────────────────────────
    private void OnEditClicked(object? s, EventArgs e)
    {
        using var dlg = new MedicalRecordDialog(_record);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var r = dlg.Result;
        _record.AppointmentId = r.AppointmentId; _record.PetId = r.PetId; _record.PetName = r.PetName;
        _record.CustomerId    = r.CustomerId;    _record.CustomerName  = r.CustomerName;
        _record.VetId         = r.VetId;         _record.VetName       = r.VetName;
        _record.Diagnosis     = r.Diagnosis;     _record.Treatment     = r.Treatment;
        _record.Notes         = r.Notes;         _record.FollowUpDate  = r.FollowUpDate;

        try
        {
            DataStore.Update(_record);
            DataStore.SaveRecordMedications(_record.Id, dlg.Prescriptions);
            VetMS.Forms.Toast.Success("Medical record updated!");
            MainForm.Instance.LoadForm(new MedicalRecordDetailForm(_record, _onBack));
        }
        catch (Exception ex)
        {
            VetMS.Forms.CustomMessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Round rect ────────────────────────────────────────────────────────────
    private static GraphicsPath Round(Rectangle rc, int r)
    {
        int d = r * 2;
        var p = new GraphicsPath();
        p.AddArc(rc.Left,       rc.Top,        d, d, 180, 90);
        p.AddArc(rc.Right - d,  rc.Top,        d, d, 270, 90);
        p.AddArc(rc.Right - d,  rc.Bottom - d, d, d,   0, 90);
        p.AddArc(rc.Left,       rc.Bottom - d, d, d,  90, 90);
        p.CloseFigure();
        return p;
    }
}
