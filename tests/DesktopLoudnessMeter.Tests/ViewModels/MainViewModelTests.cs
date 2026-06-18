using DesktopLoudnessMeter.App.Audio;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.ViewModels;

public sealed class MainViewModelTests
{
    [Fact]
    public void RefreshNowAddsUpdatesAndRemovesSessionsByKey()
    {
        var sessionA1 = Snapshot("a", "Alpha", 0.25f);
        var sessionA2 = Snapshot("a", "Alpha", 0.5f);
        var sessionB = Snapshot("b", "Beta", 0.1f);
        var service = new SequenceAudioSessionMeterService(
            Snapshots(sessionA1),
            Snapshots(sessionA2, sessionB),
            Snapshots(sessionB));
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();
        var alphaRow = Assert.Single(viewModel.Sessions);
        Assert.Equal("Alpha", alphaRow.DisplayName);

        viewModel.RefreshNow();
        Assert.Equal(2, viewModel.Sessions.Count);
        Assert.Same(alphaRow, viewModel.Sessions.Single(session => session.SessionKey == "a"));
        Assert.Equal("0.5 dB", viewModel.Sessions.Single(session => session.SessionKey == "a").PeakDbText);

        viewModel.RefreshNow();
        var betaRow = Assert.Single(viewModel.Sessions);
        Assert.Equal("b", betaRow.SessionKey);
    }

    [Fact]
    public void RefreshNowKeepsLoopAliveAfterServiceFailure()
    {
        var service = new SequenceAudioSessionMeterService(
            new InvalidOperationException("device disappeared"),
            Snapshots(Snapshot("a", "Alpha", 0.2f)));
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            missingRowRetentionTicks: 0,
            isProcessAlive: _ => false);

        viewModel.RefreshNow();

        Assert.Contains("Refresh failed", viewModel.StatusText);
        Assert.Empty(viewModel.Sessions);

        viewModel.RefreshNow();

