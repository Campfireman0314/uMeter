# Test Report: 03-process-loopback-integration

## Task 1: Merge process analyzer metrics into session snapshots

### Red Phase
- **Timestamp:** 2026-06-16 15:55
- **Failing Tests:**
  - `WasapiSessionMeterServiceTests` - `CS0246`: process metric provider seam did not exist yet.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-16 15:57
- **Passing Tests:**
  - `WasapiSessionMeterServiceTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/IProcessMetricProvider.cs` - added - Defined process metric provider contract.
  - `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs` - modified - Merged process analyzer metrics by PID and preserved peak fallback.
  - `tests/DesktopLoudnessMeter.Tests/Audio/WasapiSessionMeterServiceTests.cs` - added - Covered metric merge and provider failure fallback.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Sessions with process IDs receive available analyzer metrics. | `WasapiSessionMeterServiceTests.MergesAvailableProcessMetricsIntoSnapshots` | pass |
| Sessions without process IDs keep sample-derived metrics unavailable. | `WasapiSessionMeterServiceTests.MergesAvailableProcessMetricsIntoSnapshots` | pass |
| Provider receives visible process ID set each refresh. | `WasapiSessionMeterServiceTests.MergesAvailableProcessMetricsIntoSnapshots` | pass |
| Provider failure does not escape refresh. | `WasapiSessionMeterServiceTests.ProviderFailuresDoNotEscapeRefresh` | pass |

## Task 2: Add process-loopback worker lifecycle and PCM conversion

### Red Phase
- **Timestamp:** 2026-06-16 15:58
- **Failing Tests:**
  - `ProcessLoopbackMetricProviderTests` - `CS0246`: worker factory/worker lifecycle contracts did not exist yet.
  - `WaveFormatSampleConverterTests` - `CS0103`: PCM conversion helper did not exist yet.
- **False Red Fixes:** none

### Green Phase
- **Timestamp:** 2026-06-16 16:00
- **Passing Tests:**
  - `ProcessLoopbackMetricProviderTests` - pass
  - `WaveFormatSampleConverterTests` - pass
  - `WasapiSessionMeterServiceTests` - pass
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackMetricProvider.cs` - modified - Added worker lifecycle, stale cleanup, failure backoff, and snapshot lookup.
  - `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackCaptureWorker.cs` - added - Added Windows process-loopback capture worker that feeds `PcmMetricAnalyzer`.
  - `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackInterop.cs` - added - Added `ActivateAudioInterfaceAsync` process-loopback activation interop.
  - `src/DesktopLoudnessMeter.App/Audio/WaveFormatSampleConverter.cs` - added - Added IEEE float and PCM 16-bit conversion into interleaved float samples.
  - `tests/DesktopLoudnessMeter.Tests/Audio/ProcessLoopbackMetricProviderTests.cs` - added - Covered start/stop lifecycle, backoff, and metric lookup.
  - `tests/DesktopLoudnessMeter.Tests/Audio/WaveFormatSampleConverterTests.cs` - added - Covered float32 and PCM16 conversion.
- **Defect Found During Smoke:**
  - A recoverable `0x80070002` process-loopback activation failure initially crashed a background thread; `FileNotFoundException` is now treated as recoverable and logs as unavailable instead.
  - A second probe found the interop path was using the SDK macro name instead of the required virtual device literal. `ProcessLoopbackInterop.VirtualAudioDeviceProcessLoopback` is now pinned to `VAD\Process_Loopback` with a regression test.
- **Acceptance Criteria Coverage:**

| Acceptance Criterion | Test Case | Status |
|----------------------|-----------|--------|
| Provider starts new workers and stops stale workers. | `ProcessLoopbackMetricProviderTests.StartsNewWorkersAndStopsStaleWorkers` | pass |
| Failed starts are backed off. | `ProcessLoopbackMetricProviderTests.FailedStartsAreBackedOff` | pass |
| Provider returns worker snapshots. | `ProcessLoopbackMetricProviderTests.ReturnsWorkerSnapshotForProcess` | pass |
| IEEE float and PCM16 buffers convert to float samples. | `WaveFormatSampleConverterTests.ConvertsIeeeFloat32Buffer`, `ConvertsPcm16Buffer` | pass |

## Task 3: Increase default detail viewport height

### Direct Validation
- **Timestamp:** 2026-06-16 16:03
- **Build Result:** `dotnet build desktop-loudness-meter.sln` passed.
- **Change Summary:**
  - `src/DesktopLoudnessMeter.App/MainWindow.xaml` - modified - Increased default height from 620 to 760 and minimum height from 440 to 620 while preserving detail scroll behavior.

## Live Loopback Validation

- [x] [automated probe] `ProcessLoopbackCaptureWorker` captured a 440 Hz NAudio render stream from the current process. Result: `faulted=False`, momentary/short/integrated LUFS populated, true peak populated, RMS populated, correlation populated.
- [x] [automated probe] `WasapiSessionMeterService` merged process-loopback metrics for an active render session from the same process. Result: momentary LUFS populated after the 400 ms window; short-term LUFS populated after the 3 s window.
- [x] [automated probe] `WasapiSessionMeterService` merged process-loopback metrics for an external child process. Result: the target process row populated LUFS, true peak, RMS, and correlation.

## Smoke Validation

- `dotnet build desktop-loudness-meter.sln` passed with 0 warnings and 0 errors.
- `dotnet test desktop-loudness-meter.sln --no-restore` passed with 49 tests.
- `dotnet format desktop-loudness-meter.sln --verify-no-changes` passed.
- Direct exe launch stayed alive for 6 seconds and produced a main window handle/title: `Desktop Loudness Meter`.
- After the virtual device path fix, live service probes produced non-null process-loopback metrics instead of `0x80070002` activation failures.
