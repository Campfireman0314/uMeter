# Signal Source Foundation Design

## Overview

This change renames and reshapes the app's public capture-facing model from "audio sessions" to "signal sources" while keeping the current WASAPI implementation intact. The central decision is to introduce source-oriented contracts at the app/view-model boundary first, then let WASAPI remain the initial source provider behind those contracts.

## Key Decisions

- The analyzer metric payload stays unchanged in this slice. The refactor only changes source identity and provider naming. This satisfies R1 without risking DSP regressions.
- WASAPI remains the only concrete provider in this slice. It receives source-oriented interface names but keeps its duplicate collapse, self-filter, silent-filter, and process-loopback merge behavior. This satisfies R2 and R6.
- Source identity contains two layers: a stable `SourceKey` used by the app and a provider-specific process id for WASAPI process-loopback routing. Future network/plugin providers can add stable handles without pretending to be processes. This satisfies R3 and R4.
- UI labels are deliberately small badges/text fields in the existing layouts. The goal is clear source type marking, not a full source-management redesign. This satisfies R5.

## Data Model

- `SignalSourceKind`: enum for source type. Initial values are `WasapiSession`, `LocalLoopback`, `Plugin`, and `Unknown`, though only WASAPI is active in this slice.
- `SignalSourceSnapshot`: replacement/compatibility successor for `AudioSessionSnapshot`. It carries:
  - `SourceKey`
  - `SourceKind`
  - `SourceKindLabel`
  - `ProcessId`
  - `DisplayName`
  - `Peak`, `PeakDb`, `PeakDbText`
  - `State`
  - existing analyzer metric fields

## Interfaces

- `ISignalSourceMeterService`: returns current source snapshots. [U1]
- `ISignalSourceResetService`: resets one source or all sources. [U1]
- `ISignalSourceAnalysisSettingsService`: applies true-peak, waveform, and spectrum analysis settings to one source. [U1]
- `WasapiSignalSourceMeterService`: concrete WASAPI implementation. It may preserve a compatibility wrapper name temporarily if that keeps the change low-risk. [U2]

## Implementation Units

### U1: Source Contracts
Module Boundary:
- `src/DesktopLoudnessMeter.App/Audio/SignalSourceSnapshot.cs`
- `src/DesktopLoudnessMeter.App/Audio/SignalSourceKind.cs`
- `src/DesktopLoudnessMeter.App/Audio/ISignalSourceMeterService.cs`
- Compatibility aliases or wrappers in the previous audio-session files if needed.

Responsibilities:
- Define source identity and metric payload.
- Provide source-oriented reset/settings interfaces.
- Keep construction concise for tests.

### U2: WASAPI Provider Adapter
Module Boundary:
- `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs`
- Optional new `src/DesktopLoudnessMeter.App/Audio/WasapiSignalSourceMeterService.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/WasapiSessionMeterServiceTests.cs`

Responsibilities:
- Return `SignalSourceSnapshot` values.
- Preserve existing duplicate-process collapse and self/silent filtering.
- Preserve process-loopback merge and settings delegation.

### U3: View Model Source Identity
Module Boundary:
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
- Optional new source-oriented view-model naming aliases.
- `src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs`
- `src/DesktopLoudnessMeter.App/ViewModels/DetailWindowViewModel.cs`
- View-model tests under `tests/DesktopLoudnessMeter.Tests/ViewModels/`

Responsibilities:
- Use `SourceKey`, `SourceKind`, and source labels internally.
- Keep existing commands and data binding stable where possible.
- Preserve missing-handle retention and detail reattachment behavior.

### U4: UI Source Markers
Module Boundary:
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml`
- `src/DesktopLoudnessMeter.App/App.xaml.cs`

Responsibilities:
- Add compact source-type labels to global rows and detail headers.
- Include source type in tray pop-up item text.

## Traceability

- R1: U1, U3
- R2: U2
- R3: U1, U2, U3
- R4: U3
- R5: U4
- R6: U1, U2

## Risks

- Renaming every class in one pass could create churn with little value. The implementation may keep compatibility type names where external behavior is stable, while adding source-oriented names and properties for the next slices.
- XAML bindings are sensitive to property renames. Source-oriented aliases should be introduced before old binding names are removed.
- WASAPI process id is not a universal source identity. The design keeps it provider-specific so plugin/network sources can use true handles later.
