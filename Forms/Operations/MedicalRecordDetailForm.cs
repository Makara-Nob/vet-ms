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
        Text = $"Medical Record - {_record.PetName}";
        BackColor = Color.FromArgb(248, 250, 252);

        var header = BuildHeader();
        var content = BuildMainContent();

        Controls.Add(content);
        Controls.Add(header);
    }

    // ─────────────────────────────────────────────────────────────
    // HEADER
    // ─────────────────────────────────────────────────────────────
    private Panel BuildHeader()
    {
        var hdr = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = Color.White };
        hdr.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 230, 240), 1f);
            e.Graphics.DrawLine(pen, 0, hdr.Height - 1, hdr.Width, hdr.Height - 1);
        };

        var btnBack = new Button
        {
            Text = "← Back to list",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Theme.AppTheme.Primary,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            AutoSize = true,
            Location = new Point(32, 16),
            BackColor = Color.Transparent
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (_, _) => _onBack?.Invoke();
        hdr.Controls.Add(btnBack);

        var lblTitle = new Label
        {
            Text = $"Clinical Record for {_record.PetName}",
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = Color.FromArgb(20, 30, 50),
            AutoSize = true,
            Location = new Point(32, 40)
        };
        hdr.Controls.Add(lblTitle);

        // Position subtitle AFTER adding title so AutoSize has resolved its height
        var lblSub = new Label
        {
            Text = $"Visit Date: {_record.CreatedAt:MMM d, yyyy}   ·   Owner: {_record.CustomerName}   ·   Dr. {_record.VetName}",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(110, 125, 148),
            AutoSize = true
        };
        hdr.Controls.Add(lblSub);
        lblSub.Location = new Point(32, lblTitle.Bottom + 8);

        var btnEdit = new Button
        {
            Text = "✎  Edit Record",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Theme.AppTheme.Accent,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Size = new Size(140, 40)
        };
        btnEdit.FlatAppearance.BorderSize = 0;
        btnEdit.Click += OnEditClicked;

        void PositionEditBtn() => btnEdit.Location = new Point(hdr.Width - btnEdit.Width - 32, (hdr.Height - btnEdit.Height) / 2);
        hdr.Resize += (_, _) => PositionEditBtn();
        hdr.HandleCreated += (_, _) => PositionEditBtn();
        hdr.Controls.Add(btnEdit);

        return hdr;
    }

    private void OnEditClicked(object? s, EventArgs e)
    {
        using var dlg = new MedicalRecordDialog(_record);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var r = dlg.Result;
        _record.AppointmentId = r.AppointmentId; _record.PetId = r.PetId; _record.PetName = r.PetName;
        _record.CustomerId = r.CustomerId; _record.CustomerName = r.CustomerName;
        _record.VetId = r.VetId; _record.VetName = r.VetName;
        _record.Diagnosis = r.Diagnosis; _record.Treatment = r.Treatment;
        _record.Notes = r.Notes; _record.FollowUpDate = r.FollowUpDate;

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

    // ─────────────────────────────────────────────────────────────
    // MAIN CONTENT
    // ─────────────────────────────────────────────────────────────
    private Control BuildMainContent()
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(248, 250, 252) };

        // Root stacking container — always matches scroll client width
        var stack = new Panel { Location = new Point(0, 0), AutoSize = true, BackColor = Color.Transparent };
        scroll.Resize += (_, _) => stack.Width = scroll.ClientSize.Width;
        scroll.HandleCreated += (_, _) => stack.Width = scroll.ClientSize.Width;

        // ── Two-column row ──────────────────────────────────────
        var twoColRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(32, 32, 32, 0)
        };
        twoColRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        twoColRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        twoColRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var overviewCard = BuildCard("CLINICAL OVERVIEW", BuildOverviewContent());
        overviewCard.Margin = new Padding(0, 0, 12, 0);
        overviewCard.Dock = DockStyle.Fill;

        var treatmentCard = BuildCard("TREATMENT & NOTES", BuildTreatmentContent());
        treatmentCard.Margin = new Padding(12, 0, 0, 0);
        treatmentCard.Dock = DockStyle.Fill;

        twoColRow.Controls.Add(overviewCard, 0, 0);
        twoColRow.Controls.Add(treatmentCard, 1, 0);

        // ── Medications row ─────────────────────────────────────
        var meds = DataStore.GetRecordMedications(_record.Id);
        Panel? medsRow = null;
        if (meds.Any())
        {
            medsRow = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(32, 24, 32, 40)
            };
            var medsCard = BuildMedicationsCard(meds);
            medsCard.Dock = DockStyle.Top;
            medsRow.Controls.Add(medsCard);
        }

        // Add in reverse order because Dock.Top stacks bottom-up
        if (medsRow != null) stack.Controls.Add(medsRow);
        stack.Controls.Add(twoColRow);

        scroll.Controls.Add(stack);
        return scroll;
    }

    // ─────────────────────────────────────────────────────────────
    // CARD WRAPPER
    // ─────────────────────────────────────────────────────────────
    private static Panel BuildCard(string title, Control content)
    {
        var outer = new Panel { AutoSize = true, BackColor = Color.Transparent };

        var lblHead = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 42, 62),
            AutoSize = true,
            Location = new Point(0, 0)
        };
        outer.Controls.Add(lblHead);

        var card = new Panel { Top = 28, Left = 0, BackColor = Color.White, AutoSize = true };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(224, 228, 238), 1.5f);
            using var path = RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            e.Graphics.DrawPath(pen, path);
        };

        content.Dock = DockStyle.Top;
        card.Controls.Add(content);

        // Card width follows outer panel
        outer.Resize += (_, _) =>
        {
            card.Width = outer.Width;
        };

        outer.Controls.Add(card);
        return outer;
    }

    // ─────────────────────────────────────────────────────────────
    // OVERVIEW CONTENT (2×2 prop grid)
    // ─────────────────────────────────────────────────────────────
    private Control BuildOverviewContent()
    {
        var tbl = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(24),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        tbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        tbl.Controls.Add(Prop("VETERINARIAN", $"Dr. {_record.VetName}"), 0, 0);
        tbl.Controls.Add(Prop("DATE OF VISIT", _record.CreatedAt.ToString("MMM d, yyyy · h:mm tt")), 1, 0);
        tbl.Controls.Add(Prop("PRIMARY DIAGNOSIS", _record.Diagnosis), 0, 1);

        var followUpColor = _record.FollowUpDate.HasValue ? Color.FromArgb(30, 40, 60) : Color.FromArgb(160, 170, 185);
        tbl.Controls.Add(Prop("FOLLOW-UP DATE",
            _record.FollowUpDate?.ToString("MMMM d, yyyy") ?? "Not scheduled",
            followUpColor), 1, 1);

        return tbl;
    }

    // ─────────────────────────────────────────────────────────────
    // TREATMENT CONTENT
    // ─────────────────────────────────────────────────────────────
    private Control BuildTreatmentContent()
    {
        var flow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(24)
        };

        if (!string.IsNullOrWhiteSpace(_record.Treatment))
            flow.Controls.Add(SectionBlock("TREATMENT PLAN", _record.Treatment, bottomMargin: 20));

        if (!string.IsNullOrWhiteSpace(_record.Notes))
            flow.Controls.Add(SectionBlock("CLINICAL NOTES", _record.Notes, bottomMargin: 0));

        return flow;
    }

    // ─────────────────────────────────────────────────────────────
    // MEDICATIONS CARD
    // ─────────────────────────────────────────────────────────────
    private Panel BuildMedicationsCard(List<RecordMedication> meds)
    {
        var outer = new Panel { AutoSize = true, BackColor = Color.Transparent };

        var lblHead = new Label
        {
            Text = "PRESCRIBED MEDICATIONS",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 42, 62),
            AutoSize = true,
            Location = new Point(0, 0)
        };
        outer.Controls.Add(lblHead);

        var card = new Panel { Top = 28, Left = 0, BackColor = Color.White };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(224, 228, 238), 1.5f);
            using var path = RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            e.Graphics.DrawPath(pen, path);
        };

        var grid = new DataGridView
        {
            Location = new Point(20, 20),
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ScrollBars = ScrollBars.None
        };

        UIHelper.StyleGrid(grid);
        grid.RowTemplate.Height = 42;
        grid.ColumnHeadersHeight = 40;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 247, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(80, 95, 120);
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
        grid.DefaultCellStyle.ForeColor = Color.FromArgb(35, 45, 65);
        grid.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

        grid.DataSource = meds.Select(m => new
        {
            Medication = m.MedicationName,
            Dosage = m.Dosage,
            Notes = string.IsNullOrWhiteSpace(m.Notes) ? "—" : m.Notes
        }).ToList();

        grid.Height = grid.ColumnHeadersHeight + (meds.Count * grid.RowTemplate.Height) + 4;

        card.Controls.Add(grid);
        card.Height = grid.Bottom + 20;

        // Width follows outer
        outer.Resize += (_, _) =>
        {
            card.Width = outer.Width;
            grid.Width = card.Width - 40;
        };

        outer.Controls.Add(card);
        return outer;
    }

    // ─────────────────────────────────────────────────────────────
    // SMALL HELPERS
    // ─────────────────────────────────────────────────────────────
    private static Panel Prop(string label, string value, Color? valueColor = null)
    {
        var p = new Panel { AutoSize = true, Margin = new Padding(0, 0, 0, 20), BackColor = Color.White };
        p.Controls.Add(new Label
        {
            Text = label,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(140, 152, 170),
            AutoSize = true,
            Location = new Point(0, 0)
        });
        p.Controls.Add(new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = valueColor ?? Color.FromArgb(30, 40, 60),
            AutoSize = true,
            MaximumSize = new Size(320, 0),
            Location = new Point(0, 18)
        });
        return p;
    }

    private static Panel SectionBlock(string heading, string body, int bottomMargin)
    {
        var p = new Panel { AutoSize = true, Margin = new Padding(0, 0, 0, bottomMargin), BackColor = Color.White };
        p.Controls.Add(new Label
        {
            Text = heading,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(140, 152, 170),
            AutoSize = true,
            Location = new Point(0, 0)
        });
        p.Controls.Add(new Label
        {
            Text = body,
            Font = new Font("Segoe UI", 10.5f),
            ForeColor = Color.FromArgb(50, 62, 82),
            AutoSize = true,
            MaximumSize = new Size(460, 0),
            Location = new Point(0, 20)
        });
        return p;
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