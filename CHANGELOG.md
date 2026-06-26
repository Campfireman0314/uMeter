# Changelog

## Unreleased

- Nothing yet.

## v2.3.2 - 2026-06-27

Rendering and loudness-clock polish patch.

### Added

- Added an optional loudness clock with configurable cycle duration, loudness range, warning level, fan fill, and current-position scan line.
- Added over-maximum loudness classification so clipped clock values render with a stronger danger color at the outer display boundary.

### Changed

- Waveform rendering now uses viewport-bounded point budgets to reduce long-session drag on lower-end CPUs.
- Spectrograph rendering now adapts its history and frequency resolution to the viewport and avoids rollover stripe artifacts.
- Inline and pop-up detail title editing now use the same source-title editor pattern as source rows.
- Clock controls now appear before Waveform controls in detail visualization toggles.
- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.3.2`.
- Default versioned installer artifact is now `uMeter-Setup-2.3.2.exe`.

### Fixed

- Loudness clock fan rendering bridges cycle-origin wraps so the twelve-o'clock origin does not show a thin slit.

### Verified

- `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` with 258 passing tests.
- `dotnet build .\desktop-loudness-meter.sln`.

## v2.3.0 - 2026-06-26

Source continuity and workspace-control release.

### Added

- Added single-instance desktop launch coordination; secondary launches request activation of the existing app and exit before creating receivers, tray icons, settings writers, or windows.
- Added inline row-title alias editing from the global source list.
- Added per-source hide/restore controls plus a global `Show hidden` toggle.
- Added persistent source ordering controls that survive refreshes and WASAPI hibernation/reconnect when source keys remain stable.
- Added spectrograph diagnostics around tone localization, oversampling invariance, and sharp renderer bucket mapping.

### Changed

- Same-handle loopback re-registration now reconfigures an existing source instead of closing/recreating it.
- JUCE sender same-handle restarts no longer emit an intervening `close` packet during sample-rate/channel reconfiguration.
- Temporary handle loss and analyzer warmup no longer blank existing waveform/spectrum roll buffers.
- Spectrograph rendering now uses direct bucket-to-row mapping and nearest-neighbor bitmap scaling instead of hidden vertical smoothing/gap filling.
- Spectrum FFT input now uses raw PCM history instead of the true-peak oversampled waveform buffer, keeping dominant bucket placement stable across true-peak oversampling ratios.
- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.3.0`.

### Verified

- `dotnet test .\tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` with 231 passing tests.
- `dotnet build .\desktop-loudness-meter.sln`.
- `.\scripts\build-juce-sender.ps1 -Format VST3 -Configuration Debug -RunTests`.
- `.\build-installer.ps1 -Configuration Release` produced `artifacts\uMeter-Setup-2.3.0.exe` with SHA256 `5C960D7F41969773738928B3B6145B8CED35F9278D838246B64D9995FE977263`.

## v2.2.8 - 2026-06-21

RfRapid true-peak acceleration release.

### Added

- Added a lightweight `DesktopLoudnessMeter.Benchmarks` console harness for comparing managed and RfRapid-backed true-peak reconstruction.

### Changed

- True-peak oversampled reconstruction can now use `DesktopLoudnessMeter.Native.dll` RfRapid acceleration, with automatic managed fallback when the native DLL is unavailable.
- Native true-peak processing is batched per audio block and preserves the existing 100 ms rolling true-peak window plus waveform/spectrum display samples.

### Verified

- RfRapid true-peak benchmark on a 30 s stereo 48 kHz signal: 8x improved from about `1.95 Mframes/s` managed to about `4.44 Mframes/s` native with matching peak output.
- `.\build-installer.ps1 -Configuration Release -Version 2.2.8 -JuceDir E:\JUCE`.
- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --no-restore`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --no-restore` with 206 passing tests.

## v2.2.7 - 2026-06-21

Detail-window and Milk White graph-contrast polish release.

### Changed

- Pop-up detail windows are no longer owned by the global window, preventing the global `Always on top` state from forcing detail windows above other apps.
- Pop-up detail windows still open near the main window when the main window is visible, and fall back to center screen when opened from a hidden/tray state.
- Milk White graph colors now use a warmer parchment plot track, muted porcelain-blue signal, and stronger amber spectrum accent for clearer waveform, stereo, spectrum, and collision displays.
- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.7`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.7.exe`.

### Verified

- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --no-restore`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --no-restore` with 205 passing tests.
- JUCE VST3 native CTest Debug suite.
- `.\build-installer.ps1 -Configuration Release -Version 2.2.7 -JuceDir E:\JUCE`.

## v2.2.6 - 2026-06-21

Rendering architecture and platform-strategy patch release.

### Added

