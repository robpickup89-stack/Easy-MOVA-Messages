namespace MoVALiveViewer.Models;

public sealed class EventRow
{
    public DateTime Time { get; set; }
    public string TypeLabel { get; set; } = string.Empty;
    public int? Stage { get; set; }
    public int? Link { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int SnapshotSeqId { get; set; }
    public string RawLine { get; set; } = string.Empty;
}
