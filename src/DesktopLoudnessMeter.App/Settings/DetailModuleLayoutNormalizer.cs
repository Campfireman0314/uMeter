namespace DesktopLoudnessMeter.App.Settings;

internal static class DetailModuleLayoutNormalizer
{
    public const double MinimumHeight = 160;
    public const double MaximumHeight = 1200;
    public const double FixedCollisionHeight = 112;

    private static readonly DetailModuleKind[] DefaultOrder =
    [
        DetailModuleKind.Stereo,
        DetailModuleKind.Collision,
        DetailModuleKind.LoudnessClock,
        DetailModuleKind.Waveform,
        DetailModuleKind.Spectrum
    ];

    public static IReadOnlyList<DetailModuleLayoutSettings> DefaultLayouts()
    {
        return DefaultOrder
            .Select((kind, index) => new DetailModuleLayoutSettings
            {
                Kind = kind,
                OrderIndex = index,
                Height = DefaultHeight(kind)
            })
            .ToArray();
    }

    public static List<DetailModuleLayoutSettings> Normalize(IEnumerable<DetailModuleLayoutSettings>? modules)
    {
        var normalized = new List<DetailModuleLayoutSettings>();
        var seen = new HashSet<DetailModuleKind>();
        foreach (var module in (modules ?? []).Where(module => Enum.IsDefined(module.Kind))
                     .OrderBy(module => module.OrderIndex < 0 ? int.MaxValue : module.OrderIndex))
        {
            if (!seen.Add(module.Kind))
            {
                continue;
            }

            normalized.Add(new DetailModuleLayoutSettings
            {
                Kind = module.Kind,
                OrderIndex = normalized.Count,
                Height = NormalizeHeight(module.Kind, module.Height)
            });
        }

        foreach (var kind in DefaultOrder)
        {
            if (seen.Add(kind))
            {
                normalized.Add(new DetailModuleLayoutSettings
                {
                    Kind = kind,
                    OrderIndex = normalized.Count,
                    Height = DefaultHeight(kind)
                });
            }
        }

        for (var index = 0; index < normalized.Count; index++)
        {
            normalized[index].OrderIndex = index;
        }

        return normalized;
    }

    public static double DefaultHeight(DetailModuleKind kind)
    {
        return kind switch
        {
            DetailModuleKind.Stereo => 320,
            DetailModuleKind.Collision => FixedCollisionHeight,
            DetailModuleKind.LoudnessClock => 260,
            DetailModuleKind.Waveform => 220,
            DetailModuleKind.Spectrum => 220,
            _ => 220
        };
    }

    public static double NormalizeHeight(DetailModuleKind kind, double? height)
    {
        if (kind == DetailModuleKind.Collision)
        {
            return FixedCollisionHeight;
        }

        return height is double value && double.IsFinite(value)
            ? Math.Clamp(Math.Round(value, 0, MidpointRounding.AwayFromZero), MinimumHeightFor(kind), MaximumHeight)
            : DefaultHeight(kind);
    }

    public static double MinimumHeightFor(DetailModuleKind kind)
    {
        return kind switch
        {
            DetailModuleKind.Stereo => 320,
            DetailModuleKind.Collision => FixedCollisionHeight,
            _ => MinimumHeight
        };
    }
}
