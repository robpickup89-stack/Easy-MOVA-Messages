namespace MoVALiveViewer.UI;

public static class Theme
{
    // Dark theme colors
    public static readonly Color Background = Color.FromArgb(24, 24, 32);
    public static readonly Color Surface = Color.FromArgb(32, 33, 44);
    public static readonly Color SurfaceLight = Color.FromArgb(42, 43, 56);
    public static readonly Color SurfaceHover = Color.FromArgb(52, 53, 68);
    public static readonly Color Border = Color.FromArgb(58, 60, 78);
    public static readonly Color BorderLight = Color.FromArgb(72, 74, 92);

    public static readonly Color TextPrimary = Color.FromArgb(230, 233, 240);
    public static readonly Color TextSecondary = Color.FromArgb(160, 165, 180);
    public static readonly Color TextMuted = Color.FromArgb(110, 115, 130);

    public static readonly Color Accent = Color.FromArgb(99, 140, 255);
    public static readonly Color AccentDim = Color.FromArgb(60, 90, 180);
    public static readonly Color AccentGlow = Color.FromArgb(99, 140, 255, 40);

    public static readonly Color Green = Color.FromArgb(80, 200, 120);
    public static readonly Color GreenDim = Color.FromArgb(40, 120, 65);
    public static readonly Color Yellow = Color.FromArgb(255, 200, 60);
    public static readonly Color Orange = Color.FromArgb(255, 150, 50);
    public static readonly Color Red = Color.FromArgb(240, 80, 80);
    public static readonly Color Purple = Color.FromArgb(180, 120, 255);
    public static readonly Color Cyan = Color.FromArgb(80, 200, 220);

    // LED states
    public static readonly Color LedOff = Color.FromArgb(50, 52, 65);
    public static readonly Color LedGreen = Color.FromArgb(60, 220, 100);
    public static readonly Color LedRed = Color.FromArgb(240, 60, 60);
    public static readonly Color LedAmber = Color.FromArgb(255, 180, 40);

    // Fonts
    public static readonly Font FontTitle = new("Segoe UI", 18f, FontStyle.Bold);
    public static readonly Font FontSubtitle = new("Segoe UI Semibold", 13f);
    public static readonly Font FontBody = new("Segoe UI", 10f);
    public static readonly Font FontSmall = new("Segoe UI", 8.5f);
    public static readonly Font FontMono = new("Cascadia Code", 9.5f);
    public static readonly Font FontMonoSmall = new("Cascadia Code", 8.5f);
    public static readonly Font FontMonoLarge = new("Cascadia Code", 12f);
    public static readonly Font FontLabel = new("Segoe UI Semibold", 9f);
    public static readonly Font FontLED = new("Cascadia Code", 14f, FontStyle.Bold);

    public static void ApplyToForm(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        form.Font = FontBody;
    }

    public static void StyleDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = Surface;
        dgv.GridColor = Border;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowHeadersVisible = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToDeleteRows = false;
        dgv.ReadOnly = true;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgv.DefaultCellStyle.BackColor = Surface;
        dgv.DefaultCellStyle.ForeColor = TextPrimary;
        dgv.DefaultCellStyle.SelectionBackColor = AccentDim;
        dgv.DefaultCellStyle.SelectionForeColor = TextPrimary;
        dgv.DefaultCellStyle.Font = FontMono;
        dgv.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);

        dgv.AlternatingRowsDefaultCellStyle.BackColor = SurfaceLight;
        dgv.AlternatingRowsDefaultCellStyle.ForeColor = TextPrimary;
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = AccentDim;

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(38, 40, 55);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Accent;
        dgv.ColumnHeadersDefaultCellStyle.Font = FontLabel;
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 6, 4, 6);
        dgv.ColumnHeadersHeight = 34;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        dgv.RowTemplate.Height = 26;
    }

    public static void StylePanel(Panel panel)
    {
        panel.BackColor = Surface;
        panel.ForeColor = TextPrimary;
    }

    public static void StyleButton(Button btn, bool primary = false)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderColor = primary ? Accent : Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = primary ? AccentDim : SurfaceHover;
        btn.BackColor = primary ? AccentDim : SurfaceLight;
        btn.ForeColor = TextPrimary;
        btn.Font = FontLabel;
        btn.Cursor = Cursors.Hand;
    }

    public static void StyleComboBox(ComboBox cmb)
    {
        cmb.BackColor = SurfaceLight;
        cmb.ForeColor = TextPrimary;
        cmb.FlatStyle = FlatStyle.Flat;
        cmb.Font = FontBody;
    }

    public static void StyleTextBox(TextBox txt)
    {
        txt.BackColor = SurfaceLight;
        txt.ForeColor = TextPrimary;
        txt.BorderStyle = BorderStyle.FixedSingle;
        txt.Font = FontMono;
    }

    public static void StyleTabControl(TabControl tab)
    {
        tab.Font = FontLabel;
    }

    public static Label CreateSectionLabel(string text)
    {
        return new Label
        {
            Text = text.ToUpperInvariant(),
            ForeColor = TextMuted,
            Font = FontSmall,
            AutoSize = true,
            Padding = new Padding(0, 4, 0, 2)
        };
    }

    public static Label CreateValueLabel(string text, Font? font = null)
    {
        return new Label
        {
            Text = text,
            ForeColor = TextPrimary,
            Font = font ?? FontMonoLarge,
            AutoSize = true
        };
    }
}
