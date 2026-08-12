using DesktopLoudnessMeter.App.Localization;

namespace DesktopLoudnessMeter.App;

public sealed record AboutCredit(string Name, string Role);

internal static class AboutCredits
{
    internal const string RepositoryUrl = "https://github.com/Campfireman0314/uMeter";

    internal static IReadOnlyList<AboutCredit> CreateLocalizedEntries()
    {
        return
        [
            new("Lin Yudong", LocalizationManager.Translate("Development and project direction")),
            new("wangzhe10", LocalizationManager.Translate("Project contributor"))
        ];
    }
}
