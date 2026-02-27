namespace MoVALiveViewer.Models;

public sealed class Snapshot
{
    public DateTime Time { get; set; }
    public TimeSpan TimeOfDay { get; set; }
    public int Stage { get; set; }
    public int SequenceId { get; set; }

    public Dictionary<string, object> StageFields { get; set; } = new();
    public Dictionary<int, LinkState> Links { get; set; } = new();
    public List<ParsedRecord> Records { get; set; } = new();

    // Convenience accessors
    public int? SMCYC => StageFields.TryGetValue("SMCYC", out var v) && v is int i ? i : null;
    public int? LAM => StageFields.TryGetValue("LAM", out var v) && v is int i ? i : null;
    public int? CUT => StageFields.TryGetValue("CUT", out var v) && v is int i ? i : null;
    public string? SAT => StageFields.TryGetValue("SAT", out var v) ? v?.ToString() : null;
    public int? SMIN => StageFields.TryGetValue("SMIN", out var v) && v is int i ? i : null;
    public int[]? SMF => StageFields.TryGetValue("SMF", out var v) && v is int[] arr ? arr : null;
    public int[]? DMX => StageFields.TryGetValue("DMX", out var v) && v is int[] arr ? arr : null;

    public LinkState GetOrCreateLink(int linkNo)
    {
        if (!Links.TryGetValue(linkNo, out var ls))
        {
            ls = new LinkState { LinkNo = linkNo };
            Links[linkNo] = ls;
        }
        return ls;
    }

    public Snapshot Clone()
    {
        var s = new Snapshot
        {
            Time = Time,
            TimeOfDay = TimeOfDay,
            Stage = Stage,
            SequenceId = SequenceId,
            StageFields = new Dictionary<string, object>(StageFields)
        };
        foreach (var kv in Links)
        {
            s.Links[kv.Key] = new LinkState
            {
                LinkNo = kv.Value.LinkNo,
                ESLI = kv.Value.ESLI,
                LAs = new List<LAEntry>(kv.Value.LAs),
                BDRs = new List<BDREntry>(kv.Value.BDRs),
                DEM = kv.Value.DEM,
                DEMRaw = kv.Value.DEMRaw,
                SDEM = kv.Value.SDEM,
                IG = kv.Value.IG,
                CF = kv.Value.CF,
                RCX = kv.Value.RCX != null ? (int[])kv.Value.RCX.Clone() : null,
                RCIN = kv.Value.RCIN != null ? (int[])kv.Value.RCIN.Clone() : null,
                BON = kv.Value.BON != null ? (int[])kv.Value.BON.Clone() : null,
                OptBDR_A = kv.Value.OptBDR_A,
                OptBDR_B = kv.Value.OptBDR_B,
                OptBDR_C = kv.Value.OptBDR_C,
                LastUpdated = kv.Value.LastUpdated
            };
        }
        return s;
    }
}
