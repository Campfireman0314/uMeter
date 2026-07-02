# WebView Migration Salvage Ledger

## Reference

| Field | Value |
|-------|-------|
| Main workspace | `D:\GitRepo\uMeter` |
| Clean branch | `codex/webview-migration-clean-restart` |
| Reference worktree | `D:\GitRepo\uMeter-webview-reference` |
| Reference branch | `origin/codex/native-host-webview-shell` |
| Reference commit | `cb7065d` |

No code should be copied from the reference branch unless this ledger marks the component as `copy` or `adapt` and names the reason.

## Decision Key

| Decision | Meaning |
|----------|---------|
| `copy` | Reuse nearly as-is after formatting and integration checks. |
| `adapt` | Reuse the concept or a limited implementation detail after aligning it to `main`. |
| `redesign` | Keep the product intent, but rebuild the implementation. |
| `delete` | Do not carry the behavior or component into the clean migration. |
| `defer` | Revisit in a later slice after a required boundary is settled. |

## Seed Findings

| Area | Reference Component | Decision | Reason | Owner |
|------|---------------------|----------|--------|-------|
| Frontend tooling | `src/DesktopLoudnessMeter.Web` React/Vite setup | `adapt` | Useful project shape and local development flow, but must be rebuilt on `main` with clean asset loading. | `02-webview-shell-foundation` |
| WebView hosting | WPF-hosted WebView shell | `adapt` | Useful hosting direction, but startup, tray, activation, and shutdown must preserve `main` lifecycle behavior. | `02-webview-shell-foundation` |
| HostBridge DTOs | Snapshot and command DTO concepts | `adapt` | Useful bridge vocabulary, but every field needs a single owner and every command needs a real handler. | `03-hostbridge-source-workflows` |
| Mock bridge | Mock-only command behavior | `redesign` | The mock cannot define product behavior by itself; mock-only commands are contract failures. | `01-migration-harness-and-contract-audit` |
| Source alias command | `setSourceAlias` frontend/mock path | `redesign` | Known mismatch: reference frontend and mock send the command, but the real bridge does not map it. | `03-hostbridge-source-workflows` |
| React local state | Display mode, true peak, eco mode, always-on-top, and related local-only fields | `redesign` | Migrated app state must have one host or persisted owner instead of drifting in React local state. | `02-webview-shell-foundation` and `03-hostbridge-source-workflows` |
| Recent events | Host snapshot plus view-model recent-event ownership | `redesign` | Duplicate ownership risks divergent history. One owner must publish the WebView projection. | `03-hostbridge-source-workflows` |
| Detail rendering | Chart rendering and canvas ideas | `adapt` | Useful visual direction, but must preserve `main` analysis semantics, active gates, and persisted detail settings. | `04-detail-charts-and-settings` |
| Native DSP host | Native analyzer and source-ingest experiments | `defer` | Potentially valuable, but it conflicts with the current optional-native product promise until the native boundary is decided. | `05-packaging-native-boundary-and-retirement` |
| Managed fallback removal | Mandatory native analyzer behavior | `redesign` | The reference branch appears to make native dependencies mandatory where `main` documents managed fallback. | `05-packaging-native-boundary-and-retirement` |
| WPF fallback surface | Long-term WPF/WebView compatibility shell | `delete` | User direction is to avoid permanent compatibility pressure and migrate the product surface cleanly. | `02-webview-shell-foundation` |
| Tests | Product-behavior tests from the reference branch | `adapt` | Useful only when they express preserved behavior or intentional WebView contracts, not accidental branch behavior. | All implementation slices |
| Packaging | WebView asset and native packaging targets | `adapt` | Useful as evidence, but final behavior must be rederived from clean build/publish/package requirements. | `05-packaging-native-boundary-and-retirement` |

## Current High-Risk Items

| Risk | Classification | Next Action |
|------|----------------|-------------|
| Mock-only bridge commands | `fatal` when user-facing | Enforce through `hostbridge-contract-matrix.md` and tests. |
| Native mandatory drift | `fatal` unless intentionally changed | Characterize `main` fallback and decide in packaging slice. |
| Split state ownership | `blocking-fixable` | Assign each field in `state-ownership-map.md`. |
| WPF compatibility retention | `discardable-legacy` unless needed temporarily | Delete or stage with retirement condition. |
