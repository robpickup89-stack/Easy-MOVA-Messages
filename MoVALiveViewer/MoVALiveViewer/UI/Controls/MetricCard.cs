namespace MoVALiveViewer.UI.Controls;

public sealed class MetricCard : Control
{
    private string _title = "";
    private string _value = "--";
    private string _subtitle = "";
    private Color _accentColor = Theme.Accent;

    public string Title
    {
        get => _title;
        set { _title = value; Invalidate(); }
    }

    public string Value
    {
        get => _value;
        set { _value = value; Invalidate(); }
    }

    public string Subtitle
    {
        get => _subtitle;
        set { _subtitle = value; Invalidate(); }
    }

    public Color AccentColor
    {
        get => _accentColor;
        set { _accentColor = value; Invalidate(); }
    }

    public MetricCard()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(160, 100);
        BackColor = Theme.Surface;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Background with rounded corners
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var bgPath = RoundedRect(rect, 8);
        using var bgBrush = new SolidBrush(Theme.SurfaceLight);
        g.FillPath(bgBrush, bgPath);

        // Accent bar on left
        using var accentBrush = new SolidBrush(_accentColor);
        g.FillRectangle(accentBrush, 0, 8, 3, Height - 16);

        // Title
        using var titleBrush = new SolidBrush(Theme.TextMuted);
        g.DrawString(_title.ToUpperInvariant(), Theme.FontSmall, titleBrush, 12, 10);

        // Value
        using var valueBrush = new SolidBrush(Theme.TextPrimary);
        g.DrawString(_value, Theme.FontTitle, valueBrush, 12, 28);

        // Subtitle
        if (!string.IsNullOrEmpty(_subtitle))
        {
            using var subBrush = new SolidBrush(Theme.TextSecondary);
            g.DrawString(_subtitle, Theme.FontSmall, subBrush, 12, Height - 22);
        }

        // Border
        using var borderPen = new Pen(Theme.Border, 1);
        g.DrawPath(borderPen, bgPath);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
