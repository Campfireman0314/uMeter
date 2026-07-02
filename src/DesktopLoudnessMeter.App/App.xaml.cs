using System.ComponentModel;
using System.Windows;
using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.Infrastructure;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.Themes;
using DesktopLoudnessMeter.App.ViewModels;
using DesktopLoudnessMeter.App.WebView;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace DesktopLoudnessMeter.App;

public partial class App : Application
{
    private static readonly TimeSpan ActivationRequestTimeout = TimeSpan.FromMilliseconds(750);
    internal const string TrayShowMenuText = "Show uMeter";
    internal const string TrayShowNativeUiMenuText = "Show Native UI";
    internal const string TrayPopupMenuText = "Pop-up App...";
    internal const string TrayExitMenuText = "Exit";

    internal static IReadOnlyList<string> NormalTrayMenuTexts { get; } =
    [
        TrayShowMenuText,
        TrayShowNativeUiMenuText,
        TrayPopupMenuText,
        TrayExitMenuText
    ];

    private SingleInstanceCoordinator? _singleInstanceCoordinator;
    private MainViewModel? _mainViewModel;
    private MainWindow? _mainWindow;
    private LocalLoopbackUdpReceiver? _localLoopbackReceiver;
    private ISignalSourceMeterService? _sourceMeterService;
    private UserWorkspaceSettings? _workspaceSettings;
    private Forms.NotifyIcon? _trayIcon;
    private Forms.ContextMenuStrip? _trayMenu;
    private Drawing.Icon? _trayIconImage;
    private TrayExitCoordinator? _trayExitCoordinator;
    private AppWindowRouter? _windowRouter;
    private WebViewShellWindow? _webViewShellWindow;
    private bool _activationRequestedBeforeWindowReady;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterExceptionLogging();

        AppLogger.Info("Application startup began.");
        _singleInstanceCoordinator = SingleInstanceCoordinator.CreateDefault();
        _singleInstanceCoordinator.ActivationRequested += OnSingleInstanceActivationRequested;
        var singleInstanceStartup = _singleInstanceCoordinator.Start(ActivationRequestTimeout);
        if (!singleInstanceStartup.IsPrimary)
        {
            AppLogger.Info(singleInstanceStartup.ActivationRequestSent
                ? "Existing uMeter instance detected; activation requested. Secondary instance exiting."
                : "Existing uMeter instance detected; activation request could not be delivered. Secondary instance exiting.");
            _singleInstanceCoordinator.Dispose();
            _singleInstanceCoordinator = null;
            Shutdown(0);
            return;
        }

        AppLogger.Info("Single-instance ownership acquired for this user session.");
        _workspaceSettings = UserWorkspaceSettings.Load();
        var initialTheme = AppThemes.FindById(_workspaceSettings.ThemeId);
        ThemeManager.Apply(initialTheme);

        _sourceMeterService = CreateSignalSourceService(out var sourceWarningText);
        _mainViewModel = new MainViewModel(
            _sourceMeterService,
            sourceWarningText: sourceWarningText,
            selectedTheme: initialTheme,
            themeSelected: theme =>
            {
                _workspaceSettings.ThemeId = theme.Id;
                _workspaceSettings.Save();
            },
            workspaceSettings: _workspaceSettings,
            selectFolder: DesktopLoudnessMeter.App.MainWindow.SelectFolder);
        _mainViewModel.DetailPopupRequested += OpenDetailPopup;
        _mainViewModel.RefreshNow();

        _windowRouter = new AppWindowRouter(
            showWebViewPrimary: OpenWebViewShell,
            showNativeUi: OpenNativeUi);
        InitializeTrayIcon();
        _windowRouter.ShowStartupWindow();
        if (_activationRequestedBeforeWindowReady)
        {
            _windowRouter.RestoreFromSecondaryActivation();
        }

