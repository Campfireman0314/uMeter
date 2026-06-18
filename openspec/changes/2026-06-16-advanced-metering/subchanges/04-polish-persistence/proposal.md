# 04-polish-persistence

## Goal
Make the advanced meter settings comfortable for daily use.

## In Scope
- Persist metric mode, true-peak toggle/rate, always-on-top preference, and window positions.
- Improve inline detail sizing and multi-monitor floating-window behavior.
- Add compact/expanded behavior if needed after the detail UI lands.
- Update README and troubleshooting notes.

## Out of Scope
- New DSP algorithms.
- Process-loopback capture changes.
- Installer/signing unless explicitly requested later.

## Depends On
- `01-meter-controls-detail-shell`.
- `02-dsp-metric-engine` and `03-process-loopback-integration` for final labels/settings if those slices change exposed options.

## Done Means
- User-facing settings survive restart.
- The app remains easy to use as a floating desktop utility with advanced controls enabled.