        Assert.Single(viewModel.Sessions);
        Assert.Contains("1 source", viewModel.StatusText);
    }

    [Fact]
    public void RefreshNowKeepsSourceWarningVisible()
    {
        var service = new SequenceAudioSessionMeterService(
            Snapshots(Snapshot("a", "Alpha", 0.2f)));
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            sourceWarningText: "Plugin receiver unavailable: UDP port 38173 is already in use.");

        viewModel.RefreshNow();

        Assert.Single(viewModel.Sessions);
        Assert.Equal("Plugin receiver unavailable: UDP port 38173 is already in use.", viewModel.StatusText);
    }

    [Fact]
    public void RefreshNowReattachesTransientAppRowsByDisplayName()
    {
        var service = new SequenceAudioSessionMeterService(
            Snapshots(Snapshot("pid:1", "Alpha", 0.4f)),
            Snapshots(),
            Snapshots(Snapshot("pid:2", "Alpha", 0.6f)));
        var viewModel = new MainViewModel(service, startTimer: false, isProcessAlive: _ => true);

        viewModel.RefreshNow();
        var row = Assert.Single(viewModel.Sessions);
        row.OpenDetailCommand.Execute(null);

        viewModel.RefreshNow();
        Assert.Same(row, viewModel.SelectedDetailSession);

        viewModel.RefreshNow();

        Assert.Same(row, viewModel.SelectedDetailSession);
        Assert.Equal("pid:2", row.SessionKey);
        Assert.Equal("0.6 dB", row.PeakDbText);
    }

    [Fact]
    public void RefreshNowRetainsMissingAudioHandleUntilProcessExits()
    {
        var isAlive = true;
        var service = new SequenceAudioSessionMeterService(
            Snapshots(Snapshot("pid:42", "Alpha", 0.4f, processId: 42)),
            Snapshots(),
            Snapshots());
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            missingRowRetentionTicks: 0,
            isProcessAlive: _ => isAlive);

        viewModel.RefreshNow();
        var row = Assert.Single(viewModel.Sessions);
        row.OpenDetailCommand.Execute(null);

        viewModel.RefreshNow();

        Assert.Same(row, Assert.Single(viewModel.Sessions));
        Assert.Same(row, viewModel.SelectedDetailSession);
        Assert.Equal("Source handle missing", row.State);
        Assert.Equal("handle n/a", row.PeakDbText);

        isAlive = false;
        viewModel.RefreshNow();

        Assert.Empty(viewModel.Sessions);
        Assert.Same(row, viewModel.SelectedDetailSession);
        Assert.True(row.IsInstanceMissing);
        Assert.Equal("Instance not found", row.SourceStatusText);
        Assert.Equal("instance n/a", row.PeakDbText);
    }

    [Fact]
    public void RefreshNowRemovesPluginRowsAsSoonAsTheyDisappear()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(Source("loopback:plugin-a", "VST A", SignalSourceKind.Plugin, 0.4f)),
            Sources());
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            isProcessAlive: _ => true);

        viewModel.RefreshNow();
        var row = Assert.Single(viewModel.Sessions);
        row.OpenDetailCommand.Execute(null);

        viewModel.RefreshNow();

        Assert.Empty(viewModel.Sessions);
        Assert.Same(row, viewModel.SelectedDetailSession);
        Assert.True(row.IsInstanceMissing);
        Assert.Equal("Instance not found", row.SourceStatusText);
    }

    [Fact]
    public void RefreshNowReattachesMissingPluginDetailWhenSameInstanceReturns()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(Source("loopback:plugin-a", "VST A", SignalSourceKind.Plugin, 0.4f)),
            Sources(),
            Sources(Source("loopback:plugin-a", "VST A", SignalSourceKind.Plugin, 0.6f)));
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            isProcessAlive: _ => true);

        viewModel.RefreshNow();
        var row = Assert.Single(viewModel.Sessions);
        row.OpenDetailCommand.Execute(null);

        viewModel.RefreshNow();
        Assert.Empty(viewModel.Sessions);
        Assert.True(row.IsInstanceMissing);

        viewModel.RefreshNow();

        Assert.Same(row, Assert.Single(viewModel.Sessions));
        Assert.Same(row, viewModel.SelectedDetailSession);
        Assert.False(row.IsInstanceMissing);
        Assert.Equal("Active", row.SourceStatusText);
    }

    [Fact]
    public void MissingDetailCanSwitchToLiveInstance()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(Source("wasapi:alpha", "Alpha", SignalSourceKind.WasapiSession, 0.4f, processId: 42)),
            Sources(Source("wasapi:beta", "Beta", SignalSourceKind.WasapiSession, 0.6f, processId: 43)));
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            isProcessAlive: processId => processId != 42);

        viewModel.RefreshNow();
        var alpha = Assert.Single(viewModel.Sessions);
        alpha.OpenDetailCommand.Execute(null);

        viewModel.RefreshNow();

        var beta = Assert.Single(viewModel.Sessions);
        Assert.Same(alpha, viewModel.SelectedDetailSession);
        Assert.True(alpha.IsInstanceMissing);
        Assert.Same(beta, Assert.Single(viewModel.DetailSwitchTargets));
        Assert.True(viewModel.HasDetailSwitchTargets);

        viewModel.SwitchDetailSessionCommand.Execute(beta);

        Assert.Same(beta, viewModel.SelectedDetailSession);
        Assert.False(beta.IsInstanceMissing);
    }

    [Fact]
    public void SwitchDetailSessionIgnoresTargetThatDisappearedAfterMenuOpened()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(
                Source("wasapi:alpha", "Alpha", SignalSourceKind.WasapiSession, 0.4f, processId: 42),
                Source("wasapi:beta", "Beta", SignalSourceKind.WasapiSession, 0.6f, processId: 43)),
            Sources(Source("wasapi:alpha", "Alpha", SignalSourceKind.WasapiSession, 0.5f, processId: 42)));
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            isProcessAlive: processId => processId != 43);

        viewModel.RefreshNow();
        var alpha = viewModel.Sessions.Single(row => row.SourceKey == "wasapi:alpha");
        var beta = viewModel.Sessions.Single(row => row.SourceKey == "wasapi:beta");
        alpha.OpenDetailCommand.Execute(null);
        Assert.Same(beta, Assert.Single(viewModel.DetailSwitchTargets));

        viewModel.RefreshNow();
        viewModel.SwitchDetailSessionCommand.Execute(beta);

        Assert.Same(alpha, viewModel.SelectedDetailSession);
        Assert.DoesNotContain(beta, viewModel.Sessions);
    }

    [Fact]
    public void RefreshNowAddsGenericSourcesBySourceKey()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(Source("loopback:one", "Plugin Bus", SignalSourceKind.LocalLoopback, 0.25f)),
            Sources(Source("loopback:one", "Plugin Bus", SignalSourceKind.LocalLoopback, 0.5f)));
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            missingRowRetentionTicks: 0,
            isProcessAlive: _ => false);

        viewModel.RefreshNow();
        var row = Assert.Single(viewModel.Sessions);

        Assert.Equal("loopback:one", row.SourceKey);
        Assert.Equal("loopback:one", row.SessionKey);
        Assert.Equal(SignalSourceKind.LocalLoopback, row.SourceKind);
        Assert.Equal("Loopback", row.SourceKindLabel);

        viewModel.RefreshNow();

        Assert.Same(row, Assert.Single(viewModel.Sessions));
        Assert.Equal("0.5 dB", row.PeakDbText);
    }

    [Fact]
    public void RefreshNowDoesNotRecoverRowsAcrossSourceKinds()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(Source("plugin:shared", "Shared Name", SignalSourceKind.Plugin, 0.4f)),
            Sources(Source("loopback:shared", "Shared Name", SignalSourceKind.LocalLoopback, 0.5f)));
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();
        var pluginRow = Assert.Single(viewModel.Sessions);
        pluginRow.OpenDetailCommand.Execute(null);

        viewModel.RefreshNow();

        Assert.DoesNotContain(pluginRow, viewModel.Sessions);
        Assert.Same(pluginRow, viewModel.SelectedDetailSession);
        Assert.True(pluginRow.IsInstanceMissing);
        Assert.Contains(viewModel.Sessions, row => row.SourceKey == "loopback:shared");
    }

    [Fact]
    public void RefreshNowMarksOnlyCurrentDuplicateDisplayNames()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(
                Source("plugin:shared", "Shared Name", SignalSourceKind.Plugin, 0.4f),
                Source("loopback:shared", "Shared Name", SignalSourceKind.LocalLoopback, 0.5f)),
            Sources(Source("plugin:shared", "Shared Name", SignalSourceKind.Plugin, 0.4f)));
        var viewModel = new MainViewModel(
            service,
            startTimer: false,
            missingRowRetentionTicks: 0,
            isProcessAlive: _ => false);

        viewModel.RefreshNow();

        Assert.All(viewModel.Sessions, row => Assert.Contains(" - ", row.DisplayTitle));

        viewModel.RefreshNow();

        var row = Assert.Single(viewModel.Sessions);
        Assert.Equal("Shared Name", row.DisplayName);
        Assert.Equal("Shared Name", row.DisplayTitle);
    }

    [Fact]
    public void RefreshNowSortsMixedSourcesByKindDisplayNameAndKey()
    {
        var service = new SequenceSignalSourceMeterService(
            Sources(
                Source("wasapi:z", "Chrome", SignalSourceKind.WasapiSession, 0.2f),
                Source("loopback:b", "Bus B", SignalSourceKind.LocalLoopback, 0.2f),
                Source("plugin:b", "Bus B", SignalSourceKind.Plugin, 0.2f),
                Source("plugin:a", "Bus A", SignalSourceKind.Plugin, 0.2f),
                Source("loopback:a", "Bus A", SignalSourceKind.LocalLoopback, 0.2f)));
        var viewModel = new MainViewModel(service, startTimer: false);

        viewModel.RefreshNow();

        Assert.Equal(
            ["plugin:a", "plugin:b", "loopback:a", "loopback:b", "wasapi:z"],
            viewModel.Sessions.Select(row => row.SourceKey).ToArray());
    }

    private static AudioSessionSnapshot Snapshot(string key, string name, float peak, int? processId = 42)
    {
        return new AudioSessionSnapshot(
            key,
            processId,
            name,
            peak,
            PeakMath.ToDecibels(peak),
            $"{peak:0.0} dB",
            "Active");
    }

    private static IReadOnlyList<AudioSessionSnapshot> Snapshots(params AudioSessionSnapshot[] snapshots)
    {
        return snapshots;
    }

    private static SignalSourceSnapshot Source(
        string key,
        string name,
        SignalSourceKind kind,
        float peak,
        int? processId = null)
    {
        return new SignalSourceSnapshot(
            key,
            kind,
            processId,
            name,
            peak,
            PeakMath.ToDecibels(peak),
            $"{peak:0.0} dB",
            "Active");
    }

    private static IReadOnlyList<SignalSourceSnapshot> Sources(params SignalSourceSnapshot[] snapshots)
    {
        return snapshots;
    }

    private sealed class SequenceAudioSessionMeterService : IAudioSessionMeterService
    {
        private readonly Queue<object> _responses;

        public SequenceAudioSessionMeterService(params object[] responses)
        {
            _responses = new Queue<object>(responses);
        }

        public IReadOnlyList<AudioSessionSnapshot> GetCurrentSessions()
        {
            var response = _responses.Dequeue();
            if (response is Exception exception)
            {
                throw exception;
            }

            return (IReadOnlyList<AudioSessionSnapshot>)response;
        }
    }

    private sealed class SequenceSignalSourceMeterService : ISignalSourceMeterService
    {
        private readonly Queue<IReadOnlyList<SignalSourceSnapshot>> _responses;

        public SequenceSignalSourceMeterService(params IReadOnlyList<SignalSourceSnapshot>[] responses)
        {
            _responses = new Queue<IReadOnlyList<SignalSourceSnapshot>>(responses);
        }

        public IReadOnlyList<SignalSourceSnapshot> GetCurrentSources()
        {
            return _responses.Dequeue();
        }
    }
}
