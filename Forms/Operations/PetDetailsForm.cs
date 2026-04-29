using VetMS.Data;
using VetMS.Models;
using System.Drawing.Drawing2D;

namespace VetMS.Forms.Operations;

/// <summary>
/// Centralised patient profile — all clinical history in one place.
/// </summary>
public class PetDetailsForm : Form
{
    // ── Data (loaded once on construction) ───────────────────────────────────
    private readonly Pet            _pet;
    private readonly Action?        _onBack;
    private readonly int            _startTab;
    private List<MedicalRecord>     _records   = [];
    private List<Appointment>       _appts     = [];
    private List<CbcRecord>         _cbcList   = [];
    private Customer?               _customer;

    // ── Computed summary ─────────────────────────────────────────────────────
    private string _lastVisitStr  = "—";
    private string _nextApptStr   = "—";
    private bool   _overdueAlert;
    private string _overdueMsg    = "";

    // ── Canine/Feline CBC reference ranges (low, high) ───────────────────────
    private static readonly Dictionary<string, (decimal Lo, decimal Hi)> CbcRef = new()
    {
        ["WBC"] = (6.0m,  17.0m),
        ["RBC"] = (5.5m,  8.5m),
        ["HGB"] = (12.0m, 18.0m),
        ["HCT"] = (37.0m, 55.0m),
        ["PLT"] = (200m,  500m),
        ["MCV"] = (60.0m, 77.0m),
        ["MCH"] = (19.5m, 24.5m),
        ["MCHC"] = (31.0m, 36.0m),
    };

    // ════════════════════════════════════════════════════════════════════════
    // CONSTRUCTION
    // ════════════════════════════════════════════════════════════════════════
    public PetDetailsForm(Pet pet, Action? onBack = null, int startTab = 0)
    {
        _pet      = pet;
        _onBack   = onBack;
        _startTab = startTab;
        LoadData();
        InitializeUI();
    }

    private void LoadData()
    {
        _records  = DataStore.GetMedicalRecords()
                             .Where(r => r.PetId == _pet.Id)
                             .OrderByDescending(r => r.CreatedAt)
                             .ToList();

        _appts    = DataStore.GetAppointments()
                             .Where(a => a.PetId == _pet.Id)
                             .OrderByDescending(a => a.AppointmentDate)
                             .ToList();

        _cbcList  = DataStore.GetCbcRecords()
                             .Where(c => c.PetId == _pet.Id)
                             .OrderByDescending(c => c.TestDate)
                             .ToList();

        _customer = DataStore.GetCustomers()
                             .FirstOrDefault(c => c.Id == _pet.CustomerId);

        if (_records.Any())
            _lastVisitStr = _records.First().CreatedAt.ToString("MMM d, yyyy");

        var next = _appts
            .Where(a => a.AppointmentDate.Date >= DateTime.Today && a.Status == "Scheduled")
            .OrderBy(a => a.AppointmentDate)
            .FirstOrDefault();
        if (next != null)
            _nextApptStr = next.AppointmentDate.ToString("MMM d, yyyy");

        // Overdue follow-up check
        var overdue = _records
            .Where(r => r.FollowUpDate.HasValue && r.FollowUpDate.Value.Date < DateTime.Today)
            .OrderBy(r => r.FollowUpDate)
            .ToList();

        if (overdue.Any())
        {
            _overdueAlert = true;
            var oldest = overdue.First();
            int days = (DateTime.Today - oldest.FollowUpDate!.Value.Date).Days;
            _overdueMsg = $"Follow-up overdue by {days} day{(days == 1 ? "" : "s")}  ·  " +
                          $"Due {oldest.FollowUpDate.Value:MMM d, yyyy}  ·  {Truncate(oldest.Diagnosis, 72)}";
        }
    }

    private void InitializeUI()
    {
        Text              = $"Patient — {_pet.Name}";
        BackColor         = Color.FromArgb(245, 247, 250);
        FormBorderStyle   = FormBorderStyle.None;
        Dock              = DockStyle.Fill;

        var root = new Panel { Dock = DockStyle.Fill };
        root.Controls.Add(BuildContent());   // Fill
        root.Controls.Add(BuildSidebar());   // Left
        Controls.Add(root);
    }

