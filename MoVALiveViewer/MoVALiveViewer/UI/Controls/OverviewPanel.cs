using MoVALiveViewer.Models;

namespace MoVALiveViewer.UI.Controls;

public sealed class OverviewPanel : Panel
{
    private readonly MetricCard _stageCard = new() { Title = "Stage", AccentColor = Theme.Accent };
    private readonly MetricCard _timeCard = new() { Title = "Time", AccentColor = Theme.Cyan };
    private readonly MetricCard _cycleCard = new() { Title = "SMCYC", AccentColor = Theme.Green };
    private readonly MetricCard _lamCard = new() { Title = "Lambda", AccentColor = Theme.Purple };
    private readonly MetricCard _cutCard = new() { Title = "CUT", AccentColor = Theme.Orange };
    private readonly MetricCard _sminCard = new() { Title = "SMIN", AccentColor = Theme.Yellow };

    private readonly Panel _satPanel = new();
    private readonly Label _satLabel = new();
    private readonly FlowLayoutPanel _satLeds = new();
    private readonly List<LedIndicator> _satIndicators = new();

    private readonly Panel _demSummaryPanel = new();
    private readonly Label _demLabel = new();
    private readonly DataGridView _fieldsGrid = new();

    private readonly Label _igLabel = new();

    public OverviewPanel()
    {
        AutoScroll = true;
        BackColor = Theme.Background;
        Dock = DockStyle.Fill;
        Padding = new Padding(16);

        BuildLayout();
    }

    private void BuildLayout()
    {
        // Metrics row
        var metricsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 116,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 8),
            BackColor = Color.Transparent,
            AutoSize = false
        };

        foreach (var card in new[] { _stageCard, _timeCard, _cycleCard, _lamCard, _cutCard, _sminCard })
        {
            card.Size = new Size(170, 100);
            card.Margin = new Padding(0, 0, 12, 8);
            metricsFlow.Controls.Add(card);
        }

        // SAT panel
        _satPanel.Dock = DockStyle.Top;
        _satPanel.Height = 80;
        _satPanel.BackColor = Theme.SurfaceLight;
        _satPanel.Padding = new Padding(12, 8, 12, 8);

        _satLabel.Text = "SAT FLAGS";
        _satLabel.ForeColor = Theme.TextMuted;
        _satLabel.Font = Theme.FontSmall;
        _satLabel.Dock = DockStyle.Top;
        _satLabel.Height = 18;

        _satLeds.Dock = DockStyle.Fill;
        _satLeds.FlowDirection = FlowDirection.LeftToRight;
        _satLeds.BackColor = Color.Transparent;

        for (int i = 0; i < 8; i++)
        {
            var led = new LedIndicator { Label = $"S{i + 1}", Margin = new Padding(2) };
            _satIndicators.Add(led);
            _satLeds.Controls.Add(led);
        }

        _satPanel.Controls.Add(_satLeds);
        _satPanel.Controls.Add(_satLabel);

        // DEM/SDEM summary
        _demSummaryPanel.Dock = DockStyle.Top;
        _demSummaryPanel.Height = 60;
        _demSummaryPanel.BackColor = Color.Transparent;
        _demSummaryPanel.Padding = new Padding(0, 8, 0, 4);

        _demLabel.Dock = DockStyle.Fill;
        _demLabel.ForeColor = Theme.Orange;
        _demLabel.Font = Theme.FontMono;
        _demLabel.TextAlign = ContentAlignment.MiddleLeft;
        _demLabel.Text = "DEM: --   SDEM: --";

        _igLabel.Dock = DockStyle.Right;
        _igLabel.Width = 200;
        _igLabel.ForeColor = Theme.Cyan;
        _igLabel.Font = Theme.FontMono;
        _igLabel.TextAlign = ContentAlignment.MiddleRight;
        _igLabel.Text = "IG: --";

        _demSummaryPanel.Controls.Add(_demLabel);
        _demSummaryPanel.Controls.Add(_igLabel);

        // Fields grid
        _fieldsGrid.Dock = DockStyle.Fill;
        Theme.StyleDataGridView(_fieldsGrid);
        _fieldsGrid.Columns.Add("Key", "Field");
        _fieldsGrid.Columns.Add("Value", "Value");
        _fieldsGrid.Columns[0].FillWeight = 30;
        _fieldsGrid.Columns[1].FillWeight = 70;

        // Separator
        var sep1 = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };
        var sep2 = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Color.Transparent };
        var sep3 = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = Color.Transparent };

        Controls.Add(_fieldsGrid);
        Controls.Add(sep3);
        Controls.Add(_demSummaryPanel);
        Controls.Add(sep2);
        Controls.Add(_satPanel);
        Controls.Add(sep1);
        Controls.Add(metricsFlow);
    }

    public void UpdateSnapshot(Snapshot? snapshot)
    {
        if (snapshot == null)
        {
            _stageCard.Value = "--";
            _timeCard.Value = "--:--:--";
            _cycleCard.Value = "--";
            _lamCard.Value = "--";
            _cutCard.Value = "--";
            _sminCard.Value = "--";
            _demLabel.Text = "DEM: --   SDEM: --";
            _igLabel.Text = "IG: --";
            foreach (var led in _satIndicators) led.Value = '0';
            _fieldsGrid.Rows.Clear();
            return;
        }

        _stageCard.Value = snapshot.Stage.ToString();
        _stageCard.Subtitle = $"Seq #{snapshot.SequenceId}";

        _timeCard.Value = snapshot.TimeOfDay.ToString(@"hh\:mm\:ss");
        _timeCard.Subtitle = snapshot.Time.ToString("HH:mm:ss");

        _cycleCard.Value = snapshot.SMCYC?.ToString() ?? "--";
        _lamCard.Value = snapshot.LAM?.ToString() ?? "--";
        _cutCard.Value = snapshot.CUT?.ToString() ?? "--";
        _sminCard.Value = snapshot.SMIN?.ToString() ?? "--";

        // SAT LEDs
        var sat = snapshot.SAT?.Replace(" ", "") ?? "";
        for (int i = 0; i < _satIndicators.Count; i++)
        {
            _satIndicators[i].Value = i < sat.Length ? sat[i] : '0';
        }

        // DEM/SDEM summary from links
        var demParts = new List<string>();
        var sdemParts = new List<string>();
        var igParts = new List<string>();

        foreach (var kv in snapshot.Links.OrderBy(k => k.Key))
        {
            if (!string.IsNullOrEmpty(kv.Value.DEM))
                demParts.Add($"L{kv.Key}:{kv.Value.DEM}");
            if (!string.IsNullOrEmpty(kv.Value.SDEM))
                sdemParts.Add($"L{kv.Key}:{kv.Value.SDEM}");
            if (kv.Value.IG.HasValue)
                igParts.Add($"L{kv.Key}:IG{kv.Value.IG}");
        }

        _demLabel.Text = $"DEM: {(demParts.Count > 0 ? string.Join("  ", demParts) : "--")}   " +
                         $"SDEM: {(sdemParts.Count > 0 ? string.Join("  ", sdemParts) : "--")}";
        _igLabel.Text = igParts.Count > 0 ? string.Join("  ", igParts) : "IG: --";

        // Fields grid
        _fieldsGrid.Rows.Clear();
        foreach (var kv in snapshot.StageFields.OrderBy(k => k.Key))
        {
            string val = kv.Value switch
            {
                int[] arr => string.Join(", ", arr),
                string[] sarr => string.Join(", ", sarr),
                _ => kv.Value?.ToString() ?? ""
            };
            _fieldsGrid.Rows.Add(kv.Key, val);
        }
    }
}
