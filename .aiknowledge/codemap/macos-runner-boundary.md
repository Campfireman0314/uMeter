---
status: active
created_at: 2026-06-30
created_from: change:2026-06-30-macos-readiness
last_verified_at: 2026-06-30
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
applies_to:
  - src/DesktopLoudnessMeter.MacHost
  - docs/platform/macos-readiness.md
source_refs:
  - change:2026-06-30-macos-readiness
  - evidence:openspec/changes/archive/2026-06-30-macos-readiness/evidence.md
superseded_by:
merged_from:
deprecated_reason:
---

# macOS Runner Boundary

## Responsibility

Records the reserved macOS host boundary for the future uMeter WKWebView runner. This module is a readiness skeleton and platform matrix, not a supported macOS app claim.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.MacHost/Package.swift | SwiftPM build entry for the future `uMeterMacHost` runner probe on macOS |
| src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/AppDelegate.swift | Programmatic AppKit entry that creates the Mac host window controller |
| src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/MacHostWindowController.swift | WKWebView host skeleton, `hostBridge` script message handler, file asset loading, and response event dispatch |
| src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/HostBridgeHandling.swift | Placeholder HostBridge v1 JSON request/response boundary with request id echo for future smoke tests |
| src/DesktopLoudnessMeter.MacHost/Sources/UMeterMacHost/MacWebAssetLocator.swift | Web asset root resolution, including `UMETER_WEBVIEW_DIST_DIR` override and future bundle `Web/` resources |
| docs/platform/macos-readiness.md | Fact matrix and follow-up gates for WKWebView runtime, native CMake build, capture permissions, packaging, signing, and notarization |

## Implicit Constraints

- Do not treat `src/DesktopLoudnessMeter.MacHost/**` as macOS runtime support. It is a skeleton that reserves the WKWebView/HostBridge boundary.
- The Web frontend transport supports the reserved WKWebView send path (`window.webkit.messageHandlers.hostBridge.postMessage`) and reuses the existing Web message response channel. A future `macos-runner-implementation` change must still implement/verify the Swift HostBridge runtime, macOS build/smoke, capture permissions, packaging, signing, and notarization before claiming macOS UI runtime support.
- Real macOS native build evidence still requires `cmake --preset macos-release` and `cmake --build --preset macos-release` on macOS or macOS CI.
- macOS capture adapter, permission prompts, app bundle resources, dylib payload, signing, notarization, and distribution packaging are separate platform glue follow-ups.
- The skeleton must stay out of the Windows solution and must not change HostBridge DTO semantics, Windows WebView2 startup, Web runtime behavior, or native C ABI.
