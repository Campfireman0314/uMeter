---
status: active
created_at: 2026-06-29
created_from: change:2026-06-29-host-snapshot-command-contract
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: change:2026-07-01-host-snapshot-assembly-boundary
applies_to:
  - src/DesktopLoudnessMeter.App/Host
  - src/DesktopLoudnessMeter.App/WebView
  - src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs
  - tests/DesktopLoudnessMeter.Tests/Host
  - tests/DesktopLoudnessMeter.Tests/WebView
superseded_by:
source_refs:
  - change:2026-06-29-host-snapshot-command-contract
  - change:2026-06-29-ui-runtime-architecture-replan
  - change:2026-06-29-webview-windows-app-shell
  - evidence:openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/evidence.md
  - evidence:openspec/changes/archive/2026-06-29-webview-windows-app-shell/evidence.md
  - change:2026-06-30-webview-chart-parity
  - evidence:openspec/changes/archive/2026-06-30-webview-chart-parity/evidence.md
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

# Host Contract

## Responsibility
Provides the UI-neutral host snapshot and command boundary used to separate source/workspace/chart state from WPF binding shape and WebView HostBridge v1 payloads. `DesktopMeterHostController` consumes `ISignalSourceMeterService` snapshots and `UserWorkspaceSettings`, delegates source-list projection metadata to native `DlmHostSourceProjection`, delegates Host/Web source appeared recent-event lifecycle/history to native `DlmHostRecentEvents`, delegates aggregate snapshot planning to native `DlmHostAggregateSnapshot`, delegates selected-detail snapshot planning to native `DlmHostSelectedDetailSnapshot`, delegates settings snapshot and command availability projection to native `DlmHostSnapshotChromeProjection`, delegates source-order command planning to native `DlmHostSourceOrderCommand`, delegates alias/hide/restore/forget source preference command planning to native `DlmHostSourcePreferenceCommand`, delegates theme/recent-limit/event-output-folder/show-hidden global settings command planning to native `DlmHostGlobalSettingsCommand`, delegates source/detail/chart settings value planning to native `DlmHostChartSettingsCommand`, delegates reset-all/reset-source/performance-mode runtime service command planning to native `DlmHostRuntimeServiceCommand`, delegates select/back/reconcile/hidden selected-source state planning to native `DlmHostSelectedSourceCommand`, stores selected source/show-hidden/performance runtime state in native `DlmHostRuntimeState`, delegates source-scoped chart/detail command target existence guarding to native `DlmHostSourceTargetCommand`, delegates workspace-backed command effect application and persistence to `HostWorkspaceCommandEffectApplier`, delegates runtime service effect application to `HostRuntimeServiceEffectApplier`, delegates snapshot DTO assembly to `HostSnapshotAssembler`, executes typed host commands, and returns `HostCommandResult` values with a post-command snapshot or typed error.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs | Host state controller, source polling, native-backed runtime state integration, native-backed source-list projection integration, native-backed Host/Web source appeared recent-event integration, native-backed command planning integration, command execution, workspace effect boundary orchestration, runtime service effect boundary orchestration, snapshot assembly boundary orchestration, and snapshot rebuild triggers |
| src/DesktopLoudnessMeter.App/Host/HostWorkspaceCommandEffectApplier.cs | Host glue boundary that applies native workspace-backed command plans to `UserWorkspaceSettings` and preserves per-command persistence semantics |
| src/DesktopLoudnessMeter.App/Host/HostRuntimeServiceEffectApplier.cs | Host glue boundary that applies native runtime/chart service plans to optional reset and analysis settings service interfaces |
| src/DesktopLoudnessMeter.App/Host/HostSnapshotAssembler.cs | Host glue boundary that assembles `HostSnapshot`, selected-detail DTOs, settings snapshot, command availability, status text, and recent events from native planners and current Host state |
| src/DesktopLoudnessMeter.App/Host/HostSnapshot.cs | Aggregate host snapshot, selected detail shell, chart module DTOs, frequency collision DTO, recent event DTO, and command availability DTO |
| src/DesktopLoudnessMeter.App/Host/HostSourceSnapshot.cs | UI-neutral source snapshot DTO; preserves public fields while merging native source-list projection metadata with `SignalSourceSnapshot` metrics |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceProjection.cs | Managed P/Invoke wrapper for native source-list projection metadata: filtering, ordering, duplicate titles, workspace row defaults, and selected visibility |
| src/DesktopLoudnessMeter.Native/DlmHostSourceProjection.* | Native source-list projection core over generic source metadata and workspace row state |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostRecentEvents.cs | Managed P/Invoke wrapper for native Host/Web source appeared recent-event lifecycle and bounded history |
| src/DesktopLoudnessMeter.Native/DlmHostRecentEvents.* | Native recent-event core that tracks unfiltered live source keys, visible source display titles, newest-first history, and Host-provided limits |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostAggregateSnapshotPlanner.cs | Managed P/Invoke wrapper for aggregate snapshot planning consumed by `DesktopMeterHostController.BuildSnapshot` |
| src/DesktopLoudnessMeter.Native/DlmHostAggregateSnapshot.* | Native aggregate snapshot planner for selected source index, selected detail inclusion, and normal source-count status text |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostDetailModuleProjection.cs | Managed P/Invoke wrapper for selected-detail module layout/title/visibility/height projection rows |
| src/DesktopLoudnessMeter.Native/DlmHostDetailModuleProjection.* | Native detail module projection core: default order, stable order-index normalization, duplicate/invalid filtering, titles, visibility defaults, and height normalization |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSnapshotChromeProjection.cs | Managed P/Invoke wrapper for native settings snapshot and command availability projection |
| src/DesktopLoudnessMeter.Native/DlmHostSnapshotChromeProjection.* | Native snapshot chrome projection core: global settings, ordered per-source settings rows, nullable field preservation, and selected-source command availability |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostCollisionProjection.cs | Managed P/Invoke wrapper for native selected-detail collision band projection |
| src/DesktopLoudnessMeter.Native/DlmHostCollisionProjection.* | Native collision projection core: focus/comparison spectrum analysis, prominence/relative threshold evaluation, Mel/log scale mapping, and compact band output |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedDetailSnapshotPlanner.cs | Managed P/Invoke wrapper returning the native selected-detail snapshot plan consumed by `DesktopMeterHostController.BuildDetailSnapshot` |
| src/DesktopLoudnessMeter.Native/DlmHostSelectedDetailSnapshot.* | Native selected-detail snapshot core that composes module rows, shell visibility/comparison metadata, collision setting defaults, comparison spectrum selection, and collision bands |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceOrderCommandPlanner.cs | Managed P/Invoke wrapper for native source-order command planning |
| src/DesktopLoudnessMeter.Native/DlmHostSourceOrderCommand.* | Native source-order command core: move up/down, move-to-index clamp, missing-source status, and contiguous order-row output |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourcePreferenceCommandPlanner.cs | Managed P/Invoke wrapper for native source preference command planning |
| src/DesktopLoudnessMeter.Native/DlmHostSourcePreferenceCommand.* | Native source preference command core: alias normalization, hide/restore target state, forget plan, missing-source status, and selected-source clearing flag |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostGlobalSettingsCommandPlanner.cs | Managed P/Invoke wrapper for native global settings command planning |
| src/DesktopLoudnessMeter.Native/DlmHostGlobalSettingsCommand.* | Native global settings command core: theme id normalization, recent event limit clamp, event output folder normalization, and show-hidden target state |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostChartSettingsCommandPlanner.cs | Managed P/Invoke wrapper for native source/detail/chart settings command planning |
| src/DesktopLoudnessMeter.Native/DlmHostChartSettingsCommand.* | Native chart settings command core: true peak forwarding value, waveform/spectrum channel-enabled flags, detail module visibility target, range/loudness/collision value plans, and collision comparison source key normalization |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostRuntimeServiceCommandPlanner.cs | Managed P/Invoke wrapper for native runtime service command planning |
| src/DesktopLoudnessMeter.Native/DlmHostRuntimeServiceCommand.* | Native runtime service command core: reset-all target, reset-source missing-source status/target identity, and Host-to-analysis performance mode mapping |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostRuntimeState.cs | Managed P/Invoke wrapper for native selected source, show-hidden, and Host performance runtime state storage |
| src/DesktopLoudnessMeter.Native/DlmHostRuntimeState.* | Native runtime state handle: selected source storage/copy, show-hidden state, Host performance mode state, and planner-result application helpers |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedDetailShellPlanner.cs | Managed P/Invoke wrapper for native selected-detail shell planning |
| src/DesktopLoudnessMeter.Native/DlmHostSelectedDetailShell.* | Native selected-detail shell core: selected source index resolution, module visibility summary, collision comparison source index planning, comparison display metadata, and collision setting defaults |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedSourceCommandPlanner.cs | Managed P/Invoke wrapper for native selected-source command/state planning |
| src/DesktopLoudnessMeter.Native/DlmHostSelectedSourceCommand.* | Native selected-source command core: select-detail missing-source status, back-to-global clear plan, refresh reconciliation cleanup, and hidden selected-source cleanup |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSourceTargetCommandPlanner.cs | Managed P/Invoke wrapper for native source-target command planning |
| src/DesktopLoudnessMeter.Native/DlmHostSourceTargetCommand.* | Native source-target command core: exact source-key target existence guard and missing-source status for source-scoped chart/detail commands |
| src/DesktopLoudnessMeter.App/Host/HostSettingsSnapshot.cs | Read-only DTO shape for versioned `UserWorkspaceSettings` fields, including schema version; controller projection now flows through native wrapper while schema migration remains owned by settings |
| src/DesktopLoudnessMeter.App/Host/HostCommand.cs | Typed host command vocabulary for global, source-scoped workflow, and chart settings operations |
| src/DesktopLoudnessMeter.App/Host/HostCommandResult.cs | Success/error result wrapper and typed host error codes |
| src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs | WPF adapter that exposes existing bindings while routing major refresh/row/settings commands through the host controller |
| src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs | WebView HostBridge v1 command bus adapter over `DesktopMeterHostController`, including global settings commands and chart command payload validation |
| src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs | JSON DTO mapping boundary for host snapshots/results/chart payloads without WPF types |
| src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts | TypeScript consumer-facing DTO and command vocabulary, including Web settings command payload fields |
| tests/DesktopLoudnessMeter.Tests/Host | Contract, projection, controller, and command regression tests |
| tests/DesktopLoudnessMeter.Tests/WebView | HostBridge DTO/controller/asset locator regression tests |

