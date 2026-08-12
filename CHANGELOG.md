# Changelog

## Unreleased

### Added

- Added the localized `Signal Shuffle` layered triple-tile game in an owned WPF window, opened from a game icon beside the theme selector.
- Added a selectable meter-source feed for a background sheep whose bounded bounce, lean, and squash follow bass energy and detected onsets.
- Added deterministic solvable board generation, seven-slot tray matching, restart/new-board controls, and win/loss states without reusing the referenced game's name or assets.
- Added an opt-in live beat monitor with adaptive bass-onset diagnostics and sensitivity tuning.
- Added a rig-ready Plan 2 art contract covering the model sheet, pivot map, separated layer names, hidden joint overlap, and an explicit Cubism decision gate.
- Added a dedicated tall-silhouette WPF sheep rig with seven wool masses, parent-child shoulder/elbow/wrist and hip/knee/ankle chains, split hooves, two-stage ears and tail, expression parts, and a fixed rear stack for legs and wool-occluded arm roots.
- Added tile-local selection splashes plus a larger tray-centered glyph, ring, and particle burst when a triple clears.
- Added cosmetic beat-window timing with `Perfect`/`On beat` feedback, a slowly decaying groove meter, and a four-pulse phrase strip without changing legal moves or difficulty.
- Added confidence-gated BPM and beat-phase display from recent onset intervals.
- Added fixed low/mid/high spectral affinity outlines for tile families and a board-centered match echo on the next accepted onset.
- Added spectral-flux, cross-band agreement, cross-correlation, and onset-autocorrelation diagnostics to the beat debug overlay.
- Added four original signal tile kinds (`Wave`, `Pulse`, `Disc`, and `Speaker`) for the initial twelve-kind hard board.
- Added `Meter`, `Orbit`, `Bars`, and `Arrows`, bringing the low-branch board to sixteen symbol families.
- Added explicit same-seed Restart and different-seed New Game actions with board entrance transitions.
- Added one Cannon, Fan, and Digger per round: Cannon clears tray groups, Fan reconstructs a solvable table shuffle, and Digger adds an eighth tray slot.
- Removed global layer-depth concealment; cards now remain identifiable unless they are genuinely overlapped by a higher card.
- Replaced generic prop glyphs with dedicated wheeled-cannon, folding-fan, and shovel vector icons.
- Added per-card Cannon ballistics that launch the stack and drop it beyond the window boundary.
- Added a two-stage Fan sequence that blows the stack above the window and restacks shuffled cards from all four corners.
- Added a localized, non-blocking full-width celebration banner animation when the board is cleared.
- Added three articulated high-V sheep cheer jumps for wins.
- Added an inward articulated sheep rag-doll loss animation with loose limb chains, delayed head/ears/tail, closed eyes, and a held collapsed pose.

### Changed

