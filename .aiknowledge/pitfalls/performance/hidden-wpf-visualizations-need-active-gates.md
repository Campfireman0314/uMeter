---
status: active
created_at: 2026-06-27
created_from: bugfix:2026-06-27-hidden-detail-graphs
last_verified_at: 2026-06-27
last_verified_by: opsx-bugfix
verification_basis: build-and-test
applies_to:
  - src/DesktopLoudnessMeter.App/Controls/WaveformRollView.cs
  - src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs
  - src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs
superseded_by:
---

# Hidden WPF Visualizations Need Active Gates

**Tags:** [csharp, wpf, performance, rendering]

## Symptom

Detail graphs that are visually collapsed still contribute periodic UI work during long-running sessions.

## Root Cause

WPF bindings can keep updating dependency properties on collapsed controls. If tick callbacks always enqueue deferred renders or append history samples, hidden visualizations continue doing timer-driven work even though no pixels are visible.

## Fix

Add an explicit `IsActive` dependency property for heavy visualization controls, bind it to the graph module visibility, and also check loaded/visible/layout readiness before queuing render updates or appending loudness-clock samples.

## Key Takeaway

Do not rely on `Visibility=Collapsed` alone to stop live visualization work. Gate the tick callback itself so hidden controls keep only the latest pending value and do not queue background rendering.

## Source

working tree: bugfix for hidden detail graphs (2026-06-27)
