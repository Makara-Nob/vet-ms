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
}
