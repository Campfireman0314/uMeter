# Design

## Overview

Add the loudness clock as an optional detail visualization beside the existing waveform, spectrum, and collision views. The graph will be a WPF `FrameworkElement` that owns a bounded in-control history of momentary loudness samples. `AudioSessionMeterViewModel` will expose the current raw momentary loudness and a dedicated clock plot tick so the control can append samples without depending on waveform or spectrum refresh cadence.

## Implementation Units

### U1: Loudness clock plotting model and control
Module-boundary:
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs`
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs`
- `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs`

Create a testable mapper for loudness-to-radius behavior and angle progression. Add a WPF control that appends finite momentary values on clock plot ticks, keeps a bounded history, renders reference circles, and draws only mapped samples at or above the configured minimum.

Traces: R2, R3, R4, R5

### U2: Source momentary clock feed
Module-boundary:
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs`

Expose raw momentary loudness for binding and a dedicated `LoudnessClockPlotTick`. Advance that tick only for finite momentary loudness on fresh plot frames. Reset clears the feed the same way other plot ticks reset.

Traces: R1

### U3: User settings and detail integration
Module-boundary:
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs`

Persist `InlineLoudnessClockVisible`, `PopupLoudnessClockVisible`, `LoudnessClockMinimumDb`, and `LoudnessClockMaximumDb` per source. Add view-model properties with clamped range behavior and range text. Add inline and pop-up checkboxes plus range sliders to existing detail roll settings, and place the clock panel as an optional detail visualization independent of waveform/spectrum layout.

Traces: R6, R7, R8, R9

## Verification

- Unit tests cover mapper omission/clamp/proportional mapping.
- View-model tests cover clock plot tick behavior and per-source persistence.
- Build validates WPF bindings and control integration.
- Manual visual validation remains recommended for the circular graph's layout and feel during live audio.
