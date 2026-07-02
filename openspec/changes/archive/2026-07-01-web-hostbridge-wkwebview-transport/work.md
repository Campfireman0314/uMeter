# Work Packet: 2026-07-01-web-hostbridge-wkwebview-transport

Text is ASCII to avoid terminal encoding ambiguity. Paths, commands, status values, and trace IDs stay literal.

## Metadata

- profile: lean-change
- source_type: feature
- status: drafting
- created: 2026-07-01

## Slicing Decision

### Overall

- Decision: KEEP_ONE_CHANGE
- Reason: this is one closed Web frontend transport slice. It adds/validates a WKWebView `window.webkit.messageHandlers.hostBridge` sender adapter while preserving the existing Windows WebView2 `window.chrome.webview` path and the HostBridge v1 JSON DTO contract.
- OpenSpec lower-bound check: pass. The change closes a named macOS/Web transport gap from codemap/completion-audit evidence and is independently testable in TypeScript without requiring a full macOS runtime.

### Capability Clusters

| Cluster | User value | Main modules | Independently deliverable | Notes |
|---------|------------|--------------|----------------------------|-------|
| Web transport selection | Lets the same Web UI send HostBridge v1 requests from Windows WebView2 or macOS WKWebView | `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts` | yes | Keeps response handling on the existing `window` message event |
| Transport regression tests | Proves request/response correlation and platform selection remain stable | `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.test.ts` | yes | Runs under Vitest on Windows |
| Boundary knowledge | Prevents over-claiming macOS runtime support | codemap/OpenSpec evidence | yes | macOS host remains a skeleton |

## Exploration Summary

Self-check / gap scan:

- Candidate recommendation: implement Web-side WKWebView HostBridge transport support.
- Round 1 scan: blocking = this does not make the macOS runner production-ready and must not be framed as macOS parity. non-blocking = it closes a concrete adapter gap named in `ui-runtime-architecture`, `webview-shell`, and `macos-runner-boundary`.
- Revised candidate recommendation: scope the change to Web transport adapter and tests only; leave Swift placeholder bridge, macOS native build, capture, packaging, signing, and notarization out of scope.
- Round 2 scan: blocking = none after scope narrowing. non-blocking = current Swift skeleton dispatches responses through `window.dispatchEvent(new MessageEvent('message', ...))`, so the Web transport can reuse the existing response listener.
- Most likely error: changing HostBridge DTO semantics or claiming macOS runtime support from Web-only tests.
- Boundary handling: encode non-goals and final verdict in this work packet, specs, tasks, and evidence.
- Evidence vs inference: source/codemap prove current Web transport is `window.chrome.webview` only and macOS skeleton exposes `window.webkit.messageHandlers.hostBridge`; the adapter shape is an implementation inference to verify with tests.
- Final scan conclusion: no new critical fact blocks planning. Proceed to plan/readiness.

## Intent

Add the Web frontend transport boundary needed for a future macOS WKWebView runner: when `window.webkit.messageHandlers.hostBridge.postMessage` exists, the React/Vite HostBridge client can use it to send HostBridge v1 JSON requests and receive responses through the existing window `message` event.

## Scope

### In Scope

- Extend Web HostBridge transport detection to recognize Windows WebView2 and macOS WKWebView hosts.
- Preserve Windows `window.chrome.webview` behavior and response parsing.
- Add Vitest coverage for WKWebView postMessage sender selection, request id correlation, availability detection, and fallback behavior.
- Update codemap/evidence to state that the Web-side WKWebView adapter is implemented while macOS runtime remains boundary-only.

### Out Of Scope

- No Swift HostBridge core implementation beyond the existing skeleton.
- No macOS build/runtime/capture/packaging/signing/notarization support claim.
- No HostBridge DTO shape changes.
- No Windows WebView2 startup, tray, packaging, or native host changes.
- No `update_goal complete`.

## Success Criteria

