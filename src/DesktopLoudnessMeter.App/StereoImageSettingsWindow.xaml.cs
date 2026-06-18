using System.Windows;
using System.Windows.Input;

namespace DesktopLoudnessMeter.App;

public partial class StereoImageSettingsWindow : Window
{
    public StereoImageSettingsWindow()
    {
        InitializeComponent();
    }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        WindowDragBehavior.HandleHeaderMouseLeftButtonDown(this, e);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
