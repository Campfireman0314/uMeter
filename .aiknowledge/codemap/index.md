# Codemap

## Modules

| Module | Status | Last Verified | Responsibility | Entry |
|--------|--------|---------------|----------------|-------|
| [app-startup](app-startup.md) | active | 2026-06-26 | WPF startup, single-instance ownership, tray lifecycle, and activation replay | src/DesktopLoudnessMeter.App/App.xaml.cs |
| [source-list-workspace](source-list-workspace.md) | active | 2026-06-27 | Global source rows, per-source preferences, hide/show, alias/title editing, ordering, detail selection, and detail visualization settings | src/DesktopLoudnessMeter.App/ViewModels/MainViewModel.cs |
| [signal-source-ingest](signal-source-ingest.md) | active | 2026-06-26 | Local loopback source registration, reconfiguration, analyzer ownership, and source snapshots | src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs |
| [analysis-rendering](analysis-rendering.md) | active | 2026-06-27 | PCM history analysis, adaptive waveform/spectrograph rendering, loudness clock plotting, and frequency collision display mapping | src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs |
| [juce-sender](juce-sender.md) | active | 2026-06-26 | VST3 sender worker lifecycle, UDP loopback packets, ack polling, and restart semantics | src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp |
| [audio-analysis](audio-analysis.md) | active | 2026-06-23 | PCM metric calculation, loudness and true-peak analysis, plus waveform, spectrum, and stereo derived data | src/DesktopLoudnessMeter.App/Audio/Analysis/PcmMetricAnalyzer.cs |
| [native-dsp](native-dsp.md) | active | 2026-06-23 | Native C++ FFT and true-peak backend invoked through the C# P/Invoke bridge | src/DesktopLoudnessMeter.Native/DesktopLoudnessMeterNative.cpp |
| [audio-capture-sources](audio-capture-sources.md) | active | 2026-06-23 | WASAPI, local loopback, and plugin packet ingestion into `PcmMetricAnalyzer` and downstream metrics | src/DesktopLoudnessMeter.App/Audio |

## Chains

| Chain | Status | Last Verified | Modules | Notes |
|-------|--------|---------------|---------|-------|
| [same-handle reconfiguration](chains/same-handle-reconfiguration.md) | active | 2026-06-26 | juce-sender, signal-source-ingest, source-list-workspace | Preserves source identity through DAW format restarts |
| [pcm-metric-flow](chains/pcm-metric-flow.md) | active | 2026-06-23 | audio-capture-sources, audio-analysis, native-dsp | Tracks the key path from incoming PCM blocks through analysis into UI snapshots |
