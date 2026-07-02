# WebView State Ownership Map

## Purpose

This map prevents split-brain state between React local state, the C# host snapshot, persisted workspace settings, and transient view concerns. Later slices must update this file when adding snapshot fields or commands.

## Ownership Kinds

| Ownership Kind | Meaning |
|----------------|---------|
| `host-owned` | Runtime state is produced by the app host, service, controller, or view model and projected into WebView. |
| `persisted-workspace-owned` | State is saved in `UserWorkspaceSettings` or another explicit settings store and projected by the host. |
| `WebView-derived` | State is derived in the WebView from host data without becoming authoritative. |
| `transient-view-owned` | Short-lived UI state can live in WebView because it is not a product promise and does not need persistence. |

## Snapshot Fields

| Field or Group | Owner | Storage or Producer | Notes |
|----------------|-------|---------------------|-------|
| `sources[].sourceKey` | `host-owned` | `SignalSourceSnapshot` / `MainViewModel` projection | Stable identity; never derive from display title. |
| `sources[].sourceKind` | `host-owned` | source service snapshot | Required for command payloads and sorting. |
| `sources[].processId` | `host-owned` | WASAPI/process snapshot | Optional identity companion. |
| `sources[].displayName` | `host-owned` | provider-owned source metadata | Provider display name remains immutable. |
| `sources[].alias` | `persisted-workspace-owned` | `UserWorkspaceSettings.SourcePreferences` | Alias command must write here through host. |
| `sources[].effectiveDisplayName` | `WebView-derived` or host projection | host should prefer projecting final title | Must not become independent React state. |
| `sources[].displayTitle` | `WebView-derived` or host projection | duplicate-name/title planner | May be derived, but source key remains identity. |
| `sources[].isHidden` | `persisted-workspace-owned` | `UserWorkspaceSettings.SourcePreferences` | Show/hide commands write persisted preference. |
| `sources[].orderIndex` | `persisted-workspace-owned` | `UserWorkspaceSettings.SourcePreferences` | Explicit user order wins over provider order. |
| `sources[].isInstanceMissing` | `host-owned` | source service / row reconciliation | Runtime status only. |
| `sources[].peak*` | `host-owned` | analyzer snapshot | Metering output. |
| `sources[].momentaryLufs` | `host-owned` | analyzer snapshot | Metering output. |
| `sources[].shortTermLufs` | `host-owned` | analyzer snapshot | Metering output. |
| `sources[].integratedLufs` | `host-owned` | analyzer snapshot | Metering output. |
| `sources[].truePeakDb` | `host-owned` | analyzer snapshot | Native/managed boundary must be explicit. |
| `sources[].waveformSamples` | `host-owned` | analyzer snapshot / render model | WebView can downsample visually but cannot own source truth. |
| `sources[].spectrumBuckets` | `host-owned` | analyzer snapshot / render model | Preserve main spectrum semantics. |
| `sources[].stereoImage*` | `host-owned` | analyzer snapshot | Detail rendering input. |
| `sources[].truePeakOversampling` | `persisted-workspace-owned` | per-source analysis settings | Command must write host/workspace state. |
| `sources[].waveformDisplayMode` | `persisted-workspace-owned` | per-source analysis settings | Command must write host/workspace state. |
| `sources[].spectrumDisplayMode` | `persisted-workspace-owned` | per-source analysis settings | Command must write host/workspace state. |
| `selectedDetail` | `host-owned` | `MainViewModel` / host controller | WebView navigation follows host selection. |
| `selectedDetail.modules[]` | `persisted-workspace-owned` | per-source detail preferences | Visibility, order, and height remain per-source preferences. |
| `selectedDetail.collision` | `host-owned` | collision analyzer plus settings | Analysis result is host-owned; controls write settings. |
| `settings.themeId` | `persisted-workspace-owned` | application settings / theme manager | Must apply native dialogs and shell, not only CSS. |
| `settings.showHiddenSources` | `persisted-workspace-owned` | workspace settings | Drives source filtering. |
| `settings.maxRecentEventEntries` | `persisted-workspace-owned` | global settings | Host owns retention. |
| `settings.eventOutputFolder` | `persisted-workspace-owned` | global settings | Host owns persistence/output. |
| `settings.sources[]` | `persisted-workspace-owned` | `UserWorkspaceSettings` | Source preferences projection. |
| `recentEvents` | `host-owned` | one recent-event provider | Must not be simultaneously owned by host snapshot and WebView state. |
| `statusText` | `host-owned` | host/view-model projection | Uses source-facing language. |
| `commands` | `host-owned` | host command availability planner | WebView buttons consume availability. |
| hover/focus/edit draft text | `transient-view-owned` | WebView component state | Allowed only until commit/cancel. |

## Local State Rule

React local state may hold transient edit drafts, hover/focus state, and in-progress UI gestures. React local state must not be the durable owner for display mode, true-peak enablement, eco/performance mode, always-on-top, theme, source alias, source visibility, source ordering, selected detail, detail module preferences, or recent events.
