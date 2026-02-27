namespace MoVALiveViewer.Models;

public sealed class BDREntry
{
    public int A { get; set; }
    public int B { get; set; }
    public int C { get; set; }
    public List<LKEntry> LKEntries { get; set; } = new();

    public override string ToString() => $"BDR {A} {B} {C} [{LKEntries.Count} LK]";
}
