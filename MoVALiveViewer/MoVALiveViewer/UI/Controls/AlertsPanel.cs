using MoVALiveViewer.Models;
using MoVALiveViewer.State;

namespace MoVALiveViewer.UI.Controls;

public sealed class AlertsPanel : Panel
{
    private readonly DataGridView _activeGrid = new();
    private readonly DataGridView _historyGrid = new();
    private readonly Panel _buttonPanel = new();

    public event Action<int>? AlertAcknowledged;
    public event Action? AlertsClearAll;

    public AlertsPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        Padding = new Padding(8);

        BuildLayout();
    }

    private void BuildLayout()
    {
        // Button panel
        _buttonPanel.Dock = DockStyle.Top;
        _buttonPanel.Height = 36;
        _buttonPanel.BackColor = Color.Transparent;

        var ackBtn = new Button { Text = "Acknowledge", Width = 110, Location = new Point(0, 4) };
        Theme.StyleButton(ackBtn);
        ackBtn.Click += (_, _) =>
        {
            if (_activeGrid.SelectedRows.Count > 0 && _activeGrid.SelectedRows[0].Tag is int id)
                AlertAcknowledged?.Invoke(id);
        };

        var clearBtn = new Button { Text = "Clear All", Width = 90, Location = new Point(120, 4) };
        Theme.StyleButton(clearBtn);
        clearBtn.Click += (_, _) => AlertsClearAll?.Invoke();

        _buttonPanel.Controls.Add(ackBtn);
        _buttonPanel.Controls.Add(clearBtn);

        // Active alerts
        var activeLabel = Theme.CreateSectionLabel("  ACTIVE ALERTS");
        activeLabel.Dock = DockStyle.Top;
        activeLabel.Height = 24;

        _activeGrid.Dock = DockStyle.Top;
        _activeGrid.Height = 200;
        Theme.StyleDataGridView(_activeGrid);
        _activeGrid.Columns.Add("Severity", "Severity");
        _activeGrid.Columns.Add("Rule", "Rule");
        _activeGrid.Columns.Add("Message", "Message");
        _activeGrid.Columns.Add("RaisedAt", "Raised At");
        _activeGrid.Columns.Add("Acked", "Acked");

        _activeGrid.Columns["Severity"].FillWeight = 12;
        _activeGrid.Columns["Rule"].FillWeight = 18;
        _activeGrid.Columns["Message"].FillWeight = 35;
        _activeGrid.Columns["RaisedAt"].FillWeight = 20;
        _activeGrid.Columns["Acked"].FillWeight = 10;

        // History
        var histLabel = Theme.CreateSectionLabel("  ALERT HISTORY");
        histLabel.Dock = DockStyle.Top;
        histLabel.Height = 24;

        _historyGrid.Dock = DockStyle.Fill;
        Theme.StyleDataGridView(_historyGrid);
        _historyGrid.Columns.Add("Severity", "Severity");
        _historyGrid.Columns.Add("Rule", "Rule");
        _historyGrid.Columns.Add("Message", "Message");
        _historyGrid.Columns.Add("RaisedAt", "Raised");
        _historyGrid.Columns.Add("ClearedAt", "Cleared");

        _historyGrid.Columns["Severity"].FillWeight = 12;
        _historyGrid.Columns["Rule"].FillWeight = 18;
        _historyGrid.Columns["Message"].FillWeight = 30;
        _historyGrid.Columns["RaisedAt"].FillWeight = 18;
        _historyGrid.Columns["ClearedAt"].FillWeight = 18;

        var sep = new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent };

        Controls.Add(_historyGrid);
        Controls.Add(histLabel);
        Controls.Add(sep);
        Controls.Add(_activeGrid);
        Controls.Add(activeLabel);
        Controls.Add(_buttonPanel);
    }

    public void UpdateAlerts(AlertManager alerts)
    {
        var active = alerts.GetActive();
        _activeGrid.Rows.Clear();
        foreach (var a in active)
        {
            int rowIdx = _activeGrid.Rows.Add(
                a.Severity.ToString(),
                a.Rule,
                a.Message,
                a.RaisedAt.ToString("HH:mm:ss"),
                a.Acknowledged ? "Yes" : ""
            );
            _activeGrid.Rows[rowIdx].Tag = a.Id;

            var color = a.Severity switch
            {
                AlertSeverity.Critical => Theme.Red,
                AlertSeverity.Warning => Theme.Yellow,
                _ => Theme.TextSecondary
            };

            _activeGrid.Rows[rowIdx].Cells[0].Style.ForeColor = color;
            _activeGrid.Rows[rowIdx].Cells[2].Style.ForeColor = color;
        }

        var history = alerts.GetHistory();
        _historyGrid.Rows.Clear();
        foreach (var a in history)
        {
            _historyGrid.Rows.Add(
                a.Severity.ToString(),
                a.Rule,
                a.Message,
                a.RaisedAt.ToString("HH:mm:ss"),
                a.ClearedAt?.ToString("HH:mm:ss") ?? (a.IsActive ? "Active" : "")
            );
        }
    }
}
