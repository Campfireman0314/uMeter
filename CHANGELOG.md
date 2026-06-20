# Changelog

## Unreleased

- Nothing yet.

## v2.2.0 - 2026-06-20

Frequency-collision, rendering responsiveness, and release-structure polish.

### Added

- Frequency collision analyzer for detail views, with source selection, absolute dB threshold, relative threshold, themed collision bands, cursor frequency marker, and inline plus pop-up detail support.
- Spectrograph display level range control with a themed two-thumb dB slider, defaulting to `-96 to 0 dB`.
- First AAX sender project scaffold that reuses the JUCE sender processor and loopback implementation from the VST3 sender.
- Shared development notes for desktop, sender-plugin, and future iOS planning surfaces.

### Changed

- Waveform, spectrum, and collision analyzer updates now use analyzer-frame plot ticks so cached buffers can advance visibly even when collection identities are reused.
- Spectrum roll rendering uses circular bitmap history and recolors existing history when theme or display dB range changes.
- Collision analyzer refresh now follows selected and comparison spectrum ticks, including pop-up detail windows.
- Collision analyzer is independent from waveform/spectrum visibility and remains disabled by default.
- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.0`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.0.exe`.

### Fixed

- Frequency collision source selector now preserves the `Global` label and stable source names across refreshes.
- Collision bars no longer flash themed background color when there is no valid comparison data.
- Installer UI reports success after an elevated install completes successfully.
- Waveform and spectrum rolls avoid sparse or paused-looking updates on slower machines by decoupling plotting cadence from buffer reference changes.

### Verified

- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` with 182 passing tests.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`.
- `.\build-installer.ps1 -Version 2.2.0 -JuceDir E:\JUCE` generated `uMeter-Setup-2.2.0.exe`.

## v2.0.1 - 2026-06-19

Appearance polish and Windows compatibility release.

### Added

- App theme selector in the global title bar with Midnight, Milk White, Forest Green, and Pink Dream themes.
- Theme preference persistence under `%APPDATA%\uMeter\settings.json`.
- Eco mode toggle with a delayed hover hint for lower CPU use on older PCs.
- Startup status warning for unsupported or experimental Windows process-loopback support.

### Changed

- Waveform, spectrum, stereo image, and correlation drawing colors now follow the active theme.
- Milk White theme uses warm milk/ivory graph colors for signal, stereo sample points, and spectrum energy.
- Spectrum analysis and UI refresh are throttled in Eco mode.
- Spectrum FFT uses the native RfRapid backend when `DesktopLoudnessMeter.Native.dll` is present, with a logged managed fallback.
- Process-loopback capture can attempt Windows 10 19041+ with a compatibility warning for builds below documented Windows 11 support.
- App, setup, protocol, sender kit, and sender probe metadata now report version `2.0.1`.
- Default versioned installer artifact is now `uMeter-Setup-2.0.1.exe`.

### Fixed

- Per-process meters can run on compatible Windows 10 systems where the newer documented loopback API gate was too strict.
- Lower-end machines can reduce spectrum/UI CPU load with Eco mode.
- Theme switching now repaints custom plot controls immediately.

### Verified

- `dotnet build .\desktop-loudness-meter.sln`
- `dotnet test .\desktop-loudness-meter.sln --no-build` with 170 passing tests.
- `.\build-installer.ps1 -Version 2.0.1 -JuceDir E:\JUCE` generated `uMeter-Setup-2.0.1.exe`.

## v2.0.0 - 2026-06-19

VST3-compatible public release and first release under the `uMeter` product name.

### Added

- JUCE `uMeter Sender` VST3 plugin is now part of the numbered release path.
- Installer packages the desktop app together with the VST3 sender bundle.
- Installer exposes a configurable VST3 install path, defaulting to `C:\Program Files\VSTPlugins`.
- Installer always mirrors `uMeter Sender.vst3` into `C:\Program Files\Common Files\VST3` for Cubase/VST3 host discovery.
- Quiet installer mode supports `--vst-dir` and `--no-vst`.
- Uninstall cleanup relaunches elevated when protected app or VST3 paths need administrator permission.
- Installer removes legacy `Desktop Loudness Meter Sender.vst3` bundles from both the selected VST path and standard VST3 scan folders.

