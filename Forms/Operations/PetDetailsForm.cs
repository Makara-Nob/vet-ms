using VetMS.Data;
using VetMS.Models;
using System.Drawing.Drawing2D;

namespace VetMS.Forms.Operations;

public class PetDetailsForm : Form
{
    private readonly Pet _pet;
    private readonly Action? _onBack;
    private readonly int _startTab;

    // Live summary data
    private int _totalVisits;
    private int _totalCbc;
    private string _lastVisitStr = "—";
    private string _nextApptStr  = "—";

    public PetDetailsForm(Pet pet, Action? onBack = null, int startTab = 0)
    {
        _pet      = pet;
        _onBack   = onBack;
        _startTab = startTab;
        PreloadCounts();
        InitializeUI();
    }

    // ── Pre-load summary counts so stat cards render correctly ────────────────
    private void PreloadCounts()
    {
        var records = DataStore.GetMedicalRecords().Where(r => r.PetId == _pet.Id).ToList();
        _totalVisits = records.Count;

        var lastRecord = records.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
        if (lastRecord != null)
            _lastVisitStr = lastRecord.CreatedAt.ToString("MMM d, yyyy");

        var appts = DataStore.GetAppointments()
            .Where(a => a.PetId == _pet.Id && a.AppointmentDate >= DateTime.Today && a.Status == "Scheduled")
            .OrderBy(a => a.AppointmentDate)
            .FirstOrDefault();
        if (appts != null)
            _nextApptStr = appts.AppointmentDate.ToString("MMM d, yyyy");

        _totalCbc = DataStore.GetCbcRecords().Count(c => c.PetId == _pet.Id);
    }

