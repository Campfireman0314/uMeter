# Source Continuity Reconfiguration Design

## Overview

This slice treats audio infrastructure churn as source hibernation/reconfiguration instead of source death. Stable source identity remains the row owner; analyzers may be rebuilt for new audio format, but UI graph history should survive until a fresh frame replaces it or an explicit reset/true close occurs.

## Key Decisions

- Same-handle loopback/plugin `register` packets are reconfiguration events, not close/open events, even if sample rate or channel count changes. This satisfies R1.
- The JUCE sender distinguishes explicit stop from same-handle reconfigure so host sample-rate changes do not send a misleading close packet. This satisfies R2.
- View-model hibernation is a display state. It mutes live peak/status but keeps last analyzer graph buffers and plot ticks. This satisfies R3.
- Empty or null analyzer graph buffers from a live update do not replace existing rolling buffers unless the update is an explicit reset path. This prevents black slits during worker/analyzer warmup and satisfies R3/R4.
- True explicit close and dead-process removal keep existing lifecycle semantics so stale sources do not linger forever. This satisfies R5.

## Implementation Units

### [U1] Loopback Receiver Reconfiguration
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/LocalLoopbackSignalSourceService.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/LocalLoopbackSignalSourceServiceTests.cs`

Responsibilities:
- Convert same-handle changed-format register handling from remove/re-add to in-place source reconfiguration.
- Preserve handle, source kind, display name, preferences, and lifetime state.
- Rebuild analyzer for changed sample rate/channel count while carrying source-level preferences.

### [U2] JUCE Sender Same-Handle Restart
**Module Boundary**
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.h`
- `src/DesktopLoudnessMeter.JucePlugin/Source/LoopbackSender.cpp`
- `src/DesktopLoudnessMeter.JucePlugin/Tests/LoopbackSenderTests.cpp`

Responsibilities:
- Stop/restart the worker thread without a close packet when `start` receives the same handle.
- Keep explicit `stop` and destruction close behavior.
- Keep register/heartbeat/audio/rename behavior unchanged after restart.

### [U3] Hibernating Row Graph Retention
**Module Boundary**
- `src/DesktopLoudnessMeter.App/ViewModels/AudioSessionMeterViewModel.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/AudioSessionMeterViewModelTests.cs`
- `tests/DesktopLoudnessMeter.Tests/ViewModels/MainViewModelTests.cs`

Responsibilities:
- Keep graph buffers and plot ticks when a row is marked handle-missing.
- Keep existing graph buffers when a live snapshot carries no replacement graph data.
- Preserve explicit reset and true instance-not-found clearing behavior.

## Requirement Tracking

| Requirement | Implementation Unit | Notes |
|-------------|---------------------|-------|
| R1 | U1, U3 | Receiver rebuilds analyzer but row and graph state stay continuous. |
| R2 | U2 | Sender avoids misleading close during same-handle restart. |
| R3 | U3 | Hibernation and empty warmup frames do not clear graph history. |
| R4 | U3 | Existing process-alive retention remains but no longer clears plots. |
| R5 | U1, U2, U3 | Explicit close/exit paths stay destructive. |

## Validation Strategy

- Unit-test loopback same-handle changed sample rate keeps one source and preserves settings.
- Native JUCE test observes packet ordering during same-handle restart.
- View-model unit tests prove handle-missing and empty live updates preserve rolling buffers/ticks, while reset still clears.
- Existing main-view tests cover process-alive retention, plugin close, and reattachment behavior.

## Risks

- Holding last graph buffers can make stale content visible during long hibernation. The row status text must clearly indicate handle-missing/reconfiguring state.
- Analyzer rebuild necessarily resets loudness/true-peak state for changed formats; only visual continuity is preserved until fresh data arrives.
