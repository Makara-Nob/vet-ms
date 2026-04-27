using System.Drawing.Drawing2D;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms;

public class LoginForm : Form
{
    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;

    private static readonly Color ClinicTeal = Color.FromArgb(0, 169, 157);

    public LoginForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text            = "Broem Brey Veterinary Clinic — Login";
        WindowState     = FormWindowState.Maximized;
        BackColor       = Color.White;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize     = new Size(600, 500);
        DoubleBuffered  = true;

        // ── Logo ──────────────────────────────────────────────────────────────
        // ── Logo wrapper (full-width so FlowLayout centers it) ────────────────
        var logoWrapper = new Panel { Width = 320, Height = 150, BackColor = Color.White };
        string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.png");
        if (!File.Exists(logoPath))
            logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "logo.jpg");
        if (File.Exists(logoPath))
        {
            var pic = new PictureBox
            {
                Size      = new Size(150, 150),
                SizeMode  = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Image     = Image.FromFile(logoPath),
                Left      = (320 - 150) / 2,
                Top       = 0
            };
            logoWrapper.Controls.Add(pic);
        }
        else
        {
            var lbl = new Label
            {
                Text      = "🐾",
                Font      = new Font("Segoe UI", 52f),
                AutoSize  = false,
                Size      = new Size(150, 150),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White,
                Left      = (320 - 150) / 2,
                Top       = 0
            };
            logoWrapper.Controls.Add(lbl);
        }

        var lblName = new Label
        {
            Text      = "Broem Brey Veterinary Clinic",
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = ClinicTeal,
            AutoSize  = false,
            Width     = 320,
            Height    = 26,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.White
        };

        var lblSub = new Label
        {
            Text      = "Sign in to your account",
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(160, 170, 185),
            AutoSize  = false,
            Width     = 320,
            Height    = 22,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.White
        };

        // ── Inputs ────────────────────────────────────────────────────────────
        var (wrapUser, inUser) = MakeUnderlineInput("👤  Username", false);
        var (wrapPass, inPass) = MakeUnderlineInput("🔒  Password", true);
        txtUsername = inUser;
        txtPassword = inPass;
        txtUsername.Text = "admin";
        txtPassword.Text = "admin123";

        // ── Log In button ─────────────────────────────────────────────────────
        var btnLogin = new Button
        {
            Text      = "Log In",
            Width = 320, Height = 48,
            BackColor = ClinicTeal, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold)
        };
        btnLogin.FlatAppearance.BorderSize         = 0;
        btnLogin.FlatAppearance.MouseOverBackColor = ClinicTeal;
        btnLogin.Click += BtnLogin_Click;
        ApplyRoundedRegion(btnLogin, 24);
        btnLogin.SizeChanged += (_, _) => ApplyRoundedRegion(btnLogin, 24);

        btnLogin.Paint += (s, e) =>
        {
            var btn = (Button)s!;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path  = RoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 24);
            using var brush = new SolidBrush(ClinicTeal);
            e.Graphics.FillPath(brush, path);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(btn.Text, btn.Font, Brushes.White, new RectangleF(0, 0, btn.Width, btn.Height), sf);
        };

        var lblFooter = new Label
        {
            Text      = "Broem Brey Veterinary Clinic  •  v1.0",
            Font      = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(190, 200, 215),
            AutoSize  = false,
            Width     = 320,
            Height    = 20,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.White
        };

        // ── Layout: stack everything in a FlowLayoutPanel ─────────────────────
        var flow = new FlowLayoutPanel
        {
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            BackColor     = Color.White,
            Padding       = new Padding(0)
        };

        logoWrapper.Margin = new Padding(0, 0, 0, 10);
        lblName.Margin     = new Padding(0, 0, 0, 4);
        lblSub.Margin      = new Padding(0, 0, 0, 32);
        wrapUser.Margin    = new Padding(0, 0, 0, 14);
        wrapPass.Margin    = new Padding(0, 0, 0, 36);
        btnLogin.Margin    = new Padding(0, 0, 0, 0);
        lblFooter.Margin   = new Padding(0, 32, 0, 0);

        flow.Controls.AddRange(new Control[] { logoWrapper, lblName, lblSub, wrapUser, wrapPass, btnLogin, lblFooter });

        void CenterFlow()
        {
            // Center each control horizontally within the flow panel
            flow.Width = 320;
            foreach (Control c in flow.Controls)
            {
                c.Left = (flow.Width - c.Width) / 2;
            }
            flow.Left = (ClientSize.Width  - flow.Width)  / 2;
            flow.Top  = (ClientSize.Height - flow.Height) / 2;
        }

        Controls.Add(flow);
        Load   += (_, _) => CenterFlow();
        Resize += (_, _) => CenterFlow();
        AcceptButton = btnLogin;
    }

    // ── Underline input ──────────────────────────────────────────────────────
    private (Panel wrapper, TextBox txt) MakeUnderlineInput(string placeholder, bool isPassword)
    {
        var wrapper = new Panel
        {
            Width = 320, Height = 44,
            BackColor = Color.White
        };

        bool focused = false;

        wrapper.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var lineColor = focused ? ClinicTeal : Color.FromArgb(205, 215, 225);
            using var pen = new Pen(lineColor, focused ? 2f : 1.5f);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        };

        var txt = new TextBox
        {
            Left = 0, Top = 10, Width = 320,
            Font            = new Font("Segoe UI", 11f),
            BorderStyle     = BorderStyle.None,
            BackColor       = Color.White,
            ForeColor       = Color.FromArgb(25, 40, 65),
            PlaceholderText = placeholder
        };
        if (isPassword) txt.PasswordChar = '●';

        txt.Enter += (_, _) => { focused = true;  wrapper.Invalidate(); };
        txt.Leave += (_, _) => { focused = false; wrapper.Invalidate(); };

        wrapper.Controls.Add(txt);
        return (wrapper, txt);
    }

    // ── Rounded helpers ──────────────────────────────────────────────────────
    private static void ApplyRoundedRegion(Control ctrl, int r)
    {
        if (ctrl.Width <= 0 || ctrl.Height <= 0) return;
        ctrl.Region = new Region(RoundedPath(new Rectangle(0, 0, ctrl.Width, ctrl.Height), r));
    }

    private static GraphicsPath RoundedPath(Rectangle rc, int r)
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

    // ── Login logic ──────────────────────────────────────────────────────────
    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            VetMS.Forms.CustomMessageBox.Show("Please enter both username and password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var user = Database.AuthenticateUser(username, password);
        if (user is null)
        {
            VetMS.Forms.CustomMessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!user.IsActive)
        {
            VetMS.Forms.CustomMessageBox.Show("This account has been disabled.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var mainForm = new MainForm(user);
        mainForm.FormClosed += (s, args) => Application.Exit();
        mainForm.Show();
        Hide();
        VetMS.Forms.Toast.Success($"Welcome back, {user.FullName}!");
    }
}
