# Evidence: 2026-07-01-web-hostbridge-wkwebview-transport

Text is ASCII to avoid terminal encoding ambiguity.

## Plan Evidence

- `opsx changes list`: no active changes before creating this change.
- Dedicated branch created: `codex/2026-07-01-web-hostbridge-wkwebview-transport`.
- Change initialized:
  - `opsx changes init 2026-07-01-web-hostbridge-wkwebview-transport spec-driven --profile lean-change --source-type feature`
- Base HEAD: `b4c24e07110ddb3183c74a6d3d7ac1962a7d6882`.
- Read archived planning:
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/work.md`
  - `openspec/changes/archive/2026-06-29-cross-platform-architecture-plan/design.md`
  - `openspec/changes/archive/2026-07-01-completion-audit-refresh/evidence.md`
- Read codemap:
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/macos-runner-boundary.md`
  - `.aiknowledge/codemap/source-list-workspace.md`
- Source facts:
  - `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts` currently detects only `window.chrome.webview`.
  - `src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/MacHostWindowController.swift` receives `hostBridge` script messages and dispatches response JSON through a `window` `MessageEvent`.
  - `src/DesktopLoudnessMeter.MacHost/README.md` documents `window.webkit.messageHandlers.hostBridge.postMessage(...)` as the reserved macOS boundary.
- Inference:
  - The smallest useful next change is a Web-side WKWebView transport adapter with tests, not full macOS runner implementation.

## Readiness

2026-07-01T18:48:50+08:00 readiness: pass

Scope reviewed:
- `.openspec.yaml`
- `work.md`
- `specs/change/spec.md`
- `design.md`
- `tasks.md`
- `evidence.md`

Checks:
- `opsx changes resolve 2026-07-01-web-hostbridge-wkwebview-transport`: pass, resolved to `/e/gitlab/uMeter/openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport`.
- Trace grep found R1-R5 in specs, work/design/task references, and U1-U2 implementation units.
- Task contract grep found `Allowed Files`, `Forbidden Files`, `Evidence Required`, `Worker Return Format`, `Done When`, workspace rationale, and `not_complete` lifecycle guard.
- Template residue grep found only expected future evidence placeholders and the word `pending` inside the R3 requirement text; no blocking TODO/TBD/undecided placeholders were found.
- `git diff --check`: pass.
- `git status --short --branch`: branch `codex/2026-07-01-web-hostbridge-wkwebview-transport`; dirty scope is the current OpenSpec change only.

Conclusion:
- This is a single closed Web transport adapter slice.
- It does not authorize Swift/native/HostBridge DTO changes.
- It does not claim macOS runtime parity or overall goal completion.
- Implementation may proceed.

## Implementation Evidence

### Task 1.1 - Web transport adapter

Mode: `[test-first]`

Files changed:
- `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts`
- `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.test.ts`

Red evidence:
- `npm test -- webViewTransport` from `src/DesktopLoudnessMeter.Web`: failed as expected.
- Result: 3 passed / 1 failed.
- Failure: WKWebView host availability assertion expected true but got false.
- Interpretation: tests proved the existing transport only detected `window.chrome.webview`.

Implementation:
- Added `window.webkit.messageHandlers.hostBridge.postMessage` detection.
- Kept `window.chrome.webview` as the preferred host when both APIs exist.
- Adapted WKWebView sends to `hostBridge.postMessage(request)`.
- Reused the existing `message` event response listener and HostBridge request-id correlation.

Green evidence:
- `npm test -- webViewTransport` from `src/DesktopLoudnessMeter.Web`: pass, 4 passed / 0 failed.

Result:
- Task 1.1 complete.

### Task 2.1 - Web regression and boundary check

Mode: `[direct]`

Commands:
- `npm test` from `src/DesktopLoudnessMeter.Web`: pass, 6 files / 28 tests passed.
- `npm run build` from `src/DesktopLoudnessMeter.Web`: pass, `tsc -b && vite build` completed; 1586 modules transformed.
- `git diff --stat -- src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts src/DesktopLoudnessMeter.App src/DesktopLoudnessMeter.MacHost src/DesktopLoudnessMeter.Native`: pass, no output.

Boundary result:
- No HostBridge DTO, C# Host, Native, or Swift MacHost files were modified.
- Windows WebView2 behavior remains covered by focused tests and full Web tests.

