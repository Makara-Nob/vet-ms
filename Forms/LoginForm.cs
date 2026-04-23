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
        Text = "VetMS - Login";
        WindowState = FormWindowState.Maximized;
        BackColor = UIHelper.Sidebar;
        
        var pnlCard = new Panel
        {
            Size = new Size(420, 500),
            BackColor = UIHelper.LightBg,
            BorderStyle = BorderStyle.None
        };

        Resize += (_, _) =>
        {
            pnlCard.Left = (ClientSize.Width - pnlCard.Width) / 2;
            pnlCard.Top = (ClientSize.Height - pnlCard.Height) / 2;
        };

        var lblTitle = new Label
        {
            Text = "🐾 VetMS Login",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = UIHelper.Primary,
            AutoSize = true,
            Top = 60
        };

        var lblSub = new Label
        {
            Text = "Please enter your credentials",
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.Gray,
            AutoSize = true,
            Top = 105
        };

        var lblUsername = new Label { Text = "Username", Left = 60, Top = 170, AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 80, 80) };
        txtUsername = new TextBox { Left = 60, Top = 200, Width = 300, Font = new Font("Segoe UI", 12f), Text = "admin" };

        var lblPassword = new Label { Text = "Password", Left = 60, Top = 260, AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.FromArgb(80, 80, 80) };
        txtPassword = new TextBox { Left = 60, Top = 290, Width = 300, Font = new Font("Segoe UI", 12f), PasswordChar = '•', Text = "admin123" };

        var btnLogin = new Button
        {
            Text = "LOGIN",
            Left = 60,
            Top = 370,
            Width = 300,
            Height = 45,
            BackColor = UIHelper.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold)
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;

        pnlCard.Controls.Add(lblTitle);
        pnlCard.Controls.Add(lblSub);
        pnlCard.Controls.Add(lblUsername);
        pnlCard.Controls.Add(txtUsername);
        pnlCard.Controls.Add(lblPassword);
        pnlCard.Controls.Add(txtPassword);
        pnlCard.Controls.Add(btnLogin);

        // Center dynamically
        pnlCard.Layout += (s, e) =>
        {
            lblTitle.Left = (pnlCard.Width - lblTitle.Width) / 2;
            lblSub.Left = (pnlCard.Width - lblSub.Width) / 2;
        };

        Controls.Add(pnlCard);

        AcceptButton = btnLogin;
    }

    private void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            VetMS.Forms.CustomMessageBox.Show("Please enter both username and password.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        User? user = Database.AuthenticateUser(username, password);

        if (user != null)
        {
            if (!user.IsActive)
            {
                VetMS.Forms.CustomMessageBox.Show("This account has been disabled.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var mainForm = new MainForm(user);
            mainForm.FormClosed += (s, args) => Application.Exit();
            mainForm.Show();
            this.Hide();
            VetMS.Forms.Toast.Success($"Welcome back, {user.FullName}!");
        }
        else
        {
            VetMS.Forms.CustomMessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
