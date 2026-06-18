# 02-dsp-metric-engine

## Goal
Implement a tested audio-analysis engine that computes loudness and stereo metrics from PCM sample windows.

## In Scope
- Momentary/short-term/integrated loudness model with industry-standard time windows.
- True-peak estimation with selectable 2x/4x/8x oversampling options.
- 100 ms windowed RMS.
- Stereo image sample positions for the semi-circle fan display and a -1..+1 correlation scalar.
- Resettable analyzer state keyed by audio session/application.
- Synthetic-signal unit tests for stable DSP behavior.

## Out of Scope
- Capturing per-process PCM from Windows.
- UI redesign beyond binding the existing metric slots.
- Claiming formal compliance until validated against reference vectors.

## Depends On
- `01-meter-controls-detail-shell` for display slots and reset semantics.

## Done Means
- Tests validate analyzer behavior on silence, mono, hard-panned, anti-phase, and sine-like signals.
- Metrics are exposed through app models with explicit unavailable/valid states.
- Documentation states which results are approximate versus standards-validated.
- The analyzer is independent of WASAPI capture and can be fed by the later process-loopback integration slice.
