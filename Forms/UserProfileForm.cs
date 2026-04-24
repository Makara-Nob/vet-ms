using System.IO;
using VetMS.Data;
using VetMS.Models;
using VetMS.Forms;

public class UserProfileForm : Form
{
    private readonly User _user;
    private readonly Action? _onSaved;
    private bool _isEditMode;

    private PictureBox _picProfile = null!;

    private Label _lblUsername = null!;
    private Label _lblFullName = null!;
    private Label _lblEmail = null!;

    private TextBox _txtFullName = null!;
    private TextBox _txtEmail = null!;

    private Button _btnEdit = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    public UserProfileForm(User user, Action? onSaved = null)
    {
        _user = user;
        _onSaved = onSaved;
        InitUI();
        SetEditMode(false);
    }

    private void InitUI()
    {
        Text = "User Profile";
        BackColor = UIHelper.LightBg;
        Width = 920;
        Height = 540;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);

        var title = new Label
        {
            Text = "Account Settings",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = UIHelper.Primary,
            AutoSize = true,
            Left = 40,
            Top = 25
        };

        var card = CreateCard();

        BuildLayout(card);

        Controls.Add(title);
        Controls.Add(card);
    }

    // ───────────────────────── CARD (modern look) ─────────────────────────
    private Panel CreateCard()
    {
        return new Panel
        {
            Left = 40,
            Top = 85,
            Width = 820,
            Height = 380,
            BackColor = Color.White,
            Padding = new Padding(25)
        };
    }

    // ───────────────────────── MODERN LAYOUT ─────────────────────────
    private void BuildLayout(Panel card)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.White
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(BuildLeft(), 0, 0);
        layout.SetRowSpan(layout.GetControlFromPosition(0, 0), 2);

        layout.Controls.Add(BuildRight(), 1, 0);
        layout.Controls.Add(BuildActions(), 1, 1);

        card.Controls.Add(layout);
    }

    // ───────────────────────── LEFT (avatar modern) ─────────────────────────
    private Panel BuildLeft()
    {
        var pnl = new Panel { Dock = DockStyle.Fill };

        _picProfile = new PictureBox
        {
            Width = 130,
            Height = 130,
            Top = 10,
            Left = 20,
            SizeMode = PictureBoxSizeMode.Zoom
        };

        LoadImage();
        UIHelper.AttachImageViewer(_picProfile, () => _picProfile.Image);

        var btnPhoto = UIHelper.CreateButton("Change Photo", UIHelper.AltRow, 130, 34);
        btnPhoto.ForeColor = UIHelper.Primary;
        btnPhoto.Top = 150;
        btnPhoto.Left = 20;
        btnPhoto.Click += BtnUpload_Click;

        pnl.Controls.Add(_picProfile);
        pnl.Controls.Add(btnPhoto);

        return pnl;
    }

    // ───────────────────────── RIGHT (clean grouped info) ─────────────────────────
    private Panel BuildRight()
    {
        var pnl = new Panel { Dock = DockStyle.Fill };

        _lblUsername = CreateLabel($"Username: {_user.Username}", 0, 10, true);

        _lblFullName = CreateLabel(_user.FullName, 0, 55);
        _txtFullName = CreateTextBox(_user.FullName, 0, 55);

        _lblEmail = CreateLabel(_user.Email, 0, 100);
        _txtEmail = CreateTextBox(_user.Email, 0, 100);

        pnl.Controls.AddRange(new Control[]
        {
            _lblUsername,
            _lblFullName,
            _txtFullName,
            _lblEmail,
            _txtEmail
        });

        return pnl;
    }

    // ───────────────────────── ACTION BAR (modern UX) ─────────────────────────
    private Panel BuildActions()
    {
        var pnl = new Panel { Dock = DockStyle.Fill, Height = 70 };

        _btnEdit = UIHelper.CreateButton("Edit Profile", UIHelper.Accent, 140);
        _btnEdit.Left = 0;
        _btnEdit.Top = 15;
        _btnEdit.Click += (s, e) => SetEditMode(true);

        _btnSave = UIHelper.CreateButton("Save Changes", UIHelper.Success, 140);
        _btnSave.Left = 150;
        _btnSave.Top = 15;
        _btnSave.Click += Save_Click;

        _btnCancel = UIHelper.CreateButton("Cancel", UIHelper.Danger, 120);
        _btnCancel.Left = 300;
        _btnCancel.Top = 15;
        _btnCancel.Click += (s, e) => SetEditMode(false);

        pnl.Controls.AddRange(new Control[] { _btnEdit, _btnSave, _btnCancel });

        return pnl;
    }

    // ───────────────────────── MODE CONTROL (simplified UX) ─────────────────────────
    private void SetEditMode(bool enable)
    {
        _isEditMode = enable;

        _lblFullName.Visible = !enable;
        _lblEmail.Visible = !enable;

        _txtFullName.Visible = enable;
        _txtEmail.Visible = enable;

        _btnSave.Visible = enable;
        _btnCancel.Visible = enable;

        _btnEdit.Visible = !enable;
    }

    // ───────────────────────── SAVE ─────────────────────────
    private void Save_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtFullName.Text))
        {
            Toast.Error("Full name cannot be empty.");
            return;
        }

        _user.FullName = _txtFullName.Text.Trim();
        _user.Email = _txtEmail.Text.Trim();

        DataStore.Update(_user);

        _lblFullName.Text = _user.FullName;
        _lblEmail.Text = _user.Email;

        SetEditMode(false);
        _onSaved?.Invoke();
        Toast.Success("Profile updated successfully.");
    }

    // ───────────────────────── HELPERS ─────────────────────────
    private Label CreateLabel(string text, int x, int y, bool bold = false)
        => new Label
        {
            Text = text,
            Left = x,
            Top = y,
            AutoSize = true,
            Font = new Font("Segoe UI", bold ? 12f : 11f, bold ? FontStyle.Bold : FontStyle.Regular),
            ForeColor = Color.FromArgb(50, 50, 50)
        };

    private TextBox CreateTextBox(string text, int x, int y)
        => new TextBox
        {
            Text = text,
            Left = x,
            Top = y - 3,
            Width = 300,
            Visible = false,
            Font = new Font("Segoe UI", 10.5f)
        };

    // ───────────────────────── IMAGE ─────────────────────────
    private void LoadImage()
    {
        if (_user.ProfilePicture?.Length > 0)
        {
            using var ms = new MemoryStream(_user.ProfilePicture);
            _picProfile.Image = Image.FromStream(ms);
        }
        else
        {
            _picProfile.Image = UIHelper.CreateAvatar(_user.FullName, 130);
        }
    }

    private void BtnUpload_Click(object? sender, EventArgs e)
    {
        using var fd = new OpenFileDialog
        {
            Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (fd.ShowDialog() == DialogResult.OK)
        {
            _user.ProfilePicture = File.ReadAllBytes(fd.FileName);
            DataStore.Update(_user);
            LoadImage();
            _onSaved?.Invoke();
            Toast.Success("Profile photo updated.");
        }
    }
}