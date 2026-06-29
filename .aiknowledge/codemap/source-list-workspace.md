---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-04-source-list-workspace-controls
last_verified_at: 2026-06-27
last_verified_by: opsx-codemap
verification_basis: codemap-refresh
applies_to:
  - src/DesktopLoudnessMeter.App/ViewModels
  - src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs
  - src/DesktopLoudnessMeter.App/MainWindow.xaml
  - src/DesktopLoudnessMeter.App/DetailWindow.xaml
  - src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs
superseded_by:
---

# Source List Workspace

## Responsibility
Maintains the global source row collection, per-source workspace state, row commands, detail selection, recent events, inline analyzer settings, detail visualization preferences, and shared source-title editing across global, inline detail, and pop-up detail surfaces. It bridges live `SignalSourceSnapshot` refreshes with persisted user preferences keyed by stable source keys.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs | Reconciles source snapshots, applies hide/show filters, persists ordering, saves per-source analysis and inline detail visualization settings including loudness clock range/cycle/warning, and drives detail state |
| src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs | Row-level display state, alias editing commands, hide/restore and move command events, graph retention behavior, and loudness clock feed |
| src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs | Serializable workspace storage for aliases, hidden state, order indices, and detail analysis/visualization preferences including clock cycle/warning settings |
| src/DesktopLoudnessMeter.App/MainWindow.xaml | Global source-row UI, inline detail title editing, hide/restore buttons, move buttons, drag/drop hints, forget action, show-hidden toggle, and inline detail visualizations |
| src/DesktopLoudnessMeter.App/DetailWindow.xaml | Pop-up detail title editing, detail roll/clock controls, and display-only draggable chrome title |
| src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs | Pop-up detail title editing handlers and pop-up command/menu plumbing |
| tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs | Preference persistence, hide/show, ordering, collision options, and detail behavior coverage |
| tests/DesktopLoudnessMeter.Tests/ViewModels/DetailWindowViewModelTests.cs | Pop-up detail preference persistence and range control behavior coverage |

## Implicit Constraints
- Source identity must be stable by source key; display name changes cannot be used as persistence identity.
- Title editors must route through `AudioSessionMeterViewModel` alias edit commands so global rows, inline detail, and pop-up detail share commit/cancel/persistence behavior.
- Hidden rows remain in workspace preferences while being filtered from `Sessions` unless `ShowHiddenSources` is enabled.
- Explicit user order wins over kind/name sorting, while unordered new sources still fall back to kind, display title, and key.
- Inline and pop-up detail visualization visibility/range settings, including loudness clock range, warning level, and cycle duration, are persisted per stable source key and reset through the forget-source flow.
- Detail module ordering persists for all graph modules, but the collision analyzer uses its original compact fixed height; imported or saved collision module heights normalize back to that fixed value.
