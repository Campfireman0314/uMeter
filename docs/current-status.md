# Current Status

## Checkpoint

`v2.2.0` - Frequency-collision, graph responsiveness, and release-structure polish, pinned on 2026-06-20.

This checkpoint builds on the v2.0.1 appearance/performance release with frequency collision analysis, smoother waveform/spectrum/collision plotting, a spectrograph display dB range slider, AAX sender groundwork, and installer success-message polish. The installer still ships the desktop app plus the `uMeter Sender.vst3` bundle and exposes both the app install folder and VST3 install folder.

## What Works

- Floating WPF/.NET 8 desktop app.
- WASAPI render-session enumeration through NAudio.
- Mixed WASAPI, localhost loopback, and plugin sender rows in one source list.
- JUCE `uMeter Sender` VST3 audio-effect plugin with pass-through audio.
- Minimal VST3 sender editor with source-name rename while typing and temporary mute.
- Host-based VST3 default source names such as `Cubase 7F3A`.
- Stable persisted plugin source handles and periodic register packets for late desktop-app startup.
- Plugin rows are removed from the main source list on unload; open detail views show an `Instance not found` overlay until the same instance returns or the view is switched.
- Configurable installer VST3 path, defaulting to `C:\Program Files\VSTPlugins`.
- Forced standard VST3 mirror install to `C:\Program Files\Common Files\VST3` for Cubase/VST3 host discovery.
- Plugin receiver port-collision warning in the desktop app.
- Deterministic source ordering with duplicate-name disambiguation and source identity text.
- Audible app rows with duplicate row collapse, self-filtering, and silent active row filtering.
- Process-loopback capture for per-process PCM metrics on supported Windows builds.
- Peak/LUFS primary display mode.
- Global true-peak option shell with 2x, 4x, and 8x ratios.
- Inline detail view with Back navigation.
- Per-app pop-up detail windows with independent always-on-top state.
- Independent stereo image scale settings per pop-up and inline detail view.
- Stereo image auto-scale controls for range, lower limit, manual top, speed, trace persistence, and reset.
- Per-app true-peak oversampling selector in inline and pop-up detail views.
- Momentary, short-term, and integrated loudness.
- Recent-window true peak plus history max readout.
- 100 ms RMS.
- Polar stereo fan sample dots.
- Correlation arrow in the `[-1, 1]` range.
- Global and per-detail reset actions.
- Detail/app rows retain once-seen process audio handles until the source process exits.
- Per-source analysis preferences survive reconnect while the app is running when the source key is stable.
- Hide-to-tray main window behavior with tray restore, pop-up-app, and exit menu actions.
- Custom app/tray icon assets.
- Custom styling for buttons, combo boxes, checkboxes, scrollbars, and selectable app themes.
- Persisted selected theme under %APPDATA%\uMeter\settings.json.
- Themed stereo image, waveform, spectrum, and correlation plot drawing.
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
- Frequency collision analyzer compares spectrum bins between sources and highlights similar frequency ranges.
- Spectrograph display range can be adjusted with a themed two-thumb dB slider.
- Spectrum FFT can use the optional `DesktopLoudnessMeter.Native.dll` RfRapid C++ accelerator, with automatic managed fallback.
- Crash/startup logging under `%LOCALAPPDATA%\uMeter`.
- Installer with visible app install path, shortcut options, VST3 install path, uninstall registration, and quiet mode.

## Run

```powershell
cd D:\GitRepo\desktop-loudness-meter
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

Optional native FFT acceleration:

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

- Build: clean.
- Native build: clean.
- Tests: 182 passing.
- JUCE VST3 native CTest: 1 passing.
- JUCE VST3 Release build: succeeded; local JUCE/HarfBuzz source emitted non-fatal C4819 code-page warnings.
- Format: clean.
- Installer build: `artifacts\uMeter-Setup-2.2.0.exe` generated.
- Installer payload: confirmed to contain `uMeter.exe` and `Plugins\VST3\uMeter Sender.vst3`.

## Installer Notes

- Default app install folder: `%LOCALAPPDATA%\Programs\uMeter`.
- Default VST3 install folder: `C:\Program Files\VSTPlugins`.
- Standard VST3 mirror folder: `C:\Program Files\Common Files\VST3`.
- Default artifacts: `artifacts\uMeter.Setup.exe` and `artifacts\uMeter-Setup-2.2.0.exe`.
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
git commit -m "Release uMeter v2.2.0"
git push
```

Before committing, review generated folders and build outputs are still ignored as expected.

## Important Files

- `README.md` - project overview, run commands, roadmap.
- `CHANGELOG.md` - version history.
- docs/v2.2.0-release.md - v2.2.0 release notes.
- docs/v2.0.1-release.md - v2.0.1 release notes.
- docs/v2.0.0-release.md - v2.0.0 release notes.
- `docs/v1.1.0-release.md` - v1.1.0 polish release notes.
- `docs/vst3-first-usable-version.md` - first usable JUCE VST3 sender checkpoint.
- `docs/audio-metering-notes.md` - DSP and WASAPI behavior notes.
- `docs/local-loopback-protocol.md` - localhost loopback protocol notes.
- `docs/plugin-sender-prototype.md` - sender probe usage and plugin packaging status.
- `docs/juce-vst3-sender.md` - JUCE VST3 sender build, install, and smoke-test notes.
- `docs/source-management.md` - mixed source ordering, identity, duplicate-name, and reconnect behavior.
- `src/DesktopLoudnessMeter.App` - WPF app and audio code.
- `src/DesktopLoudnessMeter.JucePlugin` - native JUCE VST3 sender plugin.
- `src/DesktopLoudnessMeter.Native` - optional native RfRapid FFT bridge.
- `tests/DesktopLoudnessMeter.Tests` - unit tests.

## Known Limits

- Source aliases/visibility rules are not persisted yet.
- Per-source analysis preferences are remembered only while the app is running.
- Multiple detail windows watching the same process share one analyzer stream, so the latest true-peak oversampling choice for that process wins.
- Process-loopback depends on Windows support and target-session behavior.
- AAX sender groundwork exists, but distributable AAX builds still require Avid/PACE signing, validation, and Pro Tools smoke testing.
- The VST3 sender still uses the inspectable JSON/base64 UDP loopback protocol and fixed receiver port `38173`.
- DSP behavior has synthetic coverage but is not certified against official loudness reference vectors yet.
- The installer is unsigned, so Windows SmartScreen may warn during first launch on other machines.

## Sensible Next Slice

- Add connection/status feedback to the VST3 sender editor.
- Consider a binary loopback protocol path after the UI direction settles.
- Add signed installer or release signing workflow.
- Compare loudness and true-peak behavior against official reference vectors.
