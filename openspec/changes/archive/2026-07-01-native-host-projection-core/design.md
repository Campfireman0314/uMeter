# Design: 2026-07-01-native-host-projection-core

## Consolidation Design

The previous archive represented one continuous Host projection migration as many small changes. This consolidated change keeps the evidence boundary at a human-reviewable level:

- Source-list and recent-event projections define what sources are visible and what lifecycle history the Host exposes.
- Snapshot chrome, aggregate snapshot and selected-detail snapshot planners define the Host snapshot surface.
- Detail module, selected-detail shell and collision projection remain lower-level native components used by the unified selected-detail snapshot planner.

## Ownership Boundaries

Native owns deterministic planning/projection:
- source row projection,
- recent-event lifecycle/history,
- settings/chrome projection,
- command availability,
- aggregate snapshot metadata,
- selected-detail module/shell/collision snapshot planning.

Host remains the integration owner:
- reads source services,
- supplies workspace settings,
- maps native plans to `HostSnapshot` DTOs,
- persists workspace settings,
- invokes reset/analysis services,
- validates HostBridge payloads outside native planning.

## Evidence Preservation

The individual archived change directories were removed from the current tree to reduce archive noise. Their IDs, scope, gate outcomes and retained warnings are preserved in this consolidated evidence file; full historical contents remain recoverable from Git history before this consolidation commit.
