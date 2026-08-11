using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class SignalTileBrushConverter : IValueConverter
{
    private static readonly IReadOnlyDictionary<SignalTileKind, Brush> Brushes = CreateBrushes();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is SignalTileKind kind ? ForKind(kind) : ForKind(SignalTileKind.Cloud);
    }

    public static Brush ForKind(SignalTileKind kind) => Brushes[kind];

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static IReadOnlyDictionary<SignalTileKind, Brush> CreateBrushes()
    {
        return new Dictionary<SignalTileKind, Brush>
        {
            [SignalTileKind.Star] = CreateBrush(224, 168, 46),
            [SignalTileKind.Note] = CreateBrush(59, 130, 196),
            [SignalTileKind.Cloud] = CreateBrush(98, 122, 139),
            [SignalTileKind.Diamond] = CreateBrush(39, 168, 154),
            [SignalTileKind.Bloom] = CreateBrush(216, 92, 139),
            [SignalTileKind.Sun] = CreateBrush(224, 122, 63),
            [SignalTileKind.Moon] = CreateBrush(122, 107, 194),
            [SignalTileKind.Bolt] = CreateBrush(88, 166, 92),
            [SignalTileKind.Wave] = CreateBrush(54, 142, 151),
            [SignalTileKind.Pulse] = CreateBrush(211, 88, 86),
            [SignalTileKind.Disc] = CreateBrush(111, 116, 148),
            [SignalTileKind.Speaker] = CreateBrush(177, 83, 64),
            [SignalTileKind.Meter] = CreateBrush(80, 132, 104),
            [SignalTileKind.Orbit] = CreateBrush(149, 91, 166),
            [SignalTileKind.Bars] = CreateBrush(58, 111, 126),
            [SignalTileKind.Arrows] = CreateBrush(196, 105, 43)
        };
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
    }
}
