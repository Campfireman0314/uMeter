using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using DesktopLoudnessMeter.App.Localization;

namespace DesktopLoudnessMeter.App;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        VersionText = LocalizationManager.Format("Version {0}", GetProductVersion());
        Credits = AboutCredits.CreateLocalizedEntries();
        DataContext = this;
    }

    public string VersionText { get; }

    public IReadOnlyList<AboutCredit> Credits { get; }

    private void Header_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        WindowDragBehavior.HandleHeaderMouseLeftButtonDown(this, e);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void RepositoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(AboutCredits.RepositoryUrl)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            Clipboard.SetText(AboutCredits.RepositoryUrl);
            MessageBox.Show(
                this,
                LocalizationManager.Translate("The repository address was copied to the clipboard."),
                LocalizationManager.Translate("Project repository"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private static string GetProductVersion()
    {
        var assembly = typeof(AboutWindow).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "2.4.0";
    }
}
