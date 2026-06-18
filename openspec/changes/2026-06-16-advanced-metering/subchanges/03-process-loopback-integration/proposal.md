# 03-process-loopback-integration

## Goal
Feed the analyzer engine with per-application PCM where Windows process-loopback capture is available.

## In Scope
- Windows process-loopback capture feasibility and OS-version detection.
- Managed interop or library integration for process/process-tree capture.
- Capture lifecycle per application/session.
- Fallback behavior when process-loopback is unavailable or unsupported.
- Latency/CPU validation notes.

## Out of Scope
- Kernel driver or virtual audio cable development.
- Cross-platform capture.
- Full packaging polish.

## Depends On
- `02-dsp-metric-engine` for analyzer input and output contracts.
- Current Windows Core Audio process-loopback API availability.

## Done Means
- Supported systems can feed per-application PCM into the analyzer.
- Unsupported systems continue to show peak-session data and clear unavailable states for sample-derived metrics.
- Validation records OS/API constraints and observed performance.

