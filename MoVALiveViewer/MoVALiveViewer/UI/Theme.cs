namespace MoVALiveViewer.UI;

public static class Theme
{
    public static bool IsDark { get; private set; } = true;

    // Colors (switched by Toggle)
    public static Color Background;
    public static Color Surface;
    public static Color SurfaceLight;
    public static Color SurfaceHover;
    public static Color Border;
    public static Color BorderLight;

    public static Color TextPrimary;
    public static Color TextSecondary;
    public static Color TextMuted;

    public static Color Accent;
    public static Color AccentDim;
    public static Color AccentGlow;

    public static Color Green;
    public static Color GreenDim;
    public static Color Yellow;
    public static Color Orange;
    public static Color Red;
    public static Color Purple;
    public static Color Cyan;

    // LED states
    public static Color LedOff;
    public static Color LedGreen;
    public static Color LedRed;
    public static Color LedAmber;

    // Fonts (always the same)
    public static readonly Font FontTitle = new("Segoe UI", 18f, FontStyle.Bold);
    public static readonly Font FontSubtitle = new("Segoe UI Semibold", 13f);
    public static readonly Font FontBody = new("Segoe UI", 10f);
    public static readonly Font FontSmall = new("Segoe UI", 8.5f);
    public static readonly Font FontMono = new("Cascadia Code", 9.5f);
    public static readonly Font FontMonoSmall = new("Cascadia Code", 8.5f);
    public static readonly Font FontMonoLarge = new("Cascadia Code", 12f);
    public static readonly Font FontLabel = new("Segoe UI Semibold", 9f);
    public static readonly Font FontLED = new("Cascadia Code", 14f, FontStyle.Bold);

    static Theme()
    {
        ApplyDark();
    }

    public static void SetDark(bool dark)
    {
        if (dark) ApplyDark();
        else ApplyLight();
    }

    public static void Toggle()
    {
        if (IsDark) ApplyLight();
        else ApplyDark();
    }

    private static void ApplyDark()
    {
        IsDark = true;

        Background = Color.FromArgb(24, 24, 32);
        Surface = Color.FromArgb(32, 33, 44);
        SurfaceLight = Color.FromArgb(42, 43, 56);
        SurfaceHover = Color.FromArgb(52, 53, 68);
        Border = Color.FromArgb(58, 60, 78);
        BorderLight = Color.FromArgb(72, 74, 92);

        TextPrimary = Color.FromArgb(230, 233, 240);
        TextSecondary = Color.FromArgb(160, 165, 180);
        TextMuted = Color.FromArgb(110, 115, 130);

        Accent = Color.FromArgb(99, 140, 255);
        AccentDim = Color.FromArgb(60, 90, 180);
        AccentGlow = Color.FromArgb(40, 99, 140, 255);

        Green = Color.FromArgb(80, 200, 120);
        GreenDim = Color.FromArgb(40, 120, 65);
        Yellow = Color.FromArgb(255, 200, 60);
        Orange = Color.FromArgb(255, 150, 50);
        Red = Color.FromArgb(240, 80, 80);
        Purple = Color.FromArgb(180, 120, 255);
        Cyan = Color.FromArgb(80, 200, 220);

        LedOff = Color.FromArgb(50, 52, 65);
        LedGreen = Color.FromArgb(60, 220, 100);
        LedRed = Color.FromArgb(240, 60, 60);
        LedAmber = Color.FromArgb(255, 180, 40);
    }

    private static void ApplyLight()
    {
        IsDark = false;

        Background = Color.FromArgb(245, 245, 248);
        Surface = Color.FromArgb(255, 255, 255);
        SurfaceLight = Color.FromArgb(235, 236, 240);
        SurfaceHover = Color.FromArgb(220, 222, 228);
        Border = Color.FromArgb(200, 202, 210);
        BorderLight = Color.FromArgb(215, 217, 225);

        TextPrimary = Color.FromArgb(28, 30, 38);
        TextSecondary = Color.FromArgb(90, 95, 110);
        TextMuted = Color.FromArgb(140, 145, 160);

        Accent = Color.FromArgb(37, 99, 235);
        AccentDim = Color.FromArgb(219, 234, 254);
        AccentGlow = Color.FromArgb(40, 37, 99, 235);

        Green = Color.FromArgb(34, 160, 80);
        GreenDim = Color.FromArgb(200, 240, 210);
        Yellow = Color.FromArgb(200, 150, 0);
        Orange = Color.FromArgb(220, 120, 20);
        Red = Color.FromArgb(210, 50, 50);
        Purple = Color.FromArgb(130, 80, 220);
        Cyan = Color.FromArgb(20, 150, 180);

        LedOff = Color.FromArgb(200, 205, 215);
        LedGreen = Color.FromArgb(60, 220, 100);
        LedRed = Color.FromArgb(240, 60, 60);
        LedAmber = Color.FromArgb(255, 180, 40);
    }

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

        dgv.ColumnHeadersDefaultCellStyle.BackColor = IsDark ? Color.FromArgb(38, 40, 55) : Color.FromArgb(228, 230, 238);
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

    /// <summary>
    /// Recursively re-applies current theme colors to a control tree.
    /// Custom-painted controls just need Invalidate(); standard controls need property updates.
    /// </summary>
    public static void ReapplyRecursive(Control root)
    {
        foreach (Control ctrl in root.Controls)
        {
            switch (ctrl)
            {
                case DataGridView dgv:
                    StyleDataGridView(dgv);
                    break;
                case RichTextBox rtb:
                    rtb.BackColor = Surface;
                    rtb.ForeColor = TextPrimary;
                    break;
                case TreeView tv:
                    tv.BackColor = Surface;
                    tv.ForeColor = TextPrimary;
                    tv.LineColor = Border;
                    break;
                case TabControl tc:
                    foreach (TabPage tp in tc.TabPages)
                    {
                        tp.BackColor = Background;
                        ReapplyRecursive(tp);
                    }
                    break;
                case Button btn:
                    StyleButton(btn);
                    break;
            }

            // Recurse into children
            if (ctrl.Controls.Count > 0)
                ReapplyRecursive(ctrl);

            ctrl.Invalidate();
        }

        root.Invalidate();
    }
}
