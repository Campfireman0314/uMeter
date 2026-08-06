using System.Globalization;
using System.Windows.Data;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App.Localization;

public sealed class LocalizedEnumConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not Enum enumValue)
        {
            return value;
        }

        if (enumValue is TruePeakOversampling oversampling)
        {
            return LocalizationManager.Format("{0}x oversampling", (int)oversampling);
        }

        var sourceText = enumValue switch
        {
            MeterDisplayMode.Lufs => "LUFS",
            _ => enumValue.ToString()
        };
        return LocalizationManager.Translate(sourceText);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
