using System.Windows;
using System.Text.Json;
using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace DesktopLoudnessMeter.App.WebView;

public partial class WebViewShellWindow : Window
{
    private readonly HostBridgeController _bridgeController;
    private readonly WebViewAssetLocator _assetLocator;
    private readonly UserWorkspaceSettings? _workspaceSettings;
    private GlobalSettingsWindow? _globalSettingsWindow;

    public WebViewShellWindow(
        HostBridgeController bridgeController,
        WebViewAssetLocator? assetLocator = null,
        UserWorkspaceSettings? workspaceSettings = null)
    {
        _bridgeController = bridgeController;
        _assetLocator = assetLocator ?? new WebViewAssetLocator();
        _workspaceSettings = workspaceSettings;
        InitializeComponent();
        Loaded += WebViewShellWindow_Loaded;
        Closed += WebViewShellWindow_Closed;
    }

    public static WebViewShellWindow Create(
        MainViewModel viewModel,
        Action? openNativeUi = null,
        WebViewAssetLocator? assetLocator = null,
        UserWorkspaceSettings? workspaceSettings = null)
    {
        WebViewShellWindow? shellWindow = null;
        var bridgeController = new HostBridgeController(
            viewModel.HostController,
            openNativeUi,
            command => shellWindow is not null && shellWindow.HandleShellCommand(command),
            () => viewModel.RecentEvents.Select(HostRecentEventSnapshot.FromViewModel).ToArray());
        shellWindow = new WebViewShellWindow(bridgeController, assetLocator, workspaceSettings)
        {
            DataContext = viewModel,
            Topmost = viewModel.IsAlwaysOnTop
        };
        return shellWindow;
    }

