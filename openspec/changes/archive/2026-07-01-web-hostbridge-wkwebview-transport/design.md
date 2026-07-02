# Design: Web HostBridge WKWebView Transport

Text is ASCII to avoid terminal encoding ambiguity.

## Context

The Windows WebView2 shell already uses HostBridge v1 over `window.chrome.webview`. The macOS readiness skeleton reserves a WKWebView host boundary using `window.webkit.messageHandlers.hostBridge.postMessage(...)`, and returns placeholder responses by dispatching a `MessageEvent` on `window`. The Web frontend currently detects only `window.chrome.webview`, leaving a concrete Web-side adapter gap before macOS runner work can claim HostBridge transport parity.

## Goals

- Add WKWebView send support to the existing Web transport.
- Preserve Windows WebView2 send/listen behavior.
- Preserve HostBridge v1 request/response DTOs and typed command bus.
- Keep macOS runtime support explicitly incomplete.

## Non-Goals

- No Swift HostBridge core implementation.
- No macOS app support claim.
- No C# HostBridge DTO changes.
- No Native/Core or Windows host changes.

## Requirements Trace

- [R1] -> [U1]
- [R2] -> [U1]
- [R3] -> [U1]
- [R4] -> [U1]
- [R5] -> [U2]

## Implementation Units

### [U1] Web transport host adapter

- Requirements: [R1], [R2], [R3], [R4]
- Module Boundary:
  - `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts`
  - `src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.test.ts`
- Design:
  - Keep `WebViewMessageTransport` as the public transport class used by `App.tsx`.
  - Introduce an internal host shape with `postMessage`, `addEventListener`, and `removeEventListener`.
  - Adapt Windows WebView2 directly from `window.chrome.webview`.
  - Adapt WKWebView by using `window.webkit.messageHandlers.hostBridge.postMessage` for sends and `window.addEventListener("message", ...)` for responses.
  - Prefer Windows WebView2 when both APIs are present, because that preserves current Windows behavior in hybrid or test environments.
- Verification:
  - Focused Vitest coverage for Windows send, WK send, id correlation, and no-host availability.

### [U2] Boundary knowledge and lifecycle guard

- Requirements: [R5]
- Module Boundary:
  - `.aiknowledge/codemap/ui-runtime-architecture.md`
  - `.aiknowledge/codemap/webview-shell.md`
  - `.aiknowledge/codemap/macos-runner-boundary.md`
  - `.aiknowledge/logs/2026-07.md`
  - `openspec/changes/2026-07-01-web-hostbridge-wkwebview-transport/evidence.md`
- Design:
  - Update codemap statements from "Web adapter missing" to "Web adapter implemented, macOS runtime still follow-up".
  - Keep `final_verdict: not_complete`.
- Verification:
  - Grep for stale language that says the Web frontend has not implemented a WKWebView transport adapter.
  - Grep for updated language that preserves macOS runtime limitations.

## Decisions

1. Use one public transport class rather than splitting `ChromeWebViewTransport` and `WKWebViewTransport`.
   - Reason: HostBridgeClient does not need to know which native WebView is under it; the protocol remains HostBridge v1 JSON.
2. Reuse the existing `message` event response parser for WKWebView.
   - Reason: current Swift skeleton dispatches response JSON through `window.dispatchEvent(new MessageEvent('message', ...))`.
3. Prefer Windows WebView2 when both APIs exist.
   - Reason: this preserves the current Windows runtime and avoids changing behavior in WebView2.

## Risks

- A future real WKWebView runner might choose a different response callback shape. Mitigation: keep this adapter aligned with the current skeleton and cover only the existing boundary.
- Tests run in jsdom on Windows, not in macOS WKWebView. Mitigation: mark runtime support as follow-up and avoid parity claims.
- Global `window` mutation in tests can leak. Mitigation: test cleanup restores host globals.

## Rollback

Revert `webViewTransport.ts`, related tests, codemap updates, and this OpenSpec change. Windows WebView2 behavior should fall back to the previous chrome-only transport.
