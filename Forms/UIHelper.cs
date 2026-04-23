namespace VetMS.Forms;

public static class UIHelper
{
    // Color palette
    public static readonly Color Primary   = Color.FromArgb(26, 60, 94);       // Dark navy
    public static readonly Color Accent    = Color.FromArgb(0, 120, 215);      // Bright blue
    public static readonly Color Success   = Color.FromArgb(40, 167, 69);      // Green
    public static readonly Color Danger    = Color.FromArgb(220, 53, 69);      // Red
    public static readonly Color Warning   = Color.FromArgb(255, 193, 7);      // Amber
    public static readonly Color LightBg   = Color.FromArgb(245, 247, 250);    // Off-white bg
    public static readonly Color AltRow    = Color.FromArgb(232, 244, 253);    // Light blue row
    public static readonly Color Sidebar   = Color.FromArgb(18, 44, 70);       // Darker navy
    public static readonly Color SideHover = Color.FromArgb(0, 80, 150);       // Hover state

    public static Button CreateButton(string text, Color back, int width = 90, int height = 32)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = height,
            BackColor = back,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9f),
            Margin = new Padding(0, 0, 8, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    public static void StyleGrid(DataGridView dgv)
    {
        dgv.Font = new Font("Segoe UI", 9f);
        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Primary,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        dgv.ColumnHeadersHeight = 36;
        dgv.EnableHeadersVisualStyles = false;
        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            Padding = new Padding(8, 0, 0, 0),
            SelectionBackColor = Accent,
            SelectionForeColor = Color.White
        };
        dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = AltRow };
        dgv.GridColor = Color.FromArgb(220, 225, 230);
        dgv.RowTemplate.Height = 30;
        dgv.BackgroundColor = Color.White;
        dgv.BorderStyle = BorderStyle.None;
    }

    public static Panel CreateHeader(string title, string subtitle)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Primary };

        var lblTitle = new Label
        {
            Text = title,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize = true,
            Left = 16,
            Top = 10
        };
        var lblSub = new Label
        {
            Text = subtitle,
            ForeColor = Color.FromArgb(170, 200, 230),
            Font = new Font("Segoe UI", 8.5f),
            AutoSize = true,
            Left = 16,
            Top = 38
        };

        panel.Controls.Add(lblTitle);
        panel.Controls.Add(lblSub);
        return panel;
    }

    public static Label CreateFormLabel(string text)
        => new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            AutoSize = true,
            Margin = new Padding(0, 8, 0, 2)
        };

    public static TextBox CreateTextBox(int width = 280)
        => new TextBox
        {
            Width = width,
            Font = new Font("Segoe UI", 9.5f),
            Margin = new Padding(0, 0, 0, 6)
        };

    public static Label CreateEmptyDataLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12f, FontStyle.Italic),
            ForeColor = Color.Gray,
            BackColor = Color.White,
            Visible = false
        };
    }

    public static void PaintActionColumn(DataGridView grid, DataGridViewCellPaintingEventArgs e, string action1 = "Edit", string action2 = "Delete")
    {
        PaintDynamicActionColumn(grid, e, action1, action2);
    }

    public static void PaintDynamicActionColumn(DataGridView grid, DataGridViewCellPaintingEventArgs e, params string[] actions)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || grid.Columns[e.ColumnIndex].Name != "ColAction" || actions.Length == 0) return;

        e.PaintBackground(e.CellBounds, true);
        bool isSelected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
        using var font = new Font("Segoe UI", 9f, FontStyle.Underline);

        int currentX = e.CellBounds.X + 16;

        foreach (var act in actions)
        {
            if (string.IsNullOrEmpty(act)) continue;
            
            var sz = TextRenderer.MeasureText(e.Graphics, act, font);
            int y = e.CellBounds.Y + (e.CellBounds.Height - sz.Height) / 2;
            
            Color c = Accent; // Default Edit
            if (act == "Delete") c = Danger;
            else if (act == "View") c = Success;
            if (isSelected) c = Color.White;

            TextRenderer.DrawText(e.Graphics, act, font, new Point(currentX, y), c);
            currentX += sz.Width + 16;
        }

        e.Handled = true;
    }

    public static void HandleActionColumnClick(DataGridView grid, DataGridViewCellMouseEventArgs e, Action<int> onEdit, Action<int>? onDelete = null, string action1 = "Edit", string action2 = "Delete")
    {
        if (onDelete != null)
            HandleDynamicActionColumnClick(grid, e, (action1, onEdit), (action2, onDelete));
        else
            HandleDynamicActionColumnClick(grid, e, (action1, onEdit));
    }

    public static void HandleDynamicActionColumnClick(DataGridView grid, DataGridViewCellMouseEventArgs e, params (string Name, Action<int> Action)[] actions)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Button != MouseButtons.Left || grid.Columns[e.ColumnIndex].Name != "ColAction" || actions.Length == 0) return;

        using var font = new Font("Segoe UI", 9f, FontStyle.Underline);
        int currentX = 16;

        foreach (var act in actions)
        {
            if (string.IsNullOrEmpty(act.Name)) continue;

            var sz = TextRenderer.MeasureText(act.Name, font);
            var rect = new Rectangle(currentX - 6, 0, sz.Width + 12, grid.Rows[e.RowIndex].Height);
            
            if (rect.Contains(e.Location))
            {
                act.Action(e.RowIndex);
                return;
            }
            currentX += sz.Width + 16;
        }
    }

    public static Panel WrapControl(string labelText, Control ctrl)
    {
        // Increased height from 25 to 35, and Margin from 10 to 22 for 'High-Breathing' room
        var p = new Panel { Width = ctrl.Width, Height = ctrl.Height + 35, Margin = new Padding(0, 0, 0, 22) };
        var lbl = new Label { 
            Text = labelText, 
            Font = new Font("Segoe UI", 9f, FontStyle.Bold), 
            ForeColor = Color.FromArgb(80, 90, 105), 
            Top = 0, Left = 0, AutoSize = true 
        };
        // Increased Top from 22 to 28 for comfortable gap between label and input
        ctrl.Top = 28; ctrl.Left = 0;
        p.Controls.Add(lbl); p.Controls.Add(ctrl);
        return p;
    }

    public static Label CreateSectionLabel(string text)
    {
        return new Label 
        { 
            Text = text.ToUpperInvariant(), 
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), 
            ForeColor = Color.FromArgb(120, 130, 150), 
            AutoSize = true, 
            Margin = new Padding(0, 25, 0, 12) // Significant top margin to separate sections
        };
    }

    public static Image CreateProfilePlaceholder(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(240, 242, 245)); // Soft light background
        
        using var pen = new Pen(Color.FromArgb(200, 205, 215), 2f);
        g.DrawEllipse(pen, 2, 2, size - 4, size - 4);
        
        // Draw a simple person/pet silhouette
        using var brush = new SolidBrush(Color.FromArgb(180, 185, 195));
        g.FillEllipse(brush, size * 0.3f, size * 0.25f, size * 0.4f, size * 0.4f); // Head
        g.FillEllipse(brush, size * 0.2f, size * 0.65f, size * 0.6f, size * 0.6f); // Shoulders/Body
        
        return bmp;
    }

    public static Image CreateAvatar(string name, int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // Draw circle background with a tiny padding
        using var brush = new SolidBrush(Color.FromArgb(170, 190, 210));
        g.FillEllipse(brush, 1, 1, size - 2, size - 2);

        // Draw initial letter, reduced font size to prevent cutting off
        string letter = string.IsNullOrWhiteSpace(name) ? "?" : name.Substring(0, 1).ToUpper();
        using var font = new Font("Segoe UI", size / 2.8f, FontStyle.Bold);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(letter, font, Brushes.White, new RectangleF(0, 0, size, size), format);

        return bmp;
    }

    /// <summary>
    /// Attaches a click-to-preview lightbox to any PictureBox.
    /// Pass getImage as a func so the image can be resolved lazily at click time.
    /// Usage: UIHelper.AttachImageViewer(picAvatar, () => picAvatar.Image);
    /// </summary>
    public static void AttachImageViewer(PictureBox pb, Func<Image?> getImage)
    {
        // Magnifier cursor hint
        pb.Cursor = Cursors.Hand;

        // Hover: subtle zoom/opacity hint
        pb.MouseEnter += (_, _) => {
            if (getImage() != null) {
                pb.BackColor = Color.FromArgb(235, 238, 242);
            }
        };
        pb.MouseLeave += (_, _) => {
            pb.BackColor = LightBg;
        };

        // Click: open lightbox
        pb.Click += (_, _) => {
            var img = getImage();
            if (img == null) return;
            ImageLightbox.Show(pb.FindForm()!, img);
        };
    }
}

