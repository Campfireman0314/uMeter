# Design: 2026-07-01-native-host-command-and-state-core

## Consolidation Design

The previous archive represented one continuous command/state migration as many small changes. This consolidated change keeps the evidence boundary at the command-planning level:

- source commands,
- global settings commands,
- chart/detail settings commands,
- runtime service commands,
- selected-source commands,
- source-target guards,
- runtime state storage.

## Ownership Boundaries

Native owns deterministic planning and runtime state storage:
- command result values,
- missing-source status decisions,
- value normalization/clamping,
- selected-source/show-hidden/performance runtime storage.

Host owns side effects:
- writes `UserWorkspaceSettings`,
- calls optional reset and analysis-settings services,
- saves workspace state,
- maps plans to `HostCommandResult`,
- rebuilds snapshots,
- validates HostBridge payload domains before planning where required.

## Evidence Preservation

The individual archived change directories were removed from the current tree to reduce archive noise. Their IDs, scope and gate outcomes are preserved in this consolidated evidence file; full historical contents remain recoverable from Git history before this consolidation commit.
