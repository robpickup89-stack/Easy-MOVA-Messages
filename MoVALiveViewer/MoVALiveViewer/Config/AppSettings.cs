using System.Text.Json;

namespace MoVALiveViewer.Config;

public sealed class AppSettings
{
    public string SourceMode { get; set; } = "FileReplay";
    public string? ProcessName { get; set; }
    public string? WindowTitle { get; set; }
    public string? AutomationId { get; set; }
    public int PollIntervalMs { get; set; } = 200;
    public int UiRefreshMs { get; set; } = 250;
    public int RingBufferSize { get; set; } = 1000;
    public string? LastReplayFile { get; set; }
    public string? LastReplaySpeed { get; set; } = "Fast200";
    public bool FilterStageHeaders { get; set; }
    public bool FilterOPTBDR { get; set; }
    public bool FilterDEM { get; set; }
    public bool FilterNX { get; set; }
    public int WindowWidth { get; set; } = 1400;
    public int WindowHeight { get; set; } = 900;

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MoVALiveViewer",
        "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
