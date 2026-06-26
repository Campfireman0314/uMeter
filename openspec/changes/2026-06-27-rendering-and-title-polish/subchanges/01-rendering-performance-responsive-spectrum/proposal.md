# 01-rendering-performance-responsive-spectrum

## Goal
Reduce periodic CPU spikes and make waveform/spectrum rendering feel responsive on lower-end CPUs while allowing the spectrograph to adapt its rendering resolution to the visible viewport size.

## In Scope
- Investigate render/update paths that can create periodic CPU spikes or draggy waveform updates.
- Improve waveform rendering responsiveness without changing metering semantics.
- Add responsive spectrograph resolution based on viewport dimensions, with stable minimum and maximum bounds.
- Preserve existing pan/zoom, display range, deferred render coalescing, and spectrum sharpness behavior.
- Add focused tests for any new pure sizing, throttling, or scheduling logic.

## Out of Scope
- Replacing the WPF rendering stack.
- Changing loudness, true-peak, RMS, stereo, or collision analysis semantics.
- Adding a benchmark harness beyond lightweight unit-testable helpers.
- Installer, version, or release metadata changes.

## Depends On
- None.

## Done Means
- Waveform rendering avoids avoidable full redraw or over-dense point work for a given viewport.
- Spectrograph history/bucket resolution adapts to viewport size within documented bounds.
- Existing waveform/spectrum interaction behavior remains intact.
- Focused tests and the managed test suite pass.
