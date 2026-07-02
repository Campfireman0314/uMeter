---
status: active
created_at: 2026-06-26
created_from: change:2026-06-26-v2.3.0-polish-02-spectrograph-fidelity
last_verified_at: 2026-07-01
last_verified_by: opsx-knowledge
verification_basis: archive-follow-up
applies_to:
  - src/DesktopLoudnessMeter.App/Audio/Analysis
  - src/DesktopLoudnessMeter.App/Controls/WaveformRollView.cs
  - src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs
  - src/DesktopLoudnessMeter.App/Controls/SpectrumRollColumnRasterizer.cs
  - src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs
  - src/DesktopLoudnessMeter.App/Controls/LoudnessClockFanSegmentPlanner.cs
  - src/DesktopLoudnessMeter.App/Controls/LoudnessClockScalePlanner.cs
  - src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs
  - src/DesktopLoudnessMeter.App/Controls/FrequencyCollisionBar.cs
  - src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs
  - src/DesktopLoudnessMeter.Web/src/charts
  - src/DesktopLoudnessMeter.Web/src/charts/chartGeometry.test.ts
source_refs:
  - change:2026-06-30-webview-chart-parity
  - evidence:openspec/changes/archive/2026-06-30-webview-chart-parity/evidence.md
  - change:2026-07-01-native-host-projection-core
  - evidence:openspec/changes/archive/2026-07-01-native-host-projection-core/evidence.md
superseded_by:
---

# Analysis Rendering

## Responsibility
Converts PCM into loudness, waveform, spectrum, stereo, loudness-clock, and collision displays. WPF remains the detailed rendering oracle for rich controls; the WebView path now consumes host-projected source arrays/settings and renders selected-detail waveform, spectrum, stereo, loudness clock, and collision modules with Canvas. Host selected-detail collision bands are now returned through native `DlmHostSelectedDetailSnapshot`, which composes `DlmHostCollisionProjection`; WPF local collision event workflows remain managed.

## Key Files

| File | Role |
|------|------|
| src/DesktopLoudnessMeter.App/Audio/Analysis/RecentSampleWindow.cs | Rolling PCM metrics, raw spectrum sample history, waveform display samples, and FFT bucket generation |
| src/DesktopLoudnessMeter.App/Controls/WaveformRenderResolution.cs | Testable plot-width to waveform point-budget mapping |
| src/DesktopLoudnessMeter.App/Controls/WaveformRollView.cs | Adaptive waveform display-point generation, pan/zoom, and high-density line rendering |
| src/DesktopLoudnessMeter.App/Controls/SpectrumRollResolution.cs | Testable viewport-size to spectrograph history/frequency resolution mapping |
| src/DesktopLoudnessMeter.App/Controls/SpectrumRollBitmapSegmentPlan.cs | Testable wrapped-history full-bitmap draw mapping that avoids per-render cropped bitmap sources |
| src/DesktopLoudnessMeter.App/Controls/SpectrumRollColumnRasterizer.cs | Testable bucket-to-pixel column conversion, max-preserving row resampling, and display-floor transform |
| src/DesktopLoudnessMeter.App/Controls/SpectrumRollView.cs | Dynamic-resolution rolling WPF bitmap, pan/zoom, recoloring, and nearest-neighbor spectrograph rendering |
| src/DesktopLoudnessMeter.App/Controls/LoudnessClockPointMapper.cs | Testable momentary loudness-to-circular-radius, elapsed cycle phase, warning classification, and over-maximum clipping mapping |
| src/DesktopLoudnessMeter.App/Controls/LoudnessClockFanSegmentPlanner.cs | Testable loudness clock fan segment planning, including cycle-origin bridging at wrap boundaries |
| src/DesktopLoudnessMeter.App/Controls/LoudnessClockScalePlanner.cs | Testable loudness clock scale marker calculation with at least five rings |
| src/DesktopLoudnessMeter.App/Controls/LoudnessClockView.cs | Optional circular momentary loudness history with bounded timestamped samples, stable cycle phase, fan fill, warning/danger color, current-position scan line, and reset clear behavior |
| src/DesktopLoudnessMeter.App/ViewModels/FrequencyCollisionAnalyzer.cs | Managed prominent overlap detection and band compaction used by WPF local/fallback collision workflows and native parity tests |
| src/DesktopLoudnessMeter.App/ViewModels/CollisionFrequencyScaleMapper.cs | Mel/logarithmic display ratio conversions for collision bands and bars |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostCollisionProjection.cs | Lower-level Host collision band P/Invoke wrapper over native projection, still covered by parity tests |
| src/DesktopLoudnessMeter.Native/DlmHostCollisionProjection.* | Native Host collision projection core for focus/comparison spectrum band analysis and compact bands |
| src/DesktopLoudnessMeter.App/Audio/Analysis/NativeHostSelectedDetailSnapshotPlanner.cs | Host selected-detail snapshot P/Invoke wrapper that returns chart module visibility and collision bands for Host/Web DTOs |
| src/DesktopLoudnessMeter.Native/DlmHostSelectedDetailSnapshot.* | Native selected-detail snapshot core that composes module rows, shell metadata/defaults, comparison spectra, and collision bands |
| src/DesktopLoudnessMeter.Web/src/charts/chartGeometry.ts | Testable Web chart geometry for waveform paths, spectrum bars, stereo points, loudness clock history, and collision bands |
| src/DesktopLoudnessMeter.Web/src/charts/ChartModuleCanvas.tsx | Web Canvas renderer for selected-detail chart modules, using HostBridge source arrays and per-source chart settings |
| tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollColumnRasterizerTests.cs | One-hot and low-level rasterizer coverage |
| tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollResolutionTests.cs | Spectrograph resolution bounds and viewport scaling coverage |
| tests/DesktopLoudnessMeter.Tests/Controls/SpectrumRollBitmapSegmentPlanTests.cs | Wrapped spectrograph segment draw-plan coverage |
| tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockFanSegmentPlannerTests.cs | Loudness clock continuous and wrapped fan segment coverage |
| tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockPointMapperTests.cs | Loudness clock omission, radius, clockwise angle, warning, and over-maximum mapping coverage |
| tests/DesktopLoudnessMeter.Tests/Controls/LoudnessClockScalePlannerTests.cs | Loudness clock scale marker count and endpoint coverage |
| tests/DesktopLoudnessMeter.Tests/Controls/WaveformRenderResolutionTests.cs | Waveform point-budget bounds and invalid width coverage |
| tests/DesktopLoudnessMeter.Tests/Audio/Analysis/TruePeakAndStereoMetricTests.cs | Spectrum tone localization and true-peak oversampling invariance coverage |
| src/DesktopLoudnessMeter.Web/src/charts/chartGeometry.test.ts | Web chart geometry, settings-consumption, and loudness clock runtime reset coverage |