Result:
- Task 2.1 complete.

### Task 3.1 - Knowledge and final evidence

Mode: `[direct]`

Files changed:
- `.aiknowledge/codemap/index.md`
- `.aiknowledge/codemap/ui-runtime-architecture.md`
- `.aiknowledge/codemap/webview-shell.md`
- `.aiknowledge/codemap/macos-runner-boundary.md`
- `.aiknowledge/logs/2026-07.md`

Updates:
- Codemap now records that `WebViewMessageTransport` supports Windows `window.chrome.webview` and macOS `window.webkit.messageHandlers.hostBridge` send paths.
- Codemap still records that macOS runtime/build/capture/packaging/signing/notarization remain follow-up work.
- July knowledge log records this as a Web adapter gap closure only.

Goal decision:
- final_verdict remains `not_complete`.
- This change does not implement macOS runtime parity and does not complete the full WebView UI + C++ Core architecture.

Result:
- Task 3.1 complete.

### Implement Amendment - codemap stale phrase cleanup

2026-07-01T18:55:59+08:00 classification: within-scope

Reason:
- Final stale-language grep found two residual codemap statements saying "Web transport adapter follow-up" after the Web adapter had been implemented.
- The stale statements were in `.aiknowledge/codemap/index.md` and `.aiknowledge/codemap/webview-shell.md`.

Action:
- Updated codemap index to name Web-side WKWebView transport support and remaining macOS runtime readiness gates.
- Updated WebView shell codemap's macOS docs row to remove the closed Web transport adapter follow-up.

Gate impact:
- Product source did not change.
- Verification/review/archive evidence was refreshed after this knowledge-only amendment.

## Verify

2026-07-01T18:55:59+08:00 verify: pass

Scope reviewed:
- Change root: `/e/gitlab/uMeter/openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport`
- Branch: `codex/2026-07-01-web-hostbridge-wkwebview-transport`
- HEAD before verification: `b4c24e07110ddb3183c74a6d3d7ac1962a7d6882`
- Current dirty scope belongs to this change: Web transport/test files, three codemap files, July knowledge log, and current OpenSpec artifacts.

Spec compliance:
- R1 is satisfied by tests proving Chrome WebView host availability and send path, with Windows WebView2 preferred when both hosts exist.
- R2 is satisfied by tests proving WKWebView `window.webkit.messageHandlers.hostBridge.postMessage` send path when Chrome WebView is unavailable.
- R3 is satisfied by existing and expanded tests proving unrelated response ids are ignored and matching request ids resolve the pending promise.
- R4 is satisfied by availability tests for Windows, WKWebView, and no-host fallback.
- R5 is satisfied by boundary diff checks and codemap/evidence statements preserving macOS runtime follow-up status and `not_complete` final verdict.

Fresh verification commands:
- `npm test -- webViewTransport` from `src/DesktopLoudnessMeter.Web`: pass, 1 file / 4 tests passed.
- `npm test` from `src/DesktopLoudnessMeter.Web`: pass, 6 files / 28 tests passed.
- `npm run build` from `src/DesktopLoudnessMeter.Web`: pass, `tsc -b && vite build`; 1586 modules transformed.
- `git diff --stat -- src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts src/DesktopLoudnessMeter.App src/DesktopLoudnessMeter.MacHost src/DesktopLoudnessMeter.Native`: pass, no output.
- Stale-language grep for old codemap claims that the WKWebView adapter is missing or still a Web transport adapter follow-up: exit 1, no matches.
- `rg -n "WKWebView transport|window.webkit|macOS runtime|not_complete" .aiknowledge openspec\changes\2026-07-01-web-hostbridge-wkwebview-transport\evidence.md`: pass, found updated Web transport support and runtime follow-up statements.
- `git diff --check`: pass. Git reported LF/CRLF conversion warnings only.
- `rg -n "\[ \]|TODO|TBD|undecided|Pending\." tasks.md evidence.md`: pass for tasks; only verify/review/archive placeholders existed before this verify update.

Manual/platform status:
- No manual UI smoke is required for this Web adapter change.
- macOS runtime smoke/build is not claimed; this change verifies Web transport behavior in jsdom and leaves real macOS runtime work to a later change.

