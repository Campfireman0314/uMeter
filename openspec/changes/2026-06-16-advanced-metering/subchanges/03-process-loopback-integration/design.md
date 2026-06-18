# Process Loopback Integration Design

## Overview

This slice wires the existing WASAPI session rows to the PCM analyzer through a process metric provider. `WasapiSessionMeterService` remains responsible for session enumeration and peak values; a new `IProcessMetricProvider` owns live PCM capture and returns analyzer snapshots keyed by process ID.

## Key Decisions

- Keep process-loopback capture behind an interface so unit tests do not need OS audio devices.
- Use Windows process-loopback activation when available; systems without support keep peak meters and `n/a` sample-derived metrics.
- Request 48 kHz stereo IEEE-float capture with WASAPI auto-conversion so the DSP engine receives the sample rate it currently models best.
- Use per-process worker backoff after activation failure to avoid noisy retries on unsupported systems.
- Increase the default window height while leaving the existing scroll behavior in place.

## Implementation Units

### [U1] Session Snapshot Metric Merge
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/IProcessMetricProvider.cs`
- `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/WasapiSessionMeterServiceTests.cs`

Adds the process metric provider seam and merges analyzer snapshots onto session snapshots by process ID.

### [U2] Process Loopback Provider
**Module Boundary**
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackMetricProvider.cs`
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackCaptureWorker.cs`
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackInterop.cs`
- `src/DesktopLoudnessMeter.App/Audio/WaveFormatSampleConverter.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/ProcessLoopbackMetricProviderTests.cs`
- `tests/DesktopLoudnessMeter.Tests/Audio/WaveFormatSampleConverterTests.cs`

Manages process capture worker lifecycle, interop activation, PCM conversion, analyzer feeding, failure state, and stale-worker cleanup.

### [U3] Detail View Default Size
**Module Boundary**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`

Raises the default/minimum window height so the detail panel is usable without immediate resizing.

## Requirement Tracking

| Requirement | Implementation Unit | Notes |
|-------------|---------------------|-------|
| R1 | U1 | Merge available process metrics into `AudioSessionSnapshot`. |
| R2 | U2 | Start/stop process workers based on visible session PIDs. |
| R3 | U2 | Windows process-loopback activation, PCM conversion, analyzer feeding, and graceful fallback. |
| R4 | U3 | Taller default window. |

## Validation Strategy

- Unit-test provider metric merge using a fake process provider.
- Unit-test worker lifecycle/backoff without starting OS capture by injecting a fake worker factory.
- Unit-test PCM conversion for IEEE float and PCM 16-bit buffers.
- Build/test/format and smoke-launch the WPF app.

## Open Risks

- Process-loopback requires Windows 10 build 20348 or later and may still fail for protected/system or stale processes.
- Multiple active process captures can increase CPU usage; later polish may need a cap or lazy detail-only capture mode.
