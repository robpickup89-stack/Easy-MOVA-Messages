namespace MoVALiveViewer.UI.Controls;

public sealed class StatusBadge : Control
{
    private string _text = "Disconnected";
    private Color _badgeColor = Theme.TextMuted;
    private bool _pulse;
    private int _pulsePhase;
    private System.Windows.Forms.Timer? _pulseTimer;

    public new string Text
    {
        get => _text;
        set { _text = value; Invalidate(); }
    }

    public Color BadgeColor
    {
        get => _badgeColor;
        set { _badgeColor = value; Invalidate(); }
    }

    public bool Pulse
    {
        get => _pulse;
        set
        {
            _pulse = value;
            if (value)
            {
                _pulseTimer ??= new System.Windows.Forms.Timer { Interval = 80 };
                _pulseTimer.Tick += (_, _) => { _pulsePhase = (_pulsePhase + 1) % 20; Invalidate(); };
                _pulseTimer.Start();
            }
            else
            {
                _pulseTimer?.Stop();
                _pulsePhase = 0;
                Invalidate();
            }
        }
    }

    public StatusBadge()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
        Size = new Size(140, 24);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int dotRadius = 5;
        float alpha = _pulse ? 0.5f + 0.5f * (float)Math.Sin(_pulsePhase * Math.PI / 10.0) : 1f;
        var dotColor = Color.FromArgb((int)(255 * alpha), _badgeColor);

        using var dotBrush = new SolidBrush(dotColor);
        g.FillEllipse(dotBrush, 4, (Height - dotRadius * 2) / 2, dotRadius * 2, dotRadius * 2);

        using var textBrush = new SolidBrush(Theme.TextSecondary);
        g.DrawString(_text, Theme.FontLabel, textBrush, dotRadius * 2 + 10, (Height - Theme.FontLabel.Height) / 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pulseTimer?.Stop();
            _pulseTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
