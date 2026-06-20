using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.App;

public partial class DetailWindow : Window
{
    public DetailWindow()
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

    private void StereoSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DetailWindowViewModel viewModel)
        {
            return;
        }

        var window = new StereoImageSettingsWindow
        {
            Owner = this,
            DataContext = viewModel.StereoSettings
        };
        window.ShowDialog();
    }

    private void DetailRollSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        DetailRollSettingsPopup.IsOpen = !DetailRollSettingsPopup.IsOpen;
    }

    private void DetailCollisionSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        DetailCollisionSettingsPopup.IsOpen = !DetailCollisionSettingsPopup.IsOpen;
    }

    private void DetailCollisionSourceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is DetailWindowViewModel viewModel)
        {
            viewModel.RefreshSwitchTargets();
        }

        OpenButtonContextMenu(sender);
    }

    private void SwitchSessionButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is DetailWindowViewModel viewModel)
        {
            viewModel.RefreshSwitchTargets();
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
