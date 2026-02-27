using MoVALiveViewer.Models;

namespace MoVALiveViewer.State;

public sealed class AlertManager
{
    private readonly List<AlertInfo> _alerts = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    private DateTime _lastDataReceived = DateTime.Now;
    private DateTime _lastStageChange = DateTime.Now;
    private DateTime _lastOptReceived = DateTime.Now;
    private readonly Dictionary<int, DateTime> _demActiveLinks = new();

    // Configurable thresholds (seconds)
    public int NoDataThresholdSec { get; set; } = 30;
    public int NoStageChangeThresholdSec { get; set; } = 180;
    public int NoOptThresholdSec { get; set; } = 120;
    public int SatOverThresholdSec { get; set; } = 60;
    public int DemStuckThresholdSec { get; set; } = 120;

    public event Action<AlertInfo>? AlertRaised;
    public event Action<AlertInfo>? AlertCleared;

    public void OnDataReceived()
    {
        _lastDataReceived = DateTime.Now;
        TryClear("NoData");
    }

    public void OnStageChanged(Snapshot snapshot)
    {
        _lastStageChange = DateTime.Now;
        TryClear("NoStageChange");
    }

    public void OnOptReceived(int linkNo)
    {
        _lastOptReceived = DateTime.Now;
        TryClear("NoOpt");
    }

    public void OnSATUpdated(string? sat, Snapshot snapshot)
    {
        if (string.IsNullOrEmpty(sat)) return;

        bool oversat = sat.Any(c => c >= '4' && c <= '9');
        if (oversat)
            TryRaise("SATOver", "Capacity mode detected", AlertSeverity.Warning, snapshot.SequenceId);
        else
            TryClear("SATOver");
    }

    public void OnDEMUpdated(int linkNo, string? dem)
    {
        if (string.IsNullOrEmpty(dem) || dem.All(c => c == '0' || c == ' '))
        {
            _demActiveLinks.Remove(linkNo);
            if (_demActiveLinks.Count == 0)
                TryClear($"DemStuck_{linkNo}");
            return;
        }

        if (!_demActiveLinks.ContainsKey(linkNo))
            _demActiveLinks[linkNo] = DateTime.Now;
    }

    public void PeriodicCheck()
    {
        var now = DateTime.Now;

        if ((now - _lastDataReceived).TotalSeconds > NoDataThresholdSec)
            TryRaise("NoData", $"No data for {NoDataThresholdSec}s", AlertSeverity.Critical, null);

        if ((now - _lastStageChange).TotalSeconds > NoStageChangeThresholdSec)
            TryRaise("NoStageChange", $"No stage change for {NoStageChangeThresholdSec}s", AlertSeverity.Warning, null);

        if ((now - _lastOptReceived).TotalSeconds > NoOptThresholdSec)
            TryRaise("NoOpt", $"No OPT blocks for {NoOptThresholdSec}s", AlertSeverity.Info, null);

        foreach (var kv in _demActiveLinks.ToList())
        {
            if ((now - kv.Value).TotalSeconds > DemStuckThresholdSec)
                TryRaise($"DemStuck_{kv.Key}", $"DEM stuck on link {kv.Key} for {DemStuckThresholdSec}s", AlertSeverity.Warning, null);
        }
    }

    public void Acknowledge(int alertId)
    {
        lock (_lock)
        {
            var alert = _alerts.FirstOrDefault(a => a.Id == alertId);
            if (alert != null) alert.Acknowledged = true;
        }
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            foreach (var a in _alerts.Where(a => a.IsActive))
                a.ClearedAt = DateTime.Now;
        }
    }

    public AlertInfo[] GetActive()
    {
        lock (_lock)
            return _alerts.Where(a => a.IsActive).ToArray();
    }

    public AlertInfo[] GetHistory(int max = 100)
    {
        lock (_lock)
            return _alerts.TakeLast(max).Reverse().ToArray();
    }

    private void TryRaise(string rule, string message, AlertSeverity severity, int? snapId)
    {
        lock (_lock)
        {
            if (_alerts.Any(a => a.Rule == rule && a.IsActive))
                return;

            var alert = new AlertInfo
            {
                Id = _nextId++,
                Rule = rule,
                Message = message,
                Severity = severity,
                RaisedAt = DateTime.Now,
                SnapshotSeqId = snapId
            };
            _alerts.Add(alert);
            AlertRaised?.Invoke(alert);
        }
    }

    private void TryClear(string rule)
    {
        lock (_lock)
        {
            var active = _alerts.Where(a => a.Rule == rule && a.IsActive).ToList();
            foreach (var a in active)
            {
                a.ClearedAt = DateTime.Now;
                AlertCleared?.Invoke(a);
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _alerts.Clear();
            _demActiveLinks.Clear();
            _lastDataReceived = DateTime.Now;
            _lastStageChange = DateTime.Now;
            _lastOptReceived = DateTime.Now;
        }
    }
}
