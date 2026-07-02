# Design: 2026-07-01-completion-audit-refresh

Text is ASCII in this change to avoid terminal encoding ambiguity.

## Context

The archived `2026-06-30-completion-audit` returned `not_complete`. Since then, several old high-priority gaps have gained archived evidence, including Web settings workflow parity, native protocol ingest, and multiple native-backed Host projection/command planners. Current codemap still says Host does not yet qualify as glue-only and macOS still has a WKWebView boundary/skeleton rather than full runtime parity.

This change refreshes the audit and current next-step map. It does not implement product behavior.

## Requirement Trace

- [R1] -> [U1]
- [R2] -> [U2]
- [R3] -> [U3]
- [R4] -> [U4]
- [R5] -> [U5]

## Implementation Units

### [U1] Final-state matrix refresh

- Requirement: [R1]
- Module boundary:
  - `openspec/changes/archive/2026-06-30-completion-audit/evidence.md`
  - `openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/**`
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/native-dsp.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/macos-runner-boundary.md`
- Verification: evidence contains a requirements matrix with source references.

### [U2] Old gap map recheck

- Requirement: [R2]
- Module boundary:
  - `openspec/changes/archive/2026-06-30-completion-audit/evidence.md`
  - `openspec/changes/archive/2026-06-30-web-settings-workflow-parity/**`
  - `openspec/changes/archive/2026-06-30-native-protocol-ingest-core/**`
  - `openspec/changes/archive/2026-07-01-native-host-*-core/**`
- Verification: evidence classifies old gaps as `closed`, `partially_addressed`, `still_open`, or `superseded`.

### [U3] Fresh verification

- Requirement: [R3]
- Module boundary:
  - `src/DesktopLoudnessMeter.Web/**`
  - `tests/DesktopLoudnessMeter.Tests/**`
  - `src/DesktopLoudnessMeter.Native/**`
  - current Git worktree
- Verification: Web test/build, .NET test, and scope/check commands are recorded.

### [U4] Current remaining gap map

- Requirement: [R4]
- Module boundary:
  - `openspec/changes/2026-07-01-completion-audit-refresh/evidence.md`
- Verification: if final verdict is not complete, evidence lists gap, impact, suggested next change, and priority.

### [U5] Lifecycle guard

- Requirement: [R5]
- Module boundary:
  - `openspec/changes/2026-07-01-completion-audit-refresh/.openspec.yaml`
  - `openspec/changes/2026-07-01-completion-audit-refresh/tasks.md`
  - `openspec/changes/2026-07-01-completion-audit-refresh/evidence.md`
- Verification: readiness, verify, review, and archive pass. If verdict remains `not_complete`, do not call `update_goal complete`.

## Decisions

1. Read-only audit, no product code edits.
   - Reason: audit should judge current state; fixing gaps in the same change would blur evidence.
   - Impact: product gaps become later implementation changes.

2. Recompute old gaps instead of carrying them forward.
   - Reason: the previous audit is stale after later archives.
   - Impact: this change's main value is the current gap map.

3. Treat Host glue-only as a strict evidence claim.
   - Reason: native-backed planners reduce Host responsibility, but Host may still apply workspace state, call services, assemble DTOs, and orchestrate snapshots.
   - Impact: do not equate "native-backed decision" with "Host only glue."

4. Treat macOS as boundary-proven only unless runtime evidence exists.
   - Reason: this Windows session cannot prove macOS runner/capture/package runtime behavior.
   - Impact: WKWebView skeleton/readiness can be proven as boundary; runtime parity remains follow-up unless evidence exists.

## Risks / Tradeoffs

- Risk: this looks like "docs only." Mitigation: it directly prevents duplicate implementation against stale gaps.
- Risk: over-conservatism keeps the goal open too long. Mitigation: use explicit evidence grades; if all requirements are proven, completion is allowed.
- Risk: verification commands are time-consuming. Mitigation: prioritize Web/.NET regression and record native/macOS gaps honestly.

## Rollback

Delete or revise this audit-refresh change directory. Product code is unaffected.

## Parallel / Subagent Boundary

Default is serial. Final verdict remains with the main agent because evidence grading needs integrated judgment.
