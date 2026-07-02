# Spec: Web HostBridge WKWebView Transport

Text is ASCII to avoid terminal encoding ambiguity.

## Requirements

### Requirement: Windows WebView2 Transport Is Preserved

When the Web frontend runs inside Windows WebView2 and `window.chrome.webview` exists, the HostBridge transport shall send requests through `window.chrome.webview.postMessage`.

**Trace**: R1

#### Scenario: Chrome WebView host exists

- Given `window.chrome.webview.postMessage` is available
- When the Web app creates a default WebView HostBridge transport
- Then `isWebViewMessageHostAvailable()` returns `true`
- And requests are posted to `window.chrome.webview.postMessage`

### Requirement: WKWebView Transport Is Supported

When the Web frontend runs inside macOS WKWebView and `window.webkit.messageHandlers.hostBridge.postMessage` exists, the HostBridge transport shall send requests through that WKWebView message handler if the Windows WebView2 host is unavailable.

**Trace**: R2

#### Scenario: WKWebView host exists without Chrome WebView host

- Given `window.webkit.messageHandlers.hostBridge.postMessage` is available
- And `window.chrome.webview` is unavailable
- When the Web app creates a default WebView HostBridge transport
- Then `isWebViewMessageHostAvailable()` returns `true`
- And requests are posted to `window.webkit.messageHandlers.hostBridge.postMessage`

### Requirement: Response Correlation Uses HostBridge Request Id

The transport shall resolve only the pending request whose `id` matches the response received from the Web message response channel.

**Trace**: R3

#### Scenario: Matching response arrives after unrelated response

- Given a pending request with id `request-1`
- When a response for id `other` arrives
- And a response for id `request-1` arrives
- Then only the `request-1` pending promise resolves

### Requirement: Availability Detection Covers Both Native Hosts

The Web app shall choose the real WebView transport when either supported native host exists and shall fall back to the mock bridge when neither exists.

**Trace**: R4

#### Scenario: No native host exists

- Given neither Windows WebView2 nor WKWebView HostBridge APIs exist
- When `isWebViewMessageHostAvailable()` is called
- Then it returns `false`

### Requirement: DTO And Runtime Claims Remain Stable

This change shall not alter HostBridge request/response DTO semantics and shall not claim macOS runtime parity.

**Trace**: R5

#### Scenario: Change boundary is reviewed

- Given this change is complete
- When the diff and codemap updates are reviewed
- Then HostBridge DTO files outside `webViewTransport.ts` are unchanged
- And codemap records that macOS runtime support remains a follow-up beyond the Web-side transport adapter
