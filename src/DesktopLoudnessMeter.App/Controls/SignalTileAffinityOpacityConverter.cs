using System.Globalization;
using System.Windows.Data;
using DesktopLoudnessMeter.App.Game;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class SignalTileAffinityOpacityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4
            || values[0] is not SignalTileKind kind
            || values[1] is not double low
            || values[2] is not double mid
            || values[3] is not double high)
        {
            return 0d;
        }

        var affinity = RhythmSpectrumAnalyzer.ForTileKind(kind) switch
        {
            RhythmBand.Low => low,
            RhythmBand.Mid => mid,
            RhythmBand.High => high,
            _ => 0
        };
        return Math.Clamp(affinity, 0, 1) * 0.9;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
