# 01-source-continuity-reconfiguration

## Goal
Keep source identity, row state, detail views, and analysis buffers coherent when audio infrastructure changes without the source/app actually ending.

## In Scope
- Fix source continuity when a DAW/plugin sender changes sample rate or channel shape.
- Fix source continuity when the default WASAPI render device changes.
- Distinguish true source close/exit from temporary handle loss, endpoint reconfiguration, heartbeat gaps, and process-loopback worker recreation.
- Preserve or intentionally reset analyzer state across reconfiguration according to explicit rules.
- Prevent spectrograph black slits caused by transient handle loss or analyzer worker replacement.
- Add focused tests for source reattachment, hibernated row retention, and no-empty-frame rendering during temporary outages.

## Out of Scope
- Changing spectrograph FFT/window/bucket math except where required to avoid transient empty columns.
- User-facing hide/reorder controls beyond exposing source status semantics needed by later slices.
- Installer/version bump work.
- Official loudness or spectrum calibration claims.

## Depends On
- Existing mixed-source foundation and workspace settings.
- Existing process-loopback stale-worker retention and loopback sender register/heartbeat behavior.

## Done Means
- Reconfiguring a DAW sender with the same stable handle updates sample rate/channel metadata without losing user-visible source identity.
- Changing the Windows output device does not permanently orphan live WASAPI app rows or detail views.
- Temporary handle/worker loss displays a retained source state and does not append blank spectrograph columns.
- True process/app exit and plugin close still remove or mark sources according to existing lifecycle expectations.
