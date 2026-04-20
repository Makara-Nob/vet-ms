using VetMS.Forms.MasterData;

namespace VetMS.Forms;

public class MainForm : Form
{
    private Panel pnlSidebar = null!;
    private Panel pnlContent = null!;
    private Form? _currentChild;

    private readonly Size _baseSize = new(1100, 680);
    private readonly Dictionary<Control, float> _baseFonts = [];
    private readonly Dictionary<Control, Font> _originalFonts = [];

    private int _sidebarY = 12;

    public MainForm()
    {
        InitializeUI();
        CaptureBaseFonts(this);
        ShowWelcome();

        Resize += (_, _) => ApplyResponsiveScaling();
    }

    private void InitializeUI()
    {
        Text = "VetMS — Veterinary Management System";
        Size = _baseSize;
        MinimumSize = new Size(900, 580);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UIHelper.LightBg;
        Font = new Font("Segoe UI", 9f);
        AutoScaleMode = AutoScaleMode.None;

        // Sidebar
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 220,
            BackColor = UIHelper.Sidebar
        };
        BuildSidebar();

        // Right container
        var rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UIHelper.LightBg
        };

        var header = BuildHeader();

        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UIHelper.LightBg
        };

        rightPanel.Controls.Add(pnlContent);
        rightPanel.Controls.Add(header);

        Controls.Add(rightPanel);
        Controls.Add(pnlSidebar);
    }

    private Panel BuildHeader()
    {
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 88,
            BackColor = UIHelper.Primary
        };

        var lblLogo = new Label
        {
            Text = "🐾 Veterinary Management System",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            AutoSize = true,
            Left = 20,
            Top = 12
        };

        
        var lblDate = new Label
        {
            Text = DateTime.Today.ToString("dddd, MMMM dd, yyyy"),
            ForeColor = Color.FromArgb(180, 210, 235),
            Font = new Font("Segoe UI", 9f),
            AutoSize = true
        };

        pnlHeader.Controls.Add(lblLogo);
        pnlHeader.Controls.Add(lblDate);

        pnlHeader.Resize += (_, _) =>
        {
            lblDate.Left = pnlHeader.Width - lblDate.Width - 18;
            lblDate.Top = 24;
        };

        return pnlHeader;
    }

    private void BuildSidebar()
    {
        AddSectionLabel("MASTER DATA");

        AddNavButton("Animal Species", () => LoadForm(new AnimalSpeciesForm()));
        AddNavButton("Breeds", () => LoadForm(new BreedForm()));
        AddNavButton("Service Types", () => LoadForm(new ServiceTypeForm()));
        AddNavButton("Medications", () => LoadForm(new MedicationForm()));
        AddNavButton("Suppliers", () => LoadForm(new SupplierForm()));

        var lblVersion = new Label
        {
            Text = "VetMS v1.0",
            ForeColor = Color.FromArgb(100, 130, 160),
            Font = new Font("Segoe UI", 8f),
            AutoSize = false,
            Height = 30,
            Dock = DockStyle.Bottom,
            TextAlign = ContentAlignment.MiddleCenter
        };

        pnlSidebar.Controls.Add(lblVersion);
    }

    private void AddSectionLabel(string text)
    {
        var lbl = new Label
        {
            Text = text,
            Left = 14,
            Top = _sidebarY,
            AutoSize = true,
            ForeColor = Color.FromArgb(110, 145, 180),
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold)
        };

        pnlSidebar.Controls.Add(lbl);
        _sidebarY += 28;
    }

    private void AddNavButton(string text, Action clickAction)
    {
        var btn = new Button
        {
            Text = "   " + text,
            Left = 0,
            Top = _sidebarY,
            Width = 220,
            Height = 42,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(220, 230, 240),
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor = Cursors.Hand
        };

        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = UIHelper.SideHover;
        btn.FlatAppearance.MouseDownBackColor = UIHelper.Accent;
        btn.Click += (_, _) => clickAction();

        pnlSidebar.Controls.Add(btn);
        _sidebarY += 42;
    }

    private void LoadForm(Form form)
    {
        _currentChild?.Close();
        _currentChild?.Dispose();

        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;

        pnlContent.Controls.Clear();
        pnlContent.Controls.Add(form);

        form.Show();
        _currentChild = form;
    }

    private void ShowWelcome()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UIHelper.LightBg
        };

        var lblTitle = new Label
        {
            Text = "Welcome to VetMS",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = UIHelper.Primary,
            AutoSize = true
        };

        var lblSub = new Label
        {
            Text = "Select a module from the sidebar to get started.",
            Font = new Font("Segoe UI", 11f),
            ForeColor = Color.Gray,
            AutoSize = true
        };

        panel.Controls.Add(lblTitle);
        panel.Controls.Add(lblSub);

        panel.Resize += (_, _) =>
        {
            lblTitle.Left = (panel.Width - lblTitle.Width) / 2;
            lblTitle.Top = panel.Height / 2 - 50;

            lblSub.Left = (panel.Width - lblSub.Width) / 2;
            lblSub.Top = lblTitle.Bottom + 12;
        };

        pnlContent.Controls.Clear();
        pnlContent.Controls.Add(panel);
    }

    private void CaptureBaseFonts(Control parent)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (!_originalFonts.ContainsKey(ctrl))
                _originalFonts[ctrl] = ctrl.Font;

            if (ctrl.HasChildren)
                CaptureBaseFonts(ctrl);
        }
    }

    private void ApplyResponsiveScaling()
    {
        float scaleX = (float)Width / _baseSize.Width;
        float scaleY = (float)Height / _baseSize.Height;

        float scale = Math.Min(scaleX, scaleY);

        scale = Math.Clamp(scale, 0.9f, 1.25f);
        ScaleFonts(this, scale);
    }

    private void ScaleFonts(Control parent, float scale)
    {
        foreach (Control ctrl in parent.Controls)
        {
            if (_originalFonts.TryGetValue(ctrl, out Font? baseFont))
            {
                float newSize = Math.Max(8f, baseFont.Size * scale);

                ctrl.Font = new Font(
                    baseFont.FontFamily,
                    newSize,
                    baseFont.Style);
            }

            if (ctrl.HasChildren)
                ScaleFonts(ctrl, scale);
        }
    }
}