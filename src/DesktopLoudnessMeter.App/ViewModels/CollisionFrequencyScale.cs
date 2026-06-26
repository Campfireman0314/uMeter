namespace DesktopLoudnessMeter.App.ViewModels;

public enum CollisionFrequencyScale
{
    Mel = 0,
    Logarithmic = 1
}

public static class CollisionFrequencyScaleExtensions
{
    public static string ToDisplayLabel(this CollisionFrequencyScale scale)
    {
        return scale switch
        {
            CollisionFrequencyScale.Logarithmic => "Logarithmic",
            _ => "Mel"
        };
    }
}
