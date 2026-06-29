# Performance

| Entry | Status | Last Verified | Summary |
|-------|--------|---------------|---------|
| [Avoid Per-Render Bitmap Source Churn](avoid-per-render-bitmap-source-churn.md) | active | 2026-06-27 | In WPF render loops, clip and transform a stable bitmap source instead of allocating derived image sources every frame. |
| [Hidden WPF Visualizations Need Active Gates](hidden-wpf-visualizations-need-active-gates.md) | active | 2026-06-27 | Collapsed WPF visualizations can still receive bound ticks unless the callback gates live render work explicitly. |
| [Throttle Intervals Must Exceed Caller Cadence](throttle-intervals-must-exceed-caller-cadence.md) | active | 2026-06-27 | A cache throttle only reduces periodic CPU work when its eligibility interval is longer than the caller's refresh cadence. |