/// <summary>
/// A reusable full-screen lightbox overlay for viewing images.
/// Open with: ImageLightbox.Show(ownerForm, image);
/// </summary>
public sealed class ImageLightbox : Form
{
    private double _targetOpacity = 1.0;
    private System.Windows.Forms.Timer? _fadeTimer;

    private ImageLightbox(Form owner, Image image)
    {
        // Full-screen borderless dark overlay
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.FromArgb(0, 0, 5); // Near-perfect black
        Opacity = 0;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Bounds = owner.RectangleToScreen(new Rectangle(Point.Empty, owner.ClientSize));
        // Cover the whole owner
        Bounds = Screen.FromControl(owner).Bounds;
        TopMost = true;
        KeyPreview = true;

        // ── Close on Escape or click backdrop ────────────────────────────────
        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) FadeOut(); };
        Click   += (_, _) => FadeOut();

        // ── Close "×" button ─────────────────────────────────────────────────
        var btnClose = new Label
        {
            Text      = "✕",
            Font      = new Font("Segoe UI", 18f),
            ForeColor = Color.FromArgb(200, 200, 200),
            BackColor = Color.Transparent,
            AutoSize  = true,
            Cursor    = Cursors.Hand,
            Padding   = new Padding(10)
        };
        btnClose.Click += (_, _) => FadeOut();

        // ── Image display ─────────────────────────────────────────────────────
        var pic = new PictureBox
        {
            Image    = image,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };

        // Size image box to max 80% of screen, maintaining aspect ratio
        var screen = Screen.FromControl(owner).WorkingArea;
        int maxW = (int)(screen.Width  * 0.82);
        int maxH = (int)(screen.Height * 0.82);
        double ratio = Math.Min((double)maxW / image.Width, (double)maxH / image.Height);
        int picW = (int)(image.Width  * ratio);
        int picH = (int)(image.Height * ratio);
        pic.Size     = new Size(picW, picH);
        pic.Location = new Point((screen.Width - picW) / 2, (screen.Height - picH) / 2);
        pic.Click   += (_, _) => FadeOut();

        // Caption: resolution info
        var lblInfo = new Label
        {
            Text      = $"{image.Width} × {image.Height} px    •    ESC or click to close",
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Color.FromArgb(160, 160, 160),
            BackColor = Color.Transparent,
            AutoSize  = true,
        };

        Controls.Add(pic);
        Controls.Add(lblInfo);
        Controls.Add(btnClose);

        Load += (_, _) => {
            btnClose.Location = new Point(screen.Width - btnClose.Width - 20, 20);
            lblInfo.Location  = new Point((screen.Width - lblInfo.Width) / 2,
                                           pic.Bottom + 14);
            FadeIn();
        };
    }

    // ── Fade animation ────────────────────────────────────────────────────────
    private void FadeIn()
    {
        _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _fadeTimer.Tick += (_, _) => {
            Opacity += 0.07;
            if (Opacity >= _targetOpacity) { Opacity = _targetOpacity; _fadeTimer.Stop(); }
        };
        _fadeTimer.Start();
    }

    private void FadeOut()
    {
        _fadeTimer?.Stop();
        _fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
        _fadeTimer.Tick += (_, _) => {
            Opacity -= 0.09;
            if (Opacity <= 0) { _fadeTimer.Stop(); Close(); }
        };
        _fadeTimer.Start();
    }

    /// <summary>Show a lightbox over the given owner form.</summary>
    public static void Show(Form owner, Image image)
    {
        var lb = new ImageLightbox(owner, image);
        lb.Show(owner);
        lb.Activate();
    }
}
