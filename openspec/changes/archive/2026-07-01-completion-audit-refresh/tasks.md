# Tasks: 2026-07-01-completion-audit-refresh

Text is ASCII in this change to avoid terminal encoding ambiguity.

## Default Constraints

**Forbidden Files**:
- `src/**`
- `tests/**`
- `docs/**`
- `.aiknowledge/**`
- Other `openspec/changes/**`

**Shared Artifacts**:
- `.openspec.yaml` - controller serial update
- `tasks.md` - controller serial update
- `evidence.md` - controller serial update

**Worker Return Format**: DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED

## Task 1: [stabilization-first] Refresh the final-state audit matrix

**Requirements**: [R1], [R2] -> [U1], [U2]
**Mode**: [stabilization-first]
**Allowed Files**:
- `openspec/changes/2026-07-01-completion-audit-refresh/evidence.md` - record audit matrix and old gap recheck

**Acceptance Criteria**:
- [x] Every final-state requirement in the user goal has an evidence grade.
- [x] The old `2026-06-30-completion-audit` gap map is rechecked item by item.
- [x] Archived-but-insufficient items are not marked complete.

**Verification Command / Method**:
- `rg -n "final_verdict|Requirements Matrix|Old Gap Recheck|Remaining gap" openspec\changes\2026-07-01-completion-audit-refresh\evidence.md`, expected: matrix, old gap recheck, and verdict are present.

**Evidence Required**:
- requirements matrix
- old gap recheck table
- facts/inferences/weak evidence summary

**Stabilization Rationale**:
- Uncertainty source: the goal spans many archived changes and the old audit gap map is partly stale.
- Why not test-first: completion is an architecture evidence audit, not one behavior assertion.
- Exit condition: every final-state requirement has a current evidence grade and every old gap has a current status.

**Manual / Platform Matrix**:
| Coverage | Method | Blocking? | Expected |
|----------|--------|-----------|----------|
| macOS runtime | read readiness/codemap | no | no real macOS evidence means weak/follow-up |
| Windows UI | current commands and archived smoke | no | fresh regression or explicit citation |

**Final Hardening Plan**:
- Run grep checks for required evidence sections after the matrix is written.
- doc impact: TASK_EVIDENCE_ONLY.

**Done When**:
- `evidence.md` contains final-state matrix and old gap recheck.

## Task 2: [stabilization-first] Collect current verification evidence

**Requirements**: [R3] -> [U3]
**Mode**: [stabilization-first]
**Allowed Files**:
- `openspec/changes/2026-07-01-completion-audit-refresh/evidence.md` - record command summaries and unavailable checks

**Acceptance Criteria**:
- [x] Web frontend test/build has fresh evidence or explicit failure.
- [x] .NET regression has fresh evidence or explicit failure.
- [x] Scope/check commands prove product source files were not edited by this change.
- [x] Native/macOS unavailable checks are not recorded as pass.

**Verification Command / Method**:
- `npm test` in `src/DesktopLoudnessMeter.Web`
- `npm run build` in `src/DesktopLoudnessMeter.Web`
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`
- `git diff --stat -- src tests docs .aiknowledge`
- `git status --short --branch`

**Evidence Required**:
- exit code and key summary for each command
- allowed failures / weak evidence list

**Stabilization Rationale**:
- Uncertainty source: the repository may have changed after the previous audit.
- Why not test-first: this task refreshes regression evidence and does not change behavior.
- Exit condition: command results are recorded and failures/skips are classified.

**Manual / Platform Matrix**:
| Coverage | Method | Blocking? | Expected |
|----------|--------|-----------|----------|
| Windows/WebView automated path | Web/.NET commands | yes if failing without allowed reason | pass |
| macOS native/runtime | current Windows session only records evidence | no | do not claim pass |

**Final Hardening Plan**:
- Run `git diff --check` as final scope hygiene.
- doc impact: TASK_EVIDENCE_ONLY.

**Done When**:
- `evidence.md` contains fresh verification ledger.

## Task 3: [direct] State current verdict and next change

**Requirements**: [R4], [R5] -> [U4], [U5]
**Mode**: [direct]
**Allowed Files**:
- `openspec/changes/2026-07-01-completion-audit-refresh/evidence.md` - final verdict and next change map

**Acceptance Criteria**:
- [x] Final verdict matches the matrix.
- [x] If verdict is `not_complete`, current completion-blocking gaps are listed.
- [x] The evidence clearly says whether `update_goal complete` is allowed.
- [x] The next OpenSpec implementation change is recommended.

**Verification Command / Method**:
- `rg -n "final_verdict|Goal completion decision|Recommended next implementation" openspec\changes\2026-07-01-completion-audit-refresh\evidence.md`, expected: verdict and next step are present.

**Evidence Required**:
- Final Gate Summary draft
- Goal completion decision
- Recommended next implementation

**Done When**:
- verdict, goal decision, and next change are consistent in `evidence.md`.
