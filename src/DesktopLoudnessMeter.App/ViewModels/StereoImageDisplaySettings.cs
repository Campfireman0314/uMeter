using System.Windows.Input;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class StereoImageDisplaySettings : ObservableObject
{
    public const double DefaultScaleRangeDb = 24;
    public const double DefaultManualCeilingDb = 0;
    public const double DefaultAutoScaleFloorDb = -24;
    public const double DefaultAutoScaleSpeedDbPerSecond = 8;
    public const double DefaultTracePersistenceSeconds = 0.08;
    public const bool DefaultAutoScale = true;

    private bool _isAutoScale = DefaultAutoScale;
    private double _scaleRangeDb = DefaultScaleRangeDb;
    private double _manualCeilingDb = DefaultManualCeilingDb;
    private double _autoScaleFloorDb = DefaultAutoScaleFloorDb;
    private double _autoScaleSpeedDbPerSecond = DefaultAutoScaleSpeedDbPerSecond;
    private double _tracePersistenceSeconds = DefaultTracePersistenceSeconds;

    public StereoImageDisplaySettings()
    {
        ResetCommand = new RelayCommand(_ => Reset());
    }

    public bool IsAutoScale
    {
        get => _isAutoScale;
        set
        {
            if (SetProperty(ref _isAutoScale, value))
            {
                OnPropertyChanged(nameof(ScaleSummary));
            }
        }
    }

    public double ScaleRangeDb
    {
        get => _scaleRangeDb;
        set
        {
            var clamped = Math.Clamp(value, 12, 96);
            if (SetProperty(ref _scaleRangeDb, clamped))
            {
                OnPropertyChanged(nameof(ManualFloorDb));
                OnPropertyChanged(nameof(ScaleSummary));
            }
        }
    }

    public double ManualCeilingDb
    {
        get => _manualCeilingDb;
        set
        {
            var clamped = Math.Clamp(value, -60, 12);
            if (SetProperty(ref _manualCeilingDb, clamped))
            {
                OnPropertyChanged(nameof(ManualFloorDb));
                OnPropertyChanged(nameof(ScaleSummary));
            }
        }
    }

    public double AutoScaleFloorDb
    {
        get => _autoScaleFloorDb;
        set
        {
            var clamped = Math.Clamp(value, -72, 0);
            if (SetProperty(ref _autoScaleFloorDb, clamped))
            {
                OnPropertyChanged(nameof(ScaleSummary));
            }
        }
    }

    public double AutoScaleSpeedDbPerSecond
    {
        get => _autoScaleSpeedDbPerSecond;
        set
        {
            var clamped = Math.Clamp(value, 1, 24);
            if (SetProperty(ref _autoScaleSpeedDbPerSecond, clamped))
            {
                OnPropertyChanged(nameof(ScaleSummary));
            }
        }
    }

    public double TracePersistenceSeconds
    {
        get => _tracePersistenceSeconds;
        set
        {
            var clamped = Math.Clamp(value, 0.02, 0.25);
            if (SetProperty(ref _tracePersistenceSeconds, clamped))
            {
                OnPropertyChanged(nameof(TracePersistenceMilliseconds));
            }
        }
    }

    public double TracePersistenceMilliseconds => TracePersistenceSeconds * 1000;

    public string ScaleSummary => IsAutoScale
        ? $"Auto / {ScaleRangeDb:0} dB / {AutoScaleFloorDb:0} dB"
        : $"{ManualFloorDb:0}..{ManualCeilingDb:0} dB";

    public double ManualFloorDb => ManualCeilingDb - ScaleRangeDb;

    public ICommand ResetCommand { get; }

    public void Reset()
    {
        IsAutoScale = DefaultAutoScale;
        ScaleRangeDb = DefaultScaleRangeDb;
        ManualCeilingDb = DefaultManualCeilingDb;
        AutoScaleFloorDb = DefaultAutoScaleFloorDb;
        AutoScaleSpeedDbPerSecond = DefaultAutoScaleSpeedDbPerSecond;
        TracePersistenceSeconds = DefaultTracePersistenceSeconds;
        OnPropertyChanged(nameof(ManualFloorDb));
    }
}
