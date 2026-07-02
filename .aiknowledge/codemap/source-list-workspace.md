---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-04-source-list-workspace-controls
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: change:2026-07-01-host-snapshot-assembly-boundary
applies_to:
  - src/DesktopLoudnessMeter.App/ViewModels
  - src/DesktopLoudnessMeter.App/Host
  - src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs
  - src/DesktopLoudnessMeter.App/MainWindow.xaml
  - src/DesktopLoudnessMeter.App/DetailWindow.xaml
  - src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs
superseded_by:
source_refs:
  - change:2026-06-30-web-settings-workflow-parity
  - evidence:openspec/changes/archive/2026-06-30-web-settings-workflow-parity/evidence.md
  - change:2026-07-01-native-host-projection-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-projection-core/evidence.md
  - change:2026-07-01-native-host-command-and-state-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-command-and-state-core/evidence.md
  - change:2026-07-01-host-workspace-command-effects-boundary
  - change:2026-07-01-host-runtime-service-effects-boundary
  - change:2026-07-01-host-snapshot-assembly-boundary
---

# Source List Workspace

## Responsibility
Maintains the WPF global source row collection, versioned workspace/settings persistence, per-source workspace state, row commands, detail selection, WPF-local recent events, inline analyzer settings, detail visualization preferences, and shared source-title editing across global, inline detail, and pop-up detail surfaces. It now acts as a WPF adapter over the UI-neutral host contract while still preserving WPF-local binding and window glue. Source-list metadata projection, Host/Web source appeared recent history, source-order command planning, source preference command planning, global settings command planning, chart/source settings value planning, source-target command guarding, selected-detail shell planning, and selected-source command/state planning are native-backed through Host, but workspace persistence and WPF bindings remain managed.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs | Reconciles source snapshots, applies hide/show filters, persists ordering, saves per-source analysis and inline detail visualization settings including loudness clock range/cycle/warning, and drives detail state |
| src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs | UI-neutral host state controller used by `MainViewModel` for refresh, source commands, native-backed Host/Web source appeared recent history, native-backed source-order command planning, workspace/runtime service effect orchestration, snapshot assembly orchestration, and host snapshots |
| src/DesktopLoudnessMeter.App/Host/HostWorkspaceCommandEffectApplier.cs | Host glue boundary that applies native workspace-backed command plans to `UserWorkspaceSettings` for global workspace settings, source preference/order, and chart/detail settings |
| src/DesktopLoudnessMeter.App/Host/HostRuntimeServiceEffectApplier.cs | Host glue boundary that forwards native runtime/analyzer service plans to optional reset and analysis settings service interfaces |
| src/DesktopLoudnessMeter.App/Host/HostSnapshotAssembler.cs | Host glue boundary that assembles current source rows and workspace-backed detail settings into Host snapshots using native aggregate/chrome/detail planners |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceProjection.cs | Native-backed source-list projection wrapper used by Host for visible rows, ordering, duplicate title disambiguation, and selected-row validity |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceOrderCommandPlanner.cs | Native-backed source-order command planner wrapper used by Host move commands before persisting `OrderIndex` values |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourcePreferenceCommandPlanner.cs | Native-backed source preference command planner wrapper used by Host alias/hide/restore/forget commands before persisting source workspace rows |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostGlobalSettingsCommandPlanner.cs | Native-backed global settings command planner wrapper used by Host theme/recent-limit/event-folder/show-hidden commands before applying workspace/runtime state |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostChartSettingsCommandPlanner.cs | Native-backed chart/source settings command planner wrapper used by Host true peak, waveform/spectrum mode, detail visibility, range, loudness clock, and collision settings commands before applying workspace rows |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedDetailShellPlanner.cs | Native-backed selected-detail shell planner wrapper used by Host to resolve selected source index, module visibility summary, collision comparison sources, and collision defaults |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedSourceCommandPlanner.cs | Native-backed selected-source command/state planner wrapper used by Host select/back/reconcile/hide cleanup flows before applying results into native runtime state |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceTargetCommandPlanner.cs | Native-backed source-target command wrapper used by Host source-scoped chart/detail settings commands before mutating workspace rows or forwarding analyzer settings |
| src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs | Host aggregate snapshot consumed by `MainViewModel` and reserved for future WebView HostBridge v1 consumption |
| src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs | Row-level display state, alias editing commands, hide/restore and move command events, graph retention behavior, and loudness clock feed |
| src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs | Versioned serializable workspace storage for aliases, hidden state, order indices, theme/global settings, and detail analysis/visualization preferences including clock cycle/warning settings; owns legacy settings normalization |
| src/DesktopLoudnessMeter.App/MainWindow.xaml | Global source-row UI, inline detail title editing, hide/restore buttons, move buttons, drag/drop hints, forget action, show-hidden toggle, and inline detail visualizations |
| src/DesktopLoudnessMeter.App/DetailWindow.xaml | Pop-up detail title editing, detail roll/clock controls, and display-only draggable chrome title |
| src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs | Pop-up detail title editing handlers and pop-up command/menu plumbing |
| tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs | Preference persistence, hide/show, ordering, collision options, and detail behavior coverage |
| tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs | Pop-up detail preference persistence and range control behavior coverage |

