using System.Windows.Media;

namespace DesktopLoudnessMeter.App.Themes;

public sealed record AppTheme(
    string Id,
    string DisplayName,
    Color AppChrome,
    Color Panel,
    Color PanelAlt,
    Color Accent,
    Color Warm,
    Color Text,
    Color SubtleText,
    Color Control,
    Color ControlHover,
    Color ControlPressed,
    Color ControlBorder,
    Color DisabledText,
    Color ScrollRail,
    Color ChromeBorder,
    Color Selection,
    Color MeterTrack,
    Color Signal,
    Color SpectrumWarm);
