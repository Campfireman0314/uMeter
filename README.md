# uMeter

Fast floating Windows desktop meter for seeing which app, DAW, or plugin sender is producing level.

## Current Version

`v2.0.1` is the current public release. This polish release adds theme customization, persisted theme preference, Eco mode performance controls, and Windows compatibility notices while retaining the packaged VST3 sender support from v2.0.0.

## Usable Baseline

- WPF/.NET 8 Windows desktop app.
- Always-on-top compact overlay window with hide-to-tray, restore, pop-up detail, and exit tray actions.
- Mixed source rows for WASAPI app/session audio, localhost loopback probes, and plugin sender instances.
- JUCE `uMeter Sender` VST3 audio-effect plugin with pass-through audio, stable source identity, host-based default names, live source rename, temporary mute, and late desktop-app discovery.
- Plugin sources disappear from the main list on unload, while open detail views show an `Instance not found` overlay and can switch to another active source.
- Stable source ordering, duplicate-name disambiguation, source identity text, and process-lifetime handle retention.
- Peak/LUFS global display mode, true-peak options, topmost behavior, resets, and per-session details.
- DSP engine for momentary/short-term/integrated loudness, recent-window true peak, 100 ms RMS, polar stereo image, and correlation.
- Optional pop-up detail windows with independent topmost state, stereo scale settings, and true-peak oversampling ratio.
- Optional waveform and spectrum panels with overall/channel display modes, vertical pan, mouse-wheel zoom, double-click reset, and stretchable panel height.
- Oversampled waveform display with dBFS scale markings.
- Horizontally rolling Mel-scaled FFT spectrum display from 20 Hz to 22 kHz.
- Stereo image dot history/intensity control through trace persistence.
- Optional native RfRapid C++ FFT accelerator for the spectrum path, with managed fallback.
- Packaged installer with app install path, shortcut options, and configurable VST3 install path.
- App and tray icons are included under `src/DesktopLoudnessMeter.App/Assets`.

## Run

```powershell
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

To enable the native RfRapid FFT path before running:

```powershell
.\src\DesktopLoudnessMeter.Native\build-native.ps1 -Configuration Release
dotnet build .\desktop-loudness-meter.sln
dotnet run --project .\src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj
```

The app automatically falls back to the managed FFT if `DesktopLoudnessMeter.Native.dll` is not present or cannot be loaded.

## Validate

```powershell
dotnet build .\desktop-loudness-meter.sln
dotnet test .\desktop-loudness-meter.sln
```

For the JUCE VST3 sender:

```powershell
cmake -S .\src\DesktopLoudnessMeter.JucePlugin -B .\build\juce-vst3 -DJUCE_DIR=C:/JUCE
cmake --build .\build\juce-vst3 --config Debug --target DesktopLoudnessMeterJucePluginTests
ctest --test-dir .\build\juce-vst3 -C Debug --output-on-failure
cmake --build .\build\juce-vst3 --config Release --target DesktopLoudnessMeterSender_VST3
```

## Package

```powershell
.\build-installer.ps1
```

The generated installers are written to:

```text
artifacts\uMeter.Setup.exe
artifacts\uMeter-Setup-2.0.1.exe
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
- `src/DesktopLoudnessMeter.Native` - optional RfRapid-backed native FFT wrapper.
- `tests/DesktopLoudnessMeter.Tests` - unit tests for math and view-model reconciliation.
- `docs/current-status.md` - current release status and remaining known limits.
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
