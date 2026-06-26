---
status: active
created_at: 2026-06-27
created_from: change:2026-06-27-rendering-and-title-polish
last_verified_at: 2026-06-27
last_verified_by: opsx-knowledge
verification_basis: review
applies_to:
  - src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs
  - src/DesktopLoudnessMeter.App/Audio/Analysis/PcmAnalysisPerformanceSettings.cs
superseded_by:
---

# Throttle Intervals Must Exceed Caller Cadence

**Tags:** [csharp, wpf, performance, rendering]

## Symptom

A rendering cache appears throttled in code, but CPU spikes still happen at the UI refresh cadence on lower-end machines.

## Root Cause

The eligibility interval was close to or below the caller's periodic refresh cadence, so each UI tick still had enough new audio frames to rebuild the expensive waveform buffer.

## Before Fix

```diff
-        WaveformRefreshIntervalFrames: 1024);
```

## After Fix

```diff
+        WaveformRefreshIntervalFrames: 2048);
```

## Key Takeaway

A cache throttle only reduces periodic CPU work when its eligibility interval is longer than the caller's refresh cadence.

## Source

working tree: change `2026-06-27-rendering-and-title-polish` (2026-06-27)
