using DesktopLoudnessMeter.App.Settings;
using DesktopLoudnessMeter.App.Localization;

namespace DesktopLoudnessMeter.App.ViewModels;

public sealed class DetailModuleViewModel : ObservableObject
{
    private int _orderIndex;
    private bool _isVisible;
    private double _height;
    private bool _isReorderDropBefore;
    private bool _isReorderDropAfter;

    public DetailModuleViewModel(DetailModuleKind kind, string title, bool isVisible)
    {
        Kind = kind;
        SourceTitle = title;
        _isVisible = isVisible;
        _height = DetailModuleLayoutNormalizer.DefaultHeight(kind);
    }

    public DetailModuleKind Kind { get; }

    private string SourceTitle { get; }

    public string Title => LocalizationManager.Translate(SourceTitle);

    public void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(Title));
    }

    public int OrderIndex
    {
        get => _orderIndex;
        set => SetProperty(ref _orderIndex, Math.Max(0, value));
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (SetProperty(ref _isVisible, value))
            {
                OnPropertyChanged(nameof(DisplayHeight));
                OnPropertyChanged(nameof(DisplayMinimumHeight));
            }
        }
    }

    public double Height
    {
        get => _height;
        set
        {
            if (SetProperty(ref _height, DetailModuleLayoutNormalizer.NormalizeHeight(Kind, value)))
            {
                OnPropertyChanged(nameof(DisplayHeight));
            }
        }
    }

    public double DisplayHeight
    {
        get => IsVisible ? Height : double.NaN;
        set
        {
            if (IsVisible && double.IsFinite(value))
            {
                Height = value;
            }
        }
    }

    public double DisplayMinimumHeight => IsVisible ? DetailModuleLayoutNormalizer.MinimumHeightFor(Kind) : 0;

    public bool IsReorderDropBefore
    {
        get => _isReorderDropBefore;
        private set => SetProperty(ref _isReorderDropBefore, value);
    }

    public bool IsReorderDropAfter
    {
        get => _isReorderDropAfter;
        private set => SetProperty(ref _isReorderDropAfter, value);
    }

    public DetailModuleLayoutSettings ToLayout()
    {
        return new DetailModuleLayoutSettings
        {
            Kind = Kind,
            OrderIndex = OrderIndex,
            Height = Height
        };
    }

    public void ApplyLayout(DetailModuleLayoutSettings layout)
    {
        OrderIndex = layout.OrderIndex;
        Height = layout.Height ?? DetailModuleLayoutNormalizer.DefaultHeight(Kind);
    }

    public void SetReorderDropHint(bool before, bool after)
    {
        IsReorderDropBefore = before;
        IsReorderDropAfter = after;
    }
}