### Changed

- Product-facing app name is now `uMeter`.
- Company/author metadata is now `Campfireman Studio`.
- App, setup, protocol, sender kit, and sender probe metadata now report version `2.0.0`.
- Default installer artifacts are now `uMeter.Setup.exe` and `uMeter-Setup-2.0.0.exe`.
- Runtime crash/startup logging moved to `%LOCALAPPDATA%\uMeter\umeter.log`.
- VST3 plugin product name is now `uMeter Sender`.

### Fixed

- Custom chrome windows can be restored after Windows snap/maximize drag behavior.
- Detail true-peak meters keep their value lane aligned while showing current and history-max values.

### Verified

- `dotnet build .\desktop-loudness-meter.sln --no-restore`
- `dotnet test .\desktop-loudness-meter.sln --no-restore` with 158 passing tests.
- `dotnet format .\desktop-loudness-meter.sln --verify-no-changes --no-restore`
- JUCE native CTest passed with 1 test.
- `.\build-installer.ps1 -Version 2.0.0` generated `uMeter-Setup-2.0.0.exe`.
- Installer payload inspected and confirmed to contain `uMeter.exe` plus `Plugins\VST3\uMeter Sender.vst3`.

## v1.1.0 - 2026-06-17

Second public polish release.

### Added

- Per-detail waveform and spectrum mode menus for overall summed display or one plot per channel.
- Separate waveform and spectrum visibility toggles with detail panels that open at usable full-scale height.
- Vertical pan and mouse-wheel zoom for waveform and spectrum roll views, with double-click reset.
- Stereo image trace persistence control for tuning dot intensity/history feel.
- One-sided waveform/spectrum splitter behavior so stretching the waveform does not resize the spectrum.

### Changed

- App and setup executable metadata now report version `1.1.0`.
- Default installer artifact name is now `DesktopLoudnessMeter-Setup-1.1.0.exe`.
- Waveform and spectrum roll views now clamp pan/zoom inside valid signal and 20 Hz - 22 kHz ranges.
- Single-track waveform/spectrum panels default to and cannot shrink below 185 px plot height.
- Stereo image settings now use a gear icon in both inline and pop-up detail views.
- App scrollbar styling now uses a full-height right-side rail instead of a short capped vertical bar.

### Fixed

- Waveform and spectrum settings remain selectable even when the corresponding plot is hidden.
- Detail roll panels preserve practical drag/stretch behavior after minimum-height constraints.
- Removed the global vertical scrollbar `Height=10` cap that made the right scrollbar look too short.

### Verified

- `dotnet build .\desktop-loudness-meter.sln`
- `dotnet test .\desktop-loudness-meter.sln --no-build` with 93 passing tests.
- `dotnet format .\desktop-loudness-meter.sln --verify-no-changes`
- `.\build-installer.ps1` generated `DesktopLoudnessMeter-Setup-1.1.0.exe`.
- WPF app smoke launch stayed running for the probe window and closed cleanly.

## v1.0.0 - 2026-06-16

First public release.

### Added

- Per-user Windows installer with install-folder selection, Desktop shortcut option, Start Menu shortcut option, uninstall registration, and quiet mode.
- Versioned release installer artifact: `DesktopLoudnessMeter-Setup-1.0.0.exe`.
- Shared spectrum scale from 20 Hz to 22 kHz.
- Combined waveform/spectrum roll panels can now be stretched vertically from the bottom grip.

### Changed

- App and setup executable metadata now report version `1.0.0`.
- Spectrum display now uses 320 Mel-spaced buckets for finer frequency detail.
- Spectrum stretching uses smoother bitmap scaling.

### Verified

- `dotnet build .\desktop-loudness-meter.sln`
- `dotnet test .\desktop-loudness-meter.sln --no-build` with 79 passing tests.
- `dotnet format .\desktop-loudness-meter.sln --verify-no-changes`
- Installer default install, app launch, shortcut cleanup, uninstall cleanup.
- Installer `--no-shortcuts` install and uninstall cleanup.

## v0.3.0 - 2026-06-16

Third usable developer-run version.

### Added

