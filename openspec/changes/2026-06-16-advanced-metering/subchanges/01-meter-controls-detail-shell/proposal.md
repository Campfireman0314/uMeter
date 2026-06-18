# 01-meter-controls-detail-shell

## Goal
Add the advanced metering control surface: default peak/LUFS display selection, true-peak upsample controls, per-session detail expansion, always-on-top toggle, and reset controls.

## In Scope
- Main-window controls for Peak/LUFS display mode.
- Main-window controls for true-peak visibility and 2x/4x/8x oversampling preference.
- A per-session inline details surface opened from each application row.
- Detail layout slots for short-term LUFS, integrated/long-term LUFS, true peak, 100 ms RMS, stereo image, and correlation.
- Marked dB-scale level meters, a semi-circle stereo fan, and a -1..+1 correlation arrow in the inline detail surface.
- Global and per-session reset commands that clear display counters and future analyzer state.
- Always-on-top toggle bound to the main window.
- View-model/data-model fields that can carry analyzer values when the DSP and capture slices land.

## Out of Scope
- Actual LUFS, true-peak oversampling, RMS, stereo image, or correlation DSP calculations.
- Windows process-loopback PCM capture.
- Persistence of settings across app restarts.
- Tray behavior or installer polish.

## Depends On
- `2026-06-16-desktop-loudness-meter/01-session-meter-mvp`.

## Done Means
- The app builds and launches with the new controls.
- Peak/LUFS default mode changes the primary value label without crashing when LUFS data is unavailable.
- Each session row can switch the main window into an inline detail panel with all requested metric slots.
- Always-on-top can be toggled from the UI.
- Global and detail reset commands are wired and test-covered at the view-model level.
