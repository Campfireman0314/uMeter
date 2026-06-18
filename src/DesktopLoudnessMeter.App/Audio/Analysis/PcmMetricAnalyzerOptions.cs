namespace DesktopLoudnessMeter.App.Audio.Analysis;

public sealed record PcmMetricAnalyzerOptions
{
    public PcmMetricAnalyzerOptions(int sampleRate, int channels, int truePeakOversampling)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be positive.");
        }

        if (channels is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(channels), "Only mono and stereo PCM are supported.");
        }

        if (truePeakOversampling is not (2 or 4 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(truePeakOversampling), "True-peak oversampling must be 2x, 4x, or 8x.");
        }

        SampleRate = sampleRate;
        Channels = channels;
        TruePeakOversampling = truePeakOversampling;
    }

    public int SampleRate { get; }

    public int Channels { get; }

    public int TruePeakOversampling { get; }
}
