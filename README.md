# uMeter

Fast floating Windows desktop meter for seeing which app, DAW, or plugin sender is producing level.

## Current Version

`v2.3.4` is the current public release. This patch closes the draggable detail-graph polish pass with fixed-height collision analysis, inactive hidden graphs, steadier reorder hints, and detail title hover guidance.

## Usable Baseline

- **App shell:** WPF/.NET 8 floating Windows meter with always-on-top mode, hide-to-tray, native tray restore/pop-up/exit actions, single-instance activation, themed controls, app/tray icons, and persisted user settings under `%APPDATA%\uMeter`.
- **Source workspace:** Mixed source list for WASAPI app/session audio, localhost loopback probes, and JUCE `uMeter Sender` VST3 instances. Rows support stable source identity, user ordering, duplicate-name disambiguation, aliases, hide/show filtering, reconnect retention, and detail switching when a watched plugin instance disappears.
- **Sender plugin:** JUCE VST3 audio-effect sender with pass-through audio, stable source handles, host-based default names, live source rename, temporary mute, late desktop-app discovery, and receiver acknowledgement/status feedback.
- **Detail workspace:** Inline and pop-up detail panels share the same per-source settings model. Detail modules can be independently shown, hidden, dragged, reordered, saved, loaded from JSON presets, or restored from an optional validated default layout file.
- **Detail graphs:** Stereo image plus correlation, loudness clock, waveform, and spectrograph are separate graph modules. Stereo/waveform/spectrograph/clock modules are resizeable where useful; the collision analyzer is a compact fixed-height module.
- **Metering:** Peak/LUFS display, true-peak oversampling options, momentary/short-term/integrated loudness, recent-window true peak, 100 ms RMS, polar stereo image, and correlation.
- **Waveform:** Oversampled dBFS waveform display with overall/channel modes, viewport-bounded rendering, vertical pan, mouse-wheel zoom, double-click reset, and inactive hidden-module gating.
- **Spectrograph:** Horizontally rolling Mel-scaled FFT display from 20 Hz to 22 kHz using raw PCM history, dynamic viewport resolution, adjustable display range, hover frequency readout, sharp bucket rendering, and rollover-stripe protection.
- **Loudness clock:** Configurable cycle length, loudness range, warning threshold, fan-fill plotting, scan line, time markers, hover readout, and over-range danger coloring.
- **Collision analyzer:** Prominent spectrum-overlap comparison between sources with source selection, logarithmic/Mel scale, level floor, prominence, match tolerance, frequency cursor marker, and double-click numeric slider entry.
- **Events and settings:** Recent Events strip for source lifecycle, true-peak max, loudness jumps, and conservative collision overlaps, with type filtering and configurable retained entry count. Global settings cover event history, future event output folder selection, and default detail-layout loading. The persisted workspace/settings file is versioned and remains compatible with legacy `%APPDATA%\uMeter\settings.json` data.
- **Performance and reliability:** Eco mode, hidden graph active gates, deferred render update coalescing, theme refresh across open windows/popups, bitmap-lock retry handling, and optional native RfRapid C++ FFT/true-peak acceleration with managed fallback.
- **Packaging:** Installer includes the desktop app and `uMeter Sender.vst3`, with app install path, shortcut options, configurable VST3 path, standard VST3 mirror install, quiet mode, and versioned artifacts.

## Run

```powershell
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

To enable the native RfRapid FFT and true-peak acceleration paths before running:

```powershell
.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release
dotnet build .\desktop-loudness-meter.sln
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

The app automatically falls back to managed FFT and true-peak reconstruction if `DesktopLoudnessMeter.Native.dll` is not present or cannot be loaded.

