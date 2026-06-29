# Bridge Circular Plot Cycle Origins

## Status
active

## Last Verified
2026-06-27

## Symptom
A circular loudness-clock fan left a narrow slit at the initial twelve-o'clock position after the history wrapped into a new cycle.

## Root Cause
The renderer skipped decreasing phase pairs to avoid drawing across the cycle wrap, but timer cadence rarely produces a sample exactly at phase zero. That left tiny uncovered sectors on both sides of the cycle origin.

## Fix
When a mapped sample sequence wraps from high phase back to low phase, synthesize two fan segments: previous sample to the cycle origin, and cycle origin to the first new-cycle sample. Use each side's radius and warning state so the fill and trace remain visually continuous.

## Key Takeaway
For circular or periodic history renderers, handle the mathematical cycle boundary explicitly. Do not rely on real-time sampling cadence to hit the origin exactly.

## Source
Bugfix for `src/DesktopLoudnessMeter.App/Controls/LoudnessClockFanSegmentPlanner.cs` and `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs`.
