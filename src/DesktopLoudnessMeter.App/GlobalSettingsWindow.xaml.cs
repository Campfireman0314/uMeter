using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopLoudnessMeter.App.ViewModels;
using DesktopLoudnessMeter.App.Localization;
using Microsoft.Win32;

namespace DesktopLoudnessMeter.App;

public partial class GlobalSettingsWindow : Window
{
    public GlobalSettingsWindow()
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

    private void AboutButton_OnClick(object sender, RoutedEventArgs e)
    {
        new AboutWindow
        {
            Owner = this
        }.ShowDialog();
    }

    private void SettingsTextBox_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    private void DefaultDetailLayoutFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = LocalizationManager.Translate("Choose default detail layout"),
            DefaultExt = ".json",
            Filter = "uMeter detail layout (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            viewModel.DefaultDetailLayoutFile = dialog.FileName;
            viewModel.LoadDefaultDetailLayoutFromFile = true;
        }
    }
}
