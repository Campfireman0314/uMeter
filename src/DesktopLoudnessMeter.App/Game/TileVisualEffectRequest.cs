namespace DesktopLoudnessMeter.App.Game;

public enum TileVisualEffectKind
{
    Selection,
    Match,
    RhythmEcho
}

public readonly record struct TileVisualEffectRequest(
    TileVisualEffectKind EffectKind,
    SignalTileKind TileKind,
    double CenterX,
    double CenterY);
