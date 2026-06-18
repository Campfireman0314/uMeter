# First Usable Version

## Version

`v0.1.0` - pinned on 2026-06-16.

This is the first version that is useful as a live desktop audio supervisor. It is still a developer-run app, but the core loop is in place: enumerate audible apps, keep a compact always-on-top window, show global level, and open a detail view with PCM-derived metrics where process-loopback capture succeeds.

## What Works

- WPF/.NET 8 floating desktop window.
- Always-on-top toggle.
- Audible app rows normalized from WASAPI render sessions.
- Duplicate session rows collapsed toward one app row.
- Self-meter and silent active sessions filtered from the app list.
- Peak/LUFS primary display mode.
- Inline detail view with Back navigation.
- Momentary, short-term, and integrated loudness values.
- Recent-window true peak.
- 100 ms RMS.
- Polar stereo fan sample dots.
- Correlation arrow from `-1` to `+1`.
- Global and per-detail reset actions wired into analyzer workers.
- Detail view follows quick session-handle release/recovery by display name.
- Crash/startup logging under `%LOCALAPPDATA%\DesktopLoudnessMeter`.

## Verification Snapshot

The checkpoint was validated with:

```powershell
dotnet build .\desktop-loudness-meter.sln --no-restore
dotnet test .\desktop-loudness-meter.sln --no-restore
dotnet format .\desktop-loudness-meter.sln --verify-no-changes
```

At the checkpoint, the test suite contained 55 passing tests.

Live NAudio probes also confirmed:

- Process-loopback metrics populate for an external render process.
- The meter process is not shown as an app row.
- Recent true peak falls when the target signal drops from loud to quiet.
- Polar stereo points remain populated during live audio.

## Known Limits

- No packaged installer yet.
- Settings are not persisted yet.
- Process-loopback requires supported Windows builds and can fail for protected or stale sessions.
- Loudness and true-peak behavior is standards-inspired, not yet certified against official reference vectors.
- App identity recovery is heuristic: it uses stable row keys first and display-name recovery for short handle dropouts.

## Next Practical Slice

Move into polish and persistence:

- Persist window position, topmost state, display mode, and true-peak options.
- Add clearer unavailable/error state copy per row.
- Add packaging or a publish profile for daily use.
- Validate DSP behavior against reference vectors.
- Improve app identity beyond display-name recovery where Windows exposes stable metadata.
