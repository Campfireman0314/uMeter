using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace DesktopLoudnessMeter.App.Controls;

/// <summary>
/// UI Automation peer for <see cref="EditableValueSlider"/>. The control behaves
/// like a single-thumb slider, so the peer exposes one range-value pattern.
/// </summary>
internal sealed class EditableValueSliderAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly EditableValueSlider _owner;
    private double _notifiedValue = double.NaN;
    private double _notifiedMinimum = double.NaN;
    private double _notifiedMaximum = double.NaN;

    public EditableValueSliderAutomationPeer(EditableValueSlider owner)
        : base(owner)
    {
        _owner = owner;
    }

    internal int RaisedValueChangeCount { get; private set; }

    public bool IsReadOnly => false;

    public double Value => _owner.Value;

    public double Minimum => Math.Min(_owner.Minimum, _owner.Maximum);

    public double Maximum => Math.Max(_owner.Minimum, _owner.Maximum);

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
            throw new ArgumentOutOfRangeException(nameof(value), value, "Value is outside the slider range.");
        }

        _owner.SetAutomationValue(value);
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

    protected override string GetClassNameCore() => nameof(EditableValueSlider);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    protected override bool IsControlElementCore() => true;

    protected override bool IsEnabledCore() => _owner.IsEnabled;

    protected override bool IsKeyboardFocusableCore() => _owner.Focusable && _owner.IsEnabled;

    protected override bool HasKeyboardFocusCore() => _owner.IsKeyboardFocusWithin;

    private static bool AreSame(double previous, double current)
    {
        return !double.IsNaN(previous) && previous.Equals(current);
    }
}
