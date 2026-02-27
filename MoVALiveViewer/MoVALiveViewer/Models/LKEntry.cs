namespace MoVALiveViewer.Models;

public sealed class LKEntry
{
    public int LaneIndex { get; set; }
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
    public int D { get; set; }

    public override string ToString() => $"{LaneIndex}LK {A} {B} {C} {D}";
}