Conclusion:
- Decision: pass.
- No allowed failures or manual follow-ups.
- Route next: `opsx-review`.

## Review

2026-07-01T18:56:10+08:00 review: pass

Review Scope Snapshot:
- Branch: `codex/2026-07-01-web-hostbridge-wkwebview-transport`
- HEAD: `b4c24e07110ddb3183c74a6d3d7ac1962a7d6882`
- Reviewed diff: current working-tree diff and untracked current OpenSpec change artifacts.
- Product files changed:
  - `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts`
  - `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.test.ts`
- Knowledge files changed:
  - `.aiknowledge/codemap/index.md`
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/macos-runner-boundary.md`
  - `.aiknowledge/logs/2026-07.md`
- Excluded/no-diff by boundary check: HostBridge DTOs, C# App/Host, Native, and Swift MacHost files.

Risk review:
- Windows WebView2 behavior remains preferred when both Windows and WK APIs exist, preserving current runtime behavior.
- WKWebView uses only the current skeleton boundary: send through `window.webkit.messageHandlers.hostBridge.postMessage` and receive through the existing `window` message event path.
- `dispose()` removes the same listener path that constructor registers for both host types.
- Tests restore mutated `window.chrome` and `window.webkit` globals after each case.
- No HostBridge DTO semantics changed.
- No macOS runtime support claim was introduced; codemap/evidence explicitly leave Swift runtime/build/capture/package/signing follow-ups open.

Findings:
- CRITICAL: none.
- WARNING: none.
- SUGGESTION: none.

Conclusion:
- Decision: pass.
- No VERIFY_DRIFT observed after the knowledge-only stale phrase cleanup.
- Route next: `opsx-archive`.

## Archive

2026-07-01T18:56:20+08:00 archive: pass

Archive summary:
- change: `2026-07-01-web-hostbridge-wkwebview-transport`
- archive_path: `openspec/changes/archive/2026-07-01-web-hostbridge-wkwebview-transport`
- schema: `spec-driven`
- specs count: 1 (`specs/change/spec.md`)
- readiness: pass
- verify: pass
- review: pass
- archive: pass

Pre-archive checks:
- `gates.verify.status`: pass at `2026-07-01T18:55:59+08:00`.
- `gates.review.status`: pass at `2026-07-01T18:56:10+08:00`.
- `tasks.md`: no unfinished task checkboxes.
- `evidence.md`: no unresolved DOC_DRIFT, final hardening gap, manual blocker, allowed failure, or pending blocker.
- archive destination did not exist before move.

Knowledge status:
- status: done
- mode: in-change codemap/log updates
- pre_knowledge_status: codemap stated that the Web-side WKWebView adapter was missing.
- post_knowledge_status: codemap now states that Web transport supports `window.webkit.messageHandlers.hostBridge`, while macOS runtime/build/capture/packaging/signing remain follow-up work.
- diff_stat:
  - `.aiknowledge/codemap/ui-runtime-architecture.md`: 4 changed lines
  - `.aiknowledge/codemap/webview-shell.md`: 6 changed lines
  - `.aiknowledge/codemap/macos-runner-boundary.md`: 2 changed lines
  - `.aiknowledge/codemap/index.md`: 1 changed line
  - `.aiknowledge/logs/2026-07.md`: 6 added lines

Final verdict:
- not_complete.
- This archived module closes the Web-side WKWebView HostBridge transport adapter gap only. The full WebView UI + C++ Core architecture still requires later Host glue/core-state reduction work, macOS runtime/build/capture/packaging evidence, and a final completion audit.

## Final Gate Summary

- final_readiness:
  - status: pass
  - evidence_ref: `## Readiness`
- final_verify:
  - status: pass
  - evidence_ref: `## Verify`
- final_review:
  - status: pass
  - evidence_ref: `## Review`
- final_archive:
  - status: pass
  - evidence_ref: `## Archive`
  - archive_path: `openspec/changes/archive/2026-07-01-web-hostbridge-wkwebview-transport`
- final_verdict:
  - status: not_complete
  - reason: this change only closes the Web-side WKWebView HostBridge transport adapter gap and does not implement macOS runtime/build/capture/packaging or the full WebView UI + C++ Core architecture.
- allowed_failures: []
- manual_followups: []
- knowledge_status:
  - status: done
  - evidence_ref: `## Archive`