## Implicit Constraints
- Source identity must be stable by source key; display name changes cannot be used as persistence identity.
- `UserWorkspaceSettings` owns workspace schema versioning and legacy no-version compatibility; HostBridge and Web UI only consume projected DTOs.
- Web global settings workflow edits `ThemeId`, `MaxRecentEventEntries`, `EventOutputFolder`, and `ShowHiddenSources` through the host contract; command planning is native-backed, workspace-backed persistence lands in `UserWorkspaceSettings` through `HostWorkspaceCommandEffectApplier`, show-hidden remains runtime Host state, and Web UI only consumes Host settings snapshots.
- Title editors must route through `AudioSessionMeterViewModel` alias edit commands so global rows, inline detail, and pop-up detail share commit/cancel/persistence behavior.
- Hidden rows remain in workspace preferences while being filtered from `Sessions` unless `ShowHiddenSources` is enabled.
- Host-level source lifecycle events are keyed from live source snapshots, not the visible WPF `Sessions` collection; visibility filtering must not be treated as source arrival.
- Host/Web source appeared recent history is native-backed through `DlmHostRecentEvents`, but WPF-local recent events remain in `MainViewModel` and still cover WPF-only source disappeared, true peak, loudness, collision, and filter behavior.
- Explicit user order wins over kind/name sorting, while unordered new sources still fall back to kind, display title, and key. Move command target planning now flows through native `DlmHostSourceOrderCommand`, but `UserWorkspaceSettings` remains the persistence owner for `OrderIndex`.
- Source-list projection rules now live in native `DlmHostSourceProjection`; source-order command planning now lives in native `DlmHostSourceOrderCommand`; alias/hide/restore/forget source preference command planning now lives in native `DlmHostSourcePreferenceCommand`; global settings command planning now lives in native `DlmHostGlobalSettingsCommand`; chart/source settings value planning now lives in native `DlmHostChartSettingsCommand`; selected-detail shell planning now lives in native `DlmHostSelectedDetailShell`; selected-source command/state planning now lives in native `DlmHostSelectedSourceCommand`; source-target guarding for source-scoped chart/detail commands now lives in native `DlmHostSourceTargetCommand`; workspace command effect application now lives in `HostWorkspaceCommandEffectApplier`; reset/performance/analyzer service effect forwarding now lives in `HostRuntimeServiceEffectApplier`. WPF/ViewModel code must treat the Host snapshot and Host command result as the source of truth and should not reimplement hidden filtering, fallback ordering, duplicate title disambiguation, source projection visibility metadata, selected-source select/back/reconcile/hide cleanup planning, move target planning, source preference missing-source handling, alias normalization, global settings normalization, chart setting value planning, source-target existence checks for chart/detail settings, collision comparison key normalization, selected-detail comparison fallback, collision defaults, recent limit clamping, workspace-backed command effect application, or runtime service effect forwarding.
- Inline and pop-up detail visualization visibility/range settings, including loudness clock range, warning level, and cycle duration, are persisted per stable source key and reset through the forget-source flow.
- Detail module ordering persists for all graph modules, but the collision analyzer uses its original compact fixed height; imported or saved collision module heights normalize back to that fixed value.