        AppLogger.Info("WebView shell startup target shown.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Info($"Application exiting with code {e.ApplicationExitCode}.");
        _webViewShellWindow?.Close();
        _localLoopbackReceiver?.Dispose();
        DisposeTrayIcon();
        _mainViewModel?.Dispose();
        _singleInstanceCoordinator?.Dispose();
        base.OnExit(e);
    }

    private ISignalSourceMeterService CreateSignalSourceService(out string? warningText)
    {
        var warnings = new List<string>();
        if (WasapiCompatibilityNotice.Create(Environment.OSVersion.Version) is { } compatibilityWarning)
        {
            warnings.Add(compatibilityWarning);
            AppLogger.Info(compatibilityWarning);
        }

        var loopbackService = new LocalLoopbackSignalSourceService();
        if (LocalLoopbackUdpReceiver.TryStart(loopbackService, out var receiver) && receiver is not null)
        {
            _localLoopbackReceiver = receiver;
            AppLogger.Info($"Local loopback receiver listening on 127.0.0.1:{receiver.Port}.");
        }
        else
        {
            var receiverWarning = $"Plugin receiver unavailable: UDP port {LocalLoopbackUdpReceiver.DefaultPort} is already in use.";
            warnings.Add(receiverWarning);
            AppLogger.Info($"{receiverWarning} Continuing with WASAPI sources.");
        }

        warningText = warnings.Count == 0 ? null : string.Join(" ", warnings);
        return new CompositeSignalSourceMeterService(
            new WasapiSessionMeterService(),
            loopbackService);
    }

    private static void RegisterExceptionLogging()
    {
        Current.DispatcherUnhandledException += (_, args) =>
        {
            AppLogger.Error("Dispatcher unhandled exception.", args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                AppLogger.Error("AppDomain unhandled exception.", exception);
            }
            else
            {
                AppLogger.Info($"AppDomain unhandled non-exception object: {args.ExceptionObject}");
            }
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLogger.Error("Unobserved task exception.", args.Exception);
        };
    }

    private void OpenDetailPopup(AudioSessionMeterViewModel session)
    {
        var window = new DetailWindow
        {
            DataContext = new DetailWindowViewModel(session, GetActiveDetailSwitchTargets, _workspaceSettings)
        };
        DetailPopupPlacement.PositionNearOwner(window, _mainWindow);
        window.Show();
        window.Activate();
    }

    private IReadOnlyList<AudioSessionMeterViewModel> GetActiveDetailSwitchTargets()
    {
        return _mainViewModel?.Sessions
            .Where(session => !session.IsInstanceMissing)
            .ToArray()
            ?? [];
    }

    private void OnSingleInstanceActivationRequested()
    {
        AppLogger.Info("Activation requested by a secondary launch.");
        _activationRequestedBeforeWindowReady = true;
        Dispatcher.BeginInvoke(() => _windowRouter?.RestoreFromSecondaryActivation());
    }

    private void InitializeTrayIcon()
    {
        _trayMenu = new Forms.ContextMenuStrip();
        _trayMenu.Opening += TrayMenu_OnOpening;
        RebuildTrayMenu();

        _trayIconImage = LoadTrayIcon();
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIconImage,
            Text = "uMeter",
            ContextMenuStrip = _trayMenu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => _windowRouter?.RestoreFromTray();
    }

    private void TrayMenu_OnOpening(object? sender, CancelEventArgs e)
    {
        if (Dispatcher.CheckAccess())
        {
            RebuildTrayMenu();
        }
        else
        {
            Dispatcher.Invoke(RebuildTrayMenu);
        }
    }

