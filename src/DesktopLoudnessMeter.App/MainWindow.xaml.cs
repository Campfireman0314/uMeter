using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopLoudnessMeter.App.ViewModels;
using Forms = System.Windows.Forms;

namespace DesktopLoudnessMeter.App;

public partial class MainWindow : Window
{
    private GlobalSettingsWindow? _globalSettingsWindow;

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

    private void InlineRollSettingsButton_OnClick(object sender, RoutedEventArgs e)
    {
        InlineRollSettingsPopup.IsOpen = !InlineRollSettingsPopup.IsOpen;
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
