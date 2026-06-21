using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Themes;

public static class AppThemes
{
    public static IReadOnlyList<AppTheme> All { get; } =
    [
        new(
            "midnight",
            "Midnight",
            Color.FromRgb(22, 25, 31),
            Color.FromRgb(32, 37, 45),
            Color.FromRgb(43, 49, 58),
            Color.FromRgb(46, 196, 182),
            Color.FromRgb(255, 176, 0),
            Color.FromRgb(244, 247, 250),
            Color.FromRgb(174, 183, 194),
            Color.FromRgb(37, 43, 52),
            Color.FromRgb(48, 56, 68),
            Color.FromRgb(53, 65, 77),
            Color.FromRgb(70, 82, 96),
            Color.FromRgb(111, 122, 134),
            Color.FromRgb(17, 22, 28),
            Color.FromRgb(58, 66, 77),
            Color.FromRgb(36, 75, 75),
            Color.FromRgb(17, 20, 25),
            Color.FromRgb(46, 196, 182),
            Color.FromRgb(255, 176, 0)),

        new(
            "milk-white",
            "Milk White",
            Color.FromRgb(246, 241, 232),
            Color.FromRgb(255, 252, 246),
            Color.FromRgb(237, 229, 216),
            Color.FromRgb(88, 123, 178),
            Color.FromRgb(188, 126, 62),
            Color.FromRgb(48, 44, 38),
            Color.FromRgb(111, 101, 88),
            Color.FromRgb(247, 241, 231),
            Color.FromRgb(239, 231, 218),
            Color.FromRgb(229, 219, 203),
            Color.FromRgb(190, 178, 160),
            Color.FromRgb(151, 139, 122),
            Color.FromRgb(228, 219, 205),
            Color.FromRgb(195, 183, 166),
            Color.FromRgb(216, 226, 244),
            Color.FromRgb(232, 221, 205),
            Color.FromRgb(88, 123, 178),
            Color.FromRgb(196, 126, 62)),

        new(
            "forest-green",
            "Forest Green",
            Color.FromRgb(20, 33, 29),
            Color.FromRgb(29, 48, 41),
            Color.FromRgb(38, 66, 54),
            Color.FromRgb(112, 184, 137),
            Color.FromRgb(214, 166, 92),
            Color.FromRgb(236, 243, 232),
            Color.FromRgb(178, 199, 184),
            Color.FromRgb(34, 58, 49),
            Color.FromRgb(45, 76, 64),
            Color.FromRgb(56, 91, 76),
            Color.FromRgb(75, 108, 91),
            Color.FromRgb(116, 145, 126),
            Color.FromRgb(16, 27, 23),
            Color.FromRgb(65, 93, 80),
            Color.FromRgb(44, 95, 70),
            Color.FromRgb(17, 32, 27),
            Color.FromRgb(112, 184, 137),
            Color.FromRgb(214, 166, 92)),

        new(
            "pink-dream",
            "Pink Dream",
            Color.FromRgb(51, 39, 48),
            Color.FromRgb(68, 52, 64),
            Color.FromRgb(84, 63, 78),
            Color.FromRgb(235, 151, 177),
            Color.FromRgb(244, 184, 114),
            Color.FromRgb(255, 244, 249),
            Color.FromRgb(221, 190, 203),
            Color.FromRgb(77, 58, 72),
            Color.FromRgb(94, 70, 88),
            Color.FromRgb(111, 82, 103),
            Color.FromRgb(130, 98, 121),
            Color.FromRgb(177, 141, 158),
            Color.FromRgb(42, 31, 40),
            Color.FromRgb(95, 72, 90),
            Color.FromRgb(112, 63, 84),
            Color.FromRgb(39, 29, 37),
            Color.FromRgb(235, 151, 177),
            Color.FromRgb(244, 184, 114))
    ];

    public static AppTheme Default => All[0];

    public static AppTheme FindById(string? id)
    {
        var normalizedId = string.Equals(id, "teen-girl-pink", StringComparison.OrdinalIgnoreCase)
            ? "pink-dream"
            : id;
        return All.FirstOrDefault(theme => string.Equals(theme.Id, normalizedId, StringComparison.OrdinalIgnoreCase))
            ?? Default;
    }
}