- Shared `DeferredRenderUpdate` helper for coalescing waveform and spectrum plot updates onto one queued render callback per control.
- Shared `PlotBackgroundCache` helper for waveform, spectrum, and collision analyzer static plot backgrounds.
- Named `PlotColorRole` lookups for main graph controls.
- Rendering architecture notes covering current graph responsibilities and next cleanup candidates.
- Platform strategy notes separating reusable source, protocol, metering, theme, event, AAX, macOS/AU, and iOS boundaries.

### Changed

- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.6`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.6.exe`.

### Verified

- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --no-restore`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --no-restore` with 204 passing tests.
- JUCE VST3 native CTest Debug suite.
- `.\build-installer.ps1 -Configuration Release -Version 2.2.6 -JuceDir E:\JUCE`.

## v2.2.5 - 2026-06-21

Mix-event and sender-status patch release.

### Added

- Conservative `Possible overlap` Recent Events are generated from enabled detail collision analysis and deduplicated while the same overlap region remains active.
- Local loopback receivers now reply to valid sender packets with lightweight `ack` packets.
- JUCE VST3 sender editor now shows live receiver status: waiting, connected, or sending with no recent reply.
- JUCE sender native tests cover ack packet shape, ack matching, and sender state transition after receiver acknowledgement.

### Changed

- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.5`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.5.exe`.
- VST3 sender status indicator layout and painting now keep the indicator circular and visually calmer in the plugin header.

### Fixed

- Reset no longer leaves waveform and spectrograph rolls stuck waiting for pre-reset plot-frame versions.

### Verified

- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --no-restore`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` with 204 passing tests.
- JUCE VST3 native CTest Debug suite.
- `.\build-installer.ps1 -Configuration Release -Version 2.2.5 -JuceDir E:\JUCE`.

## v2.2.4 - 2026-06-21

Recent Events and global event-settings patch release.

### Added

- Global Recent Events strip for source appeared/disappeared, true-peak max, and loudness-jump events.
- Recent Events display filters for all, source, peak, and loudness event types.
- Configurable maximum retained recent-event entries, persisted in the user workspace.
- Global settings window with event-entry limit and future event output folder selection.
- Fixed-height Recent Events result area with an empty state for filters that have no matching events.

### Changed

- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.4`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.4.exe`.

### Verified

- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj` with 198 passing tests.

## v2.2.3 - 2026-06-21

Frequency-collision and source-workspace polish patch release.

### Added

- Source aliases can be edited from the row, inline detail, and pop-up detail naming bars.
- Per-source analysis preferences persist through the user workspace, including detail visibility, collision source, collision thresholds, spectrum display range, and stereo display settings.
- Frequency collision settings now include a prominence threshold so flat broadband content is not treated as a full-band feature collision.
- Collision analyzer single-value sliders support double-click numeric entry, matching the spectrograph range slider interaction.

### Changed

- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.3`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.3.exe`.
- Collision analyzer "Absolute" threshold has been redefined as `Level floor`, requiring both compared bins to clear the floor before analysis.
- Collision analyzer "Relative" threshold is now labeled `Match tolerance`.
- Collision analysis now requires locally prominent spectral features from both sources before highlighting a band.

### Fixed

- Flat broadband noise no longer lights the full collision analyzer when compared against peaked musical content.
- Collision source and detail analyzer preferences survive source reconnects when source keys remain stable.

### Verified

- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --no-restore -p:OutDir=E:\GitHub\uMeter\.tmp\v223-app-build\`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --no-restore -p:OutDir=E:\GitHub\uMeter\.tmp\v223-test-build\` with 189 passing tests.

## v2.2.2 - 2026-06-20

Spectrograph visual polish patch release.

### Changed

- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.2`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.2.exe`.
- Spectrograph rendering now uses a soft visual floor and light neighboring-bin smoothing so low-energy history looks continuous without mutating stored spectrum levels.

### Fixed

- Spectrograph rendering avoids thin full-width rollover gaps after the history wraps.
- Spectrograph color mapping avoids harsh black holes inside low-energy spectrum regions.
- Display dB range slider supports double-click numeric entry for direct min/max edits.

### Verified

- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj --no-restore -p:OutDir=E:\GitHub\uMeter\.tmp\app-build\`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --no-restore -p:OutDir=E:\GitHub\uMeter\.tmp\test-build\` with 182 passing tests.

## v2.2.1 - 2026-06-20

Spectrum crash-fix patch release.

### Changed

- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.2.1`.
- Default versioned installer artifact is now `uMeter-Setup-2.2.1.exe`.
- Spectrograph display range now covers `-200 to 0 dB` and defaults to `-120 to 0 dB`.

### Fixed

- Spectrum roll rendering now tolerates transient WPF `WriteableBitmap` read/write lock conflicts by deferring and retrying bitmap writes instead of terminating the app.

### Verified

- `dotnet build src\DesktopLoudnessMeter.App\DesktopLoudnessMeter.App.csproj`.
- `dotnet test tests\DesktopLoudnessMeter.Tests\DesktopLoudnessMeter.Tests.csproj --no-restore` with 182 passing tests.

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
