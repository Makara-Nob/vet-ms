using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using VetMS.Forms;

namespace VetMS.Forms;

public class CustomMessageBox : Form
{
    private DialogResult _result = DialogResult.None;
    private readonly string _message;
    private readonly string _title;
    private readonly MessageBoxButtons _buttons;
    private readonly MessageBoxIcon _icon;
    private readonly string _customIcon;
    private readonly Color? _customIconColor;

    private CustomMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, string customIcon = "", Color? customIconColor = null)
    {
        _message = message;
        _title = string.IsNullOrWhiteSpace(title) ? "Message" : title;
        _buttons = buttons;
        _icon = icon;
        _customIcon = customIcon;
        _customIconColor = customIconColor;

        InitializeUI();
    }

    private void InitializeUI()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        Size = new Size(420, 220);
        BackColor = Color.White;

        // Custom Drop Shadow effect wrapping
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(2), // Border thickness
            BackColor = UIHelper.Primary
        };
        
        var pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        // Header
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = UIHelper.Primary
        };

        var lblTitle = new Label
        {
            Text = _title,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };

        // Close Button
        var btnClose = new Label
        {
            Text = "✕",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12f),
            Dock = DockStyle.Right,
            Width = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (_, _) => { this._result = DialogResult.Cancel; this.Close(); };
        btnClose.MouseEnter += (_, _) => btnClose.BackColor = Color.FromArgb(40, 80, 120);
        btnClose.MouseLeave += (_, _) => btnClose.BackColor = Color.Transparent;

        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(btnClose);

        // Body Message
        var pnlBody = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        var lblMessage = new Label
        {
            Text = _message,
            Font = new Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(40, 40, 40),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Icon spacing logic
        if (_icon != MessageBoxIcon.None || !string.IsNullOrEmpty(_customIcon))
        {
            var pnlIcon = new Panel { Dock = DockStyle.Left, Width = 60 };
            var lblIcon = new Label
            {
                Text = !string.IsNullOrEmpty(_customIcon) ? _customIcon : GetIconString(_icon),
                Font = new Font("Segoe UI Emoji", 24f),
                ForeColor = !string.IsNullOrEmpty(_customIcon) ? (_customIconColor ?? UIHelper.Success) : GetIconColor(_icon),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlIcon.Controls.Add(lblIcon);
            pnlBody.Controls.Add(lblMessage);
            pnlBody.Controls.Add(pnlIcon);
        }
        else
        {
            pnlBody.Controls.Add(lblMessage);
        }

        // Action Buttons
        var pnlActions = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = UIHelper.LightBg,
            Padding = new Padding(0, 0, 16, 0)
        };

        var flpButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0),
            WrapContents = false
        };

        BuildButtons(flpButtons);

        pnlActions.Controls.Add(flpButtons);

        // Assembly
        pnlContent.Controls.Add(pnlBody);
        pnlContent.Controls.Add(pnlActions);
        pnlContent.Controls.Add(pnlHeader);

        pnlMain.Controls.Add(pnlContent);
        Controls.Add(pnlMain);
    }

    private string GetIconString(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Error => "❌",
        MessageBoxIcon.Warning => "⚠️",
        MessageBoxIcon.Information => "ℹ️",
        MessageBoxIcon.Question => "❓",
        _ => ""
    };

    private Color GetIconColor(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Error => UIHelper.Danger,
        MessageBoxIcon.Warning => UIHelper.Warning,
        MessageBoxIcon.Information => UIHelper.Accent,
        MessageBoxIcon.Question => UIHelper.Primary,
        _ => Color.Black
    };

    private void BuildButtons(FlowLayoutPanel flp)
    {
        if (_buttons == MessageBoxButtons.OKCancel || _buttons == MessageBoxButtons.YesNoCancel)
        {
            flp.Controls.Add(CreateActionBtn("Cancel", DialogResult.Cancel, Color.FromArgb(108, 117, 125)));
        }

        if (_buttons == MessageBoxButtons.YesNo || _buttons == MessageBoxButtons.YesNoCancel)
        {
            flp.Controls.Add(CreateActionBtn("No", DialogResult.No, Color.FromArgb(108, 117, 125)));
            flp.Controls.Add(CreateActionBtn("Yes", DialogResult.Yes, UIHelper.Success));
        }

        if (_buttons == MessageBoxButtons.OK || _buttons == MessageBoxButtons.OKCancel)
        {
            flp.Controls.Add(CreateActionBtn("OK", DialogResult.OK, UIHelper.Primary));
        }
    }

    private Button CreateActionBtn(string text, DialogResult res, Color backColor)
    {
        var btn = UIHelper.CreateButton(text, backColor, 90, 36);
        btn.Click += (_, _) => { this._result = res; this.Close(); };
        return btn;
    }

    public static DialogResult Show(string text, string caption = "", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
    {
        using var dlg = new CustomMessageBox(text, caption, buttons, icon);
        dlg.ShowDialog();
        return dlg._result;
    }

    public static DialogResult ShowSuccess(string text, string caption = "Success")
    {
        using var dlg = new CustomMessageBox(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, "✅", UIHelper.Success);
        dlg.ShowDialog();
        return dlg._result;
    }
}
