using System.Windows;
using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Diagnostics;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.Themes;
using DesktopLoudnessMeter.App.ViewModels;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace DesktopLoudnessMeter.App;

public partial class App : Application
{
    private MainViewModel? _mainViewModel;
    private MainWindow? _mainWindow;
    private LocalLoopbackUdpReceiver? _localLoopbackReceiver;
    private UserWorkspaceSettings? _workspaceSettings;
    private Forms.NotifyIcon? _trayIcon;
    private Forms.ContextMenuStrip? _trayMenu;
    private Drawing.Icon? _trayIconImage;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        RegisterExceptionLogging();

        AppLogger.Info("Application startup began.");
        _workspaceSettings = UserWorkspaceSettings.Load();
        var initialTheme = AppThemes.FindById(_workspaceSettings.ThemeId);
        ThemeManager.Apply(initialTheme);

        _mainViewModel = new MainViewModel(
            CreateSignalSourceService(out var sourceWarningText),
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

        _mainWindow = new MainWindow
        {
            DataContext = _mainViewModel
        };
        MainWindow = _mainWindow;
        InitializeTrayIcon();
        _mainWindow.Show();
        _mainWindow.Activate();

        AppLogger.Info($"Main window shown. Left={_mainWindow.Left:0}, Top={_mainWindow.Top:0}, Width={_mainWindow.Width:0}, Height={_mainWindow.Height:0}, Topmost={_mainWindow.Topmost}.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Info($"Application exiting with code {e.ApplicationExitCode}.");
        _localLoopbackReceiver?.Dispose();
        DisposeTrayIcon();
        _mainViewModel?.Dispose();
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
            Owner = _mainWindow?.IsVisible == true ? _mainWindow : null,
            DataContext = new DetailWindowViewModel(session, GetActiveDetailSwitchTargets, _workspaceSettings)
        };
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

    private void InitializeTrayIcon()
    {
        _trayMenu = new Forms.ContextMenuStrip();
        RebuildTrayMenu();

        _trayIconImage = LoadTrayIcon();
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIconImage,
            Text = "uMeter",
            Visible = true
        };
        _trayIcon.MouseUp += TrayIcon_OnMouseUp;
        _trayIcon.DoubleClick += (_, _) => ShowMainWindow();
    }

    private void TrayIcon_OnMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Right)
        {
            ShowTrayMenu();
        }
    }

    private void ShowTrayMenu()
    {
        Dispatcher.Invoke(() =>
        {
            if (_trayMenu is null)
            {
                return;
            }

            RebuildTrayMenu();
            _trayMenu.Show(Forms.Cursor.Position);
        });
    }

    private void RebuildTrayMenu()
    {
        if (_trayMenu is null)
        {
            return;
        }

        _trayMenu.Items.Clear();
        _trayMenu.Items.Add("Show uMeter", null, (_, _) => ShowMainWindow());

        var popupMenu = new Forms.ToolStripMenuItem("Pop-up App...");
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
        _trayMenu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(Shutdown));
    }

    private void ShowMainWindow()
    {
        Dispatcher.Invoke(() =>
        {
            if (_mainWindow is null)
            {
                return;
            }

            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Activate();
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
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _trayMenu?.Dispose();
        _trayMenu = null;
        _trayIconImage?.Dispose();
        _trayIconImage = null;
    }
}
