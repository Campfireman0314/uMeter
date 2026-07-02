# Work Packet: 2026-07-01-native-host-command-and-state-core

This consolidated archive replaces the finer-grained native Host command/state changes that split one command-planning migration into one change per planner.

## Intent

Move deterministic Host command planning and runtime state storage into native core while keeping Host responsible for applying plans, persisting settings, invoking services, validating payloads and building command results.

## Consolidated Changes

- `2026-07-01-native-host-source-order-command-core`
- `2026-07-01-native-host-source-preference-command-core`
- `2026-07-01-native-host-global-settings-command-core`
- `2026-07-01-native-host-chart-settings-command-core`
- `2026-07-01-native-host-runtime-service-command-core`
- `2026-07-01-native-host-selected-source-command-core`
- `2026-07-01-native-host-source-target-command-core`
- `2026-07-01-native-host-runtime-state-core`

## Scope

In scope:
- Native source order planning.
- Native source preference planning for alias, hide, restore and forget.
- Native global settings planning.
- Native chart/detail/source settings value planning.
- Native runtime service command planning.
- Native selected-source command/reconcile planning.
- Native source-target existence planning.
- Native runtime state storage for selected source, show-hidden and performance mode.

Out of scope:
- Source-list, selected-detail, aggregate snapshot and settings/chrome projection; those are consolidated in `2026-07-01-native-host-projection-core`.
- Applying workspace mutations to `UserWorkspaceSettings`.
- Invoking reset/analysis services.
- HostBridge payload validation and Web UI behavior.

## Success Criteria

- Native planning returns typed statuses and plan values for Host command flows.
- Host remains the owner of effect application, persistence, service invocation and DTO mapping.
- Missing-source flows remain normal command statuses rather than exceptions.
- Native ABI/build source lists include the consolidated command/state cores.
