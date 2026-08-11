using System.Globalization;
using System.Windows.Data;

namespace DesktopLoudnessMeter.App.Controls;

public sealed class RhythmPhraseStepOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not int step
            || parameter is not string parameterText
            || !int.TryParse(parameterText, out var targetStep))
        {
            return 0.25d;
        }

        return step == targetStep ? 1d : 0.25d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
