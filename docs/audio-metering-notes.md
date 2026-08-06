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

True-peak reconstruction uses the same Hann-windowed sinc kernel shape in both managed and native paths. When `DesktopLoudnessMeter.Native.dll` is available, the analyzer batches each incoming audio block through the RfRapid C++ backend and receives per-frame oversampled display samples plus rolling true-peak state. If the native DLL or exports are unavailable, it falls back to the managed estimator. A local 30 s stereo 48 kHz benchmark measured the 8x estimator at about `5.00 Mframes/s` native versus about `1.97 Mframes/s` managed with matching peak output.

## Process Loopback Status

`2026-06-16-advanced-metering/03-process-loopback-integration` wires process-backed WASAPI sessions to a Windows process-loopback capture worker. The worker requests 48 kHz stereo IEEE-float PCM, feeds `PcmMetricAnalyzer`, and merges the latest analyzer snapshot back into each row by process ID.

This path requires Windows 10 build 20348 or later and can still fail for protected, stale, or unsupported process sessions. When activation fails, the app keeps peak-derived session meters alive and leaves sample-derived LUFS/detail values unavailable for that process.

Core Audio sessions enumerated from a render endpoint do not represent capture streams, but a capture-oriented app can still own an initialized or monitoring render stream. uMeter therefore observes every non-self render session and, when process loopback is available, exposes a process-backed row only after its loopback PCM contains a non-silent recent true-peak or RMS window. A capture-only process remains hidden; if it later renders monitoring or program audio, it becomes visible on the next analyzed buffer. Session-peak filtering remains the compatibility fallback when process-loopback activation is unavailable.

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

True-peak detection and waveform/spectrum display now share the same oversampled reconstruction output within each analyzer path. The native RfRapid path keeps the reconstruction batched per incoming audio block; the managed fallback keeps the original per-frame estimator for portability.

## Next Slice

The next useful slice is performance polish: profile analyzer and WPF render allocations, reuse FFT/display buffers where safe, cache drawing resources, and keep the 33 ms refresh loop smooth.
