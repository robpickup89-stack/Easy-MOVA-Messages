using MoVALiveViewer.Models;
using MoVALiveViewer.State;

namespace MoVALiveViewer.UI.Controls;

public sealed class StagesPanel : Panel
{
    private readonly DataGridView _stagesGrid = new();
    private readonly Panel _chartPanel = new();

    public StagesPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(8);

        BuildLayout();
    }

    private void BuildLayout()
    {
        var headerLabel = Theme.CreateSectionLabel("  STAGE HISTORY");
        headerLabel.Dock = DockStyle.Top;
        headerLabel.Height = 24;

        _stagesGrid.Dock = DockStyle.Fill;
        Theme.StyleDataGridView(_stagesGrid);

        _stagesGrid.Columns.Add("Time", "Time");
        _stagesGrid.Columns.Add("Stage", "Stage");
        _stagesGrid.Columns.Add("SMCYC", "SMCYC");
        _stagesGrid.Columns.Add("LAM", "LAM");
        _stagesGrid.Columns.Add("CUT", "CUT");
        _stagesGrid.Columns.Add("SAT", "SAT");
        _stagesGrid.Columns.Add("SMIN", "SMIN");
        _stagesGrid.Columns.Add("Links", "Links");

        _stagesGrid.Columns["Time"].FillWeight = 15;
        _stagesGrid.Columns["Stage"].FillWeight = 8;
        _stagesGrid.Columns["SMCYC"].FillWeight = 10;
        _stagesGrid.Columns["LAM"].FillWeight = 8;
        _stagesGrid.Columns["CUT"].FillWeight = 8;
        _stagesGrid.Columns["SAT"].FillWeight = 15;
        _stagesGrid.Columns["SMIN"].FillWeight = 8;
        _stagesGrid.Columns["Links"].FillWeight = 10;

        // Chart panel for stage trend
        _chartPanel.Dock = DockStyle.Bottom;
        _chartPanel.Height = 120;
        _chartPanel.BackColor = Theme.SurfaceLight;
        _chartPanel.Padding = new Padding(8);
        _chartPanel.Paint += ChartPanel_Paint;

        var sep = new Panel { Dock = DockStyle.Bottom, Height = 4, BackColor = Color.Transparent };

        Controls.Add(_stagesGrid);
        Controls.Add(sep);
        Controls.Add(_chartPanel);
        Controls.Add(headerLabel);
    }

    private readonly List<(TimeSpan time, int stage, int? smcyc)> _chartData = new();

    public void UpdateSnapshots(RingBuffer<Snapshot> snapshots, Snapshot? current)
    {
        _stagesGrid.Rows.Clear();
        _chartData.Clear();

        var allSnapshots = snapshots.GetLast(200);
        if (current != null)
            allSnapshots = allSnapshots.Append(current).ToArray();

        foreach (var snap in allSnapshots.Reverse())
        {
            _stagesGrid.Rows.Add(
                snap.TimeOfDay.ToString(@"hh\:mm\:ss"),
                snap.Stage,
                snap.SMCYC?.ToString() ?? "--",
                snap.LAM?.ToString() ?? "--",
                snap.CUT?.ToString() ?? "--",
                snap.SAT ?? "--",
                snap.SMIN?.ToString() ?? "--",
                snap.Links.Count
            );

            _chartData.Add((snap.TimeOfDay, snap.Stage, snap.SMCYC));
        }

        _chartData.Reverse();
        _chartPanel.Invalidate();
    }

    private void ChartPanel_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = _chartPanel.ClientRectangle;
        rect.Inflate(-8, -8);

        if (_chartData.Count < 2)
        {
            using var noBrush = new SolidBrush(Theme.TextMuted);
            g.DrawString("Waiting for stage data...", Theme.FontSmall, noBrush, rect.X + 4, rect.Y + 4);
            return;
        }

        // Draw stage line chart
        int maxStage = _chartData.Max(d => d.stage);
        maxStage = Math.Max(maxStage, 4);

        float xStep = (float)rect.Width / Math.Max(_chartData.Count - 1, 1);

        using var stagePen = new Pen(Theme.Accent, 2);
        using var dotBrush = new SolidBrush(Theme.Accent);
        using var gridPen = new Pen(Theme.Border, 1) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };

        // Grid lines
        for (int s = 1; s <= maxStage; s++)
        {
            float y = rect.Bottom - ((float)s / maxStage * rect.Height);
            g.DrawLine(gridPen, rect.Left, y, rect.Right, y);
            using var labelBrush = new SolidBrush(Theme.TextMuted);
            g.DrawString($"S{s}", Theme.FontSmall, labelBrush, rect.Left - 2, y - 6);
        }

        var points = new PointF[_chartData.Count];
        for (int i = 0; i < _chartData.Count; i++)
        {
            float x = rect.Left + i * xStep;
            float y = rect.Bottom - ((float)_chartData[i].stage / maxStage * rect.Height);
            points[i] = new PointF(x, y);
        }

        if (points.Length >= 2)
            g.DrawLines(stagePen, points);

        foreach (var pt in points)
            g.FillEllipse(dotBrush, pt.X - 3, pt.Y - 3, 6, 6);
    }
}
