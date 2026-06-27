using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopLoudnessMeter.App.ViewModels;
using Microsoft.Win32;

namespace DesktopLoudnessMeter.App;

public partial class DetailWindow : Window
{
    private Point _detailModuleDragStartPoint;

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

    private void DetailClockSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        DetailClockSettingsPopup.IsOpen = !DetailClockSettingsPopup.IsOpen;
    }

    private void DetailWaveformSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        DetailWaveformSettingsPopup.IsOpen = !DetailWaveformSettingsPopup.IsOpen;
    }

    private void DetailSpectrumSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        DetailSpectrumSettingsPopup.IsOpen = !DetailSpectrumSettingsPopup.IsOpen;
    }

    private void SaveDetailLayoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DetailWindowViewModel viewModel)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save detail layout",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = "umeter-detail-layout.json",
            Filter = "uMeter detail layout (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true && !viewModel.SaveDetailLayout(dialog.FileName))
        {
            MessageBox.Show(this, viewModel.DetailLayoutStatusText, "uMeter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadDetailLayoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DetailWindowViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Load detail layout",
            DefaultExt = ".json",
            Filter = "uMeter detail layout (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true && !viewModel.LoadDetailLayout(dialog.FileName))
        {
            MessageBox.Show(this, viewModel.DetailLayoutStatusText, "uMeter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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

    private void DetailModuleDragHandle_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DetailModuleViewModel module } element)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            _detailModuleDragStartPoint = e.GetPosition(this);
            return;
        }

        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _detailModuleDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _detailModuleDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(element, module, DragDropEffects.Move);
        ClearDetailModuleDropHints();
    }

    private void DetailModule_OnDragOver(object sender, DragEventArgs e)
    {
        if (DataContext is not DetailWindowViewModel viewModel
            || sender is not FrameworkElement { Tag: DetailModuleViewModel target }
            || e.Data.GetData(typeof(DetailModuleViewModel)) is not DetailModuleViewModel source)
        {
            ClearDetailModuleDropHints();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (ReferenceEquals(source, target))
        {
            ClearDetailModuleDropHints();
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            return;
        }

        var insertAfter = ShouldInsertAfter(e, sender);
        foreach (var module in viewModel.DetailModules)
        {
            module.SetReorderDropHint(ReferenceEquals(module, target) && !insertAfter, ReferenceEquals(module, target) && insertAfter);
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void DetailModule_OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not DetailWindowViewModel viewModel
            || sender is not FrameworkElement { Tag: DetailModuleViewModel target }
            || e.Data.GetData(typeof(DetailModuleViewModel)) is not DetailModuleViewModel source)
        {
            return;
        }

        viewModel.MoveDetailModuleTo(source, target, ShouldInsertAfter(e, sender));
        ClearDetailModuleDropHints();
        e.Handled = true;
    }

    private void DetailModule_OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        if (e.Effects == DragDropEffects.None)
        {
            ClearDetailModuleDropHints();
            return;
        }

        Mouse.SetCursor(Cursors.SizeAll);
        e.UseDefaultCursors = false;
        e.Handled = true;
    }

    private static bool ShouldInsertAfter(DragEventArgs e, object sender)
    {
        return sender is FrameworkElement element
            && e.GetPosition(element).Y >= element.ActualHeight / 2;
    }

    private void ClearDetailModuleDropHints()
    {
        if (DataContext is DetailWindowViewModel viewModel)
        {
            viewModel.ClearDetailModuleDropHints();
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
