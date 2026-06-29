# Design

## Overview

Keep the loudness clock as one optional detail visualization, but change its internal model from list-index angle mapping to elapsed cycle phase. The control will store bounded timestamped samples for the most recent configured cycle and draw filled fan segments on a fixed circular table. View models will persist new per-source clock settings for cycle duration and warning loudness.

## Requirement Tracking

| Requirement | Implementation Unit | Status |
|-------------|---------------------|--------|
| R1 | U1 | planned |
| R2 | U2 | planned |
| R3 | U1 | planned |
| R4 | U2 | planned |
| R5 | U1 | planned |
| R6 | U1 | planned |
| R7 | U2 | planned |

## Implementation Units

### U1: Stable clock plotting and fan rendering
Module-boundary:
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs`
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockScalePlanner.cs`
- `src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs`
- `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockScalePlannerTests.cs`

Use elapsed seconds modulo cycle duration to map sample angles. Keep a bounded sample history, trim samples older than the configured cycle, and split fan geometry at cycle wrap and below-minimum gaps. Draw at least five scale rings from planner output. Fill normal fan areas with the signal color and warning fan areas with the warm spectrum color while retaining a subtle trace outline.

Traces: R1, R3, R5, R6

### U2: Clock setting persistence and UI controls
Module-boundary:
- `src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs`

Add per-source `LoudnessClockCycleSeconds` and `LoudnessClockWarningDb` settings. Clamp cycle duration to 10..600 seconds and default to 30 seconds. Clamp warning loudness to the active min/max range and keep it valid when min/max changes. Bind cycle and warning controls in both inline and pop-up clock settings, and pass the values into `LoudnessClockView`.

Traces: R2, R4, R7

## Verification

- Mapper tests cover stable elapsed-cycle angle wrapping.
- Scale planner tests cover five markers and range endpoints.
- View-model tests cover inline and pop-up persistence and clamping of cycle/warning values.
- Build validates new WPF bindings.
- Manual visual validation should confirm the filled fan, warning color, and compact/wide layout.
