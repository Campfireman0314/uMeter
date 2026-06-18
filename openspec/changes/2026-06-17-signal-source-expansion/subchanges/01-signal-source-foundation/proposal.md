# 01-signal-source-foundation

## Goal
Refactor the current WASAPI-session-centered model into a generic signal-source model while preserving the full v1.1.0 WASAPI behavior.

## In Scope
- Introduce source-domain naming and contracts, such as signal source snapshots, stable source IDs, source kinds, source lifecycle state, and provider interfaces.
- Adapt the existing WASAPI render-session and process-loopback path behind the new source contracts.
- Keep current analyzer, reset, true-peak oversampling, waveform, spectrum, pop-up, and tray behavior working through the new source identity.
- Add a minimal UI source-type marker so existing rows are explicitly labeled as WASAPI/session sources.
- Add characterization tests proving existing WASAPI rows, transient-handle retention, detail tracking, reset routing, and settings routing survive the refactor.

## Out of Scope
- Local network audio ingest.
- VST3 or AAX plugin binaries.
- User-driven source renaming beyond preserving existing app display names.
- A full source-management screen.

## Depends On
- Current v1.1.0 app architecture.
- Existing `WasapiSessionMeterService`, `AudioSessionSnapshot`, `IProcessMetricProvider`, and view-model tests as characterization anchors.

## Done Means
- Existing WASAPI functionality behaves the same under source-oriented names and contracts.
- UI rows visibly identify their source type.
- The app can later add a non-WASAPI provider without teaching every view model about plugin/network internals.
