using System;
using System.Drawing;
using System.Windows.Forms;

namespace VetMS.Forms;

public class Toast : Form
{
    private readonly System.Windows.Forms.Timer _holdTimer;
    private readonly System.Windows.Forms.Timer _fadeTimer;
    private const int DisplayMs = 2500;
    private const int FadeStepMs = 20;

    private Toast(string message, bool success)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition   = FormStartPosition.Manual;
        Size            = new Size(320, 60);
        TopMost         = true;
        ShowInTaskbar   = false;
        Opacity         = 0.95;
        BackColor       = success ? Color.FromArgb(30, 160, 80) : Color.FromArgb(200, 50, 60);

        // Rounded feel via double-buffer
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

        // Icon
        var lblIcon = new Label
        {
            Text      = success ? "✔" : "✖",
            Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.White,
            Size      = new Size(48, 60),
            Location  = new Point(0, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Separator line
        var sep = new Panel
        {
            BackColor = Color.FromArgb(255, 255, 255, 60),
            Size      = new Size(1, 40),
            Location  = new Point(48, 10)
        };

        // Message
        var lblMsg = new Label
        {
            Text      = message,
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Color.White,
            Location  = new Point(58, 0),
            Size      = new Size(252, 60),
            TextAlign = ContentAlignment.MiddleLeft
        };

        Controls.Add(lblIcon);
        Controls.Add(sep);
        Controls.Add(lblMsg);

        // Dismiss on click
        Click          += (_, _) => Dismiss();
        lblIcon.Click  += (_, _) => Dismiss();
        lblMsg.Click   += (_, _) => Dismiss();

        // Hold, then fade
        _holdTimer = new System.Windows.Forms.Timer { Interval = DisplayMs };
        _holdTimer.Tick += (_, _) =>
        {
            _holdTimer.Stop();
            _fadeTimer.Start();
        };

        _fadeTimer = new System.Windows.Forms.Timer { Interval = FadeStepMs };
        _fadeTimer.Tick += (_, _) =>
        {
            Opacity -= 0.06;
            if (Opacity <= 0) Dismiss();
        };
    }

    private void Dismiss()
    {
        _holdTimer.Stop();
        _fadeTimer.Stop();
        Close();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // Position: bottom-right of the screen
        var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1366, 768);
        Location = new Point(screen.Right - Width - 20, screen.Top + 20);
        _holdTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _holdTimer.Dispose();
            _fadeTimer.Dispose();
        }
        base.Dispose(disposing);
    }

    public static void Success(string message = "Saved successfully!")
        => ShowToast(message, true);

    public static void Error(string message = "An error occurred.")
        => ShowToast(message, false);

    private static void ShowToast(string message, bool success)
    {
        var toast = new Toast(message, success);
        toast.FormClosed += (_, _) => toast.Dispose();
        toast.Show();
    }
}
