# Meter Controls And Detail Shell Design

## Overview

This slice adds the UI and view-model surface for advanced metering while preserving the current WASAPI session-peak MVP. LUFS, true peak, RMS, stereo image, and correlation are represented as optional metric state so later DSP and process-loopback slices can fill them without reworking the shell.

## Key Decisions

- Keep Peak as the default metric because the existing data source is still session peak.
- LUFS mode displays an explicit unavailable marker until the DSP/capture slices provide analyzer values.
- Use an inline single-window detail mode rather than a separate pop-up window.
- Keep reset commands in view models so the future analyzer can subscribe to reset counters or replace them with reset messages.
- Bind always-on-top through the main view model and a small window code-behind hook.

## Implementation Units

### [U1] Advanced Metric Models And Options
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/AudioSessionSnapshot.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/MeterDisplayMode.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/TruePeakOversampling.cs`

Adds optional analyzer metric fields and enum-like option types for Peak/LUFS and 2x/4x/8x true-peak oversampling.

### [U2] Main And Row View-Model Controls
**Module Boundary**
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/RelayCommand.cs`

Owns selected metric mode, true-peak settings, topmost state, global reset command, per-row reset command, inline detail selection, back command, and smoothed display levels.

### [U3] Main Window Control Surface
**Module Boundary**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`

Adds compact controls for metric mode, true-peak toggle/rate, always-on-top, reset all, and per-row detail buttons.

### [U4] Inline Detail Surface
**Module Boundary**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/Controls/ScaledLevelMeter.cs`
- `src/DesktopLoudnessMeter.App/Controls/StereoFanView.cs`
- `src/DesktopLoudnessMeter.App/Controls/CorrelationMeter.cs`

Displays the requested per-session metrics inside the main window with marked dB-scale meters, a semi-circle stereo fan, a -1..+1 correlation arrow, a per-session reset action, and a top-left return button.

## Requirement Tracking

| Requirement | Implementation Unit | Notes |
|-------------|---------------------|-------|
| R1 | U1, U2, U3 | Metric mode enum and row primary value binding. |
| R2 | U2, U3 | True-peak toggle and oversampling selector. |
| R3 | U2, U3, U4 | Row detail command switches to inline session detail mode and return goes back to global view. |
| R4 | U2, U3 | Topmost state flows from main view model to window. |
| R5 | U2, U3, U4 | Global and per-session reset counters/commands. |
| R6 | U1, U2, U4 | Optional analyzer fields and unavailable placeholders. |
| R7 | U2, U4 | Marked dB-scale level meters, semi-circle stereo fan, and -1..+1 correlation arrow in detail mode. |
| R8 | U2, U3 | Faster refresh cadence and smoother display interpolation. |

## Validation Strategy

- Unit-test metric-mode row display behavior.
- Unit-test reset counters and true-peak option state.
- Unit-test detail-open request flow without constructing a WPF window.
- Unit-test detail meter scale mapping and stereo/correlation values exposed to custom controls.
- Build and smoke-launch the app to verify the shell still starts.

## Open Risks

- Detail metrics are placeholders until `02-dsp-metric-engine` and `03-process-loopback-integration` provide PCM-derived data.
- This slice intentionally avoids persistence; `04-polish-persistence` owns restart behavior.
