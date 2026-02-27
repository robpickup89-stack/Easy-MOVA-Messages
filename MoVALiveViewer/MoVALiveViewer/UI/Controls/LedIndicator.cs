namespace MoVALiveViewer.UI.Controls;

public sealed class LedIndicator : Control
{
    private char _value = '0';
    private string _label = "";
    private bool _active;

    public char Value
    {
        get => _value;
        set { _value = value; _active = value != '0'; Invalidate(); }
    }

    public string Label
    {
        get => _label;
        set { _label = value; Invalidate(); }
    }

    public LedIndicator()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
        Size = new Size(36, 52);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var ledColor = _active ? GetColor() : Theme.LedOff;
        int ledSize = 20;
        int x = (Width - ledSize) / 2;
        int y = 4;

        // Glow effect when active
        if (_active)
        {
            using var glowBrush = new SolidBrush(Color.FromArgb(40, ledColor));
            g.FillEllipse(glowBrush, x - 4, y - 4, ledSize + 8, ledSize + 8);
            using var glowBrush2 = new SolidBrush(Color.FromArgb(20, ledColor));
            g.FillEllipse(glowBrush2, x - 8, y - 8, ledSize + 16, ledSize + 16);
        }

        // LED body
        using var brush = new SolidBrush(ledColor);
        g.FillEllipse(brush, x, y, ledSize, ledSize);

        // Highlight
        using var highlight = new SolidBrush(Color.FromArgb(_active ? 80 : 30, 255, 255, 255));
        g.FillEllipse(highlight, x + 3, y + 2, ledSize / 2, ledSize / 3);

        // Value text
        using var valueBrush = new SolidBrush(_active ? Theme.TextPrimary : Theme.TextMuted);
        var valueSize = g.MeasureString(_value.ToString(), Theme.FontLED);
        g.DrawString(_value.ToString(), Theme.FontLED, valueBrush,
            (Width - valueSize.Width) / 2, y + ledSize + 2);

        // Label
        if (!string.IsNullOrEmpty(_label))
        {
            using var labelBrush = new SolidBrush(Theme.TextMuted);
            var labelSize = g.MeasureString(_label, Theme.FontSmall);
            g.DrawString(_label, Theme.FontSmall, labelBrush,
                (Width - labelSize.Width) / 2, Height - labelSize.Height - 1);
        }
    }

    private Color GetColor()
    {
        return _value switch
        {
            '0' => Theme.LedOff,
            '1' => Theme.LedGreen,
            '2' => Theme.LedAmber,
            '3' => Theme.Yellow,
            >= '4' => Theme.LedRed,
            _ => Theme.LedOff
        };
    }
}
