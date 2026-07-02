---
status: active
created_at: 2026-06-29
created_from: change:2026-06-29-webview-windows-app-shell
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
applies_to:
  - src/DesktopLoudnessMeter.App/WebView
  - src/DesktopLoudnessMeter.App/App.xaml.cs
  - src/DesktopLoudnessMeter.App/MainWindow.xaml
  - src/DesktopLoudnessMeter.Web
  - src/DesktopLoudnessMeter.MacHost
  - tests/DesktopLoudnessMeter.Tests/WebView
source_refs:
  - change:2026-06-29-webview-windows-app-shell
  - evidence:openspec/changes/archive/2026-06-29-webview-windows-app-shell/evidence.md
  - change:2026-06-29-webview-packaging-startup-tray
  - evidence:openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/evidence.md
  - change:2026-06-30-webview-chart-parity
  - evidence:openspec/changes/archive/2026-06-30-webview-chart-parity/evidence.md
  - change:2026-06-30-wpf-retirement
  - evidence:openspec/changes/archive/2026-06-30-wpf-retirement/evidence.md
  - change:2026-06-30-macos-readiness
  - evidence:openspec/changes/archive/2026-06-30-macos-readiness/evidence.md
  - change:2026-06-30-web-settings-workflow-parity
  - evidence:openspec/changes/archive/2026-06-30-web-settings-workflow-parity/evidence.md
  - change:2026-07-01-webview-native-ui-parity-redraw
  - evidence:openspec/changes/archive/2026-07-01-webview-native-ui-parity-redraw/evidence.md
superseded_by:
merged_from:
deprecated_reason:
---

# WebView Shell

## Responsibility

Provides the Windows WebView UI path: the WebView2 shell is the default startup, tray restore, and secondary-activation target. It loads the React/Vite frontend and talks to the host through HostBridge v1 over the native WebView message channel. The React/Vite shell owns a native-client-style DOM workflow plus Canvas-based selected-detail charts; WPF remains an internal fallback/regression oracle seam, but the normal tray menu, WebView host shell, and WPF chrome no longer expose WPF fallback as a user workflow.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs | WebView2 initialization, virtual host asset mapping, `WebMessageReceived` dispatch, and WebView disposal |
| src/DesktopLoudnessMeter.App/Infrastructure/AppWindowRouter.cs | Startup/tray/secondary-activation routing seam that makes WebView primary and WPF explicit fallback |
| src/DesktopLoudnessMeter.App/WebView/WebViewAssetLocator.cs | `UMETER_WEBVIEW_DEV_URL` override, packaged `https://app.umeter.local/index.html` URI, and output `Web/` directory resolution |
| src/DesktopLoudnessMeter.App/WebView/HostBridgeController.cs | JSON request dispatcher for snapshot/workflow commands plus global settings, chart display, module visibility, spectrum range, loudness clock, and collision settings commands |
| src/DesktopLoudnessMeter.App/WebView/HostBridgeEnvelope.cs | HostBridge v1 request/response/error and host snapshot/result DTO records, including settings `schemaVersion`, global settings command payloads, and chart/collision payloads |
| src/DesktopLoudnessMeter.App/WebView/HostBridgeDtoMapper.cs | Mapping from UI-neutral host records to JSON DTOs without WPF types, including versioned settings projection |
| src/DesktopLoudnessMeter.App/WebView/HostBridgeJson.cs | Camel-case JSON serialization/deserialization helper |
| src/DesktopLoudnessMeter.App/App.xaml.cs | Reuses the existing signal source service and workspace settings to create a WebView shell controller without duplicating capture services; owns internal WPF fallback creation and the normal tray menu |
| src/DesktopLoudnessMeter.App/MainWindow.xaml | WPF fallback/regression oracle; migration-era WebView button has been removed from normal chrome |
| src/DesktopLoudnessMeter.Web/src/bridge/hostBridge.ts | TypeScript DTO types and typed command client; `HostSettingsSnapshotDto` requires `schemaVersion` |
| src/DesktopLoudnessMeter.Web/src/bridge/webViewTransport.ts | Windows `window.chrome.webview` and macOS `window.webkit.messageHandlers.hostBridge` HostBridge v1 transport selection, request id correlation, and mockable transport boundary |
| src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/MacHostWindowController.swift | Reserved WKWebView host skeleton and `hostBridge` native message handler for future macOS runner work |
| docs/platform/macos-readiness.md | macOS readiness fact matrix, native CMake gate, and capture/package/signing follow-ups |
| src/DesktopLoudnessMeter.Web/src/AppShell.tsx | Native-style DOM shell for app chrome, status/global controls, source list, recent events, detail readouts, editable global settings workflow, Web theme token selection, and selected-detail chart module workflow |
| src/DesktopLoudnessMeter.Web/src/charts/ChartModuleCanvas.tsx | Canvas chart module renderer for waveform, spectrum, stereo, loudness clock, and frequency collision |
| src/DesktopLoudnessMeter.Web/src/charts/chartGeometry.ts | Testable Web chart geometry and short-lived loudness clock runtime history |
| src/DesktopLoudnessMeter.App/DesktopLoudnessMeter.App.csproj | WebView2 package reference plus repeatable frontend build/copy target for app output assets |
| tests/DesktopLoudnessMeter.Tests/WebView | DTO, command bus, chart command validation, and asset locator coverage |

