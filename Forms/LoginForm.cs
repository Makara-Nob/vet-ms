using System.Drawing.Drawing2D;
using VetMS.Data;
using VetMS.Models;

namespace VetMS.Forms;

public class LoginForm : Form
{
    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;

    public LoginForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        Text            = "VetMS — Login";
        WindowState     = FormWindowState.Maximized;
        BackColor       = Color.FromArgb(12, 28, 50);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize     = new Size(600, 500);
        DoubleBuffered  = true;

        // Decorative circles painted directly on the form background
        Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var b1 = new SolidBrush(Color.FromArgb(22, 255, 255, 255));
            using var b2 = new SolidBrush(Color.FromArgb(12, 255, 255, 255));
            g.FillEllipse(b1, -120, -120, 520, 520);
            g.FillEllipse(b2, ClientSize.Width - 320, ClientSize.Height - 220, 520, 420);
            g.FillEllipse(b2, ClientSize.Width - 160, -90, 310, 310);
        };

        // ── Card ───────────────────────────────────────────────────────────────
        var card = new Panel { Size = new Size(440, 545), BackColor = Color.White };
        ApplyRoundedRegion(card, 20);
        card.SizeChanged += (_, _) => ApplyRoundedRegion(card, 20);

        void CenterCard()
        {
            card.Left = (ClientSize.Width  - card.Width)  / 2;
            card.Top  = (ClientSize.Height - card.Height) / 2;
        }

        Load   += (_, _) => { Invalidate(); CenterCard(); };
        Resize += (_, _) => { Invalidate(); CenterCard(); };

        // ── Brand strip ────────────────────────────────────────────────────────
        var strip = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = UIHelper.Primary };
        var lblPaw = new Label
        {
            Text = "🐾", Font = new Font("Segoe UI", 34f),
            ForeColor = Color.White, AutoSize = true
        };
        var lblBrand = new Label
        {
            Text = "VetMS", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 210, 240), AutoSize = true
        };
        strip.Controls.AddRange(new Control[] { lblPaw, lblBrand });
        strip.Resize += (_, _) =>
        {
            lblPaw.Left   = (strip.Width - lblPaw.Width)     / 2; lblPaw.Top   = 14;
            lblBrand.Left = (strip.Width - lblBrand.Width)   / 2; lblBrand.Top = lblPaw.Bottom + 2;
        };

        // ── Headings ───────────────────────────────────────────────────────────
        var lblTitle = new Label
        {
            Text = "Welcome Back",
            Font = new Font("Segoe UI", 19f, FontStyle.Bold),
            ForeColor = Color.FromArgb(18, 44, 70),
            AutoSize = false, Left = 0, Width = 440, Top = 122, Height = 44,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var lblSub = new Label
        {
            Text = "Sign in to your account",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(130, 145, 165),
            AutoSize = false, Left = 0, Width = 440, Top = 172, Height = 26,
            TextAlign = ContentAlignment.MiddleCenter
        };

        // ── Inputs ─────────────────────────────────────────────────────────────
        var (wrapUser, inUser) = MakeInput("👤  Username", false, 215);
        var (wrapPass, inPass) = MakeInput("🔒  Password", true,  285);
        txtUsername = inUser;
        txtPassword = inPass;
        txtUsername.Text = "admin";
        txtPassword.Text = "admin123";

        // ── Button ─────────────────────────────────────────────────────────────
        var btnLogin = new Button
        {
            Text = "SIGN IN", Left = 50, Top = 375, Width = 340, Height = 50,
            BackColor = UIHelper.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold)
        };
        btnLogin.FlatAppearance.BorderSize         = 0;
        btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(36, 80, 120);
        btnLogin.Click += BtnLogin_Click;
        ApplyRoundedRegion(btnLogin, 10);
        btnLogin.SizeChanged += (_, _) => ApplyRoundedRegion(btnLogin, 10);

        // ── Separator + hint ───────────────────────────────────────────────────
        var sep = new Panel
        {
            Left = 50, Top = 450, Width = 340, Height = 1,
            BackColor = Color.FromArgb(230, 235, 245)
        };
        var lblHint = new Label
        {
            Text = "Contact your administrator if you need access.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(160, 175, 195),
            AutoSize = false, Left = 0, Width = 440, Top = 463, Height = 26,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var lblFooter = new Label
        {
            Text = "VetMS v1.0  •  Veterinary Management System",
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(185, 200, 220),
            AutoSize = false, Left = 0, Width = 440, Top = 500, Height = 26,
            TextAlign = ContentAlignment.MiddleCenter
        };

        card.Controls.AddRange(new Control[]
        {
            strip, lblTitle, lblSub,
            wrapUser, wrapPass, btnLogin,
            sep, lblHint, lblFooter
        });

        Controls.Add(card);
        AcceptButton = btnLogin;
    }

    // ── Input factory ────────────────────────────────────────────────────────
    private static (Panel wrapper, TextBox txt) MakeInput(string placeholder, bool isPassword, int top)
    {
        var wrapper = new Panel
        {
            Left = 50, Top = top, Width = 340, Height = 50,
            BackColor = Color.FromArgb(247, 249, 252)
        };
        ApplyRoundedRegion(wrapper, 10);
        wrapper.SizeChanged += (_, _) => ApplyRoundedRegion(wrapper, 10);

        wrapper.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen  = new Pen(Color.FromArgb(215, 222, 235), 1.5f);
            using var path = RoundedPath(new Rectangle(0, 0, p.Width - 1, p.Height - 1), 10);
            e.Graphics.DrawPath(pen, path);
        };

        var txt = new TextBox
        {
            Left = 14, Top = 14, Width = 312,
            Font = new Font("Segoe UI", 11f),
            BorderStyle     = BorderStyle.None,
            BackColor       = Color.FromArgb(247, 249, 252),
            ForeColor       = Color.FromArgb(25, 40, 65),
            PlaceholderText = placeholder
        };
        if (isPassword) txt.PasswordChar = '●';

        txt.Enter += (_, _) => { wrapper.BackColor = Color.White; txt.BackColor = Color.White; wrapper.Invalidate(); };
        txt.Leave += (_, _) => { wrapper.BackColor = Color.FromArgb(247, 249, 252); txt.BackColor = Color.FromArgb(247, 249, 252); wrapper.Invalidate(); };

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
        p.AddArc(rc.Left,          rc.Top,           d, d, 180, 90);
        p.AddArc(rc.Right - d,     rc.Top,           d, d, 270, 90);
        p.AddArc(rc.Right - d,     rc.Bottom - d,    d, d,   0, 90);
        p.AddArc(rc.Left,          rc.Bottom - d,    d, d,  90, 90);
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
