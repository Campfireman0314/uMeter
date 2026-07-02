# Tasks: 2026-07-01-native-host-projection-core

This is an archive consolidation change. No product implementation tasks are introduced.

## Task 1: Consolidate Projection Archives

Acceptance:
- [x] Create one archive entry for native Host projection/snapshot planning.
- [x] List all superseded fine-grained projection changes.
- [x] Preserve gate outcomes and warnings in `evidence.md`.

Verification:
- `rg -n "2026-07-01-native-host-projection-core|Consolidated Changes|Retained Warnings" openspec/changes/archive/2026-07-01-native-host-projection-core`

## Task 2: Update Knowledge References

Acceptance:
- [x] `.aiknowledge` source refs point at the consolidated projection archive instead of removed fine-grained archive paths.
- [x] Native Host projection responsibilities remain described at the implementation-file level.

Verification:
- `rg -n "openspec/changes/archive/2026-0(6-30|7-01)-native-host-(source-list|aggregate|collision|detail|recent|selected-detail|snapshot)" .aiknowledge` returns no stale removed archive paths.
