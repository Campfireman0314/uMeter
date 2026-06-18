# Source Management Polish Design

## Overview

This slice polishes the mixed-source experience without adding another transport or plugin package. The app already has generic source contracts, a loopback receiver, and a sender probe. This pass makes the source list deterministic, clarifies duplicate names, carries source status into global/detail/tray surfaces, and remembers per-source analysis settings across reconnect while the app is running.

## Key Decisions

- Keep provider display names immutable in `DisplayName`. Add display-only title/identity text for UI disambiguation.
- Sort rows in `MainViewModel` after reconciliation rather than relying on provider order. This keeps composite providers simple and makes mixed WASAPI/plugin rows predictable.
- Use source-key memory for analysis preferences. This covers plugin/loopback reconnect and WASAPI key stability without introducing a full alias/visibility settings UI.
- Defer persistent alias and hidden-source management to a later explicit UI slice. This pass documents the decision so there is one clear owner.

## Implementation Units

### U1: Row Identity Presentation
Module Boundary:
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml`
- `src/DesktopLoudnessMeter.App/App.xaml.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs`

Responsibilities:
- Add display-only row title, source identity, and status text.
- Mark duplicate display-name rows with a compact source qualifier.
- Use disambiguated labels in tray and detail headers.

### U2: Source Ordering And Preference Memory
Module Boundary:
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs`

Responsibilities:
- Sort visible rows by source kind, display name, and source key.
- Remember per-source true-peak, waveform, and spectrum preferences by source key.
- Reapply remembered preferences when a source disappears and reconnects with the same source key.

### U3: Multi-Source Docs
Module Boundary:
- `README.md`
- `docs/source-management.md`
- `docs/current-status.md`

Responsibilities:
- Explain source types, duplicate names, stable handles, reconnect behavior, and deferred alias/visibility persistence.

## Traceability

- R1: U2
- R2: U1
- R3: U1
- R4: U2
- R5: U3

## Risks

- Display-name collision detection is only based on current visible rows. This is intentional because stale rows should not force permanent qualifiers after the collision is gone.
- In-memory preference memory does not solve restart persistence. That remains deferred until a dedicated settings store and alias/visibility UI are introduced.
