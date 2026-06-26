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
        Closed += (_, _) =>
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
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

    private void NamingTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void SourceTitle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount < 2 || sender is not FrameworkElement { DataContext: AudioSessionMeterViewModel row } element)
        {
            return;
        }

        row.StartAliasEditCommand.Execute(null);
        e.Handled = true;
        element.Dispatcher.BeginInvoke((Action)(() =>
        {
            if (FindVisualChild<TextBox>(element) is { } textBox)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }));
    }

    private void AliasEditTextBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: AudioSessionMeterViewModel row })
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            row.CommitAliasEditCommand.Execute(null);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            row.CancelAliasEditCommand.Execute(null);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void AliasEditTextBox_OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: AudioSessionMeterViewModel { IsAliasEditing: true } row })
        {
            row.CommitAliasEditCommand.Execute(null);
        }
    }

    private static T? FindVisualChild<T>(DependencyObject root)
        where T : DependencyObject
    {
        for (var index = 0; index < System.Windows.Media.VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                return match;
            }

            if (FindVisualChild<T>(child) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
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