## Implicit Constraints
- Spectrum FFT input uses original sample-rate PCM history; waveform display continues to use true-peak oversampled display samples.
- Spectrum rasterization may resample analyzer buckets into fewer rows, but it preserves the strongest bucket in each row range and stores normalized levels for recoloring.
- Dynamic render buffers are bounded so viewport growth cannot create unbounded waveform points or spectrograph pixels.
- Wrapped spectrograph history should be rendered by clipping the full bitmap instead of creating cropped bitmap image sources on every render.
- Wrapped spectrograph destination splits should share a snapped device-pixel seam so full-history scroll wraps do not reveal a black stripe.
- Loudness clock samples append only on the dedicated clock plot tick; non-finite loudness on that tick clears the retained trace for reset/source-switch behavior.
- Loudness clock angle mapping is based on elapsed cycle phase, not retained sample list index, so the circular table does not rotate when the cycle wraps.
- Loudness clock rendering keeps bounded timestamped samples and draws filled fan wedges; warning-colored wedges and scan-line segments use the existing warm spectrum color, while over-maximum clipped values use the danger color role at the outer radius.
- Loudness clock current-position scanning is an overlay on the fixed elapsed-cycle table; it narrows renderer inset without changing the stable phase mapping.
- Loudness clock fan segments explicitly bridge through the cycle origin when retained samples wrap from high phase back to low phase, avoiding a visible top-origin slit caused by timer jitter.
- Reused waveform buffers should not publish plot ticks; the visual update path should run when the waveform buffer reference actually changes.
- Hidden waveform, spectrograph, and loudness-clock controls must gate tick-driven work with active/visibility checks so collapsed detail modules do not queue render updates or append clock samples in the background.
- Collision display ratios may be remapped for logarithmic display, but bucket analysis remains based on Mel-spaced spectrum buckets.
- WebView chart parity is intentionally renderer-level parity, not a migration of WPF controls. Web Canvas consumes `HostSourceSnapshotDto` arrays plus `HostSourceSettingsSnapshotDto` settings and must keep empty states dimensionally stable.
- Web loudness clock history is short-lived frontend runtime state keyed by selected source and cycle settings; host/core remain the source of current loudness truth.
- Web frequency collision consumes host-projected compact bands; Web should draw bands only, not duplicate cross-source collision business logic.
- Host selected-detail collision bands are native-backed through the unified selected-detail snapshot planner. Web still consumes compact bands from Host DTOs, and WPF `MainViewModel` local collision events remain managed fallback/oracle behavior.