## Implicit Constraints
- Host DTOs must not expose WPF-only primitives such as `ICommand`, `ObservableCollection`, `DispatcherTimer`, XAML controls, or `System.Windows` UI types.
- Source identity remains the stable source key. Display names and aliases are presentation fields and cannot become command or persistence identity.
- Mutating host commands rebuild their post-command snapshot from `LastSourceSnapshots` instead of polling capture/source services again.
- Recent source-appearance events must be based on live service snapshot keys, not the filtered visible source list; showing hidden live sources should not create false appearance events.
- `UserWorkspaceSettings` schema remains owned by the settings/workspace layer. Native-backed Host settings snapshots project the current schema version and chart settings for Web UI consumption but must not own migration, persistence, or workspace mutation during projection.
- Selected-detail snapshot planning is native-backed through `DlmHostSelectedDetailSnapshot`, which composes module row projection, selected source resolution, module visibility summary, comparison metadata/defaults, comparison spectrum selection, and numeric collision band projection. WPF local collision event workflows may reuse `FrequencyCollisionAnalyzer`, but Host/Web selected-detail snapshots should consume the unified native selected-detail snapshot plan.
- HostBridge chart setting commands must validate finite numeric payloads against the same usable ranges as settings/WPF UI before persisting them.
- HostBridge global settings commands currently expose `setTheme`, `setRecentEventLimit`, and `setEventOutputFolder`. Theme ids are validated and canonicalized at the bridge boundary before persistence so Web `data-theme` resolution and workspace state stay aligned.
- The UI runtime replan supersedes future Flutter consumption language: new UI work routes through WebView2/WKWebView HostBridge v1, with native WebView message transport and JSON DTO payloads. The Windows WebView shell covers snapshot/workflow commands plus selected-detail chart commands.
- Source-list projection metadata is native-backed for all source kinds. Host/Web source appeared recent-event history is also native-backed through `DlmHostRecentEvents`; WPF local recent events in `MainViewModel` are intentionally not migrated and still cover WPF-only source disappeared, true peak, loudness, collision, and filtering behavior.
- Aggregate snapshot planning is native-backed through `DlmHostAggregateSnapshot` for selected source index, selected detail inclusion, and normal source-count status text. Managed Host warning/failure/initial-wait status overrides remain owned by `DesktopMeterHostController`.
- Settings snapshot and command availability projection are native-backed through `DlmHostSnapshotChromeProjection`. Source-order command planning is native-backed through `DlmHostSourceOrderCommand`; source preference command planning is native-backed through `DlmHostSourcePreferenceCommand`; global settings command planning is native-backed through `DlmHostGlobalSettingsCommand`; chart/source settings value planning is native-backed through `DlmHostChartSettingsCommand`; runtime service command planning is native-backed through `DlmHostRuntimeServiceCommand`; selected-detail snapshot planning is native-backed through `DlmHostSelectedDetailSnapshot`; selected-source command/state planning is native-backed through `DlmHostSelectedSourceCommand`; selected source/show-hidden/performance runtime state storage is native-backed through `DlmHostRuntimeState`; source-target existence guarding for source-scoped chart/detail commands is native-backed through `DlmHostSourceTargetCommand`. `HostWorkspaceCommandEffectApplier` applies returned workspace-backed plans and persists workspace state where applicable. `HostRuntimeServiceEffectApplier` applies returned runtime/analyzer service plans to optional Host service interfaces. `HostSnapshotAssembler` maps native aggregate/chrome/selected-detail plans into Host DTOs and attaches recent-event state. Host reconciles recent events and rebuilds post-command snapshots through this boundary.
- Host still owns workspace schema migration/persistence, WebBridge payload validation, service invocation through Host glue boundaries, DTO assembly through a Host glue boundary, and native-wrapper orchestration. Many snapshot/command decisions have moved native, and workspace/runtime service effect plus snapshot assembly application is now isolated from `DesktopMeterHostController`, but Host still treats show-hidden as runtime state, persists managed workspace state through a Host glue boundary, orchestrates native wrappers, and does not yet qualify as glue-only.
