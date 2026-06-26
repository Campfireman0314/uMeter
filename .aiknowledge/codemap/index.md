# Codemap

## Modules

| Module | Status | Last Verified | Responsibility | Entry |
|--------|--------|---------------|----------------|-------|
| [app-startup](app-startup.md) | active | 2026-06-26 | WPF startup, single-instance ownership, tray lifecycle, and activation replay | src/DesktopLoudnessMeter.App/App.xaml.cs |
| [source-list-workspace](source-list-workspace.md) | active | 2026-06-26 | Global source rows, per-source preferences, hide/show, alias editing, ordering, and detail selection | src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs |
| [signal-source-ingest](signal-source-ingest.md) | active | 2026-06-26 | Local loopback source registration, reconfiguration, analyzer ownership, and source snapshots | src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs |
| [analysis-rendering](analysis-rendering.md) | active | 2026-06-26 | PCM history analysis, spectrograph rasterization, and frequency collision display mapping | src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs |
| [juce-sender](juce-sender.md) | active | 2026-06-26 | VST3 sender worker lifecycle, UDP loopback packets, ack polling, and restart semantics | src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp |

## Chains

| Chain | Status | Last Verified | Modules | Notes |
|-------|--------|---------------|---------|-------|
| [same-handle reconfiguration](chains/same-handle-reconfiguration.md) | active | 2026-06-26 | juce-sender, signal-source-ingest, source-list-workspace | Preserves source identity through DAW format restarts |
