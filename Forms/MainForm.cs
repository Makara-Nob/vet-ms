using VetMS.Forms.Admin;
using VetMS.Forms.MasterData;
using VetMS.Forms.Operations;
using VetMS.Models;

namespace VetMS.Forms;

public class MainForm : Form
{
    private readonly User _currentUser;
    private Panel pnlSidebar = null!;
    private Panel pnlContent = null!;
    private Form? _currentChild;

    private PictureBox _picSidebarUser = null!;
    private Button? _activeNavBtn;

    // Design tokens — tweak once, applies everywhere
    private const int SidebarW     = 248;
    private const int HeaderH      = 56;
    private const int NavBtnH      = 40;
    private const int SectionGapT  = 20;   // space above section label
    private const int SectionGapB  = 4;    // space below section label before first button

    private readonly Size _baseSize = new(1140, 700);
    private readonly Dictionary<Control, Font> _originalFonts = [];

    private int _sidebarY;

    public MainForm(User user)
    {
        _currentUser = user;
        InitializeUI();
        CaptureBaseFonts(this);
        ShowWelcome();
        Resize += (_, _) => ApplyResponsiveScaling();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INIT
    // ═══════════════════════════════════════════════════════════════════════════
    private void InitializeUI()
    {
        Text            = "VetMS — Veterinary Management System";
        Size            = _baseSize;
        MinimumSize     = new Size(960, 600);
        StartPosition   = FormStartPosition.CenterScreen;
        WindowState     = FormWindowState.Maximized;
        BackColor       = UIHelper.LightBg;
        Font            = new Font("Segoe UI", 9f);
        AutoScaleMode   = AutoScaleMode.None;

        // Sidebar
        pnlSidebar = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = SidebarW,
            BackColor = UIHelper.Sidebar
        };
        BuildSidebar();

        // Thin 1-px divider between sidebar and content
        var divider = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 1,
            BackColor = Color.FromArgb(22, 255, 255, 255)   // subtle white seam
        };