    private void InitializeUI()
    {
        Text = $"Patient Profile — {_pet.Name}";
        BackColor = Color.FromArgb(245, 247, 250);
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;

        // Root layout: Sidebar | Content
        var root = new Panel { Dock = DockStyle.Fill };

        root.Controls.Add(BuildContent());
        root.Controls.Add(BuildSidebar());
        Controls.Add(root);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SIDEBAR
    // ════════════════════════════════════════════════════════════════════════
    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Width     = 288,
            Dock      = DockStyle.Left,
            BackColor = Color.White
        };
        // Right border
        sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 229, 235), 1);
            e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
        };

        // ── Profile Header ─────────────────────────────────────────────────
        int headerHeight = 210;
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = headerHeight,
            BackColor = Theme.AppTheme.Primary   // fallback solid color
        };
        // gradient repainted on every resize
        header.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(header.Width, 1), header.Height),
                Theme.AppTheme.Primary,
                Theme.AppTheme.BrandDeep,
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, 0, 0, header.Width, header.Height);
        };

        // Avatar
        int avatarSize = 88;
        var picAvatar = new PictureBox
        {
            Width     = avatarSize,
            Height    = avatarSize,
            SizeMode  = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(240, 245, 250)
        };
        picAvatar.Image = UIHelper.CreateProfilePlaceholder(avatarSize);
        if (_pet.ProfilePicture is { Length: > 0 })
        {
            using var ms = new MemoryStream(_pet.ProfilePicture);
            picAvatar.Image = Image.FromStream(ms);
        }
        var clipPath = new GraphicsPath();
        clipPath.AddEllipse(0, 0, avatarSize, avatarSize);
        picAvatar.Region = new Region(clipPath);
        UIHelper.AttachImageViewer(picAvatar, () => picAvatar.Image);

        // Pet name label — centered, fixed width matches sidebar
        var lblName = new Label
        {
            Text      = _pet.Name,
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize  = false,
            Width     = sidebar.Width,
            Height    = 26,
            BackColor = Color.Transparent
        };

        // Status badge — use a Panel so rounded drawing is clean
        bool   active      = _pet.IsActive;
        Color  badgeColor  = active ? Theme.AppTheme.Success : Theme.AppTheme.Danger;
        string badgeText   = active ? "ACTIVE" : "INACTIVE";
        var    statusBadge = new Panel { Width = 74, Height = 22, BackColor = Color.Transparent, Cursor = Cursors.Default };
        if (!active) statusBadge.Width = 86;
        statusBadge.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path  = RoundRect(new Rectangle(0, 0, statusBadge.Width - 1, statusBadge.Height - 1), 10);
            using var brush = new SolidBrush(badgeColor);
            e.Graphics.FillPath(brush, path);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(badgeText, new Font("Segoe UI", 7.5f, FontStyle.Bold),
                Brushes.White, new RectangleF(0, 0, statusBadge.Width, statusBadge.Height), sf);
        };

        void LayoutHeader()
        {
            picAvatar.Left   = (sidebar.Width - avatarSize) / 2;
            picAvatar.Top    = 24;
            lblName.Left     = 0;
            lblName.Top      = picAvatar.Bottom + 10;
            lblName.Width    = sidebar.Width;
            statusBadge.Left = (sidebar.Width - statusBadge.Width) / 2;
            statusBadge.Top  = lblName.Bottom + 6;
        }

        header.Controls.Add(picAvatar);
        header.Controls.Add(lblName);
        header.Controls.Add(statusBadge);
        header.Resize += (_, _) => LayoutHeader();
        LayoutHeader();
        sidebar.Controls.Add(header);

        // ── Info Rows ──────────────────────────────────────────────────────
        var scroll = new Panel
        {
            Dock      = DockStyle.Fill,
            AutoScroll = true,
            Padding   = new Padding(0, 0, 0, 16)
        };

        var stack = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            AutoSize      = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            Padding       = new Padding(20, 16, 20, 0)
        };

        void AddSection(string title)
        {
            var lbl = new Label
            {
                Text      = title.ToUpperInvariant(),
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 170, 185),
                AutoSize  = true,
                Margin    = new Padding(0, 16, 0, 6)
            };
            stack.Controls.Add(lbl);
        }

        void AddRow(string label, string value, Color? valueColor = null)
        {
            var row = new Panel { Width = 248, Height = 44, Margin = new Padding(0, 0, 0, 2) };

            row.Paint += (_, e) =>
            {
                e.Graphics.Clear(Color.White);
                using var pen = new Pen(Color.FromArgb(241, 243, 246));
                e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
            };

            var lbl = new Label
            {
                Text     = label,
                Font     = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(140, 150, 165),
                Left     = 0, Top = 6,
                AutoSize = true
            };
            var val = new Label
            {
                Text      = string.IsNullOrEmpty(value) ? "—" : value,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = valueColor ?? Color.FromArgb(35, 45, 65),
                Left      = 0, Top = 23,
                AutoSize  = true,
                MaximumSize = new Size(248, 0)
            };
            row.Controls.Add(lbl);
            row.Controls.Add(val);
            stack.Controls.Add(row);
        }

        AddSection("Patient Info");
        AddRow("Species / Breed", $"{_pet.SpeciesName} · {(_pet.BreedName ?? "Mix")}");
        AddRow("Gender", _pet.Gender);
        AddRow("Age / DOB", GetAgeWithDate());
        AddRow("Weight", $"{_pet.Weight:F2} kg");
        AddRow("Color / Coat", string.IsNullOrWhiteSpace(_pet.Color) ? "—" : _pet.Color);
        AddRow("Microchip", string.IsNullOrWhiteSpace(_pet.MicrochipNo) ? "None" : _pet.MicrochipNo);

        AddSection("Owner");
        AddRow("Full Name", _pet.CustomerName);

        var customer = DataStore.GetCustomers().FirstOrDefault(c => c.Id == _pet.CustomerId);
        if (customer != null)
        {
            AddRow("Phone", customer.Phone);
            AddRow("Email", string.IsNullOrEmpty(customer.Email) ? "—" : customer.Email);
        }

        AddSection("Record");
        AddRow("Created", _pet.CreatedAt.ToString("MMM d, yyyy"));
        AddRow("Created By", _pet.CreatedBy ?? "—");
        if (_pet.UpdatedAt.HasValue)
            AddRow("Last Updated", _pet.UpdatedAt.Value.ToString("MMM d, yyyy"));

        scroll.Controls.Add(stack);
        sidebar.Controls.Add(scroll);
        return sidebar;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MAIN CONTENT
    // ════════════════════════════════════════════════════════════════════════
    private Panel BuildContent()
    {
        var content = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250) };

        // ── Top Bar ────────────────────────────────────────────────────────
        var topBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 56,
            BackColor = Color.White,
            Padding   = new Padding(16, 0, 16, 0)
        };
        topBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 229, 235));
            e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
        };

        var btnBack = new Button
        {
            Text      = "← Back to Patients",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(70, 80, 100),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            AutoSize  = true,
            Cursor    = Cursors.Hand,
            Top       = 14
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (_, _) => _onBack?.Invoke();

        var lblBreadcrumb = new Label
        {
            Text      = $"Patients  /  {_pet.Name}",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(140, 150, 165),
            AutoSize  = true,
            Top       = 20
        };

        // Quick action buttons
        var btnNewRecord = CreateTopBtn("+ Medical Record", Theme.AppTheme.Primary);
        btnNewRecord.Click += (_, _) =>
        {
            using var dlg = new MedicalRecordDialog();
            dlg.ShowDialog();
        };

        var btnNewAppt = CreateTopBtn("+ Appointment", Theme.AppTheme.Accent);
        btnNewAppt.Click += (_, _) =>
        {
            using var dlg = new AppointmentDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                Toast.Success("Appointment scheduled.");
        };

        var btnNewCbc = CreateTopBtn("+ CBC", Theme.AppTheme.Success);
        btnNewCbc.Click += (_, _) =>
        {
            using var dlg = new CbcDialog();
            dlg.ShowDialog();
        };

        topBar.Controls.Add(btnBack);
        topBar.Controls.Add(lblBreadcrumb);
        topBar.Controls.Add(btnNewRecord);
        topBar.Controls.Add(btnNewAppt);
        topBar.Controls.Add(btnNewCbc);

        topBar.Resize += (_, _) =>
        {
            btnBack.Left       = 8;
            btnBack.Top        = (topBar.Height - btnBack.Height) / 2;
            lblBreadcrumb.Left = btnBack.Right + 12;
            lblBreadcrumb.Top  = (topBar.Height - lblBreadcrumb.Height) / 2;

            btnNewCbc.Left    = topBar.Width - btnNewCbc.Width - 16;
            btnNewCbc.Top     = (topBar.Height - btnNewCbc.Height) / 2;
            btnNewAppt.Left   = btnNewCbc.Left - btnNewAppt.Width - 8;
            btnNewAppt.Top    = btnNewCbc.Top;
            btnNewRecord.Left = btnNewAppt.Left - btnNewRecord.Width - 8;
            btnNewRecord.Top  = btnNewAppt.Top;
        };

        content.Controls.Add(topBar);

        // ── Stat Cards ─────────────────────────────────────────────────────
        var statsRow = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 104,
            BackColor = Color.FromArgb(245, 247, 250),
            Padding   = new Padding(16, 12, 16, 0)
        };

        var statsFlow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents  = false
        };

        statsFlow.Controls.Add(BuildStatCard("Total Visits",    _totalVisits.ToString(),  "medical records",    Theme.AppTheme.Primary));
        statsFlow.Controls.Add(BuildStatCard("Last Visit",       _lastVisitStr,            "most recent record", Color.FromArgb(0, 120, 215)));
        statsFlow.Controls.Add(BuildStatCard("Next Appointment", _nextApptStr,             "upcoming scheduled", Theme.AppTheme.Warning));
        statsFlow.Controls.Add(BuildStatCard("CBC Tests",        _totalCbc.ToString(),     "lab results total",  Theme.AppTheme.Success));
        statsRow.Controls.Add(statsFlow);
        content.Controls.Add(statsRow);

        // ── Tabs ───────────────────────────────────────────────────────────
        var tabWrapper = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 247, 250),
            Padding   = new Padding(16, 12, 16, 16)
        };

        var tabs = BuildStyledTabs();
        tabs.Dock = DockStyle.Fill;
        tabWrapper.Controls.Add(tabs);
        content.Controls.Add(tabWrapper);

        return content;
    }

    // ════════════════════════════════════════════════════════════════════════
    // STAT CARDS
    // ════════════════════════════════════════════════════════════════════════
    private Panel BuildStatCard(string title, string value, string sub, Color accent)
    {
        // Value font: big for short numbers (e.g. "5"), compact for dates
        float valueFontSize = value.Length <= 3 ? 22f : value.Length <= 6 ? 16f : 12.5f;
        bool  isDate        = value.Length > 3 && value != "—";

        var card = new Panel
        {
            Width     = 210,
            Height    = 80,
            BackColor = Color.White,
            Margin    = new Padding(0, 0, 10, 0),
            Cursor    = Cursors.Default
        };
        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var borderPath = RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            using var borderPen  = new Pen(Color.FromArgb(226, 229, 235));
            e.Graphics.DrawPath(borderPen, borderPath);
            using var accentBrush = new SolidBrush(accent);
            e.Graphics.FillRectangle(accentBrush, 0, 16, 3, card.Height - 32);
        };

        var lblTitle = new Label
        {
            Text      = title.ToUpperInvariant(),
            Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(140, 150, 170),
            AutoSize  = true,
            Left      = 16, Top = 11
        };
        var lblValue = new Label
        {
            Text        = value,
            Font        = new Font("Segoe UI", valueFontSize, FontStyle.Bold),
            ForeColor   = Color.FromArgb(30, 40, 60),
            AutoSize    = false,
            Width       = card.Width - 22,
            Height      = isDate ? 22 : 30,
            Left        = 16,
            Top         = 28
        };
        var lblSub = new Label
        {
            Text      = sub,
            Font      = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(170, 178, 192),
            AutoSize  = true,
            Left      = 16, Top = 60
        };

        card.Controls.Add(lblTitle);
        card.Controls.Add(lblValue);
        card.Controls.Add(lblSub);
        return card;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TABS
    // ════════════════════════════════════════════════════════════════════════
    private TabControl BuildStyledTabs()
    {
        var tabs = new TabControl
        {
            Font    = new Font("Segoe UI", 9.5f),
            Padding = new Point(18, 8),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            ItemSize = new Size(0, 36)
        };
        tabs.DrawItem += DrawTabItem;

        tabs.TabPages.Add(MakeTabPage("Overview",        BuildOverview()));
        tabs.TabPages.Add(MakeTabPage("Medical Records", BuildMedicalRecords()));
        tabs.TabPages.Add(MakeTabPage("Appointments",    BuildAppointments()));
        tabs.TabPages.Add(MakeTabPage("CBC Results",     BuildCbcResults()));
        tabs.TabPages.Add(MakeTabPage("Medications",     BuildMedications()));
        tabs.TabPages.Add(MakeTabPage("Notes",           BuildNotes()));

        if (_startTab >= 0 && _startTab < tabs.TabCount)
            tabs.SelectedIndex = _startTab;

        return tabs;
    }

    private static void DrawTabItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tc) return;
        var page = tc.TabPages[e.Index];
        bool selected = e.Index == tc.SelectedIndex;

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.FillRectangle(new SolidBrush(selected ? Color.White : Color.FromArgb(248, 249, 251)), e.Bounds);

        if (selected)
        {
            using var pen = new Pen(Theme.AppTheme.Primary, 2.5f);
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        var textColor = selected ? Theme.AppTheme.Primary : Color.FromArgb(100, 110, 130);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(page.Text, new Font("Segoe UI", 9.5f, selected ? FontStyle.Bold : FontStyle.Regular),
            new SolidBrush(textColor), e.Bounds, sf);
    }

    private static TabPage MakeTabPage(string title, Control body)
    {
        var page = new TabPage(title) { BackColor = Color.White, Padding = new Padding(0) };
        body.Dock = DockStyle.Fill;
        page.Controls.Add(body);
        return page;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 1: OVERVIEW – Timeline of all recent events
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildOverview()
    {
        var container = new Panel { BackColor = Color.White, Padding = new Padding(20, 16, 20, 16) };

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var stack  = new FlowLayoutPanel
        {
            Dock          = DockStyle.Top,
            AutoSize      = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            Padding       = new Padding(0, 0, 0, 20)
        };

        var sectionLabel = new Label
        {
            Text      = "RECENT ACTIVITY  •  Last 30 events",
            Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.FromArgb(140, 155, 175),
            AutoSize  = true,
            Margin    = new Padding(0, 0, 0, 12)
        };
        stack.Controls.Add(sectionLabel);

        // Collect timeline events from all sources
        var events = new List<(DateTime Date, string Kind, string Title, string Detail, Color Accent)>();

        foreach (var r in DataStore.GetMedicalRecords().Where(r => r.PetId == _pet.Id))
            events.Add((r.CreatedAt, "MedRec", $"Medical Record — {r.VetName}", r.Diagnosis, Theme.AppTheme.Primary));

        foreach (var a in DataStore.GetAppointments().Where(a => a.PetId == _pet.Id))
        {
            var accent = a.Status switch
            {
                "Completed"   => Theme.AppTheme.Success,
                "Cancelled"   => Theme.AppTheme.Danger,
                "In Progress" => Theme.AppTheme.Warning,
                _             => Theme.AppTheme.Accent
            };
            events.Add((a.AppointmentDate, "Appt", $"Appointment — {a.ServiceTypeName}", $"{a.Status}  ·  {a.VetName}", accent));
        }

        foreach (var c in DataStore.GetCbcRecords().Where(c => c.PetId == _pet.Id))
            events.Add((c.TestDate, "CBC", "CBC / Blood Test", $"WBC {c.Wbc}  ·  RBC {c.Rbc}  ·  HGB {c.Hgb}", Color.FromArgb(142, 36, 170)));

        var sorted = events.OrderByDescending(e => e.Date).Take(30).ToList();

        if (sorted.Count == 0)
        {
            stack.Controls.Add(new Label
            {
                Text      = "No activity recorded yet for this patient.",
                Font      = new Font("Segoe UI", 11f, FontStyle.Italic),
                ForeColor = Color.FromArgb(160, 170, 185),
                AutoSize  = true,
                Margin    = new Padding(0, 20, 0, 0)
            });
        }
        else
        {
            string? lastYear = null;
            foreach (var ev in sorted)
            {
                string yearGroup = ev.Date.ToString("MMMM yyyy");
                if (yearGroup != lastYear)
                {
                    lastYear = yearGroup;
                    var yearLbl = new Label
                    {
                        Text      = yearGroup.ToUpperInvariant(),
                        Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                        ForeColor = Color.FromArgb(160, 170, 185),
                        AutoSize  = true,
                        Margin    = new Padding(0, 14, 0, 4)
                    };
                    stack.Controls.Add(yearLbl);
                }
                stack.Controls.Add(BuildTimelineRow(ev.Date, ev.Kind, ev.Title, ev.Detail, ev.Accent));
            }
        }

        scroll.Controls.Add(stack);
        container.Controls.Add(scroll);
        return container;
    }

    private Panel BuildTimelineRow(DateTime date, string kind, string title, string detail, Color accent)
    {
        var row = new Panel
        {
            Width     = 900,
            Height    = 60,
            Margin    = new Padding(0, 0, 0, 4),
            BackColor = Color.White,
            Cursor    = Cursors.Hand
        };

        row.MouseEnter += (_, _) => { row.BackColor = Color.FromArgb(250, 251, 253); row.Invalidate(); };
        row.MouseLeave += (_, _) => { row.BackColor = Color.White; row.Invalidate(); };

        row.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            // Card border
            using var border = new Pen(Color.FromArgb(232, 235, 240));
            using var path   = RoundRect(new Rectangle(0, 0, row.Width - 2, row.Height - 1), 7);
            e.Graphics.DrawPath(border, path);
            // Accent dot (left center)
            using var dot = new SolidBrush(accent);
            int cy = row.Height / 2;
            e.Graphics.FillEllipse(dot, 14, cy - 5, 10, 10);
        };

        // Kind badge label text
        string kindLabel = kind switch { "MedRec" => "VISIT", "Appt" => "APPT", "CBC" => "CBC", _ => kind.ToUpper() };

        // ── Row 1: Badge + Date ───────────────────────────────────────────
        var badge = new Label
        {
            Text      = kindLabel,
            Font      = new Font("Segoe UI", 6.5f, FontStyle.Bold),
            ForeColor = accent,
            BackColor = Color.FromArgb(22, accent.R, accent.G, accent.B),
            AutoSize  = true,
            Padding   = new Padding(5, 2, 5, 2),
            Left      = 34, Top = 10
        };

        var lblDate = new Label
        {
            Text      = date.ToString("MMM d, yyyy"),
            Font      = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(145, 158, 175),
            AutoSize  = true,
            Top       = 12
        };

        // ── Row 2: Title · Detail ─────────────────────────────────────────
        var lblTitle = new Label
        {
            Text      = title,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 40, 60),
            AutoSize  = true,
            Left      = 34, Top = 33
        };
        var lblDetail = new Label
        {
            Text      = detail,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(115, 125, 145),
            AutoSize  = true,
            Top       = 36
        };

        row.Controls.Add(badge);
        row.Controls.Add(lblDate);
        row.Controls.Add(lblTitle);
        row.Controls.Add(lblDetail);

        // Position date and detail after badge/title widths are known
        row.Layout += (_, _) =>
        {
            badge.Left    = 34;  badge.Top = 10;
            lblDate.Left  = badge.Right + 8; lblDate.Top = 12;
            lblTitle.Left = 34;  lblTitle.Top = 33;
            lblDetail.Left = lblTitle.Right + 10; lblDetail.Top = 36;
        };

        return row;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 2: MEDICAL RECORDS
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildMedicalRecords()
    {
        var wrapper = new Panel { BackColor = Color.White, Padding = new Padding(0) };

        // Toolbar within tab
        var bar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White, Padding = new Padding(16, 10, 16, 0) };
        bar.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(235, 238, 242)); e.Graphics.DrawLine(p, 0, bar.Height - 1, bar.Width, bar.Height - 1); };
        var lbl = new Label { Text = "All medical visits and diagnoses for this patient", Font = new Font("Segoe UI", 8.5f), ForeColor = Color.FromArgb(120, 130, 150), AutoSize = true, Top = 16, Left = 16 };
        bar.Controls.Add(lbl);
        wrapper.Controls.Add(bar);

        var grid = CreateStyledGrid();
        grid.Dock = DockStyle.Fill;

        var records = DataStore.GetMedicalRecords()
            .Where(r => r.PetId == _pet.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                Date      = r.CreatedAt.ToString("yyyy-MM-dd"),
                Vet       = r.VetName,
                Diagnosis = r.Diagnosis,
                Treatment = r.Treatment,
                FollowUp  = r.FollowUpDate?.ToString("yyyy-MM-dd") ?? "—",
                Notes     = string.IsNullOrWhiteSpace(r.Notes) ? "—" : r.Notes
            }).ToList();

        grid.DataSource = records;
        if (grid.Columns["Id"]    != null) grid.Columns["Id"].Visible    = false;
        if (grid.Columns["Notes"] != null) { grid.Columns["Notes"].MinimumWidth = 120; grid.Columns["Notes"].ToolTipText = "Double-click row to view full record"; }

        ApplyColumnWidths(grid, ("Date", 95), ("Vet", 130), ("Diagnosis", 0), ("Treatment", 0), ("FollowUp", 95), ("Notes", 140));

        AddEmptyOverlay(grid, "No medical records yet.\nClick \"+ Medical Record\" above to add one.");

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            int id  = (int)grid.Rows[e.RowIndex].Cells["Id"].Value;
            var rec = DataStore.GetMedicalRecords().FirstOrDefault(x => x.Id == id);
            if (rec != null) { using var dlg = new MedicalRecordDialog(rec, true); dlg.ShowDialog(); }
        };

        wrapper.Controls.Add(grid);
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 3: APPOINTMENTS
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildAppointments()
    {
        var wrapper = new Panel { BackColor = Color.White };
        var bar = BuildTabSubHeader("All appointments — past and upcoming");
        wrapper.Controls.Add(bar);

        var grid = CreateStyledGrid();
        grid.Dock = DockStyle.Fill;

        var appts = DataStore.GetAppointments()
            .Where(a => a.PetId == _pet.Id)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(a => new
            {
                Date     = a.AppointmentDate.ToString("yyyy-MM-dd HH:mm"),
                Service  = a.ServiceTypeName,
                Vet      = a.VetName,
                Status   = a.Status,
                Duration = $"{a.Duration} min",
                Notes    = string.IsNullOrWhiteSpace(a.Notes) ? "—" : a.Notes
            }).ToList();

        grid.DataSource = appts;
        ApplyColumnWidths(grid, ("Date", 140), ("Service", 0), ("Vet", 130), ("Status", 100), ("Duration", 80), ("Notes", 160));

        // Color-code status column
        grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || grid.Columns[e.ColumnIndex].Name != "Status" || e.Value == null) return;
            e.CellStyle.ForeColor = e.Value.ToString() switch
            {
                "Completed"   => Theme.AppTheme.Success,
                "Cancelled"   => Theme.AppTheme.Danger,
                "In Progress" => Theme.AppTheme.Warning,
                _             => Theme.AppTheme.Accent
            };
            e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        };

        AddEmptyOverlay(grid, "No appointments on record.\nClick \"+ Appointment\" above to schedule one.");
        wrapper.Controls.Add(grid);
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 4: CBC RESULTS
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildCbcResults()
    {
        var wrapper = new Panel { BackColor = Color.White };
        var bar = BuildTabSubHeader("Complete Blood Count laboratory results — double-click to view full panel");
        wrapper.Controls.Add(bar);

        var grid = CreateStyledGrid();
        grid.Dock = DockStyle.Fill;

        var cbcList = DataStore.GetCbcRecords()
            .Where(c => c.PetId == _pet.Id)
            .OrderByDescending(c => c.TestDate)
            .ToList();

        grid.DataSource = cbcList.Select(c => new
        {
            c.Id,
            Date    = c.TestDate.ToString("yyyy-MM-dd"),
            WBC     = c.Wbc,
            RBC     = c.Rbc,
            HGB     = c.Hgb,
            HCT     = $"{c.Hct}%",
            PLT     = c.Plt,
            MCV     = c.Mcv,
            MCH     = c.Mch,
            Remarks = string.IsNullOrWhiteSpace(c.Remarks) ? "—" : c.Remarks
        }).ToList();

        if (grid.Columns["Id"] != null) grid.Columns["Id"].Visible = false;
        ApplyColumnWidths(grid, ("Date", 95), ("WBC", 70), ("RBC", 70), ("HGB", 70), ("HCT", 65), ("PLT", 70), ("MCV", 65), ("MCH", 65), ("Remarks", 0));

        // Tooltip on header row describing units
        grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex != -1) return;
            grid.Columns[e.ColumnIndex].ToolTipText = e.ColumnIndex switch
            {
                _ when grid.Columns[e.ColumnIndex].Name == "WBC" => "White Blood Cells (10⁹/L)",
                _ when grid.Columns[e.ColumnIndex].Name == "RBC" => "Red Blood Cells (10¹²/L)",
                _ when grid.Columns[e.ColumnIndex].Name == "HGB" => "Hemoglobin (g/dL)",
                _ when grid.Columns[e.ColumnIndex].Name == "HCT" => "Hematocrit (%)",
                _ when grid.Columns[e.ColumnIndex].Name == "PLT" => "Platelets (10⁹/L)",
                _ when grid.Columns[e.ColumnIndex].Name == "MCV" => "Mean Corpuscular Volume (fL)",
                _ when grid.Columns[e.ColumnIndex].Name == "MCH" => "Mean Corpuscular Hemoglobin (pg)",
                _ => ""
            };
        };

        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            int id  = (int)grid.Rows[e.RowIndex].Cells["Id"].Value;
            var rec = cbcList.FirstOrDefault(c => c.Id == id);
            if (rec != null) { using var dlg = new CbcDialog(rec); dlg.ShowDialog(); }
        };

        AddEmptyOverlay(grid, "No CBC results on record.\nClick \"+ CBC\" above to add a lab result.");
        wrapper.Controls.Add(grid);
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 5: MEDICATIONS
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildMedications()
    {
        var wrapper = new Panel { BackColor = Color.White };
        var bar = BuildTabSubHeader("Medications dispensed across all medical records");
        wrapper.Controls.Add(bar);

        var grid = CreateStyledGrid();
        grid.Dock = DockStyle.Fill;

        var meds = DataStore.GetMedicalRecords()
            .Where(r => r.PetId == _pet.Id)
            .SelectMany(r => DataStore.GetRecordMedications(r.Id).Select(p => new
            {
                Date       = r.CreatedAt.ToString("yyyy-MM-dd"),
                Medication = p.MedicationName,
                Dosage     = p.Dosage,
                Notes      = string.IsNullOrWhiteSpace(p.Notes) ? "—" : p.Notes,
                Vet        = r.VetName
            }))
            .OrderByDescending(m => m.Date)
            .ToList();

        grid.DataSource = meds;
        ApplyColumnWidths(grid, ("Date", 95), ("Medication", 0), ("Dosage", 120), ("Vet", 130), ("Notes", 180));
        AddEmptyOverlay(grid, "No medications recorded.");
        wrapper.Controls.Add(grid);
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // TAB 6: NOTES
    // ════════════════════════════════════════════════════════════════════════
    private Control BuildNotes()
    {
        var wrapper = new Panel { BackColor = Color.White, Padding = new Padding(24, 20, 24, 20) };
        var bar = BuildTabSubHeader("Clinical notes and general remarks about this patient");
        wrapper.Controls.Add(bar);

        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Color.White,
            Padding   = new Padding(20)
        };

        bool hasNotes = !string.IsNullOrWhiteSpace(_pet.Notes);
        var txt = new TextBox
        {
            Dock        = DockStyle.Fill,
            Multiline   = true,
            ReadOnly    = true,
            BackColor   = Color.White,
            BorderStyle = BorderStyle.None,
            Font        = new Font("Segoe UI", 11f),
            ForeColor   = hasNotes ? Color.FromArgb(35, 45, 65) : Color.FromArgb(180, 185, 195),
            Text        = hasNotes ? _pet.Notes : "No clinical notes recorded for this patient.",
            ScrollBars  = ScrollBars.Vertical
        };
        card.Controls.Add(txt);
        wrapper.Controls.Add(card);
        return wrapper;
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════
    private Panel BuildTabSubHeader(string description)
    {
        var bar = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.FromArgb(251, 252, 253) };
        bar.Paint += (_, e) => { using var p = new Pen(Color.FromArgb(235, 238, 242)); e.Graphics.DrawLine(p, 0, bar.Height - 1, bar.Width, bar.Height - 1); };
        var lbl = new Label
        {
            Text      = description,
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(120, 135, 155),
            AutoSize  = true,
            Left      = 16,
            Top       = 14
        };
        bar.Controls.Add(lbl);
        return bar;
    }

    private static DataGridView CreateStyledGrid()
    {
        var dgv = new DataGridView
        {
            ReadOnly                = true,
            AllowUserToAddRows      = false,
            AllowUserToDeleteRows   = false,
            RowHeadersVisible       = false,
            SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.None,
            BackgroundColor         = Color.White,
            BorderStyle             = BorderStyle.None,
            ScrollBars              = ScrollBars.Both,
            ShowCellToolTips        = true,
            Cursor                  = Cursors.Hand
        };
        UIHelper.StyleGrid(dgv);
        dgv.RowTemplate.Height = 34;
        return dgv;
    }

    private static void ApplyColumnWidths(DataGridView dgv, params (string Name, int Width)[] cols)
    {
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        foreach (var (name, w) in cols)
        {
            if (dgv.Columns[name] == null) continue;
            if (w == 0)
                dgv.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            else
                dgv.Columns[name].Width = w;
        }
    }

    private static void AddEmptyOverlay(DataGridView grid, string message)
    {
        var lbl = UIHelper.CreateEmptyDataLabel(message);
        lbl.Font = new Font("Segoe UI", 10f, FontStyle.Italic);
        lbl.Visible = false;
        grid.Parent?.Controls.Add(lbl);
        lbl.BringToFront();

        grid.DataBindingComplete += (_, _) =>
        {
            bool empty = grid.Rows.Count == 0;
            lbl.Visible = empty;
            grid.Visible = !empty;
        };
    }

    private static Button CreateTopBtn(string text, Color color)
    {
        var btn = new Button
        {
            Text      = text,
            Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = color,
            FlatStyle = FlatStyle.Flat,
            Height    = 32,
            AutoSize  = false,
            Width     = TextRenderer.MeasureText(text, new Font("Segoe UI", 8.5f, FontStyle.Bold)).Width + 26,
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(color, 0.1f);
        return btn;
    }

    private static GraphicsPath RoundRect(Rectangle rc, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(rc.Left,         rc.Top,          d, d, 180, 90);
        path.AddArc(rc.Right - d,    rc.Top,          d, d, 270, 90);
        path.AddArc(rc.Right - d,    rc.Bottom - d,   d, d,   0, 90);
        path.AddArc(rc.Left,         rc.Bottom - d,   d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    private string GetAgeWithDate()
    {
        if (_pet.DateOfBirth == null) return "Unknown";
        var dob  = _pet.DateOfBirth.Value;
        var diff = DateTime.Today - dob;
        int years  = diff.Days / 365;
        int months = (diff.Days % 365) / 30;
        string age = years > 0 ? $"{years}y {months}m" : $"{months} months";
        return $"{age}  ({dob:MMM d, yyyy})";
    }
}
