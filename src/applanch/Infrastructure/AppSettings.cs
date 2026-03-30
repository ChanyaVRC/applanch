using System.IO;
using System.Text.Json;

namespace applanch;

internal enum AppTheme { System, Light, Dark }

internal sealed record AppSettings(bool DebugUpdate = false, bool CloseOnLaunch = true, AppTheme Theme = AppTheme.System, bool CheckForUpdatesOnStartup = true)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "applanch",
        "settings.json");

    public static AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Error(ex, "Failed to load settings");
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
