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

## Sliding Window Storage and Exact Long-Session Gating

`performance-followup-spectrum-level` (P1) changes how the analyzer stores its per-frame sliding windows and how integrated loudness is evaluated over a long block history. Numeric semantics are unchanged: double precision, the published gating formula, the 400 ms block / 100 ms hop cadence, every published snapshot and the immutable display views stay as delivered.

Frame windows (`SlidingPowerWindow`, `SlidingPeakWindow`) allocate their retention array once at the configured capacity. When the window is full the oldest value is evicted as the new one arrives instead of enqueuing a queue that then has to grow, so a full window never doubles its buffer. The running power sum keeps the historical update order - add the new value, then subtract the evicted one - so momentary, short-term, Mid/Side RMS and integrated block powers are bit-identical to the previous implementation: a matched 10 s/60 s/300 s run reports the same `M`/`S`/`TP`/`rms100`/`corr` values. Reading `SlidingPeakWindow.Peak` now scans the retained ring without allocating; the previous LINQ `Max` call over the queue allocated on every snapshot refresh.

Integrated loudness no longer rescans every completed block on each new block. `PowerAggregateIndex` is a deterministic balanced (AVL) binary search tree keyed by the exact measured block power, and every node keeps the number of blocks that share that value plus the subtree count and sum. Two queries answer the gating pass:

- The absolute-gated sum and count are accumulated chronologically while blocks complete, so the preliminary mean - and therefore the relative gate power derived from it - is bit-identical to the previous scan. Blocks are only added to that sum when they pass the -70 LUFS absolute gate, and skipping additions does not change a floating-point sum.
- The second (relative) pass asks the index for the sum and count of every block strictly above `max(absolute gate, relative gate)`. Membership is the original test `!(power <= threshold)`, including equality and duplicate powers; nothing is quantized, bucketed or truncated.

The relative-gated sum is therefore added in ascending power order instead of chronological order. A matched run of the same PCM aggregates that sum differently and moves the published integrated value by at most `4e-15 LU` (measured: `-3.2549856009585927` to `-3.254985600958591` LUFS at 10 s, `-3.254987179219084` to `-3.2549871792190825` at 60 s, `-3.254987424398371` to `-3.254987424398367` at 300 s). The documented tolerance for reordered aggregation is `1e-10 LU`; the test oracle is a separate naive two-pass implementation.

Costs, stated honestly rather than as bounded memory:

- Retained index memory grows with the number of *distinct* completed block powers, one tree node each. The worker's isolated mixed/duplicate-heavy workload measured roughly `31 KB` at 1,000 blocks, `184 KB` at 5,000 blocks and `1.22 MB` at 30,000 blocks, versus `8.5 KB`, `66 KB` and `262 KB` for the previous lists. Those workload totals are not a per-distinct-power estimate: its input deliberately repeats and rounds powers. A node stores three doubles, three ints and two references, approximately `72 B` on x64 .NET after object overhead/alignment (runtime-dependent). At ten blocks per second with mostly distinct values, node storage alone is therefore about `43.2 KB/minute` or `2.59 MB/hour` **per gate**, excluding the index/window objects and other analyzer storage. Overall, Mid and Side have separate gates; repeated powers share nodes, so their actual growth depends on the signal. The earlier `40 B/node` and `1.4 MB/hour` projection was understated and is superseded by this qualification.
- In the parent's final matched isolated runs (three warmups, three measured repeats), per-completed-block gate evaluation changed from `3.168/2.669/18.897 us` (1k/5k/30k blocks) to `0.477/0.571/0.735 us`. Work now touches a logarithmic number of tree nodes (the worker observed 17 visits at 500 blocks, 21 at 4,500, 25 at 20,000) instead of every retained block. Checksums are not claimed bit-identical: the 30k-run cumulative checksum changed from `-172566.76724169723` to `-172566.767241697`, reflecting last-bit aggregation differences accumulated over many readings. Individual results are checked against the separate two-pass reference within `1e-10 LU`; exact threshold equality and duplicate membership have explicit fixtures. Timings are single-machine measurements, not portable guarantees.
- Cumulative analyzer allocations dropped in the matched harness: 10 s Normal `18.46 MB -> 9.33 MB`, 10 s Eco `16.13 MB -> 7.00 MB`, 60 s Normal `37.32 MB -> 27.86 MB`, 60 s Eco `23.62 MB -> 14.15 MB`, 300 s Normal `127.81 MB -> 116.80 MB`, 300 s Eco `59.50 MB -> 48.48 MB`, and a channel-enabled three-source 10 s run `69.98 MB -> 42.59 MB` (Normal) / `52.78 MB -> 25.39 MB` (Eco). These are cumulative allocated bytes reported by the harness, not peak RAM or retained memory.

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
