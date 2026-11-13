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

The analyzer is standards-inspired and ready for synthetic-signal testing, but it is not yet certified against official reference vectors.

K-weighting follows ITU-R BS.1770-4 and is designed for the configured input rate rather than being fixed at 48 kHz. BS.1770-4 prints only a 48 kHz coefficient set, but that set is a bilinear transform of a rate-independent analog prototype (high shelf `f0 = 1681.974450955533 Hz`, `G = 3.999843853973347 dB`, `Q = 0.7071752369554196`; RLB high-pass `f0 = 38.13547087602444 Hz`, `Q = 0.5003270373238773`). `KWeightingFilter` evaluates that prototype at the analyzer's sample rate, so 44.1 kHz and 96 kHz sources are no longer measured with a filter whose corner frequencies have slid off the intended curve. Only the shelf and high-pass design frequencies are clamped below Nyquist (`f <= 0.49 * fs`) so unusually low but positive sample rates still produce a stable biquad; the positive-sample-rate API is unchanged.

48 kHz calibration is preserved: the derived 48 kHz coefficients reproduce the published BS.1770-4 coefficient set to within double rounding (`1.53512485958697, -2.69169618940638, 1.19839281085285, -1.69065929318241, 0.73248077421585` and `1, -2, 1, -1.99004745483398, 0.99007225036621`), and the measured loudness difference against the previously hard-coded 48 kHz coefficients is at most `3.5e-12 dB`.

Numerical trust checks live in `tests/DesktopLoudnessMeter.Tests/Audio/Analysis/KWeightingRateResponseTests.cs`:

- Published reference: EBU Tech 3341's stereo 1 kHz sine at -23.0 dBFS must read -23.0 LUFS at 44.1, 48, and 96 kHz (xunit tolerance 0.1 LU; measured -22.9905 / -22.9933 / -23.0106 LUFS).
- Precomputed reference literals at 100 Hz, 1 kHz, 2 kHz, and 10 kHz for 44.1, 48, and 96 kHz, produced offline by a NumPy implementation written from the BS.1770-4 analog prototype parameters above, never by the production filter. Expected steady-state stereo loudness is `-0.691 + 10*log10(|H(f)|^2 * A^2)` for a sine of peak amplitude `A` duplicated to both channels (xunit tolerance 0.1 LU; measured deviation `<= 0.0001 LU`).
- Streaming and reset checks: the same signal fed as one block or as 37/512-frame chunks must produce the same reading, and a reset must restore both the unavailable state and the rate-correct steady-state reading.
- True peak has one closed-form fixture in addition to the native/managed agreement test: a sine at `fs/4` with phase `pi/4` has discrete samples of `A/sqrt(2)` but a true peak of exactly `A`, so a 0.9-amplitude tone must read `-0.9151 dBFS` (measured `-0.9134 dBFS`, i.e. 3.01 dB above the discrete peak, which is the analytically expected 3.01 dB).

Stated tolerances cover the finite 400 ms observation window (single-tone mean-square ripple of about `1/(2*pi*f*T)`, roughly 0.017 LU at 100 Hz) and float32 input rounding. This is still not an ITU/EBU certification claim: the suite covers synthesised steady-state tones, not the full official compliance material, and no resampling is applied to non-48 kHz sources.

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
