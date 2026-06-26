---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-04-source-list-workspace-controls
last_verified_at: 2026-06-26
last_verified_by: opsx-codemap
verification_basis: archive
applies_to:
  - src/DesktopLoudnessMeter.App/ViewModels
  - src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs
  - src/DesktopLoudnessMeter.App/MainWindow.xaml
superseded_by:
---

# Source List Workspace

## Responsibility
Maintains the global source row collection, per-source workspace state, row commands, detail selection, recent events, and inline analyzer settings. It bridges live `SignalSourceSnapshot` refreshes with persisted user preferences keyed by stable source keys.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs | Reconciles source snapshots, applies hide/show filters, persists ordering, saves per-source analysis settings, and drives detail state |
| src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs | Row-level display state, alias editing commands, hide/restore and move command events, graph retention behavior |
| src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs | Serializable workspace storage for aliases, hidden state, order indices, and detail analysis preferences |
| src/DesktopLoudnessMeter.App/MainWindow.xaml | Global source-row UI, title editing, hide/restore buttons, move buttons, drag/drop hints, and show-hidden toggle |
| tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelAdvancedControlsTests.cs | Preference persistence, hide/show, ordering, collision options, and detail behavior coverage |

## Implicit Constraints
- Source identity must be stable by source key; display name changes cannot be used as persistence identity.
- Hidden rows remain in workspace preferences while being filtered from `Sessions` unless `ShowHiddenSources` is enabled.
- Explicit user order wins over kind/name sorting, while unordered new sources still fall back to kind, display title, and key.
