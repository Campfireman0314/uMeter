namespace DesktopLoudnessMeter.App.Game;

internal readonly record struct TileVisualEffectProfile(
    int ParticleCount,
    double TravelDistance,
    TimeSpan Duration,
    int RingCount,
    bool ShowsGlyph)
{
    public static TileVisualEffectProfile For(TileVisualEffectKind effectKind)
    {
        return effectKind switch
        {
            TileVisualEffectKind.Match => new(
                ParticleCount: 16,
                TravelDistance: 76,
                Duration: TimeSpan.FromMilliseconds(620),
                RingCount: 2,
                ShowsGlyph: true),
            TileVisualEffectKind.RhythmEcho => new(
                ParticleCount: 10,
                TravelDistance: 48,
                Duration: TimeSpan.FromMilliseconds(440),
                RingCount: 2,
                ShowsGlyph: false),
            _ => new(
                ParticleCount: 6,
                TravelDistance: 20,
                Duration: TimeSpan.FromMilliseconds(300),
                RingCount: 1,
                ShowsGlyph: false)
        };
    }
}
