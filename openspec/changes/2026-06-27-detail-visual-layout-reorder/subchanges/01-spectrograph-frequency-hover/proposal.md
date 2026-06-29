# Spectrograph Frequency Hover

## Goal

Add a lightweight mouse-following horizontal frequency marker to the spectrograph so users can quickly read the frequency under the cursor.

## In Scope

- Draw a horizontal hover marker and compact frequency label in `SpectrumRollView`.
- Reuse the existing spectrum frequency scale and pan/zoom mapping.
- Keep render allocations bounded and avoid derived bitmap churn.
- Add focused tests for the frequency-readout mapping if a helper is introduced.

## Out of Scope

- Changing FFT analysis, spectrum bucket generation, or spectrograph color mapping.
- Persistent marker pins or interactive annotations.

## Depends On

- Existing dynamic-resolution `SpectrumRollView` rendering.

## Done Means

- Moving the mouse over a spectrograph shows the hovered frequency without disrupting pan/zoom.
- Leaving the graph hides the marker.
- Managed tests/build pass.