    // ════════════════════════════════════════════════════════════════════════
    // ── SIDEBAR ─────────────────────────────────────────────────────────────
    // ════════════════════════════════════════════════════════════════════════
    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 292, Dock = DockStyle.Left, BackColor = Color.White };
        sidebar.Paint += (_, e) =>
        {
            using var p = new Pen(Color.FromArgb(226, 229, 236));
            e.Graphics.DrawLine(p, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
        };

        // ── Profile header ───────────────────────────────────────────────────
        var hdr = new Panel { Dock = DockStyle.Top, Height = 200, BackColor = Theme.AppTheme.Primary };
        hdr.Paint += (_, e) =>
        {
            using var br = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(hdr.Width, 1), hdr.Height),
                Theme.AppTheme.Primary, Theme.AppTheme.BrandDeep, LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(br, 0, 0, hdr.Width, hdr.Height);
        };

        // Circular avatar
        int avSz = 86;
        var pic  = new PictureBox { Width = avSz, Height = avSz, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        pic.Image = UIHelper.CreateProfilePlaceholder(avSz);
        if (_pet.ProfilePicture is { Length: > 0 })
        {
            using var ms = new MemoryStream(_pet.ProfilePicture);
            pic.Image = Image.FromStream(ms);
        }
        var gp = new GraphicsPath(); gp.AddEllipse(0, 0, avSz, avSz); pic.Region = new Region(gp);
        UIHelper.AttachImageViewer(pic, () => pic.Image);

        var lblName = new Label
        {
            Text = _pet.Name, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White, TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false, Width = sidebar.Width, Height = 26, BackColor = Color.Transparent
        };

        // Status badge (Panel so we can paint a proper rounded rect)
        bool   active     = _pet.IsActive;
        var    badgeClr   = active ? Theme.AppTheme.Success : Theme.AppTheme.Danger;
        string badgeTxt   = active ? "ACTIVE" : "INACTIVE";
        var    badge      = new Panel { Width = active ? 72 : 86, Height = 22, BackColor = Color.Transparent };
        badge.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path  = RoundPath(new Rectangle(0, 0, badge.Width - 1, badge.Height - 1), 10);
            using var brush = new SolidBrush(badgeClr);
            e.Graphics.FillPath(brush, path);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(badgeTxt, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Brushes.White, new RectangleF(0, 0, badge.Width, badge.Height), sf);
        };

        hdr.Controls.AddRange(new Control[] { pic, lblName, badge });
        void LayHdr()
        {
            pic.Left      = (sidebar.Width - avSz) / 2; pic.Top    = 22;
            lblName.Width = sidebar.Width;               lblName.Left = 0; lblName.Top = pic.Bottom + 8;
            badge.Left    = (sidebar.Width - badge.Width) / 2; badge.Top = lblName.Bottom + 5;
        }
        hdr.Resize += (_, _) => LayHdr(); LayHdr();
        sidebar.Controls.Add(hdr);

        // ── Scrollable info rows ─────────────────────────────────────────────
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var stack  = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            Padding = new Padding(20, 14, 20, 20)
        };

        // Helpers
        void Sec(string title)
            => stack.Controls.Add(new Label
            {
                Text = title.ToUpperInvariant(), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(158, 168, 185), AutoSize = true, Margin = new Padding(0, 16, 0, 4)
            });

        void InfoRow(string label, string val, Color? vc = null)
        {
            var row = new Panel { Width = 252, Height = 44, Margin = new Padding(0, 0, 0, 2) };
            row.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(242, 244, 248)); e.Graphics.DrawLine(p, 0, row.Height - 1, row.Width, row.Height - 1); };
            row.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(140, 152, 168), Left = 0, Top = 3, AutoSize = true });
            row.Controls.Add(new Label
            {
                Text = string.IsNullOrWhiteSpace(val) ? "—" : val,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = vc ?? Color.FromArgb(32, 42, 62),
                Left = 0, Top = 22, AutoSize = true, MaximumSize = new Size(252, 0)
            });
            stack.Controls.Add(row);
        }

        Sec("Patient");
        InfoRow("Species / Breed", $"{_pet.SpeciesName}  ·  {(_pet.BreedName ?? "Mix")}");
        InfoRow("Gender", _pet.Gender);
        InfoRow("Age / DOB", GetAgeWithDate());
        InfoRow("Weight", $"{_pet.Weight:F1} kg");
        InfoRow("Color / Coat", _pet.Color);
        InfoRow("Microchip", string.IsNullOrWhiteSpace(_pet.MicrochipNo) ? "None" : _pet.MicrochipNo);

        Sec("Owner");
        InfoRow("Name", _pet.CustomerName);
        if (_customer != null)
        {
            InfoRow("Phone", _customer.Phone);
            InfoRow("Email", _customer.Email);
        }

        Sec("Clinical Summary");
        var lastRec = _records.FirstOrDefault();
        InfoRow("Last Diagnosis", lastRec != null ? Truncate(lastRec.Diagnosis, 52) : "No records yet.");

        var nextA = _appts
            .Where(a => a.AppointmentDate.Date >= DateTime.Today && a.Status == "Scheduled")
            .OrderBy(a => a.AppointmentDate).FirstOrDefault();
        InfoRow("Next Appointment", nextA != null
            ? $"{nextA.AppointmentDate:MMM d, yyyy}  ·  {nextA.ServiceTypeName}"
            : "None scheduled");

        int medCount = _records.Take(3)
                               .SelectMany(r => DataStore.GetRecordMedications(r.Id))
                               .Count();
        InfoRow("Recent Medications", medCount > 0
            ? $"{medCount} prescribed in last {Math.Min(_records.Count, 3)} visit(s)"
            : "None on record");

        Sec("Record");
        InfoRow("Created", _pet.CreatedAt.ToString("MMM d, yyyy"));
        InfoRow("Created By", _pet.CreatedBy ?? "—");
        if (_pet.UpdatedAt.HasValue)
            InfoRow("Last Updated", _pet.UpdatedAt.Value.ToString("MMM d, yyyy"));

        scroll.Controls.Add(stack);
        sidebar.Controls.Add(scroll);
        return sidebar;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ── CONTENT AREA ────────────────────────────────────────────────────────
    // ════════════════════════════════════════════════════════════════════════
    private Panel BuildContent()
    {
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250) };

        // ── Top bar ──────────────────────────────────────────────────────────
        var topBar = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.White };
        topBar.Paint += (_, e) =>
        {
            using var p = new Pen(Color.FromArgb(226, 229, 236));
            e.Graphics.DrawLine(p, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
        };

        var btnBack = new Button
        {
            Text = "← Patients", Font = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(70, 82, 105), BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat, AutoSize = true, Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (_, _) => _onBack?.Invoke();

        var crumb = new Label
        {
            Text = $"Patients  /  {_pet.Name}",
            Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(148, 158, 175), AutoSize = true
        };

        var btnRec  = TopBtn("+ Medical Record", Theme.AppTheme.Primary);
        var btnAppt = TopBtn("+ Appointment",    Theme.AppTheme.Accent);
        var btnCbc  = TopBtn("+ CBC",            Theme.AppTheme.Success);

        btnRec.Click  += (_, _) => { using var d = new MedicalRecordDialog(); d.ShowDialog(); };
        btnAppt.Click += (_, _) => { using var d = new AppointmentDialog();   d.ShowDialog(); };
        btnCbc.Click  += (_, _) => { using var d = new CbcDialog();           d.ShowDialog(); };

        topBar.Controls.AddRange(new Control[] { btnBack, crumb, btnRec, btnAppt, btnCbc });
        topBar.Resize += (_, _) =>
        {
            btnBack.Left  = 12;                                               btnBack.Top  = (topBar.Height - btnBack.Height) / 2;
            crumb.Left    = btnBack.Right + 10;                               crumb.Top    = (topBar.Height - crumb.Height) / 2;
            btnCbc.Left   = topBar.Width - btnCbc.Width - 14;                btnCbc.Top   = (topBar.Height - btnCbc.Height) / 2;
            btnAppt.Left  = btnCbc.Left  - btnAppt.Width  - 8;               btnAppt.Top  = btnCbc.Top;
            btnRec.Left   = btnAppt.Left - btnRec.Width   - 8;               btnRec.Top   = btnAppt.Top;
        };
        content.Controls.Add(topBar);

        // ── Overdue follow-up alert ──────────────────────────────────────────
        if (_overdueAlert) content.Controls.Add(BuildAlertBanner());

        // ── Stat cards ───────────────────────────────────────────────────────
        var statsRow = new Panel
        {
            Dock = DockStyle.Top, Height = 94,
            BackColor = Color.FromArgb(245, 247, 250),
            Padding = new Padding(14, 10, 14, 0)
        };
        var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flow.Controls.Add(StatCard("Total Visits",     _records.Count.ToString(),  "medical records",        Theme.AppTheme.Primary));
        flow.Controls.Add(StatCard("Last Visit",        _lastVisitStr,              "most recent record",     Color.FromArgb(0, 120, 215)));
        flow.Controls.Add(StatCard("Next Appointment",  _nextApptStr,               "upcoming scheduled",     Theme.AppTheme.Warning));
        flow.Controls.Add(StatCard("CBC Tests",         _cbcList.Count.ToString(),  "lab results on file",    Theme.AppTheme.Success));
        statsRow.Controls.Add(flow);
        content.Controls.Add(statsRow);

        // ── Dashboard Overview ───────────────────────────────────────────────
        var tabWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(14, 10, 14, 14) };
        var overview = BuildOverview();
        overview.Dock = DockStyle.Fill;
        tabWrap.Controls.Add(overview);
        content.Controls.Add(tabWrap);

        return content;
    }

    // ── Overdue follow-up alert banner ───────────────────────────────────────
    private Panel BuildAlertBanner()
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(255, 248, 230) };
        bar.Paint += (_, e) =>
        {
            using var topBorder = new Pen(Color.FromArgb(255, 210, 110));
            using var botBorder = new Pen(Color.FromArgb(255, 210, 110));
            using var stripe    = new SolidBrush(Color.FromArgb(255, 160, 0));
            e.Graphics.FillRectangle(stripe, 0, 0, 4, bar.Height);
            e.Graphics.DrawLine(botBorder, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        };

        var icon = new Label { Text = "⚠", Font = new Font("Segoe UI", 11f), ForeColor = Color.FromArgb(170, 100, 0), AutoSize = true };
        var msg  = new Label { Text = _overdueMsg, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(135, 78, 0), AutoSize = true };
        var btn  = new Button
        {
            Text = "Schedule Now", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            BackColor = Color.FromArgb(210, 130, 0), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, AutoSize = true, Height = 28, Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => { using var d = new AppointmentDialog(); d.ShowDialog(); };

        bar.Controls.AddRange(new Control[] { icon, msg, btn });
        bar.Resize += (_, _) =>
        {
            icon.Left = 14;             icon.Top = (bar.Height - icon.Height) / 2;
            msg.Left  = icon.Right + 6; msg.Top  = (bar.Height - msg.Height) / 2;
            btn.Left  = bar.Width - btn.Width - 14; btn.Top = (bar.Height - btn.Height) / 2;
        };
        return bar;
    }

    // ── Stat card ────────────────────────────────────────────────────────────
    private static Panel StatCard(string title, string value, string sub, Color accent)
    {
        float fs = value.Length <= 3 ? 22f : value.Length <= 6 ? 16f : 11.5f;
        var card = new Panel { Width = 208, Height = 74, BackColor = Color.White, Margin = new Padding(0, 0, 10, 0) };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            using var pen  = new Pen(Color.FromArgb(226, 229, 236));
            e.Graphics.DrawPath(pen, path);
            using var br = new SolidBrush(accent);
            e.Graphics.FillRectangle(br, 0, 14, 3, card.Height - 28);
        };
        // Title
        card.Controls.Add(new Label
        {
            Text = title.ToUpperInvariant(), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(138, 150, 170), AutoSize = true, Left = 14, Top = 8
        });
        // Value
        card.Controls.Add(new Label
        {
            Text = value, Font = new Font("Segoe UI", fs, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 34, 54), AutoSize = false,
            Width = card.Width - 28, Height = 26, Left = 14, Top = 24
        });
        // Sub
        card.Controls.Add(new Label
        {
            Text = sub, Font = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(162, 172, 188), AutoSize = true, Left = 14, Top = 55
        });
        return card;
    }

    private void ShowHistoryModal(string title, Control content)
    {
        using var f = new Form
        {
            Text = title,
            Size = new Size(1000, 700),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = Color.White
        };
        content.Dock = DockStyle.Fill;
        f.Controls.Add(content);
        f.ShowDialog();
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 1 — OVERVIEW
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildOverview()
    {
        var outer  = new Panel { BackColor = Color.FromArgb(245, 247, 250), Dock = DockStyle.Fill };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };
        outer.Controls.Add(scroll);

        var tlp = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            ColumnCount = 2, RowCount = 1, Width = 1000
        };
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));

        var leftCol = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 8, 20) };
        var rightCol = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(8, 0, 0, 20) };

        tlp.Controls.Add(leftCol, 0, 0);
        tlp.Controls.Add(rightCol, 1, 0);
        scroll.Controls.Add(tlp);

        tlp.Resize += (_, _) => 
        { 
            int leftW = (int)(tlp.Width * 0.6f) - 8;
            int rightW = (int)(tlp.Width * 0.4f) - 8;
            foreach (Control c in leftCol.Controls) c.Width = leftW;
            foreach (Control c in rightCol.Controls) c.Width = rightW;
        };

        leftCol.Controls.Add(BuildClinicalStatusCard());
        leftCol.Controls.Add(BuildLabTrendsCard());
        leftCol.Controls.Add(BuildCurrentMedsCard());

        rightCol.Controls.Add(BuildApptSummaryCard());
        rightCol.Controls.Add(BuildNotesCard());
        rightCol.Controls.Add(BuildTimelineCard());

        // Initial width setup after layout logic
        scroll.Layout += (_, _) => { tlp.Width = scroll.ClientSize.Width - 32; };

        return outer;
    }

    private Panel DashboardCard(string title, int width, FlowLayoutPanel content, Action? onAction = null, string actionText = "View All")
    {
        var card = new Panel { Width = width, BackColor = Color.White, Margin = new Padding(0, 0, 0, 16) };
        
        var header = new Panel { Dock = DockStyle.Top, Height = 44 };
        header.Paint += (_, e) => { e.Graphics.DrawLine(new Pen(Color.FromArgb(235, 238, 244)), 16, 43, header.Width - 16, 43); };
        var lblTitle = new Label { Text = title.ToUpperInvariant(), Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 115, 135), AutoSize = true, Left = 16, Top = 14 };
        header.Controls.Add(lblTitle);
        
        if (onAction != null)
        {
            var btn = new Button { Text = actionText, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Theme.AppTheme.Primary, FlatStyle = FlatStyle.Flat, AutoSize = true, Cursor = Cursors.Hand, Top = 10 };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (_, _) => onAction();
            header.Controls.Add(btn);
            header.Resize += (_, _) => { btn.Left = header.Width - btn.Width - 8; };
        }
        
        content.Width = width - 32;
        content.Left = 16;
        content.Top = 44 + 12;
        
        card.Controls.Add(content);
        card.Controls.Add(header);
        
        card.Height = 44 + 12 + content.Height + 16;
        content.Resize += (_, _) => { card.Height = 44 + 12 + content.Height + 16; card.Invalidate(); };
        
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var p = RoundPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            e.Graphics.DrawPath(new Pen(Color.FromArgb(226, 229, 236)), p);
        };
        card.Resize += (_, _) => { content.Width = card.Width - 32; header.Invalidate(); card.Invalidate(); };
        return card;
    }

    private Panel BuildClinicalStatusCard()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var lastRec = _records.FirstOrDefault();
        if (lastRec == null)
        {
            flow.Controls.Add(new Label { Text = "No clinical records found.", Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = true });
            return DashboardCard("Latest Clinical Status", 500, flow);
        }

        var lDate = new Label { Text = $"{lastRec.CreatedAt:MMM d, yyyy}  ·  Dr. {lastRec.VetName}", Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(120, 130, 150), AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        var lDx = new Label { Text = lastRec.Diagnosis, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 40, 60), AutoSize = true, MaximumSize = new Size(460, 0), Margin = new Padding(0, 0, 0, 6) };
        var lTx = new Label { Text = $"Tx: {lastRec.Treatment}", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(60, 75, 95), AutoSize = true, MaximumSize = new Size(460, 0), Margin = new Padding(0, 0, 0, 12) };
        
        flow.Controls.Add(lDate);
        flow.Controls.Add(lDx);
        flow.Controls.Add(lTx);

        if (!string.IsNullOrWhiteSpace(lastRec.Notes))
        {
            var lNotes = new Label { Text = $"Notes: {lastRec.Notes}", Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.FromArgb(90, 105, 125), AutoSize = true, MaximumSize = new Size(460, 0), Margin = new Padding(0, 0, 0, 8) };
            flow.Controls.Add(lNotes);
        }

        return DashboardCard("Latest Clinical Status", 500, flow, () => ShowHistoryModal("Medical Records", BuildMedRecordsTab()), "All Records");
    }

    private Panel BuildLabTrendsCard()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var lastCbc = _cbcList.FirstOrDefault();
        if (lastCbc == null)
        {
            flow.Controls.Add(new Label { Text = "No lab results on record.", Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = true });
            return DashboardCard("Lab Results Overview", 500, flow, () => ShowHistoryModal("Lab Results History", BuildCbcTab()), "Full Lab History");
        }

        var prevCbc = _cbcList.Skip(1).FirstOrDefault();

        var lDate = new Label { Text = $"Latest CBC: {lastCbc.TestDate:MMM d, yyyy}", Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(50, 60, 80), AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
        flow.Controls.Add(lDate);

        var checks = new[] 
        {
            ("WBC", lastCbc.Wbc, prevCbc?.Wbc), ("RBC", lastCbc.Rbc, prevCbc?.Rbc), 
            ("HGB", lastCbc.Hgb, prevCbc?.Hgb), ("PLT", lastCbc.Plt, prevCbc?.Plt)
        };

        bool hasAbnormal = false;
        foreach (var (name, val, prev) in checks)
        {
            var range = CbcRef[name];
            bool isHigh = val > range.Hi;
            bool isLow = val < range.Lo;
            
            if (isHigh || isLow)
            {
                hasAbnormal = true;
                var clr = isHigh ? Theme.AppTheme.Danger : Color.FromArgb(0, 110, 200);
                string trend = "";
                if (prev.HasValue)
                {
                    decimal diff = val - prev.Value;
                    if (diff > 0) trend = $" (↑ from {prev.Value})";
                    else if (diff < 0) trend = $" (↓ from {prev.Value})";
                    else trend = $" (= from {prev.Value})";
                }
                
                string tag = isHigh ? "HIGH" : "LOW";
                var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
                row.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = clr, Width = 40 });
                row.Controls.Add(new Label { Text = $"{val}{trend}", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(40, 50, 70), AutoSize = true, MinimumSize = new Size(120, 0) });
                
                var lblTag = new Label { Text = tag, Font = new Font("Segoe UI", 7f, FontStyle.Bold), ForeColor = clr, BackColor = Color.FromArgb(20, clr.R, clr.G, clr.B), AutoSize = true, Padding = new Padding(4, 2, 4, 2), Margin = new Padding(10, 0, 0, 0) };
                row.Controls.Add(lblTag);
                flow.Controls.Add(row);
            }
        }

        if (!hasAbnormal)
        {
            flow.Controls.Add(new Label { Text = "All major parameters within normal limits.", Font = new Font("Segoe UI", 9f), ForeColor = Theme.AppTheme.Success, AutoSize = true });
        }

        return DashboardCard("Lab Results Overview", 500, flow, () => ShowHistoryModal("Lab Results History", BuildCbcTab()), "Full Lab History");
    }

    private Panel BuildCurrentMedsCard()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        
        var recentMeds = _records.Take(3)
            .SelectMany(r => DataStore.GetRecordMedications(r.Id))
            .GroupBy(m => m.MedicationName)
            .Select(g => g.First())
            .ToList();

        if (!recentMeds.Any())
        {
            flow.Controls.Add(new Label { Text = "No active medications found.", Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = true });
            return DashboardCard("Current Medications", 500, flow, () => ShowHistoryModal("Medication History", BuildMedicationsTab()), "All Medications");
        }

        foreach (var med in recentMeds)
        {
            var row = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
            row.Controls.Add(new Label { Text = "●", Font = new Font("Segoe UI", 8f), ForeColor = Theme.AppTheme.Primary, AutoSize = true, Margin = new Padding(0, 2, 6, 0) });
            row.Controls.Add(new Label { Text = med.MedicationName, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 40, 60), AutoSize = true });
            row.Controls.Add(new Label { Text = $" — {med.Dosage}", Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(80, 95, 115), AutoSize = true });
            flow.Controls.Add(row);
        }

        return DashboardCard("Current Medications", 500, flow, () => ShowHistoryModal("Medication History", BuildMedicationsTab()), "All Medications");
    }

    private Panel BuildApptSummaryCard()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        
        var nextAppt = _appts.Where(a => a.AppointmentDate >= DateTime.Now).OrderBy(a => a.AppointmentDate).FirstOrDefault();
        var lastAppt = _appts.Where(a => a.AppointmentDate < DateTime.Now).OrderByDescending(a => a.AppointmentDate).FirstOrDefault();

        void AddRow(string label, Appointment? appt)
        {
            flow.Controls.Add(new Label { Text = label, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(130, 140, 160), AutoSize = true, Margin = new Padding(0, 0, 0, 4) });
            if (appt == null)
            {
                flow.Controls.Add(new Label { Text = "None", Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
            }
            else
            {
                var clr = appt.Status == "Scheduled" ? Theme.AppTheme.Accent : (appt.Status == "Completed" ? Theme.AppTheme.Success : Color.FromArgb(60, 70, 90));
                
                var lDate = new Label { Text = $"{appt.AppointmentDate:MMM d, yyyy  HH:mm}", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = clr, AutoSize = true };
                var lDet = new Label { Text = $"{appt.ServiceTypeName}  ·  Dr. {appt.VetName}", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(50, 60, 80), AutoSize = true, Margin = new Padding(0, 2, 0, 12) };
                flow.Controls.Add(lDate);
                flow.Controls.Add(lDet);
            }
        }

        AddRow("NEXT APPOINTMENT", nextAppt);
        AddRow("LAST COMPLETED", lastAppt);

        return DashboardCard("Appointments", 350, flow, () => ShowHistoryModal("Appointment History", BuildAppointmentsTab()), "View All");
    }

    private Panel BuildNotesCard()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        bool has = !string.IsNullOrWhiteSpace(_pet.Notes);
        var lbl = new Label 
        { 
            Text = has ? Truncate(_pet.Notes, 150) : "No general clinical notes recorded for this patient.", 
            Font = new Font("Segoe UI", 9f, has ? FontStyle.Regular : FontStyle.Italic), 
            ForeColor = has ? Color.FromArgb(50, 60, 80) : Color.Gray, 
            AutoSize = true, MaximumSize = new Size(310, 0) 
        };
        flow.Controls.Add(lbl);
        return DashboardCard("General Notes", 350, flow, () => ShowHistoryModal("General Notes", BuildNotesTab()), "Expand");
    }

    private Panel BuildTimelineCard()
    {
        var flow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        
        var events = new List<(DateTime Date, string Kind, string Title, Color Accent, Action OnClick)>();
        foreach (var r in _records.Take(5)) events.Add((r.CreatedAt, "VISIT", r.Diagnosis, Theme.AppTheme.Primary, () => { using var d = new MedicalRecordDialog(r, true); d.ShowDialog(); }));
        foreach (var a in _appts.Take(5)) events.Add((a.AppointmentDate, "APPT", a.ServiceTypeName, Theme.AppTheme.Accent, () => { using var d = new AppointmentDialog(a); d.ShowDialog(); }));
        foreach (var c in _cbcList.Take(5)) events.Add((c.TestDate, "LAB", "CBC Test", Color.FromArgb(130, 30, 165), () => { using var d = new CbcDialog(c); d.ShowDialog(); }));

        var sorted = events.OrderByDescending(e => e.Date).Take(10).ToList();

        if (!sorted.Any())
        {
            flow.Controls.Add(new Label { Text = "No recent activity.", Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray, AutoSize = true });
        }
        else
        {
            foreach (var ev in sorted)
            {
                var row = new Panel { Width = 310, Height = 48, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 4) };
                row.Paint += (_, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillEllipse(new SolidBrush(ev.Accent), 6, 12, 8, 8);
                    e.Graphics.DrawLine(new Pen(Color.FromArgb(235, 238, 244)), 10, 24, 10, 48); // vertical line
                };
                
                var lDate = new Label { Text = ev.Date.ToString("MMM d, yyyy"), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(140, 150, 170), AutoSize = true, Left = 24, Top = 8 };
                var lTitle = new Label { Text = $"{ev.Kind}  ·  {Truncate(ev.Title, 35)}", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(40, 50, 70), AutoSize = true, Left = 24, Top = 24 };
                
                row.Controls.Add(lDate);
                row.Controls.Add(lTitle);
                
                row.Click += (_, _) => ev.OnClick();
                lDate.Click += (_, _) => ev.OnClick();
                lTitle.Click += (_, _) => ev.OnClick();
                
                row.MouseEnter += (_, _) => row.BackColor = Color.FromArgb(250, 251, 253);
                row.MouseLeave += (_, _) => row.BackColor = Color.Transparent;

                flow.Controls.Add(row);
                
                // Allow row to resize with the flowlayout
                row.Resize += (_, _) => { lTitle.MaximumSize = new Size(row.Width - 30, 0); };
            }
        }

        return DashboardCard("Activity Timeline", 350, flow);
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 2 — MEDICAL RECORDS  (expandable accordion cards)
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildMedRecordsTab()
    {
        var outer  = new Panel { BackColor = Color.White };
        outer.Controls.Add(SubHeader(
            $"{_records.Count} visit record{(_records.Count == 1 ? "" : "s")}  ·  click ▼ on any card to reveal notes and medications"));

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        var stack  = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            Padding = new Padding(16, 12, 16, 24)
        };

        if (!_records.Any())
            stack.Controls.Add(EmptyLbl("No medical records yet.\nClick \"+ Medical Record\" above to add the first one."));
        else
            foreach (var rec in _records)
                stack.Controls.Add(MedRecCard(rec, stack));

        scroll.Controls.Add(stack);
        outer.Controls.Add(scroll);
        return outer;
    }

    private Panel MedRecCard(MedicalRecord rec, FlowLayoutPanel owner)
    {
        var meds      = DataStore.GetRecordMedications(rec.Id);
        bool hasFU    = rec.FollowUpDate.HasValue;
        bool overdue  = hasFU && rec.FollowUpDate!.Value.Date < DateTime.Today;
        bool hasNotes = !string.IsNullOrWhiteSpace(rec.Notes);
        bool hasMeds  = meds.Any();
        bool canExpand = hasNotes || hasMeds;

        // ── Heights ───────────────────────────────────────────────────────────
        const int COLLAPSED_H = 100;
        int expandH = 0;
        int notesTextHeight = 0;
        if (hasNotes) {
            notesTextHeight = TextRenderer.MeasureText(rec.Notes, new Font("Segoe UI", 9f), new Size(900 - 32, 0), TextFormatFlags.WordBreak).Height;
            expandH += 12 + 20 + notesTextHeight + 20; // top-pad + heading + text + gap
        } else {
            expandH += 12;
        }
        if (hasMeds)   expandH += 20 + meds.Count * 24;   // heading + rows
        expandH += 10;                                           // bottom padding

        bool expanded = false;

        // ── Card shell ────────────────────────────────────────────────────────
        var card = new Panel
        {
            Width = 900, Height = COLLAPSED_H,
            BackColor = Color.White, Margin = new Padding(0, 0, 0, 8)
        };
        card.MouseEnter += (_, _) => { card.BackColor = Color.FromArgb(250, 251, 253); card.Invalidate(); };
        card.MouseLeave += (_, _) => { card.BackColor = Color.White;                   card.Invalidate(); };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path   = RoundPath(new Rectangle(0, 0, card.Width - 2, card.Height - 1), 8);
            using var border = new Pen(Color.FromArgb(226, 229, 236));
            e.Graphics.DrawPath(border, path);
            var sc = overdue ? Theme.AppTheme.Warning : Theme.AppTheme.Primary;
            using var stripe = new SolidBrush(sc);
            e.Graphics.FillRectangle(stripe, 0, 12, 3, card.Height - 24);
            // Divider above expand section when open
            if (expanded)
            {
                using var div = new Pen(Color.FromArgb(235, 238, 244));
                e.Graphics.DrawLine(div, 16, COLLAPSED_H, card.Width - 16, COLLAPSED_H);
            }
        };

        // ── Row 1 — Date · Vet ────────────────────────────────────────────────
        var lblDate = new Label { Text = rec.CreatedAt.ToString("MMMM d, yyyy"), Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(32, 42, 62), AutoSize = true, Left = 16, Top = 10 };
        var lblVet  = new Label { Text = $"Dr. {rec.VetName}", Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(105, 118, 142), AutoSize = true, Top = 12 };

        // Follow-up badge
        Label? fuBadge = null;
        if (hasFU)
        {
            var c = overdue ? Theme.AppTheme.Danger : Theme.AppTheme.Success;
            fuBadge = new Label
            {
                Text = $"↺ Follow-up: {rec.FollowUpDate!.Value:MMM d, yyyy}",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = c, BackColor = Color.FromArgb(22, c.R, c.G, c.B),
                AutoSize = true, Padding = new Padding(6, 3, 6, 3), Top = 10
            };
        }

        var btnView = new Button
        {
            Text = "View →", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Theme.AppTheme.Primary, BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat, AutoSize = true, Cursor = Cursors.Hand, Top = 7
        };
        btnView.FlatAppearance.BorderSize = 0;
        btnView.Click += (_, _) => { using var d = new MedicalRecordDialog(rec, true); d.ShowDialog(); };

        // ── Row 2 — Dx ────────────────────────────────────────────────────────
        var lblDxTag = new Label { Text = "Dx:", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(105, 118, 142), AutoSize = true, Left = 16, Top = 37 };
        var lblDx    = new Label { Text = rec.Diagnosis, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(25, 35, 55), AutoSize = false, Width = 720, Height = 20, Left = 42, Top = 35 };

        // ── Row 3 — Tx ────────────────────────────────────────────────────────
        var lblTxTag = new Label { Text = "Tx:", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(105, 118, 142), AutoSize = true, Left = 16, Top = 62 };
        var lblTx    = new Label { Text = rec.Treatment, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(65, 80, 105), AutoSize = false, Width = 680, Height = 18, Left = 42, Top = 61 };

        // ── Expand toggle button ─────────────────────────────────────────────
        Button? btnToggle = null;
        if (canExpand)
        {
            btnToggle = new Button
            {
                Text = "▼  Notes & Meds", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 115, 142), BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat, AutoSize = true, Cursor = Cursors.Hand,
                Top = COLLAPSED_H - 26
            };
            btnToggle.FlatAppearance.BorderSize = 0;
        }

        // ── Expanded section ─────────────────────────────────────────────────
        Panel? expandSection = null;
        if (canExpand)
        {
            expandSection = new Panel
            {
                Left = 0, Top = COLLAPSED_H, Width = card.Width, Height = expandH,
                BackColor = Color.White, Visible = false
            };

            int ey = 12;

            // Notes block
            if (hasNotes)
            {
                expandSection.Controls.Add(new Label { Text = "CLINICAL NOTES", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(128, 140, 165), AutoSize = true, Left = 16, Top = ey });
                ey += 20;
                var notesTxt = new Label
                {
                    Text = rec.Notes, Font = new Font("Segoe UI", 9f),
                    ForeColor = Color.FromArgb(50, 65, 90),
                    AutoSize = true, MaximumSize = new Size(card.Width - 32, 0),
                    Left = 16, Top = ey
                };
                expandSection.Controls.Add(notesTxt);
                ey += notesTextHeight + 20;
            }

            // Medications block
            if (hasMeds)
            {
                expandSection.Controls.Add(new Label { Text = "MEDICATIONS PRESCRIBED", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(128, 140, 165), AutoSize = true, Left = 16, Top = ey });
                ey += 20;

                foreach (var med in meds)
                {
                    expandSection.Controls.Add(new Label { Text = "●", Font = new Font("Segoe UI", 7f), ForeColor = Theme.AppTheme.Primary, AutoSize = true, Left = 16, Top = ey + 2 });
                    expandSection.Controls.Add(new Label
                    {
                        Text = $"{med.MedicationName}  ·  {med.Dosage}",
                        Font = new Font("Segoe UI", 9f),
                        ForeColor = Color.FromArgb(25, 38, 60),
                        AutoSize = true, Left = 30, Top = ey
                    });
                    ey += 24;
                }
            }

            card.Controls.Add(expandSection);

            btnToggle!.Click += (_, _) =>
            {
                expanded = !expanded;
                expandSection.Visible = expanded;
                card.Height = expanded ? COLLAPSED_H + expandH : COLLAPSED_H;
                btnToggle.Text = expanded ? "▲  Less" : "▼  Notes & Meds";
                card.Invalidate();
                owner.PerformLayout();
            };
        }

        // ── Assemble ─────────────────────────────────────────────────────────
        var all = new List<Control> { lblDate, lblVet, btnView, lblDxTag, lblDx, lblTxTag, lblTx };
        if (fuBadge   != null) all.Add(fuBadge);
        if (btnToggle != null) all.Add(btnToggle);
        card.Controls.AddRange(all.ToArray());

        card.Layout += (_, _) =>
        {
            lblDate.Left  = 16; lblDate.Top = 10;
            lblVet.Left   = lblDate.Right + 14; lblVet.Top = 12;
            btnView.Left  = card.Width - btnView.Width - 14; btnView.Top = 8;
            if (fuBadge != null)   { fuBadge.Left = btnView.Left - fuBadge.Width - 10; fuBadge.Top = 10; }
            if (btnToggle != null) { btnToggle.Left = card.Width - btnToggle.Width - 14; btnToggle.Top = COLLAPSED_H - 27; }
            if (expandSection != null) expandSection.Width = card.Width;
        };

        card.DoubleClick += (_, _) => { using var d = new MedicalRecordDialog(rec, true); d.ShowDialog(); };
        return card;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 3 — APPOINTMENTS  (grid + selected-row detail panel)
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildAppointmentsTab()
    {
        var wrapper = new Panel { BackColor = Color.White };
        wrapper.Controls.Add(SubHeader(
            $"{_appts.Count} appointment{(_appts.Count == 1 ? "" : "s")} total  ·  click a row to see detail and linked medical record"));

        // ── Detail panel (bottom, hidden until row selected) ─────────────────
        var detail = new Panel
        {
            Dock = DockStyle.Bottom, Height = 0,
            BackColor = Color.FromArgb(249, 250, 252)
        };
        detail.Paint += (_, e) =>
        {
            using var top = new Pen(Color.FromArgb(226, 229, 236));
            e.Graphics.DrawLine(top, 0, 0, detail.Width, 0);
        };

        // ── Grid ─────────────────────────────────────────────────────────────
        var grid = StyledGrid();
        grid.Dock = DockStyle.Fill;

        var apptRows = _appts.Select(a => new
        {
            a.Id,
            Date     = a.AppointmentDate.ToString("MMM d, yyyy  HH:mm"),
            Service  = a.ServiceTypeName,
            Vet      = a.VetName,
            Duration = $"{a.Duration} min",
            Status   = a.Status,
            Notes    = string.IsNullOrWhiteSpace(a.Notes) ? "—" : a.Notes
        }).ToList();

        grid.DataSource = apptRows;
        if (grid.Columns["Id"] != null) grid.Columns["Id"].Visible = false;
        SetCols(grid, ("Date", 148), ("Service", 0), ("Vet", 152), ("Duration", 78), ("Status", 108), ("Notes", 170));

        // Status cell colour
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Status" || e.Value == null) return;
            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            e.CellStyle.ForeColor = e.Value.ToString() switch
            {
                "Completed"   => Theme.AppTheme.Success,
                "Cancelled"   => Theme.AppTheme.Danger,
                "In Progress" => Theme.AppTheme.Warning,
                _             => Theme.AppTheme.Accent
            };
        };

        // Row click → populate detail panel
        grid.SelectionChanged += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            int id   = (int)grid.SelectedRows[0].Cells["Id"].Value;
            var appt = _appts.FirstOrDefault(a => a.Id == id);
            if (appt == null) return;

            // Linked medical record (appointment_id match)
            var linkedRec = _records.FirstOrDefault(r => r.AppointmentId == appt.Id);
            int panelH    = linkedRec != null ? 120 : 80;

            detail.Height = panelH;
            detail.Controls.Clear();

            int x = 16, y = 10;

            // ── Appointment meta row ──────────────────────────────────────────
            detail.Controls.Add(new Label { Text = "APPOINTMENT DETAIL", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(128, 140, 165), AutoSize = true, Left = x, Top = y });
            y += 22;

            var metaLine = new Label
            {
                Text = $"{appt.ServiceTypeName}  ·  {appt.VetName}  ·  {appt.AppointmentDate:MMM d, yyyy  HH:mm}  ·  {appt.Duration} min  ·  {appt.Status}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 42, 65),
                AutoSize = true, Left = x, Top = y
            };
            detail.Controls.Add(metaLine);
            y += 20;

            if (!string.IsNullOrWhiteSpace(appt.Notes) && appt.Notes != "—")
            {
                detail.Controls.Add(new Label { Text = appt.Notes, Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(75, 90, 118), AutoSize = false, Width = detail.Width - 32, Height = 18, Left = x, Top = y });
                y += 22;
            }

            // ── Linked medical record ─────────────────────────────────────────
            if (linkedRec != null)
            {
                y += 6;
                detail.Controls.Add(new Label { Text = "LINKED MEDICAL RECORD", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Theme.AppTheme.Primary, AutoSize = true, Left = x, Top = y });
                y += 20;

                var meds = DataStore.GetRecordMedications(linkedRec.Id);
                string rx = meds.Any() ? string.Join("  ·  ", meds.Select(m => m.MedicationName)) : "None";
                string linkedText = $"{linkedRec.Diagnosis}  →  {linkedRec.Treatment}  →  Rx: {rx}";
                
                var lblLinked = new Label { 
                    Text = linkedText, 
                    Font = new Font("Segoe UI", 8.5f), 
                    ForeColor = Color.FromArgb(65, 80, 105), 
                    AutoSize = false, Width = detail.Width - 32, Height = 40, Left = x, Top = y,
                    Cursor = Cursors.Hand
                };
                lblLinked.MouseEnter += (_, _) => { lblLinked.ForeColor = Theme.AppTheme.Primary; lblLinked.Font = new Font("Segoe UI", 8.5f, FontStyle.Underline); };
                lblLinked.MouseLeave += (_, _) => { lblLinked.ForeColor = Color.FromArgb(65, 80, 105); lblLinked.Font = new Font("Segoe UI", 8.5f); };
                lblLinked.Click += (_, _) => { using var d = new MedicalRecordDialog(linkedRec, true); d.ShowDialog(); };
                
                detail.Controls.Add(lblLinked);
            }
        };

        WrapGrid(wrapper, grid, "No appointments on record.");
        wrapper.Controls.Add(detail);
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 4 — CBC RESULTS  (all 13 parameters + leukogram detail panel)
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildCbcTab()
    {
        var wrapper = new Panel { BackColor = Color.White };
        wrapper.Controls.Add(SubHeader(
            "All CBC parameters  ·  ✓ normal  ↑ high  ↓ low  ·  click a row for leukogram breakdown  ·  double-click to edit"));

        // ── Colour legend ─────────────────────────────────────────────────────
        var legend = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(249, 250, 252) };
        legend.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(232, 235, 240)); e.Graphics.DrawLine(p, 0, legend.Height - 1, legend.Width, legend.Height - 1); };
        var legItems = new[] { ("● Normal ✓", Theme.AppTheme.Success), ("● High ↑", Theme.AppTheme.Danger), ("● Low ↓", Color.FromArgb(0, 110, 200)), ("Ref: canine normals", Color.FromArgb(150, 162, 178)) };
        var legLabels = legItems.Select(li => new Label { Text = li.Item1, Font = new Font("Segoe UI", 8f), ForeColor = li.Item2, AutoSize = true, Top = 7 }).ToArray();
        legend.Controls.AddRange(legLabels);
        legend.Resize += (_, _) => { int lx = 16; foreach (var l in legLabels) { l.Left = lx; lx += l.Width + 20; } };
        wrapper.Controls.Add(legend);

        // ── Leukogram detail panel (bottom) ───────────────────────────────────
        var leukoPanel = new Panel { Dock = DockStyle.Bottom, Height = 0, BackColor = Color.FromArgb(249, 250, 252) };
        leukoPanel.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(226, 229, 236)); e.Graphics.DrawLine(p, 0, 0, leukoPanel.Width, 0); };

        // ── Grid — ALL 13 parameters ─────────────────────────────────────────
        var grid = StyledGrid();
        grid.Dock = DockStyle.Fill;

        var rows = _cbcList.Select(c => new
        {
            c.Id,
            Date  = c.TestDate.ToString("MMM d, yyyy"),
            // Erythrogram
            WBC   = Flag(c.Wbc,  CbcRef["WBC"].Lo, CbcRef["WBC"].Hi),
            RBC   = Flag(c.Rbc,  CbcRef["RBC"].Lo, CbcRef["RBC"].Hi),
            HGB   = Flag(c.Hgb,  CbcRef["HGB"].Lo, CbcRef["HGB"].Hi),
            HCT   = Flag(c.Hct,  CbcRef["HCT"].Lo, CbcRef["HCT"].Hi),
            MCV   = Flag(c.Mcv,  CbcRef["MCV"].Lo, CbcRef["MCV"].Hi),
            MCH   = Flag(c.Mch,  CbcRef["MCH"].Lo, CbcRef["MCH"].Hi),
            MCHC  = Flag(c.Mchc, CbcRef["MCHC"].Lo, CbcRef["MCHC"].Hi),
            // Platelets
            PLT   = Flag(c.Plt,  CbcRef["PLT"].Lo, CbcRef["PLT"].Hi),
            // Leukogram (shown as % — no strict ref in display; detail panel shows them)
            NEU   = $"{c.Neu:F0}%",
            LYM   = $"{c.Lym:F0}%",
            MON   = $"{c.Mon:F0}%",
            EOS   = $"{c.Eos:F0}%",
            BAS   = $"{c.Bas:F0}%",
            Remarks = string.IsNullOrWhiteSpace(c.Remarks) ? "—" : c.Remarks
        }).ToList();

        grid.DataSource = rows;
        if (grid.Columns["Id"] != null) grid.Columns["Id"].Visible = false;

        if (grid.Columns["NEU"] != null) grid.Columns["NEU"].HeaderText = "NEU%";
        if (grid.Columns["LYM"] != null) grid.Columns["LYM"].HeaderText = "LYM%";
        if (grid.Columns["MON"] != null) grid.Columns["MON"].HeaderText = "MON%";
        if (grid.Columns["EOS"] != null) grid.Columns["EOS"].HeaderText = "EOS%";
        if (grid.Columns["BAS"] != null) grid.Columns["BAS"].HeaderText = "BAS%";

        SetCols(grid,
            ("Date", 110),
            ("WBC", 68), ("RBC", 68), ("HGB", 68), ("HCT", 68), ("MCV", 68), ("MCH", 68), ("MCHC", 68), ("PLT", 68),
            ("NEU", 50), ("LYM", 50), ("MON", 50), ("EOS", 50), ("BAS", 50),
            ("Remarks", 0));

        // Column tooltips with units + reference
        var tips = new Dictionary<string, string>
        {
            ["WBC"]  = "White Blood Cells (10⁹/L)      Ref: 6.0–17.0",
            ["RBC"]  = "Red Blood Cells (10¹²/L)       Ref: 5.5–8.5",
            ["HGB"]  = "Hemoglobin (g/dL)               Ref: 12.0–18.0",
            ["HCT"]  = "Hematocrit (%)                  Ref: 37–55",
            ["MCV"]  = "Mean Corp. Volume (fL)           Ref: 60–77",
            ["MCH"]  = "Mean Corp. Hemoglobin (pg)       Ref: 19.5–24.5",
            ["MCHC"] = "Mean Corp. Hgb Conc (g/dL)       Ref: 31.0–36.0",
            ["PLT"]  = "Platelets (10⁹/L)               Ref: 200–500",
            ["NEU"]  = "Neutrophils % differential",
            ["LYM"]  = "Lymphocytes % differential",
            ["MON"]  = "Monocytes % differential",
            ["EOS"]  = "Eosinophils % differential",
            ["BAS"]  = "Basophils % differential",
        };
        foreach (var (col, tip) in tips)
            if (grid.Columns[col] != null) grid.Columns[col].ToolTipText = tip;

        // Colour code flagged erythrogram + platelet cells
        var flaggedCols = new HashSet<string> { "WBC", "RBC", "HGB", "HCT", "MCV", "MCH", "MCHC", "PLT" };
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            var col = grid.Columns[e.ColumnIndex].Name;
            if (flaggedCols.Contains(col))
            {
                var s = e.Value.ToString() ?? "";
                if      (s.Contains('↑')) { e.CellStyle.ForeColor = Theme.AppTheme.Danger;          e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold); }
                else if (s.Contains('↓')) { e.CellStyle.ForeColor = Color.FromArgb(0, 100, 195);    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold); }
                else if (s.Contains('✓')) { e.CellStyle.ForeColor = Theme.AppTheme.Success; }
            }
        };

        // Row click → populate leukogram detail panel
        grid.SelectionChanged += (_, _) =>
        {
            if (grid.SelectedRows.Count == 0) return;
            int id = (int)grid.SelectedRows[0].Cells["Id"].Value;
            var c  = _cbcList.FirstOrDefault(x => x.Id == id);
            if (c == null) return;

            leukoPanel.Height = 96;
            leukoPanel.Controls.Clear();

            int lx = 16, ly = 10;
            leukoPanel.Controls.Add(new Label
            {
                Text = $"LEUKOGRAM — {c.TestDate:MMMM d, yyyy}",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(128, 140, 165), AutoSize = true, Left = lx, Top = ly
            });
            ly += 22;

            // 5-cell differential bars (visual percentage)
            var diffs = new[] { ("NEU", c.Neu, 40m, 75m), ("LYM", c.Lym, 20m, 45m), ("MON", c.Mon, 2m, 9m), ("EOS", c.Eos, 0m, 7m), ("BAS", c.Bas, 0m, 2m) };
            int barX = lx;
            foreach (var (name, val, lo, hi) in diffs)
            {
                var valClr = val > hi ? Theme.AppTheme.Danger : val < lo ? Color.FromArgb(0, 100, 195) : Theme.AppTheme.Success;
                var chip = new Panel { Width = 120, Height = 42, Left = barX, Top = ly, BackColor = Color.Transparent };
                chip.Controls.Add(new Label { Text = name, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(128, 140, 165), AutoSize = true, Left = 0, Top = 0 });
                chip.Controls.Add(new Label { Text = $"{val:F0}%", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = valClr, AutoSize = true, Left = 0, Top = 14 });
                chip.Controls.Add(new Label { Text = $"({lo}–{hi})", Font = new Font("Segoe UI", 7f), ForeColor = Color.FromArgb(158, 168, 182), AutoSize = true, Left = 0, Top = 32 });
                leukoPanel.Controls.Add(chip);
                barX += 126;
            }

            // Remarks
            if (!string.IsNullOrWhiteSpace(c.Remarks) && c.Remarks != "—")
            {
                leukoPanel.Controls.Add(new Label
                {
                    Text = $"Remarks: {c.Remarks}",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(90, 105, 130),
                    AutoSize = false, Width = leukoPanel.Width - barX - 20, Height = 42,
                    Left = barX, Top = ly
                });
            }
        };

        // Double-click → full CBC dialog
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            int id = (int)grid.Rows[e.RowIndex].Cells["Id"].Value;
            var c  = _cbcList.FirstOrDefault(x => x.Id == id);
            if (c != null) { using var d = new CbcDialog(c); d.ShowDialog(); }
        };

        WrapGrid(wrapper, grid, "No CBC results on record.\nClick \"+ CBC\" above to add lab results.");
        wrapper.Controls.Add(leukoPanel);
        return wrapper;
    }

    private static string Flag(decimal val, decimal lo, decimal hi)
        => val > hi ? $"{val:F2} ↑" : val < lo ? $"{val:F2} ↓" : $"{val:F2} ✓";

    // ════════════════════════════════════════════════════════════════════════
    // TAB 5 — MEDICATIONS
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildMedicationsTab()
    {
        var wrapper = new Panel { BackColor = Color.White };
        wrapper.Controls.Add(SubHeader("All medications prescribed across every visit record"));

        var grid = StyledGrid();
        grid.Dock = DockStyle.Fill;

        var meds = _records.SelectMany(r =>
            DataStore.GetRecordMedications(r.Id).Select(m => new
            {
                Date       = r.CreatedAt.ToString("MMM d, yyyy"),
                Medication = m.MedicationName,
                Dosage     = m.Dosage,
                Notes      = string.IsNullOrWhiteSpace(m.Notes) ? "—" : m.Notes,
                Vet        = r.VetName
            })).OrderByDescending(m => m.Date).ToList();

        grid.DataSource = meds;
        SetCols(grid, ("Date", 110), ("Medication", 0), ("Dosage", 155), ("Vet", 150), ("Notes", 210));
        WrapGrid(wrapper, grid, "No medications have been recorded yet.");
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 6 — NOTES
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildNotesTab()
    {
        var wrapper = new Panel { BackColor = Color.White };
        wrapper.Controls.Add(SubHeader("General clinical notes and owner remarks for this patient"));

        bool has = !string.IsNullOrWhiteSpace(_pet.Notes);
        var rtb = new RichTextBox
        {
            Dock        = DockStyle.Fill,
            ReadOnly    = true,
            BackColor   = Color.White,
            BorderStyle = BorderStyle.None,
            Font        = new Font("Segoe UI", 10.5f),
            ForeColor   = has ? Color.FromArgb(32, 42, 62) : Color.FromArgb(172, 182, 198),
            Text        = has ? _pet.Notes : "No clinical notes recorded for this patient.",
            ScrollBars  = RichTextBoxScrollBars.Vertical,
            Padding     = new Padding(18, 14, 18, 0)
        };
        wrapper.Controls.Add(rtb);
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // ── SHARED UI HELPERS ────────────────────────────────────────────────────
    // ════════════════════════════════════════════════════════════════════════

    private static DataGridView StyledGrid()
    {
        var g = new DataGridView
        {
            ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            ScrollBars = ScrollBars.Both, ShowCellToolTips = true, Cursor = Cursors.Hand
        };
        UIHelper.StyleGrid(g);
        g.RowTemplate.Height = 34;
        return g;
    }

    private static void SetCols(DataGridView g, params (string Name, int Width)[] cols)
    {
        foreach (var (name, w) in cols)
        {
            if (g.Columns[name] == null) continue;
            if (w == 0) g.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            else        g.Columns[name].Width         = w;
        }
    }

    /// <summary>Adds the grid to wrapper and overlays an empty-state label.</summary>
    private static void WrapGrid(Panel wrapper, DataGridView grid, string emptyMsg)
    {
        var lbl = UIHelper.CreateEmptyDataLabel(emptyMsg);
        lbl.Font = new Font("Segoe UI", 10.5f, FontStyle.Italic);
        lbl.Visible = false;
        wrapper.Controls.Add(grid);
        wrapper.Controls.Add(lbl);
        lbl.BringToFront();
        grid.DataBindingComplete += (_, _) =>
        {
            bool empty = grid.Rows.Count == 0;
            lbl.Visible  = empty;
            grid.Visible = !empty;
        };
    }

    private static Panel SubHeader(string text)
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(251, 252, 253) };
        bar.Paint += (_, e) =>
        {
            using var p = new Pen(Color.FromArgb(232, 235, 241));
            e.Graphics.DrawLine(p, 0, bar.Height - 1, bar.Width, bar.Height - 1);
        };
        bar.Controls.Add(new Label
        {
            Text = text, Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(115, 130, 152), AutoSize = true, Left = 14, Top = 12
        });
        return bar;
    }

    private static Label SectionLbl(string text)
        => new Label
        {
            Text = text, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(148, 160, 178), AutoSize = true,
            Margin = new Padding(0, 14, 0, 6)
        };

    private static Label EmptyLbl(string text)
        => new Label
        {
            Text = text, Font = new Font("Segoe UI", 10.5f, FontStyle.Italic),
            ForeColor = Color.FromArgb(158, 168, 185), AutoSize = true,
            Margin = new Padding(0, 18, 0, 0)
        };

    private static Button TopBtn(string text, Color bg)
    {
        var btn = new Button
        {
            Text = text, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White, BackColor = bg, FlatStyle = FlatStyle.Flat,
            Height = 32, AutoSize = false, Cursor = Cursors.Hand,
            Width = TextRenderer.MeasureText(text, new Font("Segoe UI", 8.5f, FontStyle.Bold)).Width + 24
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(bg, 0.1f);
        return btn;
    }

    // ── Geometric helpers ────────────────────────────────────────────────────
    private static GraphicsPath RoundPath(Rectangle rc, int r)
    {
        int d = r * 2;
        var p = new GraphicsPath();
        p.AddArc(rc.Left,         rc.Top,          d, d, 180, 90);
        p.AddArc(rc.Right  - d,   rc.Top,          d, d, 270, 90);
        p.AddArc(rc.Right  - d,   rc.Bottom - d,   d, d,   0, 90);
        p.AddArc(rc.Left,         rc.Bottom - d,   d, d,  90, 90);
        p.CloseFigure();
        return p;
    }

    // ── String helpers ───────────────────────────────────────────────────────
    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "…";

    private string GetAgeWithDate()
    {
        if (_pet.DateOfBirth == null) return "Unknown";
        var diff = DateTime.Today - _pet.DateOfBirth.Value;
        int y = diff.Days / 365, m = (diff.Days % 365) / 30;
        string age = y > 0 ? $"{y}y {m}m" : $"{m} months";
        return $"{age}  ({_pet.DateOfBirth.Value:MMM d, yyyy})";
    }
}
