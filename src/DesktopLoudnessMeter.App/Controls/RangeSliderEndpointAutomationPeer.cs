using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// UI Automation peer for one logical endpoint of a <see cref="DbRangeSlider"/>.
/// The slider exposes two of these (lower and upper) so assistive technology can
/// read, validate, and change each end of the range independently.
/// </summary>
internal sealed class RangeSliderEndpointAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly DbRangeSlider _owner;
    private readonly RangeSliderEndpointHandle _handle;
    private double _notifiedValue = double.NaN;
    private double _notifiedMinimum = double.NaN;
    private double _notifiedMaximum = double.NaN;

    public RangeSliderEndpointAutomationPeer(DbRangeSlider owner, RangeSliderEndpointHandle handle)
        : base(handle)
    {
        _owner = owner;
        _handle = handle;
    }

    internal int RaisedValueChangeCount { get; private set; }

    internal int RaisedFocusNotificationCount { get; private set; }

    public bool IsReadOnly => false;

    public double Value => _owner.GetEndpointValue(_handle.Endpoint);

    public double Minimum => _owner.GetEndpointBounds(_handle.Endpoint).Minimum;

    public double Maximum => _owner.GetEndpointBounds(_handle.Endpoint).Maximum;

    public double SmallChange => _owner.EffectiveStepValue;

    public double LargeChange => _owner.EffectiveStepValue;

    public override object? GetPattern(PatternInterface patternInterface)
    {
        return patternInterface == PatternInterface.RangeValue ? this : null;
    }

    public void SetValue(double value)
    {
        if (!_owner.IsEnabled)
        {
            throw new ElementNotEnabledException();
        }

        if (double.IsNaN(value) || value < Minimum || value > Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value is outside the endpoint range.");
        }

        _owner.SetEndpointValue(_handle.Endpoint, value);
        Synchronize();
    }

    /// <summary>
    /// Raises the value and bounds notifications that follow a real slider update
    /// and records what has already been announced.
    /// </summary>
    internal void Synchronize()
    {
        var value = Value;
        var minimum = Minimum;
        var maximum = Maximum;
        var previousValue = _notifiedValue;
        var previousMinimum = _notifiedMinimum;
        var previousMaximum = _notifiedMaximum;

        if (!AreSame(previousValue, value))
        {
            _notifiedValue = value;
            RaisedValueChangeCount++;
            RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, previousValue, value);
        }

        if (!AreSame(previousMinimum, minimum))
        {
            _notifiedMinimum = minimum;
            RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MinimumProperty, previousMinimum, minimum);
        }

        if (!AreSame(previousMaximum, maximum))
        {
            _notifiedMaximum = maximum;
            RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty, previousMaximum, maximum);
        }
    }

    internal void NotifyActivated()
    {
        RaisedFocusNotificationCount++;
        RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
    }

    /// <summary>
    /// The advertising element is a non-focusable positioning handle, so keyboard focus stays with the
    /// owning slider: the requested endpoint is selected there so subsequent arrow keys move it.
    /// </summary>
    protected override void SetFocusCore()
    {
        if (!_owner.Focusable || !_owner.IsEnabled)
        {
            return;
        }

        if (_owner.Focus())
        {
            _owner.ActivateEndpoint(_handle.Endpoint);
        }
    }

    protected override string GetNameCore()
    {
        return _handle.Endpoint == RangeSliderEndpoint.Lower
            ? Localization.LocalizationManager.Translate("Lower limit")
            : Localization.LocalizationManager.Translate("Upper limit");
    }

    protected override string GetClassNameCore() => nameof(RangeSliderEndpointHandle);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => false;

    protected override bool IsEnabledCore() => _owner.IsEnabled;

    protected override bool IsKeyboardFocusableCore() => _owner.Focusable && _owner.IsEnabled;

    protected override bool HasKeyboardFocusCore()
    {
        return _owner.IsKeyboardFocusWithin && _owner.ActiveEndpoint == _handle.Endpoint;
    }

    private static bool AreSame(double previous, double current)
    {
        return !double.IsNaN(previous) && previous.Equals(current);
    }
}
