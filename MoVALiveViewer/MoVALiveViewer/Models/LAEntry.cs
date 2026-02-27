namespace MoVALiveViewer.Models;

public sealed class LAEntry
{
    public int LaneIndex { get; set; }
    public int V1 { get; set; }
    public int V2 { get; set; }
    public int V3 { get; set; }

    public override string ToString() => $"{LaneIndex}LA {V1} {V2} {V3}";
}
