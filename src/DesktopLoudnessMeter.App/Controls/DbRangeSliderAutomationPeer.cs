using System.Windows.Automation.Peers;

namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// UI Automation peer for <see cref="DbRangeSlider"/>. The slider itself is
/// announced as a group; each logical endpoint is announced as its own slider
/// with a range-value pattern.
/// </summary>
internal sealed class DbRangeSliderAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly DbRangeSlider _owner;

    public DbRangeSliderAutomationPeer(DbRangeSlider owner)
        : base(owner)
    {
        _owner = owner;
    }

    protected override string GetClassNameCore() => nameof(DbRangeSlider);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = _owner.CreateEndpointPeers();
        return peers.Count == 0 ? null : [.. peers];
    }
}
