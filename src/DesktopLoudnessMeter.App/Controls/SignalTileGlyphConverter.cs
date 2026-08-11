using System.Globalization;
using System.Windows.Data;
using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class SignalTileGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is SignalTileKind kind ? ForKind(kind) : string.Empty;
    }

    public static string ForKind(SignalTileKind kind)
    {
        return kind switch
        {
            SignalTileKind.Star => "\u2605",
            SignalTileKind.Note => "\u266A",
            SignalTileKind.Cloud => "\u2601",
            SignalTileKind.Diamond => "\u25C6",
            SignalTileKind.Bloom => "\u273F",
            SignalTileKind.Sun => "\u2600",
            SignalTileKind.Moon => "\u25D0",
            SignalTileKind.Bolt => "\u21AF",
            SignalTileKind.Wave => "\u224B",
            SignalTileKind.Pulse => "\u223F",
            SignalTileKind.Disc => "\u25C9",
            SignalTileKind.Speaker => "\u25B6",
            SignalTileKind.Meter => "\u25D4",
            SignalTileKind.Orbit => "\u25CE",
            SignalTileKind.Bars => "\u2261",
            SignalTileKind.Arrows => "\u21C5",
            _ => "?"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
