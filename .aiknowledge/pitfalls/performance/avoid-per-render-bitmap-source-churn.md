---
status: active
created_at: 2026-06-27
created_from: change:2026-06-27-rendering-and-title-polish
last_verified_at: 2026-06-27
last_verified_by: opsx-knowledge
verification_basis: bugfix
applies_to:
  - src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs
  - src/DesktopLoudnessMeter.App/Controls/SpectrumRollBitmapSegmentPlan.cs
superseded_by:
---

# Avoid Per-Render Bitmap Source Churn

**Tags:** [csharp, wpf, performance, rendering]

## Symptom

A WPF bitmap-based visualization starts acceptably but feels progressively more sluggish during long-running updates.

## Root Cause

The render path allocated derived bitmap image sources for wrapped history segments. Those objects are created at render frequency and can add managed and unmanaged pressure over time.

## Before Fix

```diff
-        drawingContext.DrawImage(
-            new CroppedBitmap(_bitmap, new Int32Rect(sourceX, 0, sourceWidth, _bitmapBucketCount)),
-            destination);
```

## After Fix

```diff
+        var plan = SpectrumRollBitmapSegmentPlan.ForSegment(
+            sourceX,
+            sourceWidth,
+            _historyColumnCount,
+            destination);
+        drawingContext.PushClip(new RectangleGeometry(plan.Clip));
+        drawingContext.DrawImage(_bitmap, plan.ImageDestination);
+        drawingContext.Pop();
```

## Key Takeaway

In WPF render loops, clip and transform a stable bitmap source instead of allocating derived image sources every frame.

## Source

working tree: change `2026-06-27-rendering-and-title-polish` (2026-06-27)
