# Work Packet: 2026-07-01-completion-audit-refresh

Text is ASCII in this change to avoid terminal encoding ambiguity. Paths, commands, status values, and trace IDs stay literal.

## Metadata

- profile: strict-change
- source_type: feature
- status: drafting
- created: 2026-07-01

## Slicing Decision

### Overall

- Decision: KEEP_ONE_CHANGE
- Reason: this change is one complete completion-audit refresh. It does not implement product behavior. It re-evaluates the archived `2026-06-30-completion-audit` verdict against current repository evidence after the 2026-07-01 native-host changes. Mixing this audit with product fixes would blur the line between "state changed" and "state was proven."
- OpenSpec lower-bound check: pass. This is a traceable engineering loop: read current repo/codemap/OpenSpec evidence, run fresh verification, update the completion matrix, and route the next implementation change.

### Capability Clusters

| Cluster | User value | Main modules | Independently deliverable | Notes |
|---------|------------|--------------|----------------------------|-------|
| Audit baseline refresh | Avoid repeated work against stale gaps | `openspec/changes/archive/**`, `.aiknowledge/codemap/**` | yes | Read-only fact check |
| Completion matrix | Show what is proven vs still weak | WebView shell, Host, Native, macOS runner | yes | Weak evidence is not completion |
| Next-route map | Route remaining gaps to the next OpenSpec change | `ui-runtime-architecture`, `host-contract`, `native-dsp` | yes | This change does not fix product gaps |

### Recommended Top-Level Change

1. `completion-audit-refresh`
   - Scope: refresh the final architecture audit, run current verification commands, output `proven` / `incomplete` / `weak_evidence` / `not_applicable_by_scope` grades, and produce a remaining gap map.
   - Out of scope: product code, Web UI implementation, native core implementation, macOS runtime implementation, and broad architecture rewrites.
   - Dependencies: archived `2026-06-30-completion-audit`, archived 2026-07-01 native-host changes, and current codemap.
   - Change name: `2026-07-01-completion-audit-refresh`
   - Focus: remove stale gaps, keep unproven items open, and choose the next implementation change.

### Execution Order

- Current: `2026-07-01-completion-audit-refresh`
- Next: choose one implementation change from the refreshed gap map, preferring work that moves Host closer to glue-only or closes macOS/Web transport runtime gaps.

## Exploration Summary

Self-check / gap scan:

- Candidate recommendation: do `completion-audit-refresh`, not a duplicate `settings-workspace-schema`, `native-source-ingest-core`, or `juce-sender-protocol-boundary`.
- Round 1 scan: blocking = several candidates from the original recommended order are already archived; reopening them would duplicate completed scope. non-blocking = Host glue-only, macOS runtime adapter, aggregate snapshot/core state boundaries still need proof.
- Revised recommendation: this change only refreshes the audit and gap map. It does not claim the overall goal is complete and does not edit product code.
- Round 2 scan: blocking = no new key fact blocks planning. remaining uncertainty = whether Host is close enough to glue-only requires source and command evidence during implementation.
- Most likely error: treating a document audit as architecture completion, or treating many native-backed planners as proof that Host is already glue-only.
- Boundary handling: if the audit finds product gaps, keep the goal active and route the gap to a later implementation change.
- Smaller alternative: answer the user conversationally only. Rejected because the goal requires evidence-backed completion audit discipline.
- Evidence vs inference: archive list and codemap changes are proven; "audit refresh is the best next step" is an inference that implementation must validate with current commands.
- Final scan conclusion: no new blocking fact. Proceed to plan/readiness with `strict-change`.

## Intent

Refresh the completion audit for uMeter's WebView UI + C++ Core target architecture. The archived 2026-06-30 audit returned `not_complete`, but some of its high-priority gaps have since been implemented and archived. This change determines the current truthful status and the next best implementation change.

## Scope

### In Scope

- Read current archived OpenSpec changes, codemap entries, key source files, and tests.
- Re-grade each final-state requirement from the user goal.
- Re-evaluate old audit gaps as `closed`, `partially_addressed`, `still_open`, or `superseded`.
- Run current Web/.NET/native verification commands where available.
- Output a fresh remaining gap map and recommended next change.

### Out Of Scope

- No edits to `src/**`, `tests/**`, `docs/**`, or `.aiknowledge/**`.
- No implementation of Web settings, Host native migration, macOS adapter, or packaging.
- No `update_goal complete` unless current evidence proves every final-state requirement.
- No treating "many archived changes exist" as completion proof.

