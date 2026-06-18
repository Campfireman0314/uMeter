# Audio Metering Notes

## v0.1.0 Measurement Semantics

The first usable version displays peak-derived global levels from the Windows render endpoint and, where Windows process-loopback capture succeeds, augments each visible app row with PCM-derived loudness/detail metrics.

The metrics are intended for fast desktop supervision and mix/debug awareness. They are not yet certified against official ITU/EBU reference vectors, so the app should not be treated as a compliance loudness meter.

## Why Start With WASAPI Sessions

Windows Core Audio exposes render sessions and session peak meters through WASAPI. That gives the app a low-latency, low-risk way to list application audio sessions without installing a driver or virtual audio cable.

## Known Limits

- A row is normalized toward one visible app, but Windows audio sessions can still be short-lived or ambiguous for protected/system processes.
- Process IDs and friendly process metadata can be unavailable for system, protected, or short-lived sessions.
- Process-loopback capture requires OS support and may fail for protected, stale, or unsupported target sessions.
- The app currently runs from `dotnet run`; there is no installer or persistent user settings yet.
- The analyzer is standards-inspired, but not yet certified against official reference material.

## DSP Engine Semantics

`2026-06-16-advanced-metering/02-dsp-metric-engine` adds a standalone PCM analyzer that accepts interleaved floating-point mono/stereo samples. It computes:

- K-weighted momentary loudness over 400 ms.
- K-weighted short-term loudness over 3 s.
- Integrated loudness from 400 ms windows with 100 ms hops, absolute gating, and relative gating.
- Oversampled true peak over a recent 100 ms window with selectable 2x, 4x, or 8x factors.
- 100 ms windowed RMS.
- Polar stereo image sample points for the semi-circle fan display.
- Correlation in the `[-1, 1]` range.

The analyzer is standards-inspired and ready for synthetic-signal testing, but it is not yet certified against official reference vectors. The current K-weighting coefficients are the common 48 kHz coefficient set; non-48 kHz capture paths should be treated as approximate until a sample-rate-specific filter design or resampling step is added.

## Process Loopback Status

`2026-06-16-advanced-metering/03-process-loopback-integration` wires process-backed WASAPI sessions to a Windows process-loopback capture worker. The worker requests 48 kHz stereo IEEE-float PCM, feeds `PcmMetricAnalyzer`, and merges the latest analyzer snapshot back into each row by process ID.

This path requires Windows 10 build 20348 or later and can still fail for protected, stale, or unsupported process sessions. When activation fails, the app keeps peak-derived session meters alive and leaves sample-derived LUFS/detail values unavailable for that process.

## First Usable Checkpoint

`v0.1.0` pins the current behavior as usable for local desktop testing:

- Duplicate session rows are collapsed toward app-level rows.
- The meter app hides itself from the visible row list.
- Silent active sessions are filtered unless analyzer metrics indicate audible content.
- Detail rows follow quick session handle release/recovery by app display name.
- Reset actions clear analyzer state in the active process-loopback workers.

## Second Usable Checkpoint

`v0.2.0` extends the detail workflow:

- Detail panels can be opened inline or as pop-up windows per app.
- Pop-up windows keep independent topmost and stereo scale settings.
- Stereo image auto-scale exposes range, lower-limit, manual-top, speed, and reset controls.
- True-peak oversampling can be changed per app from inline and pop-up detail views.
- The active process-loopback worker receives true-peak oversampling changes immediately.
- Short render-session handle dropouts retain the analyzer worker briefly to avoid dead detail views.

## Third Usable Checkpoint

`v0.3.0` extends the detail display and tray workflow:

- The main window can hide to the status tray and restore from tray actions.
- Detail views expose separate hidden waveform and spectrum panels.
- Waveform and spectrum panels can be resized in single-panel and two-panel modes.
- Waveform display uses an oversampled mono display buffer that follows the selected true-peak oversampling ratio.
- Waveform scale markings are in dBFS.
- Spectrum display uses an FFT-derived, Mel-spaced 192-bin display for smoother horizontal rolling output.

True-peak detection and waveform/spectrum display currently use separate reconstruction buffers. True peak reports a recent-window maximum; the display path keeps ordered oversampled samples for drawing and FFT analysis. This separation keeps metering behavior isolated, but it is a performance cleanup candidate.

## Next Slice

The next useful slice is performance polish: profile analyzer and WPF render allocations, reuse FFT/display buffers where safe, cache drawing resources, and keep the 33 ms refresh loop smooth.
