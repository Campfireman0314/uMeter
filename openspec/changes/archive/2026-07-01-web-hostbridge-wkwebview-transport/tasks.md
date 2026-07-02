# Tasks: 2026-07-01-web-hostbridge-wkwebview-transport

Text is ASCII to avoid terminal encoding ambiguity.

## Default Constraints

**Forbidden Files**:
- `src/DesktopLoudnessMeter.App/**`
- `src/DesktopLoudnessMeter.Native/**`
- `src/DesktopLoudnessMeter.MacHost/**`
- `tests/DesktopLoudnessMeter.Tests/**`

**Shared Artifacts**:
- `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/evidence.md` - controller serial update
- `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/tasks.md` - controller serial update
- `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/.openspec.yaml` - gate owner only

**Worker Return Format**: DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED

## 1. Web transport adapter

- [x] 1.1 [test-first] Add WKWebView transport tests and implement adapter

  **Requirements Trace**: [R1, R2, R3, R4] -> [U1]
  **Allowed Files**:
  - `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts`
  - `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.test.ts`

  **Forbidden Files**:
  - Defaults apply.

  **Acceptance Criteria**:
  - [x] Existing Windows WebView2 send and response correlation behavior remains covered.
  - [x] WKWebView `window.webkit.messageHandlers.hostBridge.postMessage` send path is covered.
  - [x] `isWebViewMessageHostAvailable()` is true for Windows and WK hosts and false for no host.
  - [x] Response resolution still ignores unrelated response ids.

  **Verification Command / Method**:
  - `npm test -- webViewTransport` from `src/DesktopLoudnessMeter.Web`

  **Evidence Required**:
  - Record red/green test result or explain if red phase is skipped due to narrowly extending existing tests.

  **Shared Artifacts**:
  - Defaults apply.

  **Done When**:
  - Focused transport tests pass and Windows behavior remains covered.

  **Worker Return Format**:
  - Defaults apply.

## 2. Regression and build hardening

- [x] 2.1 [direct] Run Web test/build regression

  **Requirements Trace**: [R1, R2, R3, R4, R5] -> [U1, U2]
  **Allowed Files**:
  - `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/evidence.md`

  **Forbidden Files**:
  - Product source files unless Task 1.1 needs a within-scope fix.

  **Acceptance Criteria**:
  - [x] Full Web Vitest suite passes.
  - [x] Web TypeScript/Vite build passes.
  - [x] No HostBridge DTO files outside `webViewTransport.ts` are modified.

  **Verification Command / Method**:
  - `npm test` from `src/DesktopLoudnessMeter.Web`
  - `npm run build` from `src/DesktopLoudnessMeter.Web`
  - `git diff --stat -- src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts src/DesktopLoudnessMeter.App src/DesktopLoudnessMeter.MacHost src/DesktopLoudnessMeter.Native`

  **Evidence Required**:
  - Command results and boundary diff result.

  **Shared Artifacts**:
  - Defaults apply.

  **Done When**:
  - Web tests/build pass and boundary check shows no out-of-scope DTO/native/host changes.

  **Worker Return Format**:
  - Defaults apply.

## 3. Knowledge and final evidence

- [x] 3.1 [direct] Update codemap and close evidence

  **Requirements Trace**: [R5] -> [U2]
  **Allowed Files**:
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/macos-runner-boundary.md`
  - `.aiknowledge/logs/2026-07.md`
  - `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/evidence.md`

  **Forbidden Files**:
  - Defaults apply.

  **Acceptance Criteria**:
  - [x] Codemap records Web-side WKWebView transport support.
  - [x] Codemap still records macOS runtime/build/capture/packaging as follow-up.
  - [x] Evidence states this change does not complete the overall goal.

  **Verification Command / Method**:
  - `rg -n "WKWebView transport|window.webkit|macOS runtime|not_complete" .aiknowledge openspec\changes\2026-07-01-web-hostbridge-wkwebview-transport\evidence.md`
  - `git diff --check`

  **Evidence Required**:
  - Grep and diff-check results.

  **Shared Artifacts**:
  - Defaults apply.

  **Done When**:
  - Knowledge updates are consistent and final verdict remains `not_complete`.

  **Worker Return Format**:
  - Defaults apply.
