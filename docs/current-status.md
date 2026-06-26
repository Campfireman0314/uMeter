# Current Status

## Checkpoint

`v2.3.0` - Source continuity, spectrograph fidelity, single-instance launch, and source-list workspace controls, pinned on 2026-06-26.

This checkpoint builds on the v2.2.8 RfRapid acceleration patch. Source rows now survive temporary handle loss and sender format reconfiguration more gracefully, the spectrograph no longer adds hidden render-stage blur, only one desktop instance can run per user session, and global source rows now support inline alias editing, hide/restore, show-hidden, and persistent ordering. The installer still ships the desktop app plus the `uMeter Sender.vst3` bundle and exposes both the app install folder and VST3 install folder.

The remaining roadmap work after this checkpoint should focus on deeper metering trust fixtures, measured Mel-band aggregation experiments, richer sender diagnostics, or a deliberately scoped next product feature.

## What Works

- Floating WPF/.NET 8 desktop app.
- WASAPI render-session enumeration through NAudio.
- Mixed WASAPI, localhost loopback, and plugin sender rows in one source list.
- JUCE `uMeter Sender` VST3 audio-effect plugin with pass-through audio.
- Minimal VST3 sender editor with source-name rename while typing and temporary mute.
- VST3 sender editor shows receiver connection feedback from desktop-app acknowledgements.
- VST3 sender sample-rate/channel restarts re-register the same handle without an intervening close packet.
- Host-based VST3 default source names such as `Cubase 7F3A`.
- Stable persisted plugin source handles and periodic register packets for late desktop-app startup.
- Plugin rows are removed from the main source list on unload; open detail views show an `Instance not found` overlay until the same instance returns or the view is switched.
- Only one uMeter desktop instance is allowed per user session; secondary launches request activation of the primary instance and exit.
- Configurable installer VST3 path, defaulting to `C:\Program Files\VSTPlugins`.
- Forced standard VST3 mirror install to `C:\Program Files\Common Files\VST3` for Cubase/VST3 host discovery.
- Plugin receiver port-collision warning in the desktop app.
- Persistent user source ordering with duplicate-name disambiguation and source identity text.
- Per-source hide/restore controls with a global `Show hidden` toggle.
- Audible app rows with duplicate row collapse, self-filtering, and silent active row filtering.
- Process-loopback capture for per-process PCM metrics on supported Windows builds.
- Peak/LUFS primary display mode.
- Global true-peak option shell with 2x, 4x, and 8x ratios.
- Inline detail view with Back navigation.
- Per-app pop-up detail windows with independent always-on-top state.
- Pop-up detail windows do not inherit the global window's `Always on top` behavior.
- Independent stereo image scale settings per pop-up and inline detail view.
- Stereo image auto-scale controls for range, lower limit, manual top, speed, trace persistence, and reset.
- Per-app true-peak oversampling selector in inline and pop-up detail views.
- Momentary, short-term, and integrated loudness.
- Recent-window true peak plus history max readout.
- 100 ms RMS.
- Polar stereo fan sample dots.
- Correlation arrow in the `[-1, 1]` range.
- Global and per-detail reset actions.
- Detail/app rows retain once-seen process audio handles until the source process exits and keep existing graph history during temporary handle loss.
- Per-source analysis preferences survive reconnect while the app is running when the source key is stable.
- Hide-to-tray main window behavior with tray restore, pop-up-app, and exit menu actions.
- Custom app/tray icon assets.
- Custom styling for buttons, combo boxes, checkboxes, scrollbars, and selectable app themes.
- Persisted selected theme under %APPDATA%\uMeter\settings.json.
- Themed stereo image, waveform, spectrum, and correlation plot drawing.
- Main graph controls use shared plot color roles for common theme resources.
- Milk White graph colors use a higher-contrast blue/amber plot palette on a warm parchment track.
- Eco mode for lower UI refresh and spectrum analysis CPU use.
- Startup compatibility notice for unsupported or experimental Windows process-loopback builds.
- Separate hidden waveform and spectrum panels in inline and pop-up detail views.
- Waveform and spectrum mode menus can show either summed overall display or one plot per channel.
- Stretchable waveform/spectrum panels with single-panel resize handles and a two-panel splitter.
- Waveform and spectrum roll views support vertical pan, cursor-centered mouse-wheel vertical zoom, and double-click reset.
- Waveform and spectrum pan/zoom stay clamped inside valid waveform and 20 Hz to 22 kHz spectrum ranges.
- Waveform display uses the selected true-peak oversampling ratio for its display buffer.
- Waveform display uses dBFS scale markings and a smoother curve without averaging away plotted extrema.
- Spectrum display uses a horizontally rolling, Mel-scaled, 320-bin FFT-derived display from 20 Hz to 22 kHz.
- Source aliases can be edited from the global row title, inline detail, and pop-up detail naming bars.
- Per-source analysis preferences persist through the user workspace when the source key is stable.
- Frequency collision analyzer compares prominent spectrum overlap between sources and highlights similar frequency ranges.
- Collision analyzer options include source selection, `Scale` (`Logarithmic` by default, or `Mel`), `Level floor`, `Prominence`, and `Match tolerance`.
- Collision analyzer single-value sliders support double-click numeric entry.
- Global Recent Events strip shows a bounded, filterable view of source lifecycle, true-peak max, and loudness-jump events.
- Recent Events can include conservative `Possible overlap` entries from enabled detail collision analysis.
- Global settings expose maximum retained event entries and an event output folder selector.
- Spectrograph display range can be adjusted from `-200 to 0 dB` with a themed two-thumb dB slider, defaulting to `-96 to -6 dB`.
- Spectrum bitmap rendering tolerates transient WPF read/write lock conflicts without terminating the app.
- Waveform and spectrum controls share a deferred render-update scheduler so property churn coalesces into one queued UI render callback per control.
- Waveform, spectrum, and collision controls share static plot-background cache invalidation logic.
- Spectrograph rendering uses a soft visual floor, nearest-neighbor bitmap scaling, and direct bucket-to-row mapping so render smoothing does not exaggerate frequency leakage.
- Spectrum FFT input uses raw PCM history while waveform display continues to use true-peak oversampled display samples.
- Spectrum FFT and true-peak reconstruction can use the optional `DesktopLoudnessMeter.Native.dll` RfRapid C++ accelerator, with automatic managed fallback.
- Crash/startup logging under `%LOCALAPPDATA%\uMeter`.
- Installer with visible app install path, shortcut options, VST3 install path, uninstall registration, and quiet mode.