## Implicit Constraints

- WebView is the Windows default startup and activation target after `webview-packaging-startup-tray`. After `wpf-retirement`, WPF remains compiled/tested as a regression oracle, but ordinary user-visible fallback controls must not appear in the tray menu, WebView host shell, or WPF chrome.
- HostBridge v1 is layered: WebView native message channel transport, typed command bus, then JSON DTO payloads. DTO mapping must not leak WPF primitives.
- Web settings/workspace DTOs require `schemaVersion`; the Web shell consumes the projected settings contract but does not own persisted schema migration.
- Web settings parity now includes an editable settings surface for `themeId`, `maxRecentEventEntries`, `eventOutputFolder`, and `showHiddenSources`. Commands travel through `setTheme`, `setRecentEventLimit`, `setEventOutputFolder`, and existing `setShowHiddenSources`, then UI state is refreshed from post-command snapshots.
- Web theme application is local to the React/CSS shell: known theme ids map to root `data-theme` values and CSS custom properties. HostBridge should persist canonical theme ids that the Web resolver recognizes; do not persist display labels or case variants.
- Packaged WebView loading uses WebView2 virtual host mapping from `https://app.umeter.local/` to the app output `Web/` directory; file URL loading was observed as unreliable for the Vite build.
- The frontend uses the mock bridge outside WebView and `WebViewMessageTransport` when either Windows `window.chrome.webview` or macOS `window.webkit.messageHandlers.hostBridge` is available. Windows WebView2 remains preferred when both APIs exist.
- The macOS skeleton uses `window.webkit.messageHandlers.hostBridge` at the native boundary, and the Web frontend now has the matching WKWebView send adapter. Full `macos-runner-implementation` remains a follow-up because the Swift bridge still returns placeholder responses and macOS build/capture/package/signing evidence is not complete.
- Web detail charts are Canvas-based inside the React shell. Normal workflow remains DOM-driven; high-frequency chart drawing stays inside `src/DesktopLoudnessMeter.Web/src/charts/**`.
- The Web shell's core screen intentionally follows the compact WPF/native client hierarchy: app chrome, status/global controls, source list, selected detail readouts, stacked chart modules, and recent/settings secondary surfaces. UI redraw work should preserve existing HostBridge command wiring instead of faking native-only source management actions that lack HostBridge support.
- Web chart renderers must consume per-source settings projected by HostBridge, such as spectrum floor/ceiling and loudness clock cycle. Do not hard-code display settings after they exist in `snapshot.settings.sources`.
- `App.xaml.cs` must reuse the already-created `ISignalSourceMeterService` for WebView host snapshots; creating a second service would duplicate UDP/capture ownership.
- Publish and installer payloads must include both `Web/index.html` + `Web/assets/*` and `DesktopLoudnessMeter.Native.dll`; otherwise the WebView-primary published app can fail before rendering.
