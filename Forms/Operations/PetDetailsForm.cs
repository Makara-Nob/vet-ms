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
            Text = "← Back", Font = new Font("Segoe UI", 9f),
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

        // ── Tabs ─────────────────────────────────────────────────────────────
        var tabWrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(14, 10, 14, 14) };
        var tabs    = BuildTabs();
        tabs.Dock   = DockStyle.Fill;
        tabWrap.Controls.Add(tabs);
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

    // ════════════════════════════════════════════════════════════════════════
    // ── TABS ────────────────────────────────────────────────────────────────
    // ════════════════════════════════════════════════════════════════════════
    private TabControl BuildTabs()
    {
        var tabs = new TabControl
        {
            Font = new Font("Segoe UI", 9.5f),
            Padding = new Point(16, 7),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(0, 36)
        };
        tabs.DrawItem += DrawTab;
        tabs.TabPages.Add(MkTab("Overview",        BuildOverview()));
        tabs.TabPages.Add(MkTab("Medical Records", BuildMedRecordsTab()));
        tabs.TabPages.Add(MkTab("Appointments",    BuildAppointmentsTab()));
        tabs.TabPages.Add(MkTab("CBC Results",     BuildCbcTab()));
        tabs.TabPages.Add(MkTab("Medications",     BuildMedicationsTab()));
        tabs.TabPages.Add(MkTab("Notes",           BuildNotesTab()));
        if (_startTab >= 0 && _startTab < tabs.TabCount)
            tabs.SelectedIndex = _startTab;
        return tabs;
    }

    private static void DrawTab(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tc) return;
        bool sel = e.Index == tc.SelectedIndex;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillRectangle(new SolidBrush(sel ? Color.White : Color.FromArgb(248, 249, 251)), e.Bounds);
        if (sel)
        {
            using var pen = new Pen(Theme.AppTheme.Primary, 2.5f);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }
        var clr = sel ? Theme.AppTheme.Primary : Color.FromArgb(102, 114, 132);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        using var br = new SolidBrush(clr);
        e.Graphics.DrawString(tc.TabPages[e.Index].Text,
            new Font("Segoe UI", 9.5f, sel ? FontStyle.Bold : FontStyle.Regular), br, e.Bounds, sf);
    }

    private static TabPage MkTab(string title, Control body)
    {
        var p = new TabPage(title) { BackColor = Color.White, Padding = new Padding(0) };
        body.Dock = DockStyle.Fill; p.Controls.Add(body); return p;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 1 — OVERVIEW
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildOverview()
    {
        var outer  = new Panel { BackColor = Color.White };
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var stack  = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            Padding = new Padding(18, 14, 18, 24)
        };

        // ── Patient snapshot cards ────────────────────────────────────────────
        stack.Controls.Add(SectionLbl("PATIENT SNAPSHOT"));

        var snapRow = new FlowLayoutPanel
        {
            AutoSize = true, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, Margin = new Padding(0, 0, 0, 4)
        };
        var lastRec = _records.FirstOrDefault();
        var lastCbc = _cbcList.FirstOrDefault();
        snapRow.Controls.Add(SnapCard("Last Diagnosis",
            lastRec?.Diagnosis ?? "No records yet.",
            lastRec?.CreatedAt.ToString("MMM d, yyyy") ?? "—",
            Theme.AppTheme.Primary));
        snapRow.Controls.Add(SnapCard("Last CBC Summary",
            lastCbc != null
                ? $"WBC {lastCbc.Wbc}  ·  HGB {lastCbc.Hgb}  ·  PLT {lastCbc.Plt}  ·  HCT {lastCbc.Hct}%"
                : "No CBC on record.",
            lastCbc?.TestDate.ToString("MMM d, yyyy") ?? "—",
            Color.FromArgb(130, 30, 165)));
        stack.Controls.Add(snapRow);

        // ── Timeline ─────────────────────────────────────────────────────────
        int totalEvents = _records.Count + _appts.Count + _cbcList.Count;
        stack.Controls.Add(SectionLbl($"ACTIVITY TIMELINE  ·  {Math.Min(totalEvents, 40)} EVENTS"));

        var events = new List<(DateTime Date, string Kind, string Title, string Detail, Color Accent)>();

        foreach (var r in _records)
            events.Add((r.CreatedAt, "VISIT", $"Medical Record  ·  {r.VetName}", r.Diagnosis, Theme.AppTheme.Primary));

        foreach (var a in _appts)
        {
            var ac = a.Status switch
            {
                "Completed"   => Theme.AppTheme.Success,
                "Cancelled"   => Theme.AppTheme.Danger,
                "In Progress" => Theme.AppTheme.Warning,
                _             => Theme.AppTheme.Accent
            };
            events.Add((a.AppointmentDate, "APPT", $"{a.ServiceTypeName}  ·  {a.VetName}", a.Status, ac));
        }

        foreach (var c in _cbcList)
            events.Add((c.TestDate, "CBC", "Complete Blood Count",
                $"WBC {c.Wbc}  ·  HGB {c.Hgb}  ·  PLT {c.Plt}", Color.FromArgb(130, 30, 165)));

        var sorted = events.OrderByDescending(e => e.Date).Take(40).ToList();

        if (!sorted.Any())
        {
            stack.Controls.Add(new Label
            {
                Text = "No activity on record for this patient.",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(158, 168, 185), AutoSize = true, Margin = new Padding(0, 18, 0, 0)
            });
        }
        else
        {
            string? curGroup = null;
            foreach (var ev in sorted)
            {
                var grp = ev.Date.ToString("MMMM yyyy").ToUpperInvariant();
                if (grp != curGroup) { curGroup = grp; stack.Controls.Add(SectionLbl(grp)); }
                stack.Controls.Add(TimelineRow(ev.Date, ev.Kind, ev.Title, ev.Detail, ev.Accent));
            }
        }

        scroll.Controls.Add(stack);
        outer.Controls.Add(scroll);
        return outer;
    }

    private static Panel SnapCard(string title, string body, string dateStr, Color accent)
    {
        var card = new Panel
        {
            Width = 370, Height = 78,
            BackColor = Color.FromArgb(251, 252, 254),
            Margin = new Padding(0, 0, 12, 0)
        };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            using var pen  = new Pen(Color.FromArgb(226, 229, 236));
            e.Graphics.DrawPath(pen, path);
            // Top colour bar
            using var br = new SolidBrush(Color.FromArgb(45, accent.R, accent.G, accent.B));
            e.Graphics.FillRectangle(br, 0, 0, card.Width, 3);
        };
        card.Controls.Add(new Label { Text = title.ToUpperInvariant(), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = accent, AutoSize = true, Left = 14, Top = 10 });
        card.Controls.Add(new Label { Text = body, Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(32, 42, 62), Left = 14, Top = 28, Width = card.Width - 28, Height = 30, AutoSize = false });
        if (!string.IsNullOrEmpty(dateStr) && dateStr != "—")
            card.Controls.Add(new Label { Text = dateStr, Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(150, 162, 178), AutoSize = true, Left = 14, Top = 60 });
        return card;
    }

    private static Panel TimelineRow(DateTime date, string kind, string title, string detail, Color accent)
    {
        var row = new Panel
        {
            Width = 900, Height = 58,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 4),
            Cursor = Cursors.Hand
        };
        row.MouseEnter += (_, _) => { row.BackColor = Color.FromArgb(250, 251, 253); row.Invalidate(); };
        row.MouseLeave += (_, _) => { row.BackColor = Color.White;                   row.Invalidate(); };
        row.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var border = new Pen(Color.FromArgb(230, 234, 240));
            using var path   = RoundPath(new Rectangle(0, 0, row.Width - 2, row.Height - 1), 7);
            e.Graphics.DrawPath(border, path);
            // Accent dot (left)
            using var dot = new SolidBrush(accent);
            e.Graphics.FillEllipse(dot, 13, row.Height / 2 - 5, 10, 10);
        };

        var badge = new Label
        {
            Text = kind, Font = new Font("Segoe UI", 6.5f, FontStyle.Bold),
            ForeColor = accent, BackColor = Color.FromArgb(22, accent.R, accent.G, accent.B),
            AutoSize = true, Padding = new Padding(5, 2, 5, 2)
        };
        var lblDate = new Label
        {
            Text = date.ToString("MMM d, yyyy"),
            Font = new Font("Segoe UI", 7.5f), ForeColor = Color.FromArgb(148, 160, 178), AutoSize = true
        };
        var lblTitle = new Label
        {
            Text = title, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(28, 38, 58), AutoSize = true
        };
        var lblDetail = new Label
        {
            Text = Truncate(detail, 90),
            Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(112, 124, 145), AutoSize = true
        };

        row.Controls.AddRange(new Control[] { badge, lblDate, lblTitle, lblDetail });
        row.Layout += (_, _) =>
        {
            badge.Left    = 32;                     badge.Top    = 10;
            lblDate.Left  = badge.Right + 8;        lblDate.Top  = 12;
            lblTitle.Left = 32;                     lblTitle.Top = 30;
            lblDetail.Left = lblTitle.Right + 10;   lblDetail.Top = 33;
        };
        return row;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 2 — MEDICAL RECORDS (card layout)
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildMedRecordsTab()
    {
        var outer = new Panel { BackColor = Color.White };
        outer.Controls.Add(SubHeader("Clinical visit records — double-click a card to view full detail"));

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
        var stack  = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true,
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            Padding = new Padding(16, 12, 16, 24)
        };

        if (!_records.Any())
        {
            stack.Controls.Add(EmptyLbl("No medical records yet.\nClick \"+ Medical Record\" to add one."));
        }
        else
        {
            foreach (var rec in _records)
                stack.Controls.Add(MedRecCard(rec));
        }

        scroll.Controls.Add(stack);
        outer.Controls.Add(scroll);
        return outer;
    }

    private Panel MedRecCard(MedicalRecord rec)
    {
        bool hasFU  = rec.FollowUpDate.HasValue;
        bool overdue = hasFU && rec.FollowUpDate!.Value.Date < DateTime.Today;

        var card = new Panel
        {
            Width = 900, Height = 94,
            BackColor = Color.White,
            Margin = new Padding(0, 0, 0, 8),
            Cursor = Cursors.Hand
        };
        card.MouseEnter += (_, _) => { card.BackColor = Color.FromArgb(250, 251, 253); card.Invalidate(); };
        card.MouseLeave += (_, _) => { card.BackColor = Color.White;                   card.Invalidate(); };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path  = RoundPath(new Rectangle(0, 0, card.Width - 2, card.Height - 1), 8);
            using var pen   = new Pen(Color.FromArgb(226, 229, 236));
            e.Graphics.DrawPath(pen, path);
            var stripeColor = overdue ? Theme.AppTheme.Warning : Theme.AppTheme.Primary;
            using var stripe = new SolidBrush(stripeColor);
            e.Graphics.FillRectangle(stripe, 0, 12, 3, card.Height - 24);
        };

        // Row 1: date + vet + optional follow-up badge + view button
        var lblDate = new Label
        {
            Text = rec.CreatedAt.ToString("MMMM d, yyyy"),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold), ForeColor = Color.FromArgb(38, 48, 68), AutoSize = true, Left = 16, Top = 10
        };
        var lblVet = new Label
        {
            Text = $"Dr. {rec.VetName}", Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(105, 118, 142), AutoSize = true, Top = 12
        };
        var btnView = new Button
        {
            Text = "View →", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Theme.AppTheme.Primary, BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat, AutoSize = true, Cursor = Cursors.Hand, Top = 8
        };
        btnView.FlatAppearance.BorderSize = 0;
        btnView.Click += (_, _) => { using var d = new MedicalRecordDialog(rec, true); d.ShowDialog(); };

        // Follow-up badge (optional)
        Label? fuBadge = null;
        if (hasFU)
        {
            var c = overdue ? Theme.AppTheme.Danger : Theme.AppTheme.Success;
            fuBadge = new Label
            {
                Text = $"↺ {rec.FollowUpDate!.Value:MMM d}",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = c, BackColor = Color.FromArgb(25, c.R, c.G, c.B),
                AutoSize = true, Padding = new Padding(6, 3, 6, 3)
            };
        }

        // Row 2: Dx label + diagnosis text
        var lblDxTag = new Label { Text = "Dx:", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(105, 118, 142), AutoSize = true, Left = 16, Top = 36 };
        var lblDx    = new Label
        {
            Text = Truncate(rec.Diagnosis, 110),
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(28, 38, 58),
            AutoSize = false, Width = 700, Height = 20, Left = 42, Top = 34
        };

        // Row 3: Tx label + treatment text
        var lblTxTag = new Label { Text = "Tx:", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = Color.FromArgb(105, 118, 142), AutoSize = true, Left = 16, Top = 60 };
        var lblTx    = new Label
        {
            Text = Truncate(rec.Treatment, 110),
            Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(68, 82, 108),
            AutoSize = false, Width = 640, Height = 18, Left = 42, Top = 59
        };

        var controls = new List<Control> { lblDate, lblVet, btnView, lblDxTag, lblDx, lblTxTag, lblTx };
        if (fuBadge != null) controls.Add(fuBadge);
        card.Controls.AddRange(controls.ToArray());

        card.Layout += (_, _) =>
        {
            lblDate.Left  = 16; lblDate.Top = 10;
            lblVet.Left   = lblDate.Right + 14; lblVet.Top = 12;
            btnView.Left  = card.Width - btnView.Width - 14; btnView.Top = 8;
            if (fuBadge != null)
            {
                fuBadge.Left = btnView.Left - fuBadge.Width - 10;
                fuBadge.Top  = 11;
            }
        };

        card.DoubleClick += (_, _) => { using var d = new MedicalRecordDialog(rec, true); d.ShowDialog(); };
        return card;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 3 — APPOINTMENTS
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildAppointmentsTab()
    {
        var wrapper = new Panel { BackColor = Color.White };
        wrapper.Controls.Add(SubHeader("All appointments — past, present and upcoming"));

        var grid = StyledGrid();
        grid.Dock = DockStyle.Fill;
        grid.DataSource = _appts.Select(a => new
        {
            Date     = a.AppointmentDate.ToString("MMM d, yyyy  HH:mm"),
            Service  = a.ServiceTypeName,
            Vet      = a.VetName,
            Duration = $"{a.Duration} min",
            Status   = a.Status,
            Notes    = string.IsNullOrWhiteSpace(a.Notes) ? "—" : a.Notes
        }).ToList();

        SetCols(grid, ("Date", 148), ("Service", 0), ("Vet", 150), ("Duration", 78), ("Status", 108), ("Notes", 170));

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

        WrapGrid(wrapper, grid, "No appointments on record.");
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 4 — CBC RESULTS  (with reference ranges + colour flagging)
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildCbcTab()
    {
        var wrapper = new Panel { BackColor = Color.White };
        wrapper.Controls.Add(SubHeader(
            "Complete Blood Count  ·  ✓ = normal  ↑ = high (red)  ↓ = low (blue)  ·  double-click to view full panel"));

        // Legend strip
        var legend = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.FromArgb(249, 250, 252) };
        legend.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(232, 235, 240)); e.Graphics.DrawLine(p, 0, legend.Height - 1, legend.Width, legend.Height - 1); };
        var legItems = new[]
        {
            ("● Normal range",   Theme.AppTheme.Success),
            ("● High ↑",         Theme.AppTheme.Danger),
            ("● Low ↓",          Color.FromArgb(0, 110, 200)),
            ("Reference: Canine", Color.FromArgb(155, 165, 180))
        };
        var legLabels = legItems.Select(li => new Label
        {
            Text = li.Item1, Font = new Font("Segoe UI", 8f),
            ForeColor = li.Item2, AutoSize = true, Top = 7
        }).ToArray();
        legend.Controls.AddRange(legLabels);
        legend.Resize += (_, _) => { int x = 16; foreach (var l in legLabels) { l.Left = x; x += l.Width + 20; } };
        wrapper.Controls.Add(legend);

        var grid = StyledGrid();
        grid.Dock = DockStyle.Fill;

        // Build display rows with flagged values
        var rows = _cbcList.Select(c => new
        {
            c.Id,
            Date    = c.TestDate.ToString("MMM d, yyyy"),
            WBC     = Flag(c.Wbc, CbcRef["WBC"].Lo, CbcRef["WBC"].Hi),
            RBC     = Flag(c.Rbc, CbcRef["RBC"].Lo, CbcRef["RBC"].Hi),
            HGB     = Flag(c.Hgb, CbcRef["HGB"].Lo, CbcRef["HGB"].Hi),
            HCT     = Flag(c.Hct, CbcRef["HCT"].Lo, CbcRef["HCT"].Hi),
            PLT     = Flag(c.Plt, CbcRef["PLT"].Lo, CbcRef["PLT"].Hi),
            MCV     = Flag(c.Mcv, CbcRef["MCV"].Lo, CbcRef["MCV"].Hi),
            Remarks = string.IsNullOrWhiteSpace(c.Remarks) ? "—" : c.Remarks
        }).ToList();

        grid.DataSource = rows;
        if (grid.Columns["Id"] != null) grid.Columns["Id"].Visible = false;
        SetCols(grid, ("Date", 110), ("WBC", 82), ("RBC", 82), ("HGB", 82), ("HCT", 78), ("PLT", 78), ("MCV", 78), ("Remarks", 0));

        // Column header tooltips with units & reference
        var tips = new Dictionary<string, string>
        {
            ["WBC"] = "White Blood Cells (10⁹/L)   Ref: 6.0 – 17.0",
            ["RBC"] = "Red Blood Cells (10¹²/L)    Ref: 5.5 – 8.5",
            ["HGB"] = "Hemoglobin (g/dL)            Ref: 12.0 – 18.0",
            ["HCT"] = "Hematocrit (%)               Ref: 37 – 55",
            ["PLT"] = "Platelets (10⁹/L)            Ref: 200 – 500",
            ["MCV"] = "Mean Corp. Volume (fL)        Ref: 60 – 77",
        };
        foreach (var (col, tip) in tips)
            if (grid.Columns[col] != null) grid.Columns[col].ToolTipText = tip;

        // Colour-code cells by flag character
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.Value == null) return;
            var name = grid.Columns[e.ColumnIndex].Name;
            if (name is "WBC" or "RBC" or "HGB" or "HCT" or "PLT" or "MCV")
            {
                var s = e.Value.ToString() ?? "";
                if (s.Contains('↑'))      { e.CellStyle.ForeColor = Theme.AppTheme.Danger;    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold); }
                else if (s.Contains('↓')) { e.CellStyle.ForeColor = Color.FromArgb(0, 105, 195); e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold); }
                else if (s.Contains('✓')) { e.CellStyle.ForeColor = Theme.AppTheme.Success; }
            }
        };

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            int id = (int)grid.Rows[e.RowIndex].Cells["Id"].Value;
            var c  = _cbcList.FirstOrDefault(x => x.Id == id);
            if (c != null) { using var d = new CbcDialog(c); d.ShowDialog(); }
        };

        WrapGrid(wrapper, grid, "No CBC results on record.\nClick \"+ CBC\" above to add lab results.");
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
