# Spec: 2026-07-01-native-host-projection-core

## Requirements

### R1: Native Host projection is represented as one reviewable archive

The archive shall consolidate the native source-list, recent-event, snapshot chrome, aggregate snapshot, detail module, selected-detail shell, selected-detail snapshot and collision projection changes into one projection/snapshot planning change.

#### Scenarios

- Given a reviewer wants to understand Host snapshot projection migration, the reviewer can start from this archive rather than many one-planner archives.
- Given historical evidence is needed, this archive lists the superseded change IDs and retained warnings.

### R2: Host ownership boundaries remain explicit

The archive shall state that native core plans/projections only, while Host continues to own DTO mapping, workspace persistence, service invocation and wrapper orchestration.

#### Scenarios

- Given a future change edits Host snapshot behavior, it can distinguish native planning from Host glue responsibilities.
- Given a regression appears in workspace persistence or service side effects, this archive does not imply native owns those effects.
