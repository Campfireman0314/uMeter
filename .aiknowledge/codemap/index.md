# Codemap

## Modules

| Module | Status | Last Verified | Responsibility | Entry |
|--------|--------|---------------|----------------|-------|
| [app-startup](app-startup.md) | active | 2026-06-30 | Windows WebView startup, single-instance ownership, tray lifecycle, WPF fallback, and activation replay | src/DesktopLoudnessMeter.App/App.xaml.cs |
| [ui-runtime-architecture](ui-runtime-architecture.md) | active | 2026-06-30 | WebView UI + C++ Core target architecture, HostBridge v1 boundary, WPF fallback policy, and follow-up change map | openspec/changes/archive/2026-06-29-ui-runtime-architecture-replan/design.md |
| [webview-shell](webview-shell.md) | active | 2026-07-01 | Windows WebView2 primary shell, HostBridge v1 native message transport, native-style React/Vite frontend shell, Canvas detail charts, publish assets, and WPF fallback boundary | src/DesktopLoudnessMeter.App/WebView/WebViewShellWindow.xaml.cs |
| [macOS runner boundary](macos-runner-boundary.md) | active | 2026-07-01 | Reserved macOS WKWebView host skeleton, HostBridge v1 native message boundary, Web-side WKWebView transport support, and remaining macOS runtime readiness gates | src/DesktopLoudnessMeter.MacHost/Package.swift |
| [source-list-workspace](source-list-workspace.md) | active | 2026-07-01 | Global source rows, versioned workspace settings, per-source preferences, hide/show, alias/title editing, native-planned ordering/source preference/global/chart/source-target settings command persistence, native-backed selected-detail shell planning, detail selection, and detail visualization settings | src/DesktopLoudnessMeter.App/Settings/UserWorkspaceSettings.cs |
| [host-contract](host-contract.md) | active | 2026-07-01 | UI-neutral host snapshot, typed command/result vocabulary, native-backed selected-detail shell/settings/command/collision projection plus source-order, source-preference, global settings, chart settings, runtime service, selected-source, and source-target command planning, host state controller, WPF adapter bridge, and WebView HostBridge v1 consumption boundary | src/DesktopLoudnessMeter.App/Host/DesktopMeterHostController.cs |
| [signal-source-ingest](signal-source-ingest.md) | active | 2026-06-30 | Native-backed local/plugin protocol decode, source registry, lifecycle state machine, per-source analyzer ownership, and source snapshot projection | src/DesktopLoudnessMeter.Native/DlmSourceIngestCore.cpp |
| [analysis-rendering](analysis-rendering.md) | active | 2026-07-01 | PCM history analysis, WPF rendering oracle, Web Canvas selected-detail chart rendering, loudness clock plotting, native-backed Host collision bands, and frequency collision display mapping | src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs |
| [juce-sender](juce-sender.md) | active | 2026-06-29 | VST3 sender worker lifecycle, Protocol v1 vocabulary emission, UDP loopback packets, ack polling, and restart semantics | src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp |
| [audio-analysis](audio-analysis.md) | active | 2026-06-29 | C# analyzer wrapper, snapshot DTO mapping, native P/Invoke ownership, and managed reference helpers for tests/fallback context | src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs |
| [native-dsp](native-dsp.md) | active | 2026-07-01 | Native C++ PCM analyzer core, local/plugin Protocol v1 ingest, Host projection cores including source list/recent/detail/settings/commands/collision/source-order, source-preference, global settings, chart settings, runtime service, selected-detail, selected-source, and source-target command planning, Windows-verified DLL build/deployment boundary, reserved CMake boundary, and C ABI buffer-copy access | src/DesktopLoudnessMeter.Native/DlmPcmAnalyzer.Core.cpp |
| [audio-capture-sources](audio-capture-sources.md) | active | 2026-06-30 | WASAPI capture, host UDP socket glue, local/plugin protocol routing via native decode, and source packet routing into native-backed ingest | src/DesktopLoudnessMeter.App/Audio |

## Chains

| Chain | Status | Last Verified | Modules | Notes |
|-------|--------|---------------|---------|-------|
| [same-handle reconfiguration](chains/same-handle-reconfiguration.md) | active | 2026-06-29 | juce-sender, signal-source-ingest, source-list-workspace | Preserves source identity through DAW format restarts without close-before-register |
| [pcm-metric-flow](chains/pcm-metric-flow.md) | active | 2026-06-30 | audio-capture-sources, signal-source-ingest, audio-analysis, native-dsp | Tracks incoming PCM through host capture/socket glue, native Protocol v1 decode, native source ingest, native-backed analysis, wrapper snapshot mapping, and UI-facing metrics |
