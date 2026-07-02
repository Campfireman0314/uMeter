# DesktopLoudnessMeter.MacHost

macOS readiness boundary for the future uMeter WKWebView runner.

This folder is intentionally not wired into the Windows solution or build. It records the host shape that a future macOS runner should implement:

- create a native `WKWebView`
- load the same React/Vite Web assets as the Windows WebView2 host
- expose HostBridge v1 through `window.webkit.messageHandlers.hostBridge.postMessage(...)`
- return JSON responses back into the Web document without changing HostBridge DTOs

This is not a supported macOS app bundle yet. Build, signing, capture permissions, and packaging are tracked by `docs/platform/macos-readiness.md`.