- `WebViewMessageTransport` can send through Windows WebView2 or macOS WKWebView host APIs.
- Response correlation by request id works for WKWebView using the existing window message response channel.
- Existing Windows WebView2 transport tests keep passing.
- `App.tsx` uses the WebView transport when either supported native message host is available.
- Codemap no longer states the Web-side WKWebView adapter is missing, but still records macOS runtime as not complete.

## Requirements Summary

- [R1] The Web HostBridge transport must choose Windows WebView2 when `window.chrome.webview` is available.
- [R2] The Web HostBridge transport must choose macOS WKWebView when `window.webkit.messageHandlers.hostBridge.postMessage` is available and Windows WebView2 is unavailable.
- [R3] The transport must resolve responses by HostBridge request id from the existing Web message response channel.
- [R4] Availability detection must return true when either supported native host API exists and false when neither exists.
- [R5] The change must not alter HostBridge DTO semantics or claim macOS runtime parity.

## Approach

Refactor `webViewTransport.ts` around a small internal send/listen host abstraction:

- Windows sender: `window.chrome.webview.postMessage(request)`.
- macOS sender: `window.webkit.messageHandlers.hostBridge.postMessage(request)`.
- Response listener: the existing `message` event path, using `chrome.webview.addEventListener` on Windows and `window.addEventListener` for WKWebView.

The macOS skeleton already dispatches response JSON to `window` as a `MessageEvent`, so the Web transport does not need a second response protocol.

## Files / Ownership

Allowed:

- `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts`
- `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.test.ts`
- `.aiknowledge/codemap/ui-runtime-architecture.md`
- `.aiknowledge/codemap/webview-shell.md`
- `.aiknowledge/codemap/macos-runner-boundary.md`
- `.aiknowledge/logs/2026-07.md`
- `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/**`

Forbidden:

- `src/DesktopLoudnessMeter.App/**`
- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.MacHost/**`
- `tests/DesktopLoudnessMeter.Tests/**`
- HostBridge DTO files unless implementation discovers a true DTO bug and routes back to plan.

## Git / Workspace

- Workspace Strategy: `current-worktree`
- Workspace Path: `E:\gitlab\uMeter`
- Workspace Branch: `codex/2026-07-01-web-hostbridge-wkwebview-transport`
- Base Branch: `codex/2026-07-01-native-host-selected-detail-snapshot-core`
- Base HEAD: `b4c24e07110ddb3183c74a6d3d7ac1962a7d6882`
- Merge Target: user-owned integration branch
- Workspace Rationale:
  - Dirty classification: before init, the tree was clean. After init, dirty files are only `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/**`, owned by this change.
  - Chained decision: this change is based on the latest completed native-host selected-detail snapshot branch and continues the active goal from that checkpoint.
  - Isolated trigger: no isolated worktree needed. The change is single-agent, file-limited to Web transport tests/docs, and the workspace was clean.
  - Conclusion: `current-worktree`.

## Verification Plan

- `opsx changes list`
- `opsx changes resolve 2026-07-01-web-hostbridge-wkwebview-transport`
- `npm test -- webViewTransport` in `src/DesktopLoudnessMeter.Web`
- `npm test` in `src/DesktopLoudnessMeter.Web`
- `npm run build` in `src/DesktopLoudnessMeter.Web`
- `rg` checks for Windows/WK transport references and stale codemap gap language
- `git diff --check`
- `git status --short --branch`

## Rollback / Escalation

- Rollback: revert the Web transport/test/codemap/OpenSpec files from this change.
- Escalation: if WKWebView support requires changing HostBridge DTOs or Swift bridge behavior, stop implementation and route back to plan with a larger macOS runner change.

## Rejected Attempts

- Do not implement the full macOS runner here: that would include Swift host behavior, native build, capture, packaging, and signing and is too large for this slice.
- Do not duplicate `HostBridgeClient`: transport should stay under the existing typed command bus and DTO contract.
- Do not treat this as `macos-readiness` completion: it is only the Web-side adapter piece.

## Readiness Notes

- Readiness must verify R1-R5 trace through specs/design/tasks.
- Readiness must verify tasks are file-limited and do not authorize Swift/native/HostBridge DTO changes.
- Readiness must verify the change does not claim overall goal completion.
