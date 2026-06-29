# Design

## U1: Sharp Render Column Mapping

`SpectrumRollView` keeps the existing rolling `WriteableBitmap` architecture but stops rendering neighbor-derived bucket energy by default. A small internal column rasterizer owns the conversion from normalized analyzer buckets to BGRA pixels, making one-hot column behavior testable without visual screenshots.

The control uses `BitmapScalingMode.NearestNeighbor` for the roll bitmap. This favors bucket faithfulness over soft visual interpolation and makes a one-bucket signal remain a one-bucket row when the bitmap is scaled.

## U2: Raw-PCM Spectrum Window

`RecentSampleWindow` continues to use oversampled display samples for waveform display, but stores a separate raw mono/channel PCM ring for spectrum FFT input. Spectrum FFT size, frequency mapping, and refresh cadence therefore follow the source sample rate and performance mode, not true-peak oversampling.

This preserves the true-peak waveform benefit while making spectrum resolution stable across 2x/4x/8x oversampling settings.

## U3: Diagnostic Tests And Documentation

Tests cover:

- one-hot renderer columns do not create neighboring non-zero pixels;
- single-tone Mel bucket localization at 100 Hz, 1 kHz, 4 kHz, and 12 kHz;
- oversampling invariance for the same PCM tone.

Documentation updates `docs/rendering-architecture.md` with the measured causes and the final model.

## Tradeoffs

- Nearest-neighbor bitmap scaling is intentionally crisper and may look less silky than linear scaling on very tall views.
- Hann-window leakage remains physically real; this change avoids exaggerating it through render blur and oversampling coupling.
- Bucket center sampling remains in place for this slice; Mel-band aggregation is a future, more invasive DSP change.
