# 02-process-loopback-loudness

## Goal
Upgrade the MVP from WASAPI session peak metering toward true per-process loudness analysis based on captured audio samples.

## In Scope
- Research and prototype Windows process-loopback capture APIs.
- Capture process-specific or process-tree audio where supported by the OS.
- Compute momentary/short-term loudness or a clearly documented approximation.
- Compare CPU cost, latency, and permission/OS-version constraints.
- Keep fallback behavior for systems where process-loopback capture is unavailable.

## Out of Scope
- Kernel-mode driver development.
- Cross-platform audio capture.
- Installer, signing, and distribution polish.
- UI redesign beyond what is required to expose richer loudness fields.

## Depends On
- `01-session-meter-mvp` for app shell, session identity, and display model.
- Confirmation of supported Windows versions and API accessibility from managed code.

## Done Means
- A prototype or implementation can capture/analyze per-process audio where Windows supports it.
- The app distinguishes true loudness metrics from peak/RMS approximations.
- Validation documents measured latency, CPU impact, and unsupported scenarios.

