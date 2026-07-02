---
status: active
created_at: 2026-06-29
created_from: change:2026-06-29-ui-runtime-architecture-replan
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
applies_to:
  - openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan
  - openspec/changes/archive/2026-06-29-cross-platform-architecture-plan
  - openspec/changes/archive/2026-06-29-host-snapshot-command-contract
  - openspec/changes/archive/2026-06-29-webview-windows-app-shell
  - src/DesktopLoudnessMeter.App/Host
  - src/DesktopLoudnessMeter.App/WebView
  - src/DesktopLoudnessMeter.Web
  - src/DesktopLoudnessMeter.Native
  - src/DesktopLoudnessMeter.MacHost
superseded_by:
source_refs:
  - change:2026-06-29-ui-runtime-architecture-replan
  - change:2026-06-29-webview-windows-app-shell
  - change:2026-06-30-macos-readiness
  - change:2026-06-30-completion-audit
  - change:2026-07-01-completion-audit-refresh
  - evidence:openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/evidence.md
  - evidence:openspec/changes/archive/2026-06-29-webview-windows-app-shell/evidence.md
  - evidence:openspec/changes/archive/2026-06-30-macos-readiness/evidence.md
  - evidence:openspec/changes/archive/2026-06-30-completion-audit/evidence.md
  - evidence:openspec/changes/archive/2026-07-01-completion-audit-refresh/evidence.md
---

# UI Runtime Architecture

## Responsibility

Tracks the current cross-platform target architecture: WebView UI + C++ Core. The archived `2026-06-29-cross-platform-architecture-plan` remains historical context, but its Flutter UI runtime route has been superseded by `2026-06-29-ui-runtime-architecture-replan`.

## Key Files

| File | Role |
|------|------|
| openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/work.md | WebView runtime decision, follow-up top-level changes, scope, non-goals, and lifecycle guard |
| openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/design.md | WebView2/WKWebView boundary, HostBridge v1 layering, Web frontend split, layer responsibility matrix, and WPF fallback policy |
| openspec/changes/archive/2026-06-29-host-snapshot-command-contract/design.md | Existing UI-neutral host snapshot/command/result vocabulary that WebView HostBridge v1 should consume above the native message transport |
| openspec/changes/archive/2026-06-29-webview-windows-app-shell/evidence.md | Windows WebView2 shell implementation, native message transport, React/Vite shell, manual smoke, verify/review/archive evidence |
| openspec/changes/archive/2026-06-29-webview-packaging-startup-tray/evidence.md | Archived implementation that makes Windows startup/tray/secondary activation WebView-primary and validates publish/installer Web/native payloads |
| .aiknowledge/codemap/webview-shell.md | Current implemented Windows WebView shell module map |
| .aiknowledge/codemap/macos-runner-boundary.md | Reserved macOS WKWebView runner boundary and readiness gates |
| .aiknowledge/codemap/host-contract.md | Current host contract module entry and constraints |
| .aiknowledge/codemap/native-dsp.md | Current native DSP facts and the starting point for future C++ Core build boundary work |

## Implicit Constraints

- Future main UI work should use WebView, not Flutter. Windows uses WebView2; macOS currently has only the archived `macos-readiness` WKWebView skeleton and readiness matrix, not supported runtime parity.
- HostBridge v1 has three layers: native WebView message channel transport, typed command bus, and JSON DTO payloads.
- The macOS skeleton names `window.webkit.messageHandlers.hostBridge`; the Web frontend transport now supports both Windows `window.chrome.webview` and macOS `window.webkit.messageHandlers.hostBridge` send paths while reusing HostBridge v1 JSON DTOs and the existing Web message response channel.
- The Windows implementation now targets the WebView2 shell as primary startup, tray restore, and secondary activation path; WPF remains fallback/regression oracle until `wpf-retirement`.
- Published app and installer payloads must carry the Web frontend assets and native runtime DLL needed by the WebView-primary launch path.
- Web UI uses TypeScript + React + Vite. Ordinary UI uses DOM components; high-frequency charts are reserved for Canvas/WebGL or equivalent rendering in `webview-chart-parity`.
- WPF remains fallback and regression oracle until WebView parity is proven and `wpf-retirement` decides removal or isolation.
- A single module change is not the overall goal completion. Final goal completion requires the WebView UI + C++ Core architecture to land across the planned changes and pass a completion audit.
- `2026-07-01-completion-audit-refresh` returned `not_complete`. Do not mark the overall goal complete until a later completion audit proves every final-state requirement.
- The old `2026-06-30-completion-audit` high-priority gaps for `web-settings-workflow-parity` and native Protocol v1 receiver decode/ack are now closed by archived follow-up changes; do not repeat those modules unless a new scope gap is found.
- Current completion-blocking gaps are: `host-glue-boundary-reduction`, remaining native-owned core state/Host glue reduction, and final completion audit. Remaining cross-platform follow-up is `macos-runner-implementation`, including Swift HostBridge runtime implementation, macOS build/smoke evidence, capture/package/signing/notarization, and broader platform validation.
- Web workflow parity is improved for settings/theme/charts, but final WPF-oracle workflow parity is not fully proven; verify alias/source ordering/layout import-export/popup detail scope before retiring WPF completely.
