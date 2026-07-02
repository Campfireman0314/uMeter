# Evidence: 2026-07-01-completion-audit-refresh

Text is ASCII in this change to avoid terminal encoding ambiguity.

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: `## Readiness`
- final_verify:
  - status: pass
  - evidence_ref: `## Verify`
- final_review:
  - status: pass
  - evidence_ref: `## Risk Review`
- final_archive:
  - status: pass
  - evidence_ref: `## Archive`
  - archive_path: `openspec/changes/archive/2026-07-01-completion-audit-refresh`
- final_verdict:
  - status: not_complete
  - evidence_ref: `## Implementation Evidence`
- allowed_failures:
  - Existing UDP socket integration tests are skipped in the .NET suite; this audit does not claim real UDP socket coverage from those skips.
  - macOS runtime smoke/build/capture/packaging evidence is unavailable in this Windows session; macOS is graded boundary-only where applicable.
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: `## Archive`

## Plan Evidence

- `opsx changes list`: no active changes before this change was created.
- Read archived planning:
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/design.md`
- Read codemap:
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/source-list-workspace.md`
  - `.aiknowledge/codemap/host-contract.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/signal-source-ingest.md`
  - `.aiknowledge/codemap/audio-capture-sources.md`
  - `.aiknowledge/codemap/chains/pcm-metric-flow.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
- Current proven facts from planning:
  - `settings-workspace-schema` is archived. `UserWorkspaceSettings` has schema versioning, legacy load/save tests, HostBridge DTO projection, and TS DTOs.
  - `native-source-ingest-core` is archived. Native `DlmSourceIngestCore` owns local/plugin source lifecycle and analyzer ownership.
  - `juce-sender-protocol-boundary` and `native-protocol-ingest-core` are archived. The old protocol-ingest gap must be re-evaluated.
  - Current codemap still records Host as not fully glue-only and macOS Web-side WKWebView transport adapter as a follow-up.
- Inference:
  - The best next step is to refresh the completion audit rather than repeat modules from the old recommended order.

## Readiness

- 2026-07-01T23:52:00+08:00 readiness: pass
- Scope reviewed:
  - `.openspec.yaml`
  - `work.md`
  - `specs/change/spec.md`
  - `design.md`
  - `tasks.md`
  - `evidence.md`
- Evidence:
  - `opsx changes list`: current change present as `2026-07-01-completion-audit-refresh`.
  - `opsx changes resolve 2026-07-01-completion-audit-refresh`: resolved to `/e/gitlab/uMeter/openspec/changes/2026-07-01-completion-audit-refresh`.
  - `rg -n "\[R[1-5]\]|\[U[1-5]\]|Workspace Strategy|Workspace Rationale|Allowed Files|Forbidden Files|Evidence Required|Stabilization Rationale|Manual / Platform Matrix|Final Hardening Plan|\*\*Trace\*\*: R|final_verdict|Goal completion" openspec\changes\2026-07-01-completion-audit-refresh`: exit 0, found R/U trace, task dispatch fields, workspace rationale, and verdict placeholders.
  - template-residue grep check: exit 1, no template residue after cleanup.
  - `git diff --check`: exit 0.
  - `git status --short --branch`: branch `codex/2026-07-01-completion-audit-refresh`; dirty scope is untracked `openspec/changes/2026-07-01-completion-audit-refresh/**`.
- Conclusion:
  - R1-R5 are present in spec, design, and tasks.
  - `work.md` has intent, scope, non-goals, success criteria, verification plan, rollback/escalation, rejected attempts, and a complete Workspace Rationale.
  - `tasks.md` tasks are auditable and constrained to this change's OpenSpec evidence.
  - This change may enter implement.

## Implementation Evidence

### Task 1 - Requirements Matrix

Grades:
- `proven`: current source and/or archived gate evidence directly satisfies the requirement.
- `partially_proven`: a meaningful part is implemented, but a named portion remains outside the final architecture.
- `weak_evidence`: boundary or planning evidence exists, but runtime/build/user-workflow evidence is missing.
- `not_complete`: current evidence contradicts the final-state requirement.

| # | Final-state requirement | Current grade | Evidence | Conclusion |
|---|-------------------------|---------------|----------|------------|
| 1 | WebView carries the main UI | partially_proven | Windows WebView2 shell exists in `src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml(.cs)` and archived WebView startup/tray work made the Windows path runnable. macOS WKWebView is only a skeleton/boundary. | Windows main path is proven; cross-platform main UI is not complete. |
| 2 | Web frontend owns window content, interaction, theme, settings, charts, and user workflows | partially_proven | `AppShell.tsx` owns DOM shell, source list/detail/recent/settings; `AppShell.test.tsx` covers editable settings; `ChartModuleCanvas.tsx` owns chart rendering. Previous WPF retirement evidence still left workflow parity items such as alias/source ordering/layout import-export/popup detail outside full Web parity. | Web UI is the active Windows UI surface, but final workflow parity is not fully proven. |
| 3 | Windows uses WebView2 | proven | `WebViewShellWindow.xaml` references `Microsoft.Web.WebView2.Wpf`; code handles `CoreWebView2.WebMessageReceived` and posts JSON responses. | Satisfied for Windows. |
| 4 | macOS reserves a WKWebView runner boundary | proven boundary; weak runtime | `src/DesktopLoudnessMeter.MacHost/**` and `docs/platform/macos-readiness.md` define a WKWebView/HostBridge boundary. | Boundary exists; runtime implementation remains follow-up. |
| 5 | C++ Core owns DSP | proven | Archived `2026-06-23-native-core-dsp`; current native build includes `DlmPcmAnalyzer.*`; .NET tests pass with native DLL copied. | Satisfied for current Windows/native path. |
| 6 | C++ Core owns protocol ingest | partially_proven | Archived `2026-06-30-native-protocol-ingest-core` moved receiver-side Protocol v1 JSON decode and ack encode to `DlmLoopbackProtocol`; that change explicitly kept UDP socket, remote filtering, source-key mapping, and HostSnapshot out of scope. | Protocol vocabulary/decode core is native-backed; UDP transport ingest is still Host/platform glue. |
| 7 | C++ Core owns source ingest state machine | proven for local/plugin ingest | Archived `2026-06-29-native-source-ingest-core` and current native source list include `DlmSourceIngestCore.*`. | Local/plugin lifecycle is native-backed; platform capture adapters remain host/platform boundary by design. |
| 8 | C++ Core owns snapshot generation | partially_proven | Current native project includes `DlmHostSourceProjection`, `DlmHostRecentEvents`, `DlmHostDetailModuleProjection`, `DlmHostSelectedDetailShell`, `DlmHostCollisionProjection`, and `DlmHostSnapshotChromeProjection`. Current codemap says Host still assembles DTOs and orchestrates wrappers. | Many snapshot sub-projections are native-backed, but aggregate `HostSnapshot` generation is not fully in C++ Core. |
| 9 | C++ Core owns the core state machine | weak_evidence | Native command planners exist for source order/preference/global/chart/runtime/selected/source target operations, but `.aiknowledge/codemap/host-contract.md` says Host still owns selected source state, workspace persistence, service invocation, and post-command snapshot orchestration. | Core state boundary is improved but not complete. |
| 10 | Host only keeps runners, WebView host, HostBridge v1, platform capture adapter, tray, startup, single-instance, installer glue | not_complete | `DesktopMeterHostController` still owns `_workspaceSettings`, `_selectedSourceKey`, service calls, plan application, persistence, recent-event reconciliation, DTO assembly, and aggregate snapshot rebuild. Codemap explicitly states Host does not yet qualify as glue-only. | This is a completion-blocking gap. |
| 11 | HostBridge v1 uses native WebView message channel, typed command bus, JSON DTO | partially_proven | Windows WebView2 uses native message channel; TS/C# DTOs and typed commands exist. `webViewTransport.ts` only detects `window.chrome.webview`; macOS skeleton documents `window.webkit.messageHandlers.hostBridge` but Web adapter is not implemented. | Windows transport is proven; macOS transport is incomplete. |
| 12 | Web UI uses TypeScript + React + Vite; DOM for ordinary UI; Canvas/WebGL for high-frequency charts | proven | `package.json` scripts run `vitest`, `tsc -b`, and `vite build`; React app uses DOM shell and `ChartModuleCanvas`. | Satisfied. |
| 13 | JUCE sender keeps DAW sender role; Protocol keeps versioned lifecycle vocabulary | proven | Archived `2026-06-29-juce-sender-protocol-boundary` and `2026-06-30-native-protocol-ingest-core` preserve sender/protocol roles. | Satisfied by archived evidence. |
| 14 | Complete Windows runnable main path first, with clean macOS boundary | partially_proven | Windows tests/build/native build pass in this audit; macOS readiness docs and skeleton boundary exist. | Windows path is healthy; macOS runner/runtime remains follow-up. |
| 15 | WPF remains fallback/oracle until WebView parity, then retired or isolated | partially_proven | Archived WPF-retirement work moved the WebView path forward and kept WPF as oracle/fallback. Full Web workflow parity is not proven, so final retirement/isolation cannot be claimed. | WPF fallback/oracle policy is respected; final retirement is not complete. |

### Task 1 - Old Gap Recheck

| Old `2026-06-30-completion-audit` gap | Current status | Evidence | Current decision |
|---------------------------------------|----------------|----------|------------------|
| Web settings/theme workflow incomplete | closed for settings/theme surface | Archived `2026-06-30-web-settings-workflow-parity`; current `AppShell.tsx` and tests cover settings open/close, `setTheme`, `setRecentEventLimit`, `setEventOutputFolder`, show-hidden, schema display, and `data-theme`. | Remove from high-priority completion blockers; broader Web workflow parity remains separate. |
| Protocol ingest remains C# Host glue | mostly closed for Protocol v1 decode/ack | Archived `2026-06-30-native-protocol-ingest-core` moved receiver-side protocol decode/ack into native `DlmLoopbackProtocol`; UDP socket and filtering intentionally remain host/platform. | Remove old protocol-ingest blocker; do not claim UDP transport moved to Core. |
| UI-facing snapshot generation remains partly in C# Host | still open, narrowed | Native snapshot chrome/source/detail/collision/recent sub-projections now exist, but Host still assembles aggregate DTOs and orchestrates post-command snapshot rebuild. | Keep as completion blocker under aggregate snapshot/core-state boundary. |
| Host is not glue-only | still open | Codemap and source show Host still owns workspace persistence, selected source runtime state, command plan application, service invocation, and DTO assembly. | Keep as primary completion blocker. |
| Core state machine boundary not fully proven | still open | Native command planners exist, but Host still coordinates the authoritative app/workspace runtime state. | Keep as completion blocker or paired with host-glue reduction. |
| macOS Web-side WKWebView adapter not implemented | still open | `webViewTransport.ts` detects only `window.chrome.webview`; macOS README/docs define `window.webkit.messageHandlers.hostBridge`; Swift bridge returns unavailable. | Keep as macOS follow-up blocker for cross-platform final state. |

Facts:
- Web settings/theme and native protocol decode/ack have moved since the old completion audit.
- Fresh Windows/Web regression evidence is green.
- The current audit change has not edited product source files.

Inferences:
- The next implementation work should not repeat settings/schema/protocol-ingest modules that are already archived.
- The best next product change is a Host/Core boundary reduction change focused on aggregate snapshot and state ownership, because it blocks multiple final-state requirements at once.

Weak or missing evidence:
- macOS runtime smoke/build/capture/package evidence is missing in this Windows session.
- Full Web workflow parity beyond settings and charts is not proven by the current regression suite.

### Task 2 - Verification Ledger

| Command | Result | Key output / meaning |
|---------|--------|----------------------|
| `npm test` in `src/DesktopLoudnessMeter.Web` | pass | Vitest: 6 files passed, 25 tests passed. |
| `npm run build` in `src/DesktopLoudnessMeter.Web` | pass | `tsc -b && vite build`; 1586 modules transformed; build succeeded. |
| `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` | pass with existing skips | 430 passed, 5 skipped, 0 failed, 435 total. Skips are existing UDP/sender socket-style tests and are not counted as runtime UDP evidence. |
| `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release` | pass | MSBuild succeeded, 0 warnings, 0 errors; `DesktopLoudnessMeter.Native.dll` up-to-date/built. |
| `git diff --stat -- src tests docs .aiknowledge` | pass | No output: this audit change has not edited product source, tests, docs, or knowledge files. |
| `git diff --check` | pass | No whitespace/error output. |
| `git status --short --branch` | pass | Branch `codex/2026-07-01-completion-audit-refresh`; only untracked current change artifacts before this evidence update. |
| `rg -n "window\.webkit|window\.chrome|HostBridge v1|unavailable|WKWebView|WebView2" ...` | pass for evidence collection | Confirms Windows WebView2 channel and macOS boundary; also confirms Web transport is still `window.chrome.webview` only and Swift bridge placeholder returns `unavailable`. |

### Task 3 - Current Verdict And Next Change

final_verdict: `not_complete`

Goal completion decision:
- `update_goal complete` is not allowed.
- The final architecture is materially closer than the stale `2026-06-30-completion-audit` indicated, but final-state requirements 8, 9, 10, 11, 14, and 15 still have incomplete or weak evidence.

Remaining gaps:
- High: `host-glue-boundary-reduction` - make Host stop owning non-glue state and orchestration where the final architecture assigns it to C++ Core.
- High: `native-host-aggregate-snapshot-core` or equivalent - move aggregate host snapshot/state projection out of C# Host orchestration or define a native-owned authoritative boundary that Host only marshals.
- Medium/high: `native-core-state-machine-boundary` - prove selected source/runtime/workspace command state ownership is native-backed, with Host applying only platform side effects.
- Medium: `web-workflow-parity-finalization` - verify or implement remaining WPF-oracle workflows not covered by settings/chart work, such as alias/source ordering/layout import-export/popup detail if still required by current UX scope.
- Medium/high: `macos-runner-implementation` - add Web-side WKWebView transport, Swift HostBridge runner implementation, macOS build/smoke, and then platform capture/package follow-ups.

Recommended next implementation:
- Start `host-glue-boundary-reduction` as the next OpenSpec change, scoped narrowly to one closed loop:
  1. audit `DesktopMeterHostController` responsibilities,
  2. choose the smallest native-owned aggregate state/snapshot slice,
  3. move that slice behind C++ Core + managed wrapper,
  4. keep HostBridge DTOs stable,
  5. prove Host responsibility is reduced by tests and codemap updates.

Verification method for this task:
- `rg -n "final_verdict|Goal completion decision|Recommended next implementation" openspec\changes\2026-07-01-completion-audit-refresh\evidence.md`

## Verify

2026-07-01T17:18:28+08:00 verify: pass

Scope:
- Change root: `openspec/changes/2026-07-01-completion-audit-refresh`
- Branch: `codex/2026-07-01-completion-audit-refresh`
- HEAD: `5d82929d1dd85525baa43b76c4d26c672a66b4e0`
- Diff state at verification: current change artifacts are untracked before gate update; no product/source/test/docs/knowledge diff.

Spec compliance:
- [R1] Final-state matrix is present and grades all 15 user final-state requirements.
- [R2] Old `2026-06-30-completion-audit` gap map is rechecked item by item.
- [R3] Fresh verification ledger records Web test/build, .NET test, native Windows build, scope hygiene, and macOS/UDP limits.
- [R4] Current remaining gaps are listed with priority.
- [R5] Lifecycle guard is explicit: `update_goal complete` is not allowed.

Verification commands:
- `npm test` in `src/DesktopLoudnessMeter.Web`: pass, 6 files / 25 tests.
- `npm run build` in `src/DesktopLoudnessMeter.Web`: pass.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj`: pass with existing skips, 430 passed / 5 skipped / 0 failed.
- `powershell -ExecutionPolicy Bypass -File src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release`: pass, MSBuild succeeded with 0 warnings / 0 errors.
- `git diff --stat -- src tests docs .aiknowledge`: pass, no output.
- `git diff --check`: pass.
- `rg -n "\[ \]" openspec\changes\2026-07-01-completion-audit-refresh`: exit 1, no open checkboxes.
- `rg -n "Requirements Matrix|Old Gap Recheck|Remaining gaps|final_verdict|Goal completion decision|Recommended next implementation|Verification Ledger" openspec\changes\2026-07-01-completion-audit-refresh\evidence.md`: pass, required sections present.

Findings:
- No blocking verify findings for this audit change.
- The audit verdict remains `not_complete`; this is the expected result and is not a verify failure.

Decision:
- `gates.verify.status: pass`
- Route to `opsx-review`.

## Risk Review

2026-07-01T17:20:36+08:00 review: pass

Review Scope Snapshot:
- Change root: `openspec/changes/2026-07-01-completion-audit-refresh`
- Branch: `codex/2026-07-01-completion-audit-refresh`
- HEAD: `5d82929d1dd85525baa43b76c4d26c672a66b4e0`
- Reviewed diff: explicitly owned untracked OpenSpec change artifacts under `openspec/changes/2026-07-01-completion-audit-refresh/**`.
- Excluded: product source, tests, docs, and `.aiknowledge`; `git diff --stat -- src tests docs .aiknowledge` had no output.

Checks:
- `gates.verify.status` is `pass`, so review can run.
- `rg -n "\[ \]|TODO|undecided|<path>|<command>|<!--" openspec\changes\2026-07-01-completion-audit-refresh`: exit 1, no open checkbox/template residue.
- `rg -n "status: pass|verify: pass|review: pass|archive: pass|not_complete|update_goal complete|Remaining gaps" openspec\changes\2026-07-01-completion-audit-refresh`: exit 0, confirms pass gates so far and explicit `not_complete` verdict.
- `git diff -- openspec\changes\2026-07-01-completion-audit-refresh`: no output because files are untracked before archive/stage; reviewed via `git ls-files --others --exclude-standard` and file reads.

Findings:
- CRITICAL: none.
- WARNING: none.
- SUGGESTION: none.

Risk conclusion:
- This is an audit-only change with no product behavior diff.
- No VERIFY_DRIFT observed: verify pass matches completed tasks and evidence.
- The not-complete goal verdict is intentional and supported by the matrix.

Decision:
- `gates.review.status: pass`
- Route to `opsx-archive`.

## Archive

2026-07-01T17:21:54+08:00 archive: pass

Archive summary:
- Source path: `openspec/changes/2026-07-01-completion-audit-refresh`
- Archive path: `openspec/changes/archive/2026-07-01-completion-audit-refresh`
- Spec files: 1
- Gates: readiness pass, verify pass, review pass, archive pass.
- Final verdict: `not_complete`; overall goal must remain active.

Knowledge status:
- pre_knowledge_status: clean for `.aiknowledge/` before this archive follow-up.
- post_knowledge_status: `.aiknowledge/codemap/ui-runtime-architecture.md` and `.aiknowledge/logs/2026-07.md` modified.
- diff_stat: 2 files changed, 13 insertions(+), 3 deletions(-).
- Meaning: codemap now records that old settings/theme and Protocol v1 receiver decode/ack gaps are closed, while Host glue-only, aggregate snapshot/state projection, and macOS runtime remain current blockers.
