# 03-hostbridge-source-workflows

## Goal
Move source list and source-detail workflows onto a clean HostBridge contract, preserving product behavior from `main` while removing mock-only and compatibility-only command paths.

## In Scope
- Implement source snapshot fields and commands for refresh, select, alias edit, hide, restore, reorder, forget/reset, show hidden, and detail selection.
- Preserve stable source-key identity across reconnects, display-name changes, plugin restarts, and same-handle reconfiguration.
- Make persisted workspace preferences the source of truth for aliases, hidden state, explicit order, detail selection, and reset behavior.
- Ensure every WebView command has a real C# bridge handler, host execution path, mock bridge parity, and test coverage.
- Reconcile recent-event ownership so the host snapshot and WebView do not compete for the same history.

## Out of Scope
- Rendering waveform, spectrum, loudness-clock, or collision charts.
- Installer or packaging behavior.
- Native DSP boundary changes beyond what source workflow tests require.

## Depends On
- `01-migration-harness-and-contract-audit` for command matrix and scenario traces.
- `02-webview-shell-foundation` for the bridge bootstrap and primary app surface.
- Existing `source-list-workspace` and `signal-source-ingest` behavior from `main`.

## Done Means
- Source workflow scenario traces pass against the real bridge, not only the mock bridge.
- Alias, visibility, ordering, selection, reset, and reconnect behavior match preserved product promises from `main`.
- Commands absent from the real bridge are treated as build/test failures rather than UI-only optimism.
- Source state has a single owner across host, persisted workspace, and WebView projection.
