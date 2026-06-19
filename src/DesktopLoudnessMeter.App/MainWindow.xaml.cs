using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App;

public partial class MainWindow : Window
{
    public MainWindow()
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

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void ThemeButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenButtonContextMenu(sender);
    }

    private void InlineStereoSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var window = new StereoImageSettingsWindow
        {
            Owner = this,
            DataContext = viewModel.InlineStereoSettings
        };
        window.ShowDialog();
    }

    private void InlineRollSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        InlineRollSettingsPopup.IsOpen = !InlineRollSettingsPopup.IsOpen;
    }

    private void SwitchDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RefreshDetailSwitchTargets();
        }

        OpenButtonContextMenu(sender);
    }

    private static void OpenButtonContextMenu(object sender)
    {
        if (sender is not Button { ContextMenu: { } contextMenu } button)
        {
            return;
        }

        contextMenu.PlacementTarget = button;
        contextMenu.IsOpen = true;
    }
}
