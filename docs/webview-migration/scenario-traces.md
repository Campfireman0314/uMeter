# WebView Migration Scenario Traces

## Purpose

These traces define deterministic behavior checks for the clean WebView migration. They use fake sources rather than live audio devices, plugin binaries, or WebView2 automation.

Every trace records:

| Field | Meaning |
|-------|---------|
| Initial State | Sources, settings, selected detail, persisted preferences, and relevant runtime status before the action. |
| Action | User action or HostBridge command under test. |
| Expected Host State | Host-owned state after the action. |
| Expected Persisted State | Workspace or settings state after the action. |
| Expected WebView Projection | Snapshot fields or UI projection the WebView should render. |

## Fake Source Fixtures

| Fixture | Source Kind | Identity Rule |
|---------|-------------|---------------|
| WASAPI source | `wasapi` | `sourceKey` plus optional `processId`; display name can change without changing identity. |
| Local loopback source | `local-loopback` | stable loopback source key; no process id required. |
| Plugin source | `plugin` | stable plugin handle source key; reconnect keeps the same source key. |

## Source Workflow Traces

| Trace | Initial State | Action | Expected Host State | Expected Persisted State | Expected WebView Projection |
|-------|---------------|--------|---------------------|--------------------------|-----------------------------|
| refresh | One plugin source exists in the fake provider. | `refresh` command. | Host snapshot contains the source keyed by `plugin:lead`. | No new persisted preference is required. | Source row appears with provider display title. |
| selection | Two visible sources exist and no detail is selected. | `selectDetail` for `plugin:lead`. | Host selected detail points to `plugin:lead`. | No persisted preference changes. | Detail view shows the selected source by source key. |
| alias edit | `plugin:lead` has no alias. | Commit alias text through source alias command. | Host applies alias through source preferences. | Alias for `plugin:lead` is stored. | Row title uses alias while source key remains `plugin:lead`. |
| hide | `plugin:lead` is visible. | `hideSource` for `plugin:lead`. | Host filters the row when show-hidden is false. | `plugin:lead` hidden flag is stored. | Row disappears and hidden-source affordance is available. |
| restore | `plugin:lead` is hidden and show-hidden is true. | `restoreSource` for `plugin:lead`. | Host includes the row in visible source list. | `plugin:lead` hidden flag is cleared. | Row appears as a normal visible source. |
| reorder | `plugin:lead` follows `plugin:keys`. | `moveSource` up for `plugin:lead`. | Host order changes. | Explicit order indices are stored by source key. | Rows appear in the explicit user order. |
| reset | `plugin:lead` has per-source analysis settings. | `resetSource` for `plugin:lead`. | Host returns resettable row state to defaults. | Resettable source preferences are cleared according to `main`. | Row remains identified by the same source key. |
| forget | `plugin:lead` has alias, hidden state, and detail preferences. | `forgetSource` for `plugin:lead`. | Host clears selected detail if it points at that source. | Stored preferences for `plugin:lead` are removed. | Source row uses provider defaults on the next refresh. |
| show hidden | Hidden sources exist and `showHiddenSources` is false. | `setShowHiddenSources` true. | Host includes hidden rows in the snapshot. | Show-hidden preference is stored. | Hidden rows appear with hidden state indicated. |
| reconnect | Plugin source disappears, then returns with the same source key and a new display name. | Fake provider publishes absent snapshot, then reconnected snapshot. | Host preserves source identity by source key. | Alias/order/detail preferences remain attached to the source key. | Row reflects the new provider display name only where no alias overrides it. |

## Detail Settings Traces

| Trace | Initial State | Action | Expected Host State | Expected Persisted State | Expected WebView Projection |
|-------|---------------|--------|---------------------|--------------------------|-----------------------------|
| true peak | Selected source uses global oversampling default. | Set source true peak oversampling. | Analyzer receives source-specific oversampling. | Per-source persisted setting is stored. | Detail control shows the selected per-source value. |
| waveform mode | Selected source uses overall waveform mode. | Set waveform mode to channels. | Host updates source analysis setting. | Per-source persisted setting is stored. | Waveform control shows channel mode. |
| spectrum mode | Selected source uses overall spectrum mode. | Set spectrum mode to channels. | Host updates source analysis setting. | Per-source persisted setting is stored. | Spectrum control shows channel mode. |
| collision visibility | Collision module is visible. | Toggle collision module visibility off. | Host omits active collision render work. | Detail module visibility is persisted per source. | Collision module body is hidden while header remains reachable. |
| detail module order | Detail modules use default order. | Move loudness-clock before spectrum. | Host detail module order changes. | Per-source module order is persisted. | WebView renders modules in the new order. |
| loudness-clock settings | Loudness-clock settings use defaults. | Change minimum, maximum, warning, and cycle seconds. | Host validates and applies clock settings. | Per-source clock settings are persisted. | Loudness-clock controls show the saved values. |

Detail settings use these ownership labels: global, per-source persisted, and runtime-only. Each migrated detail setting must name reset behavior before implementation.

## Rendering Work Gate Traces

| Trace | Initial State | Action | Expected Host State | Expected Persisted State | Expected WebView Projection |
|-------|---------------|--------|---------------------|--------------------------|-----------------------------|
| hidden waveform | Waveform module is hidden. | Analyzer publishes a plot tick. | Host may keep latest values, but no heavy hidden waveform render work is scheduled. | No preference changes. | Hidden waveform body remains inactive behind an active gate. |
| hidden spectrum | Spectrum module is hidden. | Analyzer publishes a plot tick. | Host may keep latest values, but no heavy hidden spectrum render work is scheduled. | No preference changes. | Hidden spectrum body remains inactive behind an active gate. |
| hidden loudness-clock | Loudness-clock module is hidden. | Clock plot tick fires. | Host may keep latest values, but hidden loudness-clock history does not append expensive samples. | No preference changes. | Hidden loudness-clock body remains inactive behind an active gate. |

## Build Packaging Baseline Traces

| Trace | Initial State | Action | Expected Host State | Expected Persisted State | Expected WebView Projection |
|-------|---------------|--------|---------------------|--------------------------|-----------------------------|
| restore | Clean main-based checkout with .NET SDK available. | Run `dotnet restore .\desktop-loudness-meter.sln`. | Project graph restores without WebView assets. | No settings changes. | Not applicable before WebView shell exists. |
| build | Restored solution. | Run `dotnet build .\desktop-loudness-meter.sln`. | App and tests compile. | No settings changes. | Not applicable before WebView shell exists. |
| test | Built solution. | Run `dotnet test .\desktop-loudness-meter.sln`. | Managed tests pass or report actionable failures. | No settings changes. | Not applicable before WebView shell exists. |
| publish | Buildable app project. | Run the publish command selected by the packaging slice. | Publish output contains app binaries, required native binaries, and packaged asset files. | No settings changes. | Packaged asset files are available to WebView loader. |
| development asset | WebView shell slice uses a dev server or development asset directory. | Launch app in development mode. | Host resolves development asset source or returns a clear missing asset error. | No settings changes. | WebView loads development asset entrypoint. |
| packaged asset | Published or installed app contains built WebView files. | Launch packaged app. | Host resolves packaged asset files. | No settings changes. | WebView loads packaged asset entrypoint. |
| missing asset | Development asset or packaged asset is absent. | Launch app with that asset mode. | Host reports a clear missing asset error and avoids a blank WebView. | No settings changes. | WebView shows an error state or shell fails before blank content. |

Native boundary traces are detailed in `native-boundary-baseline.md`.