- Tightened the game window and table around the compact reference-shaped pile while keeping tile symbols upright.
- Replaced bass-only onset detection with an adaptive multi-band detector combining spectral flux, synchronized band evidence, cross-band correlation, onset autocorrelation, and confidence-bounded periodic threshold support.
- Reworked sheep motion into a centered 460 ms takeoff/apex/landing jump with symmetric knees; detected beats inside a 320 ms animation guard no longer restart the sheep mid-air.
- Replaced the independent greeting wave with a beat-aligned cheerleading arm path: low V at takeoff and landing, high V at the airborne apex, and restrained opposite elbow follow-through.
- Made the board plate 50% opaque and increased sheep visibility while preserving fully opaque tiles and controls.
- Replaced gated spectral-affinity rims with continuous band-share envelopes using an 80 ms attack and 220 ms release.
- Expanded the board from 42 tiles in three layers, first to 102 tiles and then to a guaranteed-solvable 222-tile, thirteen-layer 5-by-7 layout inspired by the viral mini-program's general difficulty pacing, without copying a published map.
- Refactored Signal Shuffle topology generation from seven independent coordinate masks into one declarative, bilaterally symmetric depth map with validated nested courses.
- Rebuilt the pile around reference-like cap stacks, side rails, a continuous eight-card bridge, a narrow transition, and a compact rectangular lower block, using original coordinates and symbols rather than copying the reference map.
- Added a consistent 0.94-card anchor pitch and symmetric inward stack fan, reducing the opening to sixteen selectable stack heads while preserving overlapping blocker routes.
- Expanded the board height to contain the ten-row base footprint, added full-card bounds regression coverage, lightened true geometric occlusion, and reduced/repositioned the dancing sheep so it no longer obscures gameplay.
- Changed generation from independent per-layer triples to a constructive solution sequence with triples crossing layer boundaries, increasing tray pressure while keeping every seed winnable.
- Deepened the 222-card board from eleven to thirteen layers with a four-card opening, and increased symbol families from twelve to sixteen.
- Replaced adjacent-layer color separation as the difficulty proxy with automated constructed-path evaluation. After one readable opening triple, the guaranteed solution now uses twenty-four randomized nine-card pressure blocks that interleave three families and repeatedly drive the seven-slot tray to six cards.
- The 48-candidate search now preserves one opening family and maximizes simulated tray pressure, family switching, new-family decoys, and dangerous non-completing choices while penalizing obvious alternate completions.
- Kept cards on the highest remaining layer and the layer immediately below identifiable, while fully obscuring every card two or more layers below that global top layer.
- Removed the abrupt 111-card tool lock; Cannon, Fan, and Digger are optional strategic aids, while difficulty comes from the generated layer and symbol arrangement.
- Kept product, setup, shared components, VST3, AAX, and installer defaults on the `2.4.0` release line.

### Fixed

- Sender-driven host startup now detects the resolved uMeter executable in the current Windows session and suppresses secondary launches when the desktop host is already running.
- The VST3 editor reports an already-running host as a successful no-op instead of starting a process that triggers single-instance activation.
- The bonus-game audio-source selector now renders real source names and saved aliases instead of falling back to view-model type names.

## v2.4.0 - 2026-08-05

Localization and host-launch workflow release.

### Added

- Desktop app, VST3 sender editor, and installer localization for Simplified Chinese, Japanese, and Russian, with English fallback.
- Automatic OS-language selection plus a persisted live language selector in Global Settings.
- Per-user Windows startup registration and optional notification-area startup.
- Sender-driven automatic host invocation and a manual `Start uMeter` button in the VST3 editor.

### Changed

- Global Settings and the installer use wider, wrapping layouts for longer localized labels and explanations.
- Automatic sender launches can add `--background`; manual plugin launches always open uMeter visibly.
- Notification-area startup can be selected independently of Windows startup and sender auto-invocation.
- VST3 host launching resolves custom installer locations from the per-user uninstall registry entry and falls back to `%LOCALAPPDATA%\Programs\uMeter`.
- Product, setup, shared component, VST3, and AAX metadata now report version `2.4.0`.

### Fixed

- Opening a VST monitor pop-up from the tray no longer assigns a never-shown hidden main window as its owner and crash WPF.
- True Peak is no longer abbreviated as `TP`, and 2x, 4x, and 8x oversampling labels express the multiplier in each supported language.
- Diagram-setting flyouts now localize their detached WPF popup content, including labels and explanations.
- Detail source status, unavailable values, true-peak maximum readouts, and the stereo correlation label now follow the selected language.
- Inline and pop-up detail source identities, raw WASAPI active/inactive states, spectrum ranges, and loudness-clock ranges now follow the selected language.
- Process-backed app rows now require non-silent process-loopback PCM when that capture path is available, keeping capture-only apps hidden while still allowing real monitoring output.
- The hide-after-automatic-start option remains available when Windows startup and sender auto-invocation are disabled.

### Verified

- Managed suite: 323 passing tests.
- JUCE sender native suite: 6 passing groups.
- Debug solution build, Release VST3 build, format verification, and installer packaging passed.
- `uMeter-Setup-2.4.0.exe` SHA-256: `761ADEC643E13FC105817F3404BD5C824D880A7023DF470E28489C85B0631D87`.

