# Rendering Architecture Notes

This note tracks the graph-rendering cleanup started after `v2.2.5`.

## Current Controls

- `StereoFanView` renders directly from current stereo-image points. It is visually smooth because it has no rolling bitmap/history queue and only redraws the current point cloud.
- `FrequencyCollisionBar` renders current collision bands directly, with cached static background ticks.
- `WaveformRollView` receives waveform sample buffers, defers UI-thread application until the control is visible and sized, then caches its background and waveform geometry.
- `SpectrumRollView` receives spectrum buckets, defers UI-thread application until the control is visible and sized, then appends one column into a rolling `WriteableBitmap`. The rolling bitmap uses nearest-neighbor scaling and direct bucket-to-row color mapping so the renderer does not add hidden frequency blur. It also retains one column of raw source bucket levels per history step, which the hover readout and later recoloring read instead of re-deriving levels from pixels.

## Shared Pieces

- `ThemedDrawing` owns common brush, pen, color blending, alpha, spectrum-pixel helpers, and named `PlotColorRole` lookup for graph surfaces.
- `DeferredRenderUpdate` owns the shared "latest pending data, one queued dispatcher render callback" policy used by waveform and spectrum controls.
- `PlotBackgroundCache` owns static background invalidation keys for graph controls that cache axes, frames, and tick labels.

The deferred scheduler exists to keep view-model property churn from queueing unbounded render work. It intentionally keeps only the latest pending payload; plot controls should not rely on every intermediate property change being rendered.

## Spectrum History and Cursor Readout

- `SpectrumHistoryLevels` is the single retained measurement history for the spectrograph: `historyColumns x sourceBuckets` normalized analyzer levels (0 = at/below the -200 dBFS analyzer floor, 1 = at/above the 0 dBFS ceiling). Row reduction happens on read, so the same stored column drives the bitmap pixels, a color/range recolor, and the numeric readout.
- The history replaces the earlier row-downsampled level array; it is not a second copy. Its bound is the existing resolution ceiling: at most 480 history columns by 320 source buckets, i.e. at most 1,228,800 bytes of `double` (plus the existing pixel buffer). Typical 320-bucket, ~200-column plots retain ~0.5 MB.
- Retained levels never depend on theme colors or the display floor/ceiling. Changing either recolors the bitmap from the retained levels and leaves numeric readouts untouched.
- `SpectrumHistoryCursorMapper` maps a pointer position to the displayed frequency, the nearest source Mel bucket, and the history column actually drawn under the pointer, reusing `SpectrumRollBitmapSegmentPlan.SplitWrappedDestination` so partial fill and the device-pixel-snapped wrap seam match rendering exactly.
- `SpectrumCursorReadout` converts the retained normalized level with `-200 + 200 * level`, formats one decimal, and distinguishes unavailable history, the -200 dBFS floor, and the 0 dBFS ceiling. Nothing is inferred from pixel colors, the rendered row maximum, or the newest spectrum.
- `SpectrumCursorLabelPlacement` keeps the label inside the plot; when the single-line `frequency · level` label does not fit, the control stacks frequency and level on two lines instead of opening a separate tooltip window.
- Pointer movement only re-invalidates the visual: no FFT, no analyzer work, and no history copies run on hover.

## Design Constraints

- DSP data should remain separate from display transforms. Spectrum soft-floor and smoothing are render-only.
- The cursor level is an FFT/bucket estimate of the retained source bucket, not a calibrated measurement at an arbitrary frequency: buckets are Mel-spaced over 20 Hz to 22 kHz in the display model, while the analyzer fills them to `min(22000 Hz, 0.49 * sample rate)`, and row reduction keeps the strongest bucket per row. Do not present it as true peak, RMS, or compliance evidence.
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
