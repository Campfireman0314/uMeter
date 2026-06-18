# Third Usable Version

## Version

`v0.3.0` - pinned on 2026-06-16.

This checkpoint promotes the app from "usable detail meter" to "usable desktop analyzer surface." The detail views now include separate waveform and spectrum panels, a better spectrum display pipeline, and a clearer waveform scale while preserving the tray behavior and multi-window workflow from v0.2.0.

## What Works

- Main window can hide to the status tray and restore from the tray menu.
- Tray menu supports app pop-up detail windows and exit.
- Main, detail, and settings windows use the app's borderless dark chrome.
- Buttons, combo boxes, checkboxes, and scrollbars use custom dark styling.
- App and tray icon assets are included.
- Inline and pop-up detail views expose separate `Waveform` and `Spectrum` toggles.
- Waveform and spectrum panels are hidden by default.
- Single waveform/spectrum panels can be resized with a small vertical resize handle.
- When both roll panels are visible, a splitter adjusts the waveform/spectrum height balance.
- Waveform display uses the selected true-peak oversampling ratio for its display buffer.
- Waveform rendering uses dBFS scale markings and a smoother curve that preserves plotted extrema.
- Spectrum display uses a horizontally rolling, Mel-scaled, FFT-derived 192-bin display.
- True-peak oversampling remains selectable per app from inline and pop-up details.

## DSP/Display Notes

- True-peak detection still owns the metering path: it reconstructs inter-sample points and reports a recent-window maximum peak.
- Waveform and spectrum display now own a separate oversampled mono display buffer, because rendering needs an ordered signal history rather than a maximum-only peak queue.
- The display buffer follows the same selected 2x, 4x, or 8x ratio as true peak.
- Loudness, RMS, stereo image, and correlation continue to use the original PCM analysis windows.
- Spectrum is now FFT-based, then resampled onto Mel-spaced display bins for smoother visual frequency detail.

## Verification Snapshot

The checkpoint was validated with:

```powershell
dotnet test .\desktop-loudness-meter.sln
dotnet format .\desktop-loudness-meter.sln --verify-no-changes
```

At the checkpoint, the test suite contained 73 passing tests. A WPF smoke launch also stayed running for the probe window and was then terminated.

## Known Limits

- No packaged installer yet.
- Settings are not persisted yet.
- Multiple detail windows watching the same process share one analyzer stream, so the latest true-peak oversampling choice for that process wins.
- The true-peak and display oversampling paths duplicate reconstruction logic and should be revisited carefully during performance cleanup.
- Spectrum and waveform display are visually improved but not yet allocation/CPU-profiled.
- DSP behavior has synthetic coverage but is not yet certified against official loudness/reference vectors.

## Next Practical Slice

Move into performance polish:

- Profile analyzer snapshot cost and WPF render allocations.
- Reduce repeated `ToArray`/collection-copy churn in waveform and spectrum paths.
- Reuse FFT buffers and display history storage where possible.
- Cache drawing resources and keep controls responsive at the current 33 ms refresh cadence.
