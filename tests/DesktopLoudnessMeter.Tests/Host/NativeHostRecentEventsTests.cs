using DesktopLoudnessMeter.App.Audio.Analysis;
using DesktopLoudnessMeter.App.Host;
using DesktopLoudnessMeter.App.ViewModels;

namespace DesktopLoudnessMeter.Tests.Host;

public sealed class NativeHostRecentEventsTests
{
    [Fact]
    public void EmitsSourceAppearedForVisibleNewLiveSource()
    {
        using var events = new NativeHostRecentEvents();

        var recent = Reconcile(
            events,
            liveKeys: ["plugin:lead"],
            visible: [("plugin:lead", "Lead Bus")],
            timeText: "10:00:00",
            limit: 6);

        var item = Assert.Single(recent);
        Assert.Equal("10:00:00", item.TimeText);
        Assert.Equal(RecentEventKind.Source, item.Kind);
        Assert.Equal("Source appeared", item.Title);
        Assert.Equal("Lead Bus", item.Detail);
        Assert.Equal(ReadoutSeverity.Normal, item.Severity);
    }

    [Fact]
    public void DoesNotDuplicateUnchangedLiveSource()
    {
        using var events = new NativeHostRecentEvents();
        Reconcile(
            events,
            liveKeys: ["plugin:lead"],
            visible: [("plugin:lead", "Lead Bus")],
            timeText: "10:00:00",
            limit: 6);

        var recent = Reconcile(
            events,
            liveKeys: ["plugin:lead"],
            visible: [("plugin:lead", "Lead Bus")],
            timeText: "10:00:01",
            limit: 6);

        var item = Assert.Single(recent);
        Assert.Equal("10:00:00", item.TimeText);
    }

    [Fact]
    public void HiddenLiveThenVisibleDoesNotEmitVisibilityOnlyAppearance()
    {
        using var events = new NativeHostRecentEvents();
        var hidden = Reconcile(
            events,
            liveKeys: ["plugin:hidden"],
            visible: [],
            timeText: "10:00:00",
            limit: 6);

        var shown = Reconcile(
            events,
            liveKeys: ["plugin:hidden"],
            visible: [("plugin:hidden", "Hidden")],
            timeText: "10:00:01",
            limit: 6);

        Assert.Empty(hidden);
        Assert.Empty(shown);
    }

    [Fact]
    public void ReappearedVisibleSourceEmitsNewAppearance()
    {
        using var events = new NativeHostRecentEvents();
        Reconcile(
            events,
            liveKeys: ["plugin:lead"],
            visible: [("plugin:lead", "Lead Bus")],
            timeText: "10:00:00",
            limit: 6);
        Reconcile(
            events,
            liveKeys: [],
            visible: [],
            timeText: "10:00:01",
            limit: 6);

        var recent = Reconcile(
            events,
            liveKeys: ["plugin:lead"],
            visible: [("plugin:lead", "Lead Bus")],
            timeText: "10:00:02",
            limit: 6);

        Assert.Equal(2, recent.Count);
        Assert.Equal("10:00:02", recent[0].TimeText);
        Assert.Equal("10:00:00", recent[1].TimeText);
    }

    [Fact]
    public void TrimsHistoryToLimitAndRetrimsAfterLimitChange()
    {
        using var events = new NativeHostRecentEvents();
        Reconcile(
            events,
            liveKeys: ["plugin:a", "plugin:b", "plugin:c"],
            visible: [("plugin:a", "A"), ("plugin:b", "B"), ("plugin:c", "C")],
            timeText: "10:00:00",
            limit: 2);

        var trimmed = events.GetEvents();

        Assert.Equal(["C", "B"], trimmed.Select(item => item.Detail).ToArray());

        Reconcile(
            events,
            liveKeys: ["plugin:a", "plugin:b", "plugin:c"],
            visible: [("plugin:a", "A"), ("plugin:b", "B"), ("plugin:c", "C")],
            timeText: "10:00:01",
            limit: 1);

        var retrimmed = events.GetEvents();

        var item = Assert.Single(retrimmed);
        Assert.Equal("C", item.Detail);
    }

    private static IReadOnlyList<HostRecentEventSnapshot> Reconcile(
        NativeHostRecentEvents events,
        IReadOnlyList<string> liveKeys,
        IReadOnlyList<(string SourceKey, string DisplayTitle)> visible,
        string timeText,
        int limit)
    {
        events.ClearCurrentSources();
        foreach (var liveKey in liveKeys)
        {
            events.AddLiveSource(liveKey);
        }

        foreach (var source in visible)
        {
            events.AddVisibleSource(source.SourceKey, source.DisplayTitle);
        }

        events.Reconcile(timeText, limit);
        return events.GetEvents();
    }
}
