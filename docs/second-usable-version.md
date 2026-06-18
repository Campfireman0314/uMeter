# Second Usable Version

## Version

`v0.2.0` - pinned on 2026-06-16.

This checkpoint keeps the first usable app-level metering loop and adds the polish needed for multiple live detail panels. Detail views can now stay inline or pop out per app, each pop-up can manage its own topmost and stereo-image scale settings, and true-peak oversampling can be selected from the detail UI.

## What Works

- Inline detail view with Back navigation.
- Pop-up detail windows for individual app rows.
- Independent always-on-top state per pop-up.
- Independent stereo image scale settings per pop-up.
- Inline stereo scale settings for the main detail view.
- Stereo image auto-scale controls for range, lower limit, manual top, speed, and reset.
- Per-app true-peak oversampling selection with 2x, 4x, and 8x options.
- True-peak oversampling changes are applied to the active process-loopback analyzer worker.
- Process-loopback workers are retained briefly through short render-session handle dropouts.

## Verification Snapshot

The checkpoint was validated with:

```powershell
dotnet build .\desktop-loudness-meter.sln --no-restore
dotnet test .\desktop-loudness-meter.sln --no-restore
dotnet format .\desktop-loudness-meter.sln --verify-no-changes
```

At the checkpoint, the test suite contained 67 passing tests. A WPF smoke launch also stayed running for the probe window and was then terminated.

## Known Limits

- No packaged installer yet.
- Settings are not persisted yet.
- Multiple detail windows watching the same process share one analyzer stream, so the latest true-peak oversampling choice for that process wins.
- Process-loopback still depends on Windows support and target-session behavior.
- DSP behavior has synthetic coverage but is not yet certified against official loudness reference vectors.

## Next Practical Slice

Move into polish and persistence:

- Persist window placement, topmost state, display mode, true-peak options, and stereo scale settings.
- Add packaging or a publish profile for daily use.
- Add explicit unavailable/error state copy per row and per detail panel.
- Validate loudness and true-peak behavior against reference vectors.