## Success Criteria

- The audit matrix covers every final-state requirement in the user goal.
- Every conclusion cites current evidence and distinguishes facts, inference, and weak evidence.
- The old high-priority gaps are individually rechecked.
- Current Windows/WebView main-path verification has fresh evidence.
- If the goal is still incomplete, the next implementation change is named and the goal remains active.

## Requirements Summary

- [R1] The audit refresh must cover all final-state requirements in the user goal.
- [R2] The audit refresh must re-evaluate the old `2026-06-30-completion-audit` gap map.
- [R3] The audit refresh must use current repository evidence and fresh verification.
- [R4] If the goal is not complete, the audit must output a current remaining gap map and next change recommendation.
- [R5] The audit refresh must complete the OpsX/OpenSpec lifecycle and must not treat the audit itself as overall goal completion.

## Approach

Use a read-only audit refresh. Implementation will read archived evidence, codemap, and key source files, run available verification commands, and build a requirements-to-evidence matrix. If any final-state requirement is `incomplete`, `weak_evidence`, or unverified, the overall goal remains active.

## Files / Ownership

Allowed:

- `openspec/changes/2026-07-01-completion-audit-refresh/.openspec.yaml`
- `openspec/changes/2026-07-01-completion-audit-refresh/work.md`
- `openspec/changes/2026-07-01-completion-audit-refresh/specs/change/spec.md`
- `openspec/changes/2026-07-01-completion-audit-refresh/design.md`
- `openspec/changes/2026-07-01-completion-audit-refresh/tasks.md`
- `openspec/changes/2026-07-01-completion-audit-refresh/evidence.md`

Forbidden:

- `src/**`
- `tests/**`
- `docs/**`
- `.aiknowledge/**`, except an archive-time knowledge follow-up if required
- Other `openspec/changes/**`

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-07-01-completion-audit-refresh`
- Base Branch: `codex/2026-07-01-native-host-source-target-command-core`
- Base HEAD: `5d82929d1dd85525baa43b76c4d26c672a66b4e0`
- Merge Target: user-owned integration branch
- Workspace Rationale:
  - Dirty classification: before init, the tree was clean. After init, dirty files are only `openspec/changes/2026-07-01-completion-audit-refresh/**`, owned by this change.
  - Chained decision: this audit is based on the previous completed native-host-source-target-command-core branch, so it is chained to base head `5d82929d1dd85525baa43b76c4d26c672a66b4e0`.
  - Isolated trigger: none. This is a single-agent OpenSpec artifact change and does not touch product shared interfaces, schema, or routes.
  - Conclusion: `current-worktree`.

## Verification Plan

- `opsx changes list`
- `rg` / `Get-Content` reads of archived changes, codemap, and key source files
- `npm test` and `npm run build` in `src/DesktopLoudnessMeter.Web`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- Native build or fresh native evidence when available; otherwise mark as weak/missing evidence, not pass
- `git diff --check`
- `git status --short --branch`

## Stabilization / Platform Uncertainty

- Requires `[stabilization-first]`: yes. Completion is a cross-change architecture evidence judgment, not a single behavior assertion.
- Uncertainty sources: macOS machine/toolchain, Host glue-only boundary, native aggregate snapshot/core state ownership.
- Manual/platform coverage: no new user manual UI verification is required in this audit. Missing command evidence remains weak or routed to a later gap.
- Final hardening: use the audit matrix and fresh regression commands; do not promote missing evidence to pass.

## Rollback / Escalation

- Rollback: delete or revise this audit-refresh change directory.
- Escalation: if product implementation is needed, close this audit with a gap and create a later implementation change.

## Bugfix Root Cause

none (feature/audit refresh).

## Rejected Attempts

- Do not recreate `settings-workspace-schema`: it is already archived and covered by current tests/DTOs.
- Do not recreate `native-source-ingest-core` or `juce-sender-protocol-boundary`: both are archived and reflected in codemap.
- Do not announce goal completion directly: current codemap still records Host glue-only and macOS adapter gaps.

## Readiness Notes

- Readiness must confirm final-state coverage.
- Readiness must confirm tasks only allow this change's OpenSpec artifacts.
- Readiness must confirm Workspace Rationale has dirty classification, chained decision, isolated trigger, and conclusion.