## v2.3.5 - 2026-08-05

VST3 sender recovery, detail metering, and workspace reliability patch.

### Changed

- Detail-module drops are direction-aware: upward drags insert before the target and downward drags insert after it.
- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.3.5`.
- Default versioned installer artifact is now `uMeter-Setup-2.3.5.exe`.
- Detailed numeric meters can independently inspect Overall, Mid, or Side signal measurements.
- Detail meter and diagram explanations can be enabled or disabled from Global Settings.
- Detail measurement selectors use compact borderless triangle controls, with `(I)`, `(M)`, and `(S)` title markers showing the active mode.
- Hover explanations activate only on meter titles and diagram banners, dismiss on pointer movement, and render inside the existing window visual tree.

### Fixed

- VST3 sender discovery remains active when a DAW releases audio resources during hibernation or audio-graph deactivation.
- An unacknowledged VST3 sender periodically renews its localhost UDP transport so a stale socket cannot require a DAW restart.
- Inline and pop-up detail-module drags capture from button-down and use preview drag events, preventing child charts and controls from interrupting the gesture.
- Moving a detail module to the top no longer requires hitting the upper half of a narrow target region.
- Closing the main window now closes owned detail popups and exits the application.
- Mid and Side loudness, true-peak, and RMS measurements remain scoped to detail views without changing global rows or event detection.
- Floating explanations no longer create separate tooltip windows that can briefly pause meter and diagram rendering.

### Verified

- `dotnet test .\desktop-loudness-meter.sln --no-restore` with 295 passing tests.
- JUCE sender native CTest passed, including five consecutive transport-recovery runs.
- Debug solution build and Release VST3 build passed.
- `dotnet format .\desktop-loudness-meter.sln --verify-no-changes --no-restore` passed.
- Release installer packaging passed with desktop app, native DSP DLL, and VST3 sender payloads.
- `uMeter-Setup-2.3.5.exe` SHA-256: `CE35B2C1FEA9C63DB10AD8EF8389897829D62E61AE4C0A1683F9BAB397A9D43C`.

## v2.3.4 - 2026-06-27

Draggable detail-graph polish patch.

### Changed

- Collision analyzer modules keep their original compact fixed height and no longer expose a stretch handle.
- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.3.4`.
- Default versioned installer artifact is now `uMeter-Setup-2.3.4.exe`.

### Fixed

- Detail graph reorder hints no longer animate layout height, avoiding block twitch near neighboring module rims.
- Hidden waveform, spectrograph, and loudness-clock modules now stop tick-driven render work while inactive.
- Detail resize grips use the themed handle template when graph-specific visibility styles are applied.
- Source and detail title banners now show `Double click to edit alias` on hover.

### Verified

- `dotnet test .\desktop-loudness-meter.sln --no-restore` with 275 passing tests.
- `dotnet build .\desktop-loudness-meter.sln --no-restore`.

## v2.3.3 - 2026-06-27

UI reliability, detail-layout preset, and loudness-clock interaction patch.

### Added

- Added JSON detail-layout presets with save/load icons for inline and pop-up detail panels.
- Added a global setting that can load the default detail layout from a preset file, with validation and built-in fallback when the file is invalid.
- Added loudness-clock time scale markers and a hover readout blob for time/value inspection.

### Changed

- Moved detail-layout preset save/load icons into the top detail header beside the detail controls.
- Tray right-click handling now uses the native `NotifyIcon.ContextMenuStrip` open flow and rebuilds the menu on open.
- Theme changes now refresh resource-backed controls across open windows and popups so slider colors update immediately.
- App, setup, protocol, sender kit, sender probe, VST3 sender project, and AAX sender project metadata now report version `2.3.3`.
- Default versioned installer artifact is now `uMeter-Setup-2.3.3.exe`.

### Verified

- `dotnet test .\desktop-loudness-meter.sln --no-restore` with 269 passing tests.
- `dotnet build .\desktop-loudness-meter.sln --no-restore`.

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
