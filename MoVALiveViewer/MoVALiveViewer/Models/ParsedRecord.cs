namespace MoVALiveViewer.Models;

public sealed class ParsedRecord
{
    public DateTime Timestamp { get; set; }
    public TimeSpan TimeOfDay { get; set; }
    public RecordType Type { get; set; }
    public int? Stage { get; set; }
    public int? Link { get; set; }
    public string RawLine { get; set; } = string.Empty;
    public Dictionary<string, object> Fields { get; set; } = new();

    public string Summary
    {
        get
        {
            return Type switch
            {
                RecordType.StageHeader => $"Stage {Stage} @ {TimeOfDay:hh\\:mm\\:ss}",
                RecordType.StageDetail => Fields.TryGetValue("SMCYC", out var cyc) ? $"SMCYC={cyc}" : "Stage detail",
                RecordType.StageMinLine => Fields.TryGetValue("SMIN", out var sm) ? $"SMIN={sm}" : "Min line",
                RecordType.NXHeader => $"NX {Link} ESLI",
                RecordType.NXBDR => $"NX {Link} BDR",
                RecordType.NXOPT => $"NX {Link} OPT" + (Fields.TryGetValue("CF", out var cf) ? $" CF={cf}" : ""),
                RecordType.NXContinuation => $"NX {Link} IG/SDEM",
                _ => RawLine.Length > 60 ? RawLine[..60] + "..." : RawLine
            };
        }
    }
}