- Main-window hide-to-tray button.
- Status tray icon with `Show Meter`, `Pop-up App...`, and `Exit` menu actions.
- App and tray icon assets.
- Regression coverage proving true-peak oversampling ratios change measured inter-sample peaks.
- Separate hidden waveform and spectrum panels at the bottom of inline and pop-up detail views.
- Stretchable waveform/spectrum panels with single-panel resize handles and a two-panel splitter.
- Regression coverage proving the display waveform follows the selected oversampling ratio.

### Changed

- Main and pop-up detail windows are taller by default.
- Buttons, combo boxes, and scrollbars now use the app's dark control styling.
- ComboBox dropdowns and checkboxes now use the app's dark control styling.
- Stereo image scale range now defaults to 24 dB.
- Stereo image auto-scale speed now defaults to 8 dB/s and clamps at 24 dB/s.
- Stereo image settings dialog now uses the app's borderless chrome instead of the native grey title bar.
- Main and detail windows now use explicit zero-caption window chrome and matching header background to remove the draggable grey top strip.
- Waveform display now uses dBFS scale markings and a smoother curve while preserving plotted extrema.
- Waveform and spectrum display now use an oversampled mono display buffer driven by the selected true-peak oversampling ratio.
- Spectrum display now uses a horizontally rolling, Mel-scaled, 192-bin FFT-derived display instead of coarse probe buckets.

### Fixed

- Custom ComboBox template now opens its dropdown when clicked.
- Embedded the main window icon as a WPF resource so startup no longer crashes while loading `Icon`.

### Verified

- `dotnet test .\desktop-loudness-meter.sln` with 73 passing tests.
- `dotnet format .\desktop-loudness-meter.sln --verify-no-changes`
- WPF app smoke launch stayed running for the probe window and was then terminated.

## v0.2.0 - 2026-06-16

### Added

- `Pop-up` detail windows for individual app rows.
- Independent topmost toggle per detail pop-up.
- Independent stereo image scale settings per detail pop-up.
- Inline detail stereo scale settings.
- Stereo image scale dialog with auto scale, dB range, manual top, and reset controls.
- Auto-scale lower limit and speed controls for stereo image scaling.
- Per-app true-peak oversampling selectors in inline and pop-up detail views.
- True-peak oversampling changes now reach the active per-process analyzer worker.

### Fixed

- Pinned the stereo scale button beside the `Stereo image` label so changing coordinate text does not shift it.
- Retained process-loopback workers briefly when an app drops its render session handle.

### Verified

- `dotnet build .\desktop-loudness-meter.sln --no-restore`
- `dotnet test .\desktop-loudness-meter.sln --no-restore` with 67 passing tests.
- `dotnet format .\desktop-loudness-meter.sln --verify-no-changes`
- WPF app smoke launch stayed running for the probe window and was then terminated.

## v0.1.0 - 2026-06-16

First usable developer-run version.

### Added

- Floating WPF/.NET 8 desktop meter.
- WASAPI render-session enumeration with app-level row normalization.
- Process-loopback capture path for per-process PCM on supported Windows builds.
- Peak/LUFS display mode selector.
- Inline detail view with momentary, short-term, and integrated loudness.
- Recent-window true peak, 100 ms RMS, polar stereo image, and correlation.
- Global and per-app reset controls.
- Always-on-top toggle.
- Startup/crash logging under `%LOCALAPPDATA%\DesktopLoudnessMeter`.

### Fixed

- Corrected process-loopback activation to use `VAD\Process_Loopback`.
- Hid the meter app from its own visible app list.
- Collapsed duplicate app/session rows.
- Filtered silent active sessions.
- Kept detail view attached across quick session-handle release/recovery.
- Changed detail true peak from sticky max to a recent-window meter.
- Changed stereo fan sample dots from Cartesian display to polar display.

### Verified

- `dotnet build .\desktop-loudness-meter.sln --no-restore`
- `dotnet test .\desktop-loudness-meter.sln --no-restore` with 55 passing tests.
- `dotnet format .\desktop-loudness-meter.sln --verify-no-changes`
- Live NAudio render probes for process-loopback metrics, self-filtering, true-peak motion, and polar stereo point population.

### Known Limits

- No installer or packaged release artifact yet.
- No persisted settings yet.
- Process-loopback remains OS/session dependent.
- DSP behavior has synthetic tests but is not yet certified against official loudness reference vectors.