## Run

```powershell
cd D:\GitRepo\desktop-loudness-meter
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

Optional native DSP acceleration:

```powershell
cd D:\GitRepo\desktop-loudness-meter
.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release
dotnet build .\desktop-loudness-meter.sln
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

## Verify

```powershell
cd D:\GitRepo\desktop-loudness-meter
.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release
dotnet build .\desktop-loudness-meter.sln --no-restore
dotnet test .\desktop-loudness-meter.sln --no-restore
dotnet format .\desktop-loudness-meter.sln --verify-no-changes
```

Optional VST3 sender verification:

```powershell
cd D:\GitRepo\desktop-loudness-meter
cmake -S .\src\DesktopLoudnessMeter.JucePlugin -B .\build\juce-vst3 -DJUCE_DIR=C:/JUCE
cmake --build .\build\juce-vst3 --config Debug --target DesktopLoudnessMeterJucePluginTests
ctest --test-dir .\build\juce-vst3 -C Debug --output-on-failure
cmake --build .\build\juce-vst3 --config Release --target DesktopLoudnessMeterSender_VST3
```

Latest verified status:

- App build: clean.
- Native build: Release passed after adding the true-peak RfRapid exports.
- Tests: 231 passing.
- Benchmark: RfRapid true-peak 8x processed the local 30 s stereo benchmark at about `4.44 Mframes/s` versus about `1.95 Mframes/s` for managed reconstruction, with matching peak output.
- JUCE VST3 native CTest: Debug passed.
- JUCE VST3 Release build: passed through installer packaging.
- Format: not rerun for this checkpoint.
- Installer build: `artifacts\uMeter-Setup-2.3.0.exe` generated successfully.
- Installer SHA256: `5C960D7F41969773738928B3B6145B8CED35F9278D838246B64D9995FE977263`.
- Installer payload: desktop app, native DSP DLL, and bundled `uMeter Sender.vst3` generated by the packaging script.

## Installer Notes

