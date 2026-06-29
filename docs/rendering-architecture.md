# Rendering Architecture Notes

This note tracks the graph-rendering cleanup started after `v2.2.5`.

## Current Controls

- `StereoFanView` renders directly from current stereo-image points. It is visually smooth because it has no rolling bitmap/history queue and only redraws the current point cloud.
- `FrequencyCollisionBar` renders current collision bands directly, with cached static background ticks.
- `WaveformRollView` receives waveform sample buffers, defers UI-thread application until the control is visible and sized, then caches its background and waveform geometry.
- `SpectrumRollView` receives spectrum buckets, defers UI-thread application until the control is visible and sized, then appends one column into a rolling `WriteableBitmap`. The rolling bitmap uses nearest-neighbor scaling and direct bucket-to-row color mapping so the renderer does not add hidden frequency blur.

## Shared Pieces

- `ThemedDrawing` owns common brush, pen, color blending, alpha, spectrum-pixel helpers, and named `PlotColorRole` lookup for graph surfaces.
- `DeferredRenderUpdate` owns the shared "latest pending data, one queued dispatcher render callback" policy used by waveform and spectrum controls.
- `PlotBackgroundCache` owns static background invalidation keys for graph controls that cache axes, frames, and tick labels.

The deferred scheduler exists to keep view-model property churn from queueing unbounded render work. It intentionally keeps only the latest pending payload; plot controls should not rely on every intermediate property change being rendered.

## Design Constraints

- DSP data should remain separate from display transforms. Spectrum soft-floor and smoothing are render-only.
- Spectrum FFT input is raw PCM mono/channel history, not the true-peak oversampled waveform display buffer. This keeps dominant bucket placement stable when the true-peak oversampling preference changes.
- Spectrum columns should not vertically smooth or gap-fill analyzer buckets by default. Hann-window leakage and Mel bucket spacing are real analyzer/display-model limits; renderer interpolation should not exaggerate them.
- Plot controls should apply data only when loaded, visible, and measured.
- Plot controls should prefer named `PlotColorRole` values over direct resource-key strings for shared graph colors.
- Expensive static drawing such as axes, labels, and frames should be cached until size, DPI, viewport, or theme color changes.
- Rolling bitmap writes must tolerate transient WPF lock conflicts and retry without crashing the app.

## Next Cleanup Candidates

- Expand `PlotColorRole` only when a concrete graph needs a new semantic color role.
- Add optional debug counters for accepted frames, coalesced frames, bitmap retries, and render invalidations.
- Consider visual-regression screenshots before larger plot-layout changes.
- If spectrum blur is revisited, compare center-sampled Mel buckets against measured Mel-band aggregation. Test single-tone, off-bin, two-tone, chirp, silence, and impulse cases before changing the bucket model.
