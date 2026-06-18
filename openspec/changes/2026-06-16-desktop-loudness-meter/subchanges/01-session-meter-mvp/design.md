# Session Meter MVP Design

## Overview

The first delivery unit is a WPF/.NET desktop overlay that enumerates WASAPI render sessions and displays live peak-derived levels per session. It intentionally avoids claiming true loudness/LUFS because the MVP uses `IAudioMeterInformation` session peak values rather than captured PCM samples.

## Key Decisions

- Use WPF for a lightweight always-on-top utility window with native Windows desktop behavior.
- Use NAudio CoreAudio APIs as managed interop for `MMDeviceEnumerator`, session enumeration, session identity, and session peak values.
- Poll on a short UI-friendly cadence instead of building a custom audio driver or kernel component.
- Keep the audio service behind an interface so the later process-loopback/loudness slice can replace or augment the data source.
- Keep the initial app dependency-light: WPF, NAudio, and small in-repo MVVM helpers.

## Interfaces And Data Model

`IAudioSessionMeterService` returns immutable snapshots:

- `SessionKey`: stable identifier for a WASAPI session row.
- `ProcessId`: nullable process id.
- `DisplayName`: friendly process/session label.
- `Peak`: normalized `0..1` peak value.
- `PeakDb`: dBFS-derived display value.
- `State`: active/inactive/expired when available.

`MainViewModel` owns refresh timing, row reconciliation, smoothing, stale-session removal, and status text. The WPF window binds only to view-model state.

## Implementation Units

### [U1] WPF Overlay Shell
**Module Boundary**
- `src/DesktopLoudnessMeter.App/App.xaml`
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`

Creates the compact topmost borderless window, drag behavior, close button, and first-screen meter layout.

### [U2] WASAPI Session Meter Service
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/IAudioSessionMeterService.cs`
- `src/DesktopLoudnessMeter.App/Audio/AudioSessionSnapshot.cs`
- `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs`

Owns NAudio/Core Audio integration, default render endpoint lookup, session enumeration, process metadata lookup, peak-to-dB conversion, and failure isolation.

### [U3] Meter View Models
**Module Boundary**
- `src/DesktopLoudnessMeter.App/ViewModels/ObservableObject.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`

Owns refresh cadence, collection reconciliation, visual smoothing, status text, and cleanup/disposal.

### [U4] Knowledge Notes
**Module Boundary**
- `docs/audio-metering-notes.md`

Documents the MVP measurement semantics, known WASAPI limits, and next-slice process-loopback/LUFS path.

## Requirement Tracking

| Requirement | Implementation Unit | Notes |
|-------------|---------------------|-------|
| R1 | U1, U3 | Window behavior and initial data-bound meter screen. |
| R2 | U2, U3 | Default render endpoint session enumeration and row reconciliation. |
| R3 | U2, U3 | Process/session naming and fallback display. |
| R4 | U2, U3 | Peak values, dBFS conversion, and live refresh. |
| R5 | U2, U3 | Exception isolation, stale-row cleanup, and refresh recovery. |
| R6 | U1, U4 | UI status copy and repository documentation. |

## Validation Strategy

- Build the WPF solution with `dotnet build`.
- Unit-test pure dB conversion and session row reconciliation where practical.
- Manually launch the app while one or more apps play audio and confirm rows update.
- Manually confirm the window stays topmost and can be dragged.

## Open Risks

- WASAPI session peak metering is not the same as true per-app loudness; this is explicit in R6 and deferred to `02-process-loopback-loudness`.
- Some system or protected sessions may not expose process metadata.
- Sessions can represent an audio session rather than an exact user-facing app window, so labels may occasionally group process output.

