using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.App.Settings;

namespace DesktopLoudnessMeter.App.Themes;

public static class UserThemeSettings
{
    public static string? LoadThemeId()
    {
        try
        {
            return UserWorkspaceSettings.Load().ThemeId;
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
            var settings = UserWorkspaceSettings.Load();
            settings.ThemeId = themeId;
            settings.Save();
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to save user theme settings.", ex);
        }
    }
}
