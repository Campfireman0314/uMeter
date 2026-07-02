# Tasks: 2026-07-01-native-host-command-and-state-core

This is an archive consolidation change. No product implementation tasks are introduced.

## Task 1: Consolidate Command/State Archives

Acceptance:
- [x] Create one archive entry for native Host command planning and runtime state.
- [x] List all superseded fine-grained command/state changes.
- [x] Preserve gate outcomes in `evidence.md`.

Verification:
- `rg -n "2026-07-01-native-host-command-and-state-core|Consolidated Changes|Consolidated Archives" openspec/changes/archive/2026-07-01-native-host-command-and-state-core`

## Task 2: Update Knowledge References

Acceptance:
- [x] `.aiknowledge` source refs point at the consolidated command/state archive instead of removed fine-grained archive paths.
- [x] Native Host command/state responsibilities remain described at the implementation-file level.

Verification:
- `rg -n "openspec/changes/archive/2026-07-01-native-host-(chart|global|runtime-service|runtime-state|selected-source|source-order|source-preference|source-target)" .aiknowledge` returns no stale removed archive paths.
