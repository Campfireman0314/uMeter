# 03-packaging-polish

## Goal
Make the loudness meter comfortable as a daily desktop utility.

## In Scope
- Tray behavior, minimize/restore, and close semantics.
- Saved window position, opacity, compact/expanded modes, and refresh settings.
- Multi-monitor positioning sanity.
- Publish profile for self-contained or framework-dependent Windows builds.
- Startup shortcut guidance and distribution notes.

## Out of Scope
- New audio capture algorithms.
- Kernel driver or virtual audio cable distribution.
- Cloud sync or account features.

## Depends On
- `01-session-meter-mvp` for the basic app.
- `02-process-loopback-loudness` only for packaging labels/settings that expose true loudness fields.

## Done Means
- The app can be packaged and launched like a normal Windows utility.
- User preferences persist across launches.
- Common desktop interactions do not disturb metering or window placement.