    private void RebuildTrayMenu()
    {
        if (_trayMenu is null)
        {
            return;
        }

        _trayMenu.Items.Clear();
        _trayMenu.Items.Add(TrayShowMenuText, null, (_, _) => _windowRouter?.RestoreFromTray());
        _trayMenu.Items.Add(TrayShowNativeUiMenuText, null, (_, _) => _windowRouter?.OpenNativeUi());

        var popupMenu = new Forms.ToolStripMenuItem(TrayPopupMenuText);
        var sessions = _mainViewModel?.Sessions.ToArray() ?? [];
        if (sessions.Length == 0)
        {
            popupMenu.DropDownItems.Add("(no active apps)").Enabled = false;
        }
        else
        {
            foreach (var session in sessions)
            {
                var item = new Forms.ToolStripMenuItem($"{session.DisplayTitle} [{session.SourceIdentityText}]")
                {
                    Tag = session
                };
                item.Click += (_, _) =>
                {
                    if (item.Tag is AudioSessionMeterViewModel selected
                        && GetActiveDetailSwitchTargets().FirstOrDefault(session => ReferenceEquals(session, selected)) is { } liveSession)
                    {
                        Dispatcher.Invoke(() => OpenDetailPopup(liveSession));
                    }
                };
                popupMenu.DropDownItems.Add(item);
            }
        }

        _trayMenu.Items.Add(popupMenu);
        _trayMenu.Items.Add(new Forms.ToolStripSeparator());
        _trayMenu.Items.Add(TrayExitMenuText, null, (_, _) => RequestTrayExit());
    }

    private void RequestTrayExit()
    {
        _trayExitCoordinator ??= new TrayExitCoordinator(
            action => Dispatcher.BeginInvoke(action, System.Windows.Threading.DispatcherPriority.ApplicationIdle),
            () => Shutdown(0));
        _trayExitCoordinator.RequestExit();
    }

    public void OpenNativeUi()
    {
        Dispatcher.Invoke(() =>
        {
            if (_mainViewModel is null)
            {
                return;
            }

            if (_mainWindow is null)
            {
                _mainWindow = new MainWindow
                {
                    DataContext = _mainViewModel
                };
                _mainWindow.Closed += (_, _) => _mainWindow = null;
            }

            _activationRequestedBeforeWindowReady = false;
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Activate();
            _mainWindow.Focus();
        });
    }

    public void OpenWebViewShell()
    {
        Dispatcher.Invoke(() =>
        {
            if (_mainViewModel is null)
            {
                return;
            }

            if (_webViewShellWindow is { IsVisible: true } visibleWindow)
            {
                MainWindow = visibleWindow;
                if (visibleWindow.WindowState == WindowState.Minimized)
                {
                    visibleWindow.WindowState = WindowState.Normal;
                }

                visibleWindow.Show();
                visibleWindow.Activate();
                visibleWindow.Focus();
                _activationRequestedBeforeWindowReady = false;
                return;
            }

            _mainViewModel.RefreshNow();
            _webViewShellWindow = WebViewShellWindow.Create(_mainViewModel, OpenNativeUi, workspaceSettings: _workspaceSettings);
            _webViewShellWindow.Closed += (_, _) => _webViewShellWindow = null;
            MainWindow = _webViewShellWindow;
            _webViewShellWindow.Show();
            _webViewShellWindow.Activate();
            _webViewShellWindow.Focus();
            _activationRequestedBeforeWindowReady = false;
        });
    }

    private static Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var resource = GetResourceStream(new Uri("pack://application:,,,/Assets/desktop-loudness-meter-tray.ico"));
            if (resource is not null)
            {
                return new Drawing.Icon(resource.Stream);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to load tray icon resource.", ex);
        }

        return Environment.ProcessPath is string processPath
            ? Drawing.Icon.ExtractAssociatedIcon(processPath) ?? Drawing.SystemIcons.Application
            : Drawing.SystemIcons.Application;
    }

    private void DisposeTrayIcon()
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.ContextMenuStrip = null;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        if (_trayMenu is not null)
        {
            _trayMenu.Opening -= TrayMenu_OnOpening;
            _trayMenu.Dispose();
            _trayMenu = null;
        }
        _trayExitCoordinator = null;
        _trayIconImage?.Dispose();
        _trayIconImage = null;
    }
}
