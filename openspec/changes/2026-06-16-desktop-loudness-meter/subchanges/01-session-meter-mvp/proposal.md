# 01-session-meter-mvp

## Goal
Build a fast Windows desktop MVP that floats above other apps and displays individual app/audio-session activity from the default render device.

## In Scope
- WPF/.NET desktop application shell.
- Always-on-top compact overlay window.
- WASAPI session enumeration for the default render endpoint.
- Per-session labels resolved from process IDs when available.
- Live meter values derived from WASAPI session peak data.
- Clear UX language that the MVP shows session peak/level, not calibrated LUFS.
- Lightweight architecture notes and validation records.

## Out of Scope
- True integrated LUFS, short-term LUFS, momentary LUFS, or EBU R128 compliance.
- Process-loopback PCM capture and per-process DSP.
- Multi-device routing, tray packaging, installer, signing, and auto-start.
- Driver/kernel audio development.

## Depends On
- Windows Core Audio / WASAPI availability.
- A .NET Windows Desktop SDK/runtime on the development machine.
- NAudio or equivalent managed Core Audio interop.

## Done Means
- The solution builds locally.
- The app opens as a borderless/topmost floating window.
- Active render sessions are listed with process-friendly names.
- Meter bars update continuously from session peak readings.
- The repository contains design notes explaining the MVP limits and next-phase path.

