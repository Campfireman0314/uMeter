# Spec: 2026-07-01-native-host-command-and-state-core

## Requirements

### R1: Native Host command/state migration is represented as one reviewable archive

The archive shall consolidate source command planning, global settings planning, chart/detail settings planning, runtime service command planning, selected-source command planning, source-target guarding and runtime state storage into one command/state change.

#### Scenarios

- Given a reviewer wants to understand Host command planning migration, the reviewer can start from this archive rather than many one-planner archives.
- Given historical evidence is needed, this archive lists the superseded change IDs and gate outcomes.

### R2: Host effect ownership remains explicit

The archive shall state that native core plans command values/statuses and stores runtime state only, while Host continues to own side effects, persistence, service invocation and command result envelopes.

#### Scenarios

- Given a future change edits workspace persistence, it can distinguish native planning from Host effect application.
- Given a missing-source command is executed, native planning can return a normal status and Host can map it to the public command result.
