using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.Infrastructure;
using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.ViewModels;

public sealed class StartupSettingsTests
{
    [Fact]
    public void StartupOptionsLoadFromWorkspaceWithoutChangingRegistration()
    {
        var workspace = new UserWorkspaceSettings
        {
            StartWithWindows = true,
            HideToTrayAfterStartup = true
        };
        var registration = new RecordingStartupRegistration();

        var viewModel = CreateViewModel(workspace, registration);

        Assert.True(viewModel.StartWithWindows);
        Assert.True(viewModel.HideToTrayAfterStartup);
        Assert.Empty(registration.Calls);
    }

    [Fact]
    public void EnablingStartupAndHiddenModeRefreshesRegistration()
    {
        var workspace = new UserWorkspaceSettings();
        var registration = new RecordingStartupRegistration();
        var viewModel = CreateViewModel(workspace, registration);

        viewModel.StartWithWindows = true;
        viewModel.HideToTrayAfterStartup = true;

        Assert.True(workspace.StartWithWindows);
        Assert.True(workspace.HideToTrayAfterStartup);
        Assert.Equal([(true, false), (true, true)], registration.Calls);
    }

    [Fact]
    public void HiddenModeDoesNotTouchRegistrationWhileStartupIsDisabled()
    {
        var workspace = new UserWorkspaceSettings();
        var registration = new RecordingStartupRegistration();
        var viewModel = CreateViewModel(workspace, registration);

        viewModel.HideToTrayAfterStartup = true;

        Assert.True(workspace.HideToTrayAfterStartup);
        Assert.Empty(registration.Calls);
    }

    [Fact]
    public void SenderAutoInvokePersistsWithoutChangingWindowsRegistration()
    {
        var workspace = new UserWorkspaceSettings();
        var registration = new RecordingStartupRegistration();
        var viewModel = CreateViewModel(workspace, registration);

        viewModel.AutoInvokeHostApp = true;

        Assert.True(workspace.AutoInvokeHostApp);
        Assert.Empty(registration.Calls);
    }

    [Fact]
    public void HiddenModeIsIndependentOfAutomaticLaunchOptions()
    {
        var workspace = new UserWorkspaceSettings();
        var registration = new RecordingStartupRegistration();
        var viewModel = CreateViewModel(workspace, registration);

        viewModel.HideToTrayAfterStartup = true;

        Assert.True(viewModel.HideToTrayAfterStartup);
        Assert.False(viewModel.StartWithWindows);
        Assert.False(viewModel.AutoInvokeHostApp);
        Assert.Empty(registration.Calls);
    }

    [Fact]
    public void DisablingStartupRemovesRegistration()
    {
        var workspace = new UserWorkspaceSettings
        {
            StartWithWindows = true,
            HideToTrayAfterStartup = true
        };
        var registration = new RecordingStartupRegistration();
        var viewModel = CreateViewModel(workspace, registration);

        viewModel.StartWithWindows = false;

        Assert.False(workspace.StartWithWindows);
        Assert.Equal([(false, true)], registration.Calls);
    }

    private static MainViewModel CreateViewModel(
        UserWorkspaceSettings workspace,
        IStartupRegistration registration)
    {
        return new MainViewModel(
            new EmptySignalSourceMeterService(),
            startTimer: false,
            workspaceSettings: workspace,
            startupRegistration: registration);
    }

    private sealed class EmptySignalSourceMeterService : ISignalSourceMeterService
    {
        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources() => [];
    }

    private sealed class RecordingStartupRegistration : IStartupRegistration
    {
        public List<(bool Enabled, bool StartHidden)> Calls { get; } = [];

        public void Configure(bool enabled, bool startHidden)
        {
            Calls.Add((enabled, startHidden));
        }
    }
}
