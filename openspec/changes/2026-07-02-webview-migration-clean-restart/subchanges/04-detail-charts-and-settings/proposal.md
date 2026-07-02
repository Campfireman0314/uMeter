# 04-detail-charts-and-settings

## Goal
Migrate detail analysis modules and their settings to the WebView surface without regressing metering correctness, persisted visualization preferences, or rendering performance.

## In Scope
- Expose detail data and settings for waveform, spectrum, stereo, true peak, loudness clock, and frequency collision displays.
- Preserve per-source analysis settings and detail module preferences, including visibility, order, height where applicable, loudness-clock range/cycle/warning, and reset behavior.
- Reuse validated analysis and mapping logic from `main` unless the harness marks a WebView-specific redesign as acceptable.
- Apply explicit active gates so hidden or collapsed detail modules do not continue expensive render work.
- Account for circular loudness-clock wrap handling, spectrum/waveform resolution bounds, and render throttling in the WebView architecture.

## Out of Scope
- Replacing the analyzer algorithms with unrelated DSP implementations.
- Source workflow commands that belong to `03-hostbridge-source-workflows`.
- Installer or native DLL packaging decisions.

## Depends On
- `01-migration-harness-and-contract-audit` for metering and rendering acceptance criteria.
- `02-webview-shell-foundation` for frontend hosting.
- `03-hostbridge-source-workflows` for stable source selection and settings identity.
- Existing `analysis-rendering` behavior from `main`.

## Done Means
- Detail visualizations and controls are available in the WebView surface with preserved per-source settings.
- Rendering work is bounded by visibility, viewport size, and refresh cadence.
- Loudness clock, waveform, spectrum, true peak, stereo, and collision behavior remain covered by unit or characterization tests.
- The WebView surface does not keep a second independent settings model for migrated detail state.