`DesktopLoudnessMeter.Native.dll` is the Windows-verified native DSP boundary. The MSBuild helper writes the DLL to `src\DesktopLoudnessMeter.Native\x64\<Configuration>\`, and managed app, test, and benchmark projects import the native copy target so the DLL lands beside the final process.

The native project also has a CMake build entry for the future cross-platform C++ Core boundary. Treat that as a reserved macOS/Linux verification path until `macos-readiness` runs on an appropriate host; Windows MSBuild remains the verified path today.

The Windows WebView shell consumes a HostBridge v1 JSON DTO boundary above the native WebView2 message channel. The settings/workspace DTO now includes `schemaVersion`, while `UserWorkspaceSettings` remains the owner of persisted schema normalization and legacy compatibility.

## Validate

```powershell
dotnet build .\desktop-loudness-meter.sln
dotnet test .\desktop-loudness-meter.sln
```

For the JUCE VST3 sender:

```powershell
.\scripts\build-juce-sender.ps1 -Format VST3 -Configuration Debug -RunTests
.\scripts\build-juce-sender.ps1 -Format VST3 -Configuration Release
```

The JUCE helper resolves `JUCE_DIR` from `-JuceDir`, the `JUCE_DIR`/`JUCE_ROOT` environment variables, or common local paths such as `C:\JUCE`. Add `-InstallCopy` when you want the built VST3 copied into the configured host scan folders.

## Package

```powershell
.\build-installer.ps1
```

The generated installers are written to:

```text
artifacts\uMeter.Setup.exe
artifacts\uMeter-Setup-2.3.4.exe
```

The installer contains the desktop app and `uMeter Sender.vst3`. The VST3 install path defaults to:

```text
C:\Program Files\VSTPlugins
```

For Cubase and other strict VST3 hosts, the installer also mirrors the plugin into Steinberg's standard VST3 scan folder:

```text
C:\Program Files\Common Files\VST3
```

Installing into Program Files may request administrator permission. Scripted quiet mode is also available:

```powershell
.\artifacts\uMeter.Setup.exe --quiet --install-dir "$env:LOCALAPPDATA\Programs\uMeter" --vst-dir "C:\Program Files\VSTPlugins"
.\artifacts\uMeter.Setup.exe --quiet --install-dir "D:\Tools\uMeter" --no-shortcuts --no-vst
```

## Troubleshooting

Startup and crash details are written here:

```powershell
$env:LOCALAPPDATA\uMeter\umeter.log
```

For mixed-source behavior, duplicate names, and reconnect preference memory, see `docs/source-management.md`.

## Project Layout

- `src/DesktopLoudnessMeter.App` - WPF app and WASAPI/session/source metering code.
- `src/DesktopLoudnessMeter.Protocol` - shared localhost loopback protocol packet model and encoder/decoder.
- `src/DesktopLoudnessMeter.SenderKit` - reusable sender-side core for plugin wrappers and diagnostics.
- `src/DesktopLoudnessMeter.SenderProbe` - DAW-free plugin-style sender probe for local smoke tests.
- `src/DesktopLoudnessMeter.JucePlugin` - native JUCE VST3 sender plugin.
- `src/DesktopLoudnessMeter.Native` - Windows-verified native DSP DLL and reserved CMake build boundary for future C++ Core reuse.
- `tests/DesktopLoudnessMeter.Benchmarks` - lightweight local benchmark harness for DSP hot paths.
- `tests/DesktopLoudnessMeter.Tests` - unit tests for math and view-model reconciliation.
- `docs/current-status.md` - current release status and remaining known limits.
- `docs/product-roadmap/` - post-v2.2.4 product roadmap and planning notes.
- `docs/rendering-architecture.md` - graph rendering cleanup notes.
- `docs/v2.3.4-release.md` - v2.3.4 release notes.
- `docs/v2.3.3-release.md` - v2.3.3 release notes.
- `docs/v2.3.2-release.md` - v2.3.2 release notes.
- `docs/v2.3.0-release.md` - v2.3.0 release notes.
- `docs/v2.2.8-release.md` - v2.2.8 release notes.
- `docs/v2.2.7-release.md` - v2.2.7 release notes.
- `docs/v2.2.6-release.md` - v2.2.6 release notes.
- `docs/v2.2.5-release.md` - v2.2.5 release notes.
- `docs/v2.2.4-release.md` - v2.2.4 release notes.
- `docs/v2.2.3-release.md` - v2.2.3 release notes.
- `docs/v2.2.2-release.md` - v2.2.2 release notes.
- `docs/v2.2.1-release.md` - v2.2.1 release notes.
- `docs/v2.2.0-release.md` - v2.2.0 release notes.
- `docs/v2.0.1-release.md` - v2.0.1 release notes.
- `docs/juce-vst3-sender.md` - JUCE VST3 sender build, install, and smoke-test notes.
- `docs/source-management.md` - mixed source ordering, identity, duplicate-name, and reconnect behavior.

## Roadmap

1. Done: WASAPI session peak overlay.
2. Done: Advanced detail metering, DSP metrics, process loopback, waveform/spectrum rolls, and performance polish.
3. Done: Mixed signal-source foundation for WASAPI, localhost loopback, and plugin-compatible sources.
4. Done: JUCE VST3 sender with DAW routing, source rename, mute, and late receiver recovery.
5. Done: v2.0.0 installer packaging with app rename and configurable VST3 install path.
6. Done: v2.0.1 appearance themes, Eco mode, and Windows compatibility polish.
7. Done: v2.2.0 frequency collision analysis, graph responsiveness, spectrograph range control, and AAX sender groundwork.
8. Done: v2.2.1 spectrum bitmap lock crash fix.
9. Done: v2.2.2 spectrograph continuity, color-floor, and numeric range-entry polish.
10. Done: v2.2.3 source workspace memory and smarter collision-analyzer polish.
11. Done: v2.2.4 Recent Events substrate and global event settings.
12. Done: v2.2.5 overlap-event slice, VST3 sender acknowledgements/status, and reset-rendering stability.
13. Done: v2.2.6 Cycle 5 rendering architecture cleanup slices and Cycle 6 platform strategy notes.
14. Done: v2.2.7 detail-window topmost isolation and Milk White graph contrast polish.
15. Done: v2.2.8 RfRapid true-peak acceleration with benchmark coverage and managed fallback.
16. Done: v2.3.0 source continuity, spectrograph fidelity, single-instance launch, and source-list workspace controls.
17. Done: v2.3.2 rendering responsiveness, unified detail title editing, and loudness clock polish.
18. Done: v2.3.3 UI reliability, detail-layout presets, and loudness clock interaction polish.
19. Done: v2.3.4 draggable detail-graph polish and fixed-height collision analyzer.
