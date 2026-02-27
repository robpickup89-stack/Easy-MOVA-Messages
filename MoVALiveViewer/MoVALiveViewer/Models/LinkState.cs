namespace MoVALiveViewer.Models;

public sealed class LinkState
{
    public int LinkNo { get; set; }
    public string? ESLI { get; set; }
    public List<LAEntry> LAs { get; set; } = new();
    public List<BDREntry> BDRs { get; set; } = new();
    public string? DEM { get; set; }
    public string? DEMRaw { get; set; }
    public string? SDEM { get; set; }
    public int? IG { get; set; }
    public int? CF { get; set; }
    public int[]? RCX { get; set; }
    public int[]? RCIN { get; set; }
    public int[]? BON { get; set; }
    public int? OptBDR_A { get; set; }
    public int? OptBDR_B { get; set; }
    public int? OptBDR_C { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}
