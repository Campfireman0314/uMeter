# Work Packet: 2026-07-01-native-host-projection-core

This consolidated archive replaces the finer-grained native Host projection changes that split one Host snapshot migration into one change per planner/projection.

## Intent

Move UI-neutral Host projection decisions into native core while keeping Host responsible for DTO assembly, workspace persistence, service invocation, and wrapper orchestration.

## Consolidated Changes

- `2026-06-30-native-host-source-list-projection-core`
- `2026-07-01-native-host-recent-events-core`
- `2026-07-01-native-host-detail-module-projection-core`
- `2026-07-01-native-host-snapshot-chrome-projection-core`
- `2026-07-01-native-host-collision-projection-core`
- `2026-07-01-native-host-selected-detail-shell-core`
- `2026-07-01-native-host-aggregate-snapshot-core`
- `2026-07-01-native-host-selected-detail-snapshot-core`

## Scope

In scope:
- Native source-list projection over merged WASAPI/local/plugin source rows.
- Native Host/Web source appeared recent-event lifecycle and bounded recent history.
- Native detail module row projection.
- Native settings/chrome snapshot projection and command availability.
- Native selected-detail shell metadata and collision-band projection.
- Native aggregate snapshot metadata planning.
- Unified native selected-detail snapshot planning that composes lower-level projection cores.

Out of scope:
- Workspace mutation commands and runtime service side effects; those are consolidated in `2026-07-01-native-host-command-and-state-core`.
- Host glue refactors such as workspace effect appliers, runtime service effect appliers, and snapshot assembler boundaries.
- WebView renderer behavior, WPF local rendering or WPF-only recent-event workflows.

## Success Criteria

- Host snapshot fields consumed by WPF/WebView remain compatible.
- Source-list ordering, filtering, duplicate title handling, selected-detail behavior, collision comparison fallback, module visibility/defaults, settings snapshot fields, recent history, and aggregate status text remain behaviorally covered.
- Native C ABI/build source lists include the consolidated projection cores.
- Remaining warnings are carried forward instead of hidden.