    private async void WebViewShellWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= WebViewShellWindow_Loaded;
        try
        {
            await ShellWebView.EnsureCoreWebView2Async();
            ShellWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                _assetLocator.ResolveVirtualHostName(),
                _assetLocator.ResolveWebRootDirectory(),
                CoreWebView2HostResourceAccessKind.Allow);
            ShellWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            ShellWebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            ShellWebView.Source = _assetLocator.ResolveStartupUri();
            ShellWebView.Visibility = Visibility.Visible;
            StatusPanel.Visibility = Visibility.Collapsed;
            AppLogger.Info($"WebView2 shell initialized at {_assetLocator.ResolveStartupUri()}.");
        }
        catch (Exception ex)
        {
            AppLogger.Error("WebView2 shell failed to initialize.", ex);
            StatusText.Text = $"WebView2 shell failed to initialize: {ex.Message}";
        }
    }

    private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            AppLogger.Info("WebView2 shell navigation completed.");
            return;
        }

        AppLogger.Info($"WebView2 shell navigation failed with {e.WebErrorStatus}.");
    }

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var requestId = HostBridgeJson.ExtractRequestId(e.WebMessageAsJson);
        try
        {
            LogShowHiddenRequest(e.WebMessageAsJson);
            var response = _bridgeController.HandleJson(e.WebMessageAsJson);
            LogShowHiddenResponse(response);
            ShellWebView.CoreWebView2.PostWebMessageAsJson(response);
        }
        catch (Exception ex)
        {
            AppLogger.Error("WebView2 HostBridge dispatch failed.", ex);
            var response = HostBridgeJson.Serialize(HostBridgeResponse.ForError(
                requestId,
                HostBridgeError.InvalidRequest($"HostBridge dispatch failed: {ex.Message}")));
            ShellWebView.CoreWebView2.PostWebMessageAsJson(response);
        }
    }

    private static void LogShowHiddenRequest(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.TryGetProperty("command", out var command)
                && command.GetString() == "setShowHiddenSources")
            {
                var requested = root.TryGetProperty("payload", out var payload)
                    && payload.TryGetProperty("showHiddenSources", out var showHidden)
                    && showHidden.ValueKind is JsonValueKind.True or JsonValueKind.False
                    ? showHidden.GetBoolean().ToString()
                    : "missing";
                AppLogger.DebugInfo($"WebView HostBridge request setShowHiddenSources requested={requested}.");
            }
        }
        catch (JsonException ex)
        {
            AppLogger.DebugInfo($"WebView HostBridge request log parse failed: {ex.Message}");
        }
    }

    private static void LogShowHiddenResponse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("result", out var result)
                || !result.TryGetProperty("commandKind", out var command)
                || command.GetString() != "setShowHiddenSources")
            {
                return;
            }

            var responseValue = result
                .GetProperty("snapshot")
                .GetProperty("settings")
                .GetProperty("showHiddenSources")
                .GetBoolean();
            AppLogger.DebugInfo($"WebView HostBridge response setShowHiddenSources snapshot.showHiddenSources={responseValue}.");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            AppLogger.DebugInfo($"WebView HostBridge response log parse failed: {ex.Message}");
        }
    }

    private void WebViewShellWindow_Closed(object? sender, EventArgs e)
    {
        if (ShellWebView.CoreWebView2 is { } core)
        {
            core.WebMessageReceived -= CoreWebView2_WebMessageReceived;
            core.NavigationCompleted -= CoreWebView2_NavigationCompleted;
        }

        _globalSettingsWindow?.Close();
        ShellWebView.Dispose();
    }

    private bool HandleShellCommand(HostCommand command)
    {
        switch (command)
        {
            case OpenGlobalSettingsHostCommand:
                OpenGlobalSettingsWindow();
                return true;
            case BeginWindowDragHostCommand:
                Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        WindowDragBehavior.BeginDragMove(this);
                    }
                    catch (InvalidOperationException)
                    {
                        // DragMove can throw if Windows consumes the mouse capture during snap/restore.
                    }
                });
                return true;
            case ToggleWindowStateHostCommand:
                WindowDragBehavior.ToggleWindowState(this);
                return true;
            case HideToTrayHostCommand:
                Dispatcher.BeginInvoke(Hide);
                return true;
            case CloseWindowHostCommand:
                Dispatcher.BeginInvoke(Close);
                return true;
            case SetAlwaysOnTopHostCommand setAlwaysOnTop:
                if (DataContext is MainViewModel viewModel)
                {
                    viewModel.IsAlwaysOnTop = setAlwaysOnTop.IsAlwaysOnTop;
                }

                Topmost = setAlwaysOnTop.IsAlwaysOnTop;
                return true;
            case SetDisplayModeHostCommand displayMode:
                return SetDisplayMode(displayMode.DisplayMode);
            case SetTruePeakEnabledHostCommand truePeak:
                return SetTruePeakEnabled(truePeak.IsEnabled);
            case SaveDetailLayoutHostCommand:
                return SaveDetailLayout();
            case LoadDetailLayoutHostCommand:
                return LoadDetailLayout();
            case OpenStereoSettingsHostCommand:
                OpenStereoSettingsWindow();
                return true;
            case OpenPopupHostCommand popup:
                return OpenDetailPopup(popup.SourceKey);
            default:
                return false;
        }
    }

    private void OpenGlobalSettingsWindow()
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

    private void OpenStereoSettingsWindow()
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

    private bool OpenDetailPopup(string sourceKey)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return false;
        }

        var session = viewModel.Sessions.FirstOrDefault(item => item.SourceKey == sourceKey);
        if (session is null)
        {
            return false;
        }

        var window = new DetailWindow
        {
            Owner = this,
            DataContext = new DetailWindowViewModel(
                session,
                () => viewModel.Sessions.Where(item => !item.IsInstanceMissing).ToArray(),
                _workspaceSettings)
        };
        DetailPopupPlacement.PositionNearOwner(window, this);
        window.Show();
        window.Activate();
        return true;
    }

    private bool SetDisplayMode(MeterDisplayMode displayMode)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return false;
        }

        viewModel.SelectedDisplayMode = displayMode;
        return true;
    }

    private bool SetTruePeakEnabled(bool isEnabled)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return false;
        }

        viewModel.TruePeakEnabled = isEnabled;
        return true;
    }

    private bool SaveDetailLayout()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return false;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Save detail layout",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = "umeter-detail-layout.json",
            Filter = "uMeter detail layout (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return true;
        }

        if (!viewModel.SaveInlineDetailLayout(dialog.FileName))
        {
            MessageBox.Show(this, viewModel.StatusText, "uMeter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return true;
    }

    private bool LoadDetailLayout()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return false;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Load detail layout",
            DefaultExt = ".json",
            Filter = "uMeter detail layout (*.json)|*.json|JSON files (*.json)|*.json|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return true;
        }

        if (!viewModel.LoadInlineDetailLayout(dialog.FileName))
        {
            MessageBox.Show(this, viewModel.StatusText, "uMeter", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            viewModel.RefreshNow();
        }

        return true;
    }
}