        // Right panel
        var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = UIHelper.LightBg };
        pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = UIHelper.LightBg };
        rightPanel.Controls.Add(pnlContent);
        rightPanel.Controls.Add(BuildHeader());

        Controls.Add(rightPanel);
        Controls.Add(divider);
        Controls.Add(pnlSidebar);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  HEADER  (slim 56px)
    // ═══════════════════════════════════════════════════════════════════════════
    private Panel BuildHeader()
    {
        var hdr = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = HeaderH,
            BackColor = UIHelper.Primary
        };

        // App name + paw
        var lblLogo = new Label
        {
            Text      = "🐾  Veterinary Management System",
            ForeColor = Color.White,
            Font      = new Font("Segoe UI Semibold", 13.5f, FontStyle.Bold),
            AutoSize  = true,
            Left      = 22,
            Top       = (HeaderH - 20) / 2
        };

        // Date chip (right side)
        var lblDate = new Label
        {
            Text      = DateTime.Today.ToString("dddd, MMMM dd, yyyy"),
            ForeColor = Color.FromArgb(175, 210, 240),
            Font      = new Font("Segoe UI", 8.5f),
            AutoSize  = true
        };

        // Bottom shadow line
        var shadow = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 2,
            BackColor = Color.FromArgb(15, 0, 0, 0)
        };

        hdr.Controls.Add(shadow);
        hdr.Controls.Add(lblLogo);
        hdr.Controls.Add(lblDate);

        void Reposition()
        {
            lblDate.Left = hdr.Width - lblDate.Width - 28;
            lblDate.Top  = (HeaderH - lblDate.Height) / 2;
        }
        hdr.Resize       += (_, _) => Reposition();
        lblDate.SizeChanged += (_, _) => Reposition();

        return hdr;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SIDEBAR
    // ═══════════════════════════════════════════════════════════════════════════
    private void BuildSidebar()
    {
        _sidebarY = 0;

        // ── Profile card ────────────────────────────────────────────────────────
        var cardH   = 82;
        var pnlCard = new Panel
        {
            Left      = 0, Top = 0,
            Width     = SidebarW, Height = cardH,
            Cursor    = Cursors.Hand,
            BackColor = Color.FromArgb(18, 255, 255, 255)    // subtle tint
        };

        _picSidebarUser = new PictureBox
        {
            Width = 40, Height = 40,
            Left  = 18, Top   = (cardH - 40) / 2,
            SizeMode   = PictureBoxSizeMode.Zoom,
            BackColor  = Color.Transparent,
            Cursor     = Cursors.Hand
        };
        MakeCircular(_picSidebarUser);

        var lblName = new Label
        {
            Text      = _currentUser.FullName,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoSize  = true,
            Left      = 66, Top = 22,
            Cursor    = Cursors.Hand
        };
        var lblRole = new Label
        {
            Text      = _currentUser.Role,
            ForeColor = Color.FromArgb(140, 165, 195),
            Font      = new Font("Segoe UI", 8f),
            AutoSize  = true,
            Left      = 66, Top = 43,
            Cursor    = Cursors.Hand
        };

        pnlCard.Controls.AddRange(new Control[] { _picSidebarUser, lblName, lblRole });

        // Bottom separator line
        pnlCard.Controls.Add(new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 1,
            BackColor = Color.FromArgb(28, 255, 255, 255)
        });

        // Hover + click wiring
        Action click  = () => LoadForm(new UserProfileForm(_currentUser));
        Action hover  = () => pnlCard.BackColor = UIHelper.SideHover;
        Action unhover = () => pnlCard.BackColor = Color.FromArgb(18, 255, 255, 255);

        foreach (Control c in new Control[] { pnlCard, _picSidebarUser, lblName, lblRole })
        {
            c.Click       += (_, _) => click();
            c.MouseEnter  += (_, _) => hover();
            c.MouseLeave  += (_, _) => unhover();
        }

        pnlSidebar.Controls.Add(pnlCard);
        RefreshSidebarProfile();

        _sidebarY = cardH + 8;

        // ── Nav items ────────────────────────────────────────────────────────────
        AddSection("OPERATIONS");
        AddNav("📅", "Appointments",    () => LoadForm(new AppointmentForm()));
        AddNav("🐾", "Patients",        () => LoadForm(new PetForm()));
        AddNav("👥", "Customers",       () => LoadForm(new CustomerForm()));
        AddNav("📋", "Medical Records", () => LoadForm(new MedicalRecordForm()));
        AddNav("🩸", "CBC Management",  () => LoadForm(new CbcForm()));

        AddSection("MASTER DATA");
        AddNav("🐶", "Animal Species",  () => LoadForm(new AnimalSpeciesForm()));
        AddNav("🦴", "Breeds",          () => LoadForm(new BreedForm()));
        AddNav("🩺", "Service Types",   () => LoadForm(new ServiceTypeForm()));
        AddNav("💊", "Medications",     () => LoadForm(new MedicationForm()));
        AddNav("📦", "Suppliers",       () => LoadForm(new SupplierForm()));

        if (_currentUser.Role == "Administrator")
        {
            AddSection("ADMINISTRATION");
            AddNav("👤", "User Management", () => LoadForm(new UserForm()));
        }

        AddSection("ACCOUNT");
        AddNav("🚪", "Logout", DoLogout);

        // Version label pinned to bottom
        pnlSidebar.Controls.Add(new Label
        {
            Text      = "VetMS v1.0",
            ForeColor = Color.FromArgb(85, 115, 150),
            Font      = new Font("Segoe UI", 7.5f),
            Dock      = DockStyle.Bottom,
            Height    = 28,
            TextAlign = ContentAlignment.MiddleCenter
        });
    }

    // ── Section label ──────────────────────────────────────────────────────────
    private void AddSection(string text)
    {
        _sidebarY += SectionGapT;

        var lbl = new Label
        {
            Text      = text,
            Left      = 18,
            Top       = _sidebarY,
            AutoSize  = true,
            ForeColor = Color.FromArgb(95, 130, 168),
            Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
            Padding   = new Padding(0)
        };
        pnlSidebar.Controls.Add(lbl);
        _sidebarY += 20 + SectionGapB;
    }

    // ── Nav button ─────────────────────────────────────────────────────────────
    private void AddNav(string icon, string label, Action clickAction)
    {
        // Outer container — holds the left-accent stripe + the button
        var wrap = new Panel
        {
            Left      = 0,
            Top       = _sidebarY,
            Width     = SidebarW,
            Height    = NavBtnH,
            BackColor = Color.Transparent
        };

        // 3-px left accent stripe (hidden by default, shown when active)
        var accent = new Panel
        {
            Dock      = DockStyle.Left,
            Width     = 3,
            BackColor = UIHelper.Success,
            Visible   = false
        };

        // Icon label (fixed 32px column)
        var lblIcon = new Label
        {
            Text      = icon,
            Left      = 3,
            Top       = 0,
            Width     = 34,
            Height    = NavBtnH,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(185, 205, 225),
            Cursor    = Cursors.Hand,
            BackColor = Color.Transparent
        };

        // Text label
        var lblText = new Label
        {
            Text      = label,
            Left      = 38,
            Top       = 0,
            Width     = SidebarW - 46,
            Height    = NavBtnH,
            TextAlign = ContentAlignment.MiddleLeft,
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(200, 220, 240),
            Cursor    = Cursors.Hand,
            BackColor = Color.Transparent
        };

        wrap.Controls.AddRange(new Control[] { accent, lblIcon, lblText });

        // ── Hover / active styling ──────────────────────────────────────────
        void SetHover(bool on)
        {
            if (wrap == GetActiveWrap()) return;   // don't dim the active item
            wrap.BackColor = on ? UIHelper.SideHover : Color.Transparent;
        }
        void SetActive()
        {
            // Deactivate previous
            if (_activeNavBtn != null)
            {
                var prevWrap = _activeNavBtn.Tag as Panel;
                if (prevWrap != null)
                {
                    prevWrap.BackColor = Color.Transparent;
                    // hide accent of previous
                    foreach (Control c in prevWrap.Controls)
                        if (c is Panel p && p.Width == 3) p.Visible = false;
                    // restore text colours
                    foreach (Control c in prevWrap.Controls)
                    {
                        if (c is Label lbl2)
                            lbl2.ForeColor = lbl2.Width == 34
                                ? Color.FromArgb(185, 205, 225)
                                : Color.FromArgb(200, 220, 240);
                    }
                }
            }
            // Activate this one
            wrap.BackColor = Color.FromArgb(28, 255, 255, 255);
            accent.Visible = true;
            lblIcon.ForeColor = Color.White;
            lblText.ForeColor = Color.White;
            lblText.Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold);

            // Store a sentinel button so we can find this wrap later
            var sentinel = new Button { Visible = false, Tag = wrap };
            _activeNavBtn = sentinel;
        }

        foreach (Control c in new Control[] { wrap, lblIcon, lblText })
        {
            c.MouseEnter += (_, _) => SetHover(true);
            c.MouseLeave += (_, _) => SetHover(false);
            c.Click      += (_, _) => { SetActive(); clickAction(); };
        }

        pnlSidebar.Controls.Add(wrap);
        _sidebarY += NavBtnH;
    }

    private Panel? GetActiveWrap()
    {
        return _activeNavBtn?.Tag as Panel;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PROFILE PICTURE
    // ═══════════════════════════════════════════════════════════════════════════
    private void MakeCircular(PictureBox pic)
    {
        pic.SizeMode = PictureBoxSizeMode.Zoom;
        pic.Paint += (_, e) =>
        {
            var rect = new Rectangle(0, 0, pic.Width - 1, pic.Height - 1);
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(rect);
            pic.Region = new Region(path);
            using var pen = new Pen(Color.FromArgb(70, Color.White), 2);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawEllipse(pen, rect);
        };
        pic.Resize += (_, _) =>
        {
            var rect = new Rectangle(0, 0, pic.Width - 1, pic.Height - 1);
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddEllipse(rect);
            pic.Region = new Region(path);
        };
    }

    public void RefreshSidebarProfile()
    {
        if (_picSidebarUser == null) return;
        var old = _picSidebarUser.Image;
        _picSidebarUser.Image = null;
        old?.Dispose();

        if (_currentUser.ProfilePicture?.Length > 0)
        {
            try
            {
                var ms = new System.IO.MemoryStream(_currentUser.ProfilePicture);
                _picSidebarUser.Image = Image.FromStream(ms);
                return;
            }
            catch { }
        }
        _picSidebarUser.Image = UIHelper.CreateAvatar(_currentUser.FullName, 40);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FORM LOADER
    // ═══════════════════════════════════════════════════════════════════════════
    private void LoadForm(Form form)
    {
        _currentChild?.Close();
        _currentChild?.Dispose();

        form.TopLevel         = false;
        form.FormBorderStyle  = FormBorderStyle.None;
        form.Dock             = DockStyle.Fill;

        pnlContent.Controls.Clear();
        pnlContent.Controls.Add(form);
        form.Show();
        _currentChild = form;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  WELCOME SCREEN
    // ═══════════════════════════════════════════════════════════════════════════
    private void ShowWelcome()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = UIHelper.LightBg };

        var paw = new Label
        {
            Text     = "🐾",
            Font     = new Font("Segoe UI", 42f),
            AutoSize = true
        };
        var lblTitle = new Label
        {
            Text      = $"Welcome back, {_currentUser.FullName.Split(' ')[0]}",
            Font      = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = UIHelper.Primary,
            AutoSize  = true
        };
        var lblSub = new Label
        {
            Text      = "Select a module from the sidebar to get started.",
            Font      = new Font("Segoe UI", 11f),
            ForeColor = Color.FromArgb(140, 150, 165),
            AutoSize  = true
        };

        panel.Controls.AddRange(new Control[] { paw, lblTitle, lblSub });
        panel.Resize += (_, _) =>
        {
            int cx = panel.Width / 2;
            int cy = panel.Height / 2 - 30;
            paw.Left   = cx - paw.Width / 2;
            paw.Top    = cy - paw.Height - 8;
            lblTitle.Left = cx - lblTitle.Width / 2;
            lblTitle.Top  = cy;
            lblSub.Left   = cx - lblSub.Width / 2;
            lblSub.Top    = lblTitle.Bottom + 10;
        };

        pnlContent.Controls.Clear();
        pnlContent.Controls.Add(panel);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  LOGOUT
    // ═══════════════════════════════════════════════════════════════════════════
    private void DoLogout()
    {
        var result = VetMS.Forms.CustomMessageBox.Show(
            "Are you sure you want to logout?",
            "Confirm Logout",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;
        new LoginForm().Show();
        Hide();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  RESPONSIVE SCALING
    // ═══════════════════════════════════════════════════════════════════════════
    private void CaptureBaseFonts(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            _originalFonts.TryAdd(ctrl, ctrl.Font);
            if (ctrl.HasChildren) CaptureBaseFonts(ctrl);
        }
    }

    private void ApplyResponsiveScaling()
    {
        float scale = Math.Clamp(
            Math.Min((float)Width / _baseSize.Width, (float)Height / _baseSize.Height),
            0.95f, 1.02f);
        ScaleFonts(this, scale);
    }

    private void ScaleFonts(Control parent, float scale)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (_originalFonts.TryGetValue(ctrl, out Font? baseFont))
            {
                float sz = Math.Max(7.5f, baseFont.Size * scale);
                ctrl.Font = new Font(baseFont.FontFamily, sz, baseFont.Style);
            }
            if (ctrl.HasChildren) ScaleFonts(ctrl, scale);
        }
    }
}