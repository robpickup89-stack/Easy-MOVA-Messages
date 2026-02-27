namespace MoVALiveViewer.Models;

public enum AlertSeverity { Info, Warning, Critical }

public sealed class AlertInfo
{
    public int Id { get; set; }
    public string Rule { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime RaisedAt { get; set; }
    public DateTime? ClearedAt { get; set; }
    public int? SnapshotSeqId { get; set; }
    public bool IsActive => ClearedAt == null;
    public bool Acknowledged { get; set; }
}
