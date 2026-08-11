using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopLoudnessMeter.App.ViewModels;
using DesktopLoudnessMeter.App.Localization;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace DesktopLoudnessMeter.App;

public partial class MainWindow : Window
{
    private GlobalSettingsWindow? _globalSettingsWindow;
    private GameWindow? _gameWindow;
    private Point _sourceDragStartPoint;
    private Point _inlineDetailModuleDragStartPoint;
    private bool _inlineDetailModuleDragArmed;

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

    private void GameButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_gameWindow is { IsVisible: true } visibleWindow)
        {
            visibleWindow.Activate();
            return;
        }

        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        _gameWindow = new GameWindow(viewModel)
        {
            Owner = this
        };
        _gameWindow.Closed += (_, _) => _gameWindow = null;
        _gameWindow.Show();
        _gameWindow.Activate();
    }

    private void GlobalSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_globalSettingsWindow is { IsVisible: true } visibleWindow)
        {
            visibleWindow.Activate();
            return;
        }

        _globalSettingsWindow = new GlobalSettingsWindow
        {
            Owner = this,
            DataContext = DataContext
        };
        _globalSettingsWindow.Closed += (_, _) => _globalSettingsWindow = null;
        _globalSettingsWindow.Show();
        _globalSettingsWindow.Activate();
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

    private void InlineClockSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        InlineClockSettingsPopup.IsOpen = !InlineClockSettingsPopup.IsOpen;
    }

    private void InlineWaveformSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        InlineWaveformSettingsPopup.IsOpen = !InlineWaveformSettingsPopup.IsOpen;
    }

    private void InlineSpectrumSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        InlineSpectrumSettingsPopup.IsOpen = !InlineSpectrumSettingsPopup.IsOpen;
    }

    private void SaveInlineDetailLayoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = LocalizationManager.Translate("Save detail layout"),
            DefaultExt = ".json",
            AddExtension = true,
            FileName = "umeter-detail-layout.json",
            Filter = "uMeter detail layout (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true && !viewModel.SaveInlineDetailLayout(dialog.FileName))
        {
            MessageBox.Show(this, viewModel.StatusText, "uMeter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadInlineDetailLayoutButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = LocalizationManager.Translate("Load detail layout"),
            DefaultExt = ".json",
            Filter = "uMeter detail layout (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true && !viewModel.LoadInlineDetailLayout(dialog.FileName))
        {
            MessageBox.Show(this, viewModel.StatusText, "uMeter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CollisionSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        CollisionSettingsPopup.IsOpen = !CollisionSettingsPopup.IsOpen;
    }

    private void CollisionSourceButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RefreshNow();
        }

        OpenButtonContextMenu(sender);
    }

    private void SwitchDetailButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.RefreshDetailSwitchTargets();
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

    private void AliasEditTextBox_OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: AudioSessionMeterViewModel row } textBox)
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

    private void SourceDragHandle_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: AudioSessionMeterViewModel row } element)
        {
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            _sourceDragStartPoint = e.GetPosition(this);
            return;
        }

        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _sourceDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _sourceDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(element, row, DragDropEffects.Move);
        ClearSourceDropHints();
    }

    private void SourceRow_OnDragOver(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || sender is not FrameworkElement { DataContext: AudioSessionMeterViewModel target }
            || e.Data.GetData(typeof(AudioSessionMeterViewModel)) is not AudioSessionMeterViewModel source)
        {
            ClearSourceDropHints();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (ReferenceEquals(source, target))
        {
            ClearSourceDropHints();
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            return;
        }

        var insertAfter = ShouldInsertAfter(e, sender);
        foreach (var row in viewModel.Sessions)
        {
            row.SetReorderDropHint(ReferenceEquals(row, target) && !insertAfter, ReferenceEquals(row, target) && insertAfter);
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void SourceRow_OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || sender is not FrameworkElement { DataContext: AudioSessionMeterViewModel target }
            || e.Data.GetData(typeof(AudioSessionMeterViewModel)) is not AudioSessionMeterViewModel source)
        {
            return;
        }

        viewModel.MoveSourceTo(source, target, ShouldInsertAfter(e, sender));
        ClearSourceDropHints();
        e.Handled = true;
    }

    private void SourceRow_OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        if (e.Effects == DragDropEffects.None)
        {
            ClearSourceDropHints();
            return;
        }

        UseMoveDragCursor(e);
    }

    private void InlineDetailModuleDragHandle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DetailModuleViewModel } element)
        {
            return;
        }

        _inlineDetailModuleDragStartPoint = e.GetPosition(this);
        _inlineDetailModuleDragArmed = element.CaptureMouse();
        e.Handled = _inlineDetailModuleDragArmed;
    }

    private void InlineDetailModuleDragHandle_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        CancelInlineDetailModuleDrag(sender as FrameworkElement);
    }

    private void InlineDetailModuleDragHandle_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: DetailModuleViewModel module } element)
        {
            return;
        }

        if (!_inlineDetailModuleDragArmed || e.LeftButton != MouseButtonState.Pressed)
        {
            CancelInlineDetailModuleDrag(element);
            return;
        }

        var position = e.GetPosition(this);
        if (Math.Abs(position.X - _inlineDetailModuleDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(position.Y - _inlineDetailModuleDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        CancelInlineDetailModuleDrag(element);
        try
        {
            DragDrop.DoDragDrop(element, module, DragDropEffects.Move);
        }
        finally
        {
            ClearInlineDetailModuleDropHints();
        }
    }

    private void InlineDetailModule_OnDragOver(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || sender is not FrameworkElement { Tag: DetailModuleViewModel target }
            || e.Data.GetData(typeof(DetailModuleViewModel)) is not DetailModuleViewModel source)
        {
            ClearInlineDetailModuleDropHints();
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        if (ReferenceEquals(source, target))
        {
            ClearInlineDetailModuleDropHints();
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            return;
        }

        var insertAfter = DetailModuleDropPlacement.ShouldInsertAfter(source, target);
        foreach (var module in viewModel.InlineDetailModules)
        {
            module.SetReorderDropHint(ReferenceEquals(module, target) && !insertAfter, ReferenceEquals(module, target) && insertAfter);
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void InlineDetailModule_OnDrop(object sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel
            || sender is not FrameworkElement { Tag: DetailModuleViewModel target }
            || e.Data.GetData(typeof(DetailModuleViewModel)) is not DetailModuleViewModel source)
        {
            return;
        }

        viewModel.MoveInlineDetailModuleTo(source, target, DetailModuleDropPlacement.ShouldInsertAfter(source, target));
        ClearInlineDetailModuleDropHints();
        e.Handled = true;
    }

    private void InlineDetailModule_OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
    {
        if (e.Effects == DragDropEffects.None)
        {
            ClearInlineDetailModuleDropHints();
            return;
        }

        UseMoveDragCursor(e);
    }

    private static void UseMoveDragCursor(GiveFeedbackEventArgs e)
    {
        Mouse.SetCursor(Cursors.SizeAll);
        e.UseDefaultCursors = false;
        e.Handled = true;
    }

    private static bool ShouldInsertAfter(DragEventArgs e, object sender)
    {
        if (sender is not FrameworkElement element)
        {
            return false;
        }

        return e.GetPosition(element).Y >= element.ActualHeight / 2;
    }

    private void CancelInlineDetailModuleDrag(FrameworkElement? element)
    {
        _inlineDetailModuleDragArmed = false;
        if (element?.IsMouseCaptured == true)
        {
            element.ReleaseMouseCapture();
        }
    }

    private void ClearSourceDropHints()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        foreach (var row in viewModel.Sessions)
        {
            row.SetReorderDropHint(before: false, after: false);
        }
    }

    private void ClearInlineDetailModuleDropHints()
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ClearInlineDetailModuleDropHints();
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

    public static string? SelectFolder(string? initialFolder)
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Choose event output folder",
            UseDescriptionForTitle = true,
            SelectedPath = string.IsNullOrWhiteSpace(initialFolder) ? string.Empty : initialFolder
        };

        return dialog.ShowDialog() == Forms.DialogResult.OK
            ? dialog.SelectedPath
            : null;
    }
}
