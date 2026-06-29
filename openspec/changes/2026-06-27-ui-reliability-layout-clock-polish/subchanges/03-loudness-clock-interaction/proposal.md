# 03-loudness-clock-interaction

## Goal
Improve the loudness clock's readability with time scale markers and hover value readouts.

## In Scope
- Render time scale markers around the fixed cycle table.
- Show a hover readout blob with time-position and loudness value information when the mouse is over the clock graph.
- Keep rendering bounded and compatible with existing fan/scan-line/warning/over-maximum behavior.
- Add tests for time marker planning and hover-value mapping where practical.

## Out of Scope
- Changing loudness clock persistence semantics.
- Adding audio analysis metrics beyond momentary loudness.
- Replacing the existing fan renderer.

## Depends On
- Existing `LoudnessClockView`, point mapping, scale planning, and cycle-duration settings.

## Done Means
- The clock visibly communicates time positions in the selected cycle.
- Hovering the graph displays a useful value readout without destabilizing rendering.
- Focused clock tests and full build pass.
