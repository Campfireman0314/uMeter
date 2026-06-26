---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-02-spectrograph-fidelity
last_verified_at: 2026-06-26
last_verified_by: opsx-codemap
verification_basis: archive
applies_to:
  - src/DesktopLoudnessMeter.App/Audio/Analysis
  - src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs
  - src/DesktopLoudnessMeter.App/Controls/FrequencyCollisionBar.cs
  - src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs
superseded_by:
---

# Analysis Rendering

## Responsibility
Converts PCM into loudness, waveform, spectrum, stereo, and collision displays. It separates raw PCM spectrum history from true-peak oversampled waveform display history and renders spectrum columns without hidden vertical smoothing.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs | Rolling PCM metrics, raw spectrum sample history, waveform display samples, and FFT bucket generation |
| src/DesktopLoudnessMeter.App/Controls/SpectrumRollColumnRasterizer.cs | Testable bucket-to-pixel column conversion and display-floor transform |
| src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs | Rolling WPF bitmap, pan/zoom, recoloring, and nearest-neighbor spectrograph rendering |
| src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs | Prominent overlap detection and band compaction |
| src/DesktopLoudnessMeter.App/ViewModels/CollisionFrequencyScaleMapper.cs | Mel/logarithmic display ratio conversions for collision bands and bars |
| tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollColumnRasterizerTests.cs | One-hot and low-level rasterizer coverage |
| tests/DesktopLoudnessMeter.Tests/Audio/Analysis/TruePeakAndStereoMetricTests.cs | Spectrum tone localization and true-peak oversampling invariance coverage |

## Implicit Constraints
- Spectrum FFT input uses original sample-rate PCM history; waveform display continues to use true-peak oversampled display samples.
- Spectrum rasterization maps one analyzer bucket to one row and preserves stored levels for recoloring.
- Collision display ratios may be remapped for logarithmic display, but bucket analysis remains based on Mel-spaced spectrum buckets.
