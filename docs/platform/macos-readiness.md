# macOS Readiness

This document records the current macOS boundary for uMeter. It is a readiness artifact, not a support claim.

## Fact Matrix

| Area | Status | Evidence | Follow-up |
|------|--------|----------|-----------|
| Windows WebView UI | fact | Windows WebView2 shell is archived and WebView-primary. | Keep as regression baseline. |
| macOS runner | boundary | `src/DesktopLoudnessMeter.MacHost/**` sketches a SwiftPM WKWebView host and `hostBridge` message handler. | `macos-runner-implementation` must run `swift build`, create an app bundle, and run a smoke test. |
| HostBridge DTO | fact | Existing HostBridge v1 uses JSON DTO request/response payloads. | macOS transport should preserve DTOs and only replace native message channel plumbing. |
| Web transport adapter | follow-up | Current React transport detects `window.chrome.webview`; the skeleton documents the macOS native channel as `window.webkit.messageHandlers.hostBridge`. | `macos-runner-implementation` must add or verify the Web-side WKWebView adapter before claiming macOS UI runtime support. |
| Native C++ Core macOS build | unknown | `src/DesktopLoudnessMeter.Native/CMakePresets.json` contains `macos-release`; current Windows session has no CMake or Darwin toolchain evidence. | On macOS run `cmake --preset macos-release` and `cmake --build --preset macos-release`. |
| Capture adapter | unknown | Current app capture uses Windows WASAPI/NAudio. | `macos-capture-adapter` must choose and verify CoreAudio, ScreenCaptureKit, or another permission-safe capture path. |
| Web assets packaging | unknown | Windows publish copies `Web/` assets. | macOS packaging must include `Web/index.html` and `Web/assets/*` in the app resources. |
| Native runtime packaging | unknown | Windows payload carries `DesktopLoudnessMeter.Native.dll`; native copy target pitfall applies to final process payloads. | macOS packaging must place the native dylib where the runner/Core loader can load it. |
| Signing/notarization | unknown | No entitlements, signing, notarization, or installer artifact exists. | `macos-packaging` must define entitlements, signing identity, notarization, and distribution format. |

## WKWebView Boundary

The macOS host should expose HostBridge v1 through the native WKWebView message channel:

```text
React/Vite UI
  -> window.webkit.messageHandlers.hostBridge.postMessage(requestJson)
  -> WKScriptMessageHandler
  -> HostBridge v1 command bus
  -> JSON response
  -> Web document message event or equivalent callback
```

The skeleton currently uses `WKScriptMessageHandler`, a `hostBridge` message handler name, file-based Web asset loading, and `evaluateJavaScript` to return a JSON response. A production runner can replace the placeholder bridge implementation without changing HostBridge DTO semantics.

The current Web frontend transport remains Windows WebView2-oriented (`window.chrome.webview`). The WKWebView host boundary is therefore not a macOS runtime pass by itself; the follow-up runner implementation must add or verify the matching Web-side adapter.

On macOS or macOS CI, the skeleton build probe is:

```sh
cd src/DesktopLoudnessMeter.MacHost
swift build
```

## Native Core Gate

Run these on macOS or macOS CI:

```sh
cmake --preset macos-release
cmake --build --preset macos-release
```

Expected outcome:

- CMake configures the `DesktopLoudnessMeter.Native` source set.
- A macOS dynamic library is produced with the current C ABI exports.
- The future macOS runner package carries the native runtime beside or inside the final app bundle.

Current Windows session result:

- `cmake --version` is unavailable.
- `C:\Program Files\CMake\bin\cmake.exe` is absent.
- `where.exe cmake` finds no CMake executable.

## Capture And Permission Gate

macOS capture is platform glue, not HostBridge DTO logic and not C++ Core DSP logic. The future capture adapter must own:

- input/output device enumeration
- app/process/system audio capture feasibility
- microphone/system audio permissions
- lifecycle handoff into the existing source ingest/core snapshot path

Candidate APIs such as CoreAudio or ScreenCaptureKit require a macOS environment and permission smoke test before implementation scope can be declared complete.

## Packaging Gate

The future macOS package must verify:

- `.app` bundle structure
- `Web/` resource payload
- native dylib payload and load path
- app icon/resources
- entitlements
- code signing
- notarization
- distribution format such as DMG or pkg

## Manual Follow-ups

- `[->]` macOS CMake configure/build: target `macos-runner-implementation` or macOS CI/toolchain change.
- `[->]` WKWebView runner smoke: target `macos-runner-implementation`.
- `[->]` macOS capture permission/capability smoke: target `macos-capture-adapter`.
- `[->]` macOS packaging/signing/notarization smoke: target `macos-packaging`.
