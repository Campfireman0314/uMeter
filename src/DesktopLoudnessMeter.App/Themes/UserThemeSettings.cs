using System.IO;
using System.Text.Json;
using DesktopLoudnessMeter.App.Diagnostics;

namespace DesktopLoudnessMeter.App.Themes;

public static class UserThemeSettings
{
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string? LoadThemeId()
    {
        try
        {
            var path = GetSettingsPath();
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SettingsDto>(json)?.ThemeId;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to load user theme settings.", ex);
            return null;
        }
    }

    public static void SaveThemeId(string themeId)
    {
        try
        {
            var path = GetSettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(new SettingsDto(themeId), JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to save user theme settings.", ex);
        }
    }

    private static string GetSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "uMeter",
            SettingsFileName);
    }

    private sealed record SettingsDto(string? ThemeId);
}