- Default app install folder: `%LOCALAPPDATA%\Programs\uMeter`.
- Default VST3 install folder: `C:\Program Files\VSTPlugins`.
- Standard VST3 mirror folder: `C:\Program Files\Common Files\VST3`.
- Default artifacts: `artifacts\uMeter.Setup.exe` and `artifacts\uMeter-Setup-2.3.0.exe`.
- Quiet install supports `--install-dir`, `--vst-dir`, `--no-vst`, `--no-desktop-shortcut`, `--no-start-menu-shortcut`, and `--no-shortcuts`.
- Installing the VST3 sender into Program Files may trigger UAC elevation.
- Uninstall cleanup relaunches elevated when protected app or VST3 locations need administrator permission.
- Installer cleanup removes legacy `Desktop Loudness Meter Sender.vst3` bundles from the selected VST folder plus standard VST3 scan folders.
- Uninstall removes `uMeter Sender.vst3` from both the selected VST folder and the standard VST3 mirror folder.

## Git Notes

Suggested release commit:

```powershell
cd D:\GitRepo\desktop-loudness-meter
git status --short
git add .
git commit -m "Release uMeter v2.3.0"
git push
```

Before committing, review generated folders and build outputs are still ignored as expected.

## Important Files

- `README.md` - project overview, run commands, roadmap.
- `CHANGELOG.md` - version history.
- docs/v2.3.0-release.md - v2.3.0 release notes.
- docs/v2.2.8-release.md - v2.2.8 release notes.
- docs/v2.2.7-release.md - v2.2.7 release notes.
- docs/v2.2.6-release.md - v2.2.6 release notes.
- docs/v2.2.5-release.md - v2.2.5 release notes.
- docs/v2.2.4-release.md - v2.2.4 release notes.
- docs/v2.2.3-release.md - v2.2.3 release notes.
- docs/v2.2.2-release.md - v2.2.2 release notes.
- docs/v2.2.1-release.md - v2.2.1 release notes.
- docs/v2.2.0-release.md - v2.2.0 release notes.
- docs/v2.0.1-release.md - v2.0.1 release notes.
- docs/v2.0.0-release.md - v2.0.0 release notes.
- `docs/v1.1.0-release.md` - v1.1.0 polish release notes.
- `docs/vst3-first-usable-version.md` - first usable JUCE VST3 sender checkpoint.
- `docs/audio-metering-notes.md` - DSP and WASAPI behavior notes.
- `docs/local-loopback-protocol.md` - localhost loopback protocol notes.
- `docs/rendering-architecture.md` - graph rendering cleanup notes.
- `docs/product-roadmap/platform-strategy.md` - cross-platform contract and boundary notes.
- `docs/plugin-sender-prototype.md` - sender probe usage and plugin packaging status.
- `docs/juce-vst3-sender.md` - JUCE VST3 sender build, install, and smoke-test notes.
- `docs/source-management.md` - mixed source ordering, identity, duplicate-name, and reconnect behavior.
- `src/DesktopLoudnessMeter.App` - WPF app and audio code.
- `src/DesktopLoudnessMeter.JucePlugin` - native JUCE VST3 sender plugin.
- `src/DesktopLoudnessMeter.Native` - optional native RfRapid FFT and true-peak bridge.
- `tests/DesktopLoudnessMeter.Tests` - unit tests.

## Known Limits

- Workspace persistence is keyed by source identity; aliases and per-source preferences follow reconnects only when source keys remain stable.
- Multiple detail windows watching the same process share one analyzer stream, so the latest true-peak oversampling choice for that process wins.
- Process-loopback depends on Windows support and target-session behavior.
- AAX sender groundwork exists, but distributable AAX builds still require Avid/PACE signing, validation, and Pro Tools smoke testing.
- The VST3 sender still uses the inspectable JSON/base64 UDP loopback protocol and fixed receiver port `38173`; sender connection status is based on lightweight receiver `ack` packets.
- DSP behavior has synthetic coverage but is not certified against official loudness reference vectors yet.
- The installer is unsigned, so Windows SmartScreen may warn during first launch on other machines.

## Sensible Next Slice

- Extend Cycle 3 overlap events with source-pair attribution beyond the current selected/global collision context.
- Add richer reconnect diagnostics to the VST3 sender editor.
- Consider a binary loopback protocol path after the UI direction settles.
- Use `docs/product-roadmap/platform-strategy.md` before starting AAX, AU/macOS, or iOS implementation work.
- Add signed installer or release signing workflow.
- Compare loudness and true-peak behavior against official reference vectors.
