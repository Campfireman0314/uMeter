using System.Windows;
using System.Windows.Automation.Peers;

namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// Invisible child element that positions a single range endpoint in the visual
/// tree so Windows UI Automation sees one range-value provider per endpoint.
/// Mouse and keyboard input stay with the owning <see cref="DbRangeSlider"/>.
/// </summary>
internal sealed class RangeSliderEndpointHandle : FrameworkElement
{
    private readonly DbRangeSlider _owner;

    public RangeSliderEndpointHandle(DbRangeSlider owner, RangeSliderEndpoint endpoint)
    {
        _owner = owner;
        Endpoint = endpoint;
        Focusable = false;
        IsHitTestVisible = false;
    }

    public RangeSliderEndpoint Endpoint { get; }

    internal RangeSliderEndpointAutomationPeer? Peer { get; private set; }

    internal RangeSliderEndpointAutomationPeer EnsurePeer()
    {
        return Peer ??= new RangeSliderEndpointAutomationPeer(_owner, this);
    }

    protected override AutomationPeer OnCreateAutomationPeer()
    {
        return EnsurePeer();
    }
}
