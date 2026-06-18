# Tasks

## Task 1: Merge process analyzer metrics into session snapshots

**Requirement Trace:** [R1] -> [U1]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/Audio/IProcessMetricProvider.cs` - process metric provider interface
- `src/DesktopLoudnessMeter.App/Audio/WasapiSessionMeterService.cs` - metric merge integration
- `tests/DesktopLoudnessMeter.Tests/Audio/WasapiSessionMeterServiceTests.cs` - merge tests

**Acceptance Criteria:**
- [x] Sessions with process IDs receive available analyzer metrics from the process provider.
- [x] Sessions without process IDs keep sample-derived metrics unavailable.
- [x] The provider receives the visible process ID set each refresh.

**Dependencies:** None

## Task 2: Add process-loopback worker lifecycle and PCM conversion

**Requirement Trace:** [R2, R3] -> [U2]  
**Execution Mode:** [test-first]

**Files:**
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackMetricProvider.cs` - provider lifecycle/backoff
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackCaptureWorker.cs` - Windows capture worker
- `src/DesktopLoudnessMeter.App/Audio/ProcessLoopbackInterop.cs` - process-loopback activation interop
- `src/DesktopLoudnessMeter.App/Audio/WaveFormatSampleConverter.cs` - PCM to float conversion
- `tests/DesktopLoudnessMeter.Tests/Audio/ProcessLoopbackMetricProviderTests.cs` - lifecycle/backoff tests
- `tests/DesktopLoudnessMeter.Tests/Audio/WaveFormatSampleConverterTests.cs` - conversion tests

**Acceptance Criteria:**
- [x] The provider starts workers for new visible process IDs and stops stale workers.
- [x] Failed process-loopback starts are retried only after a backoff interval.
- [x] IEEE float and PCM 16-bit buffers convert to interleaved float samples for the analyzer.
- [x] Unsupported OS/API failures do not escape the session refresh loop.

**Dependencies:** Task 1

## Task 3: Increase default detail viewport height

**Requirement Trace:** [R4] -> [U3]  
**Execution Mode:** [direct]

**Files:**
- `src/DesktopLoudnessMeter.App/MainWindow.xaml` - default/minimum window sizing

**Acceptance Criteria:**
- [x] Main window default height is taller than the previous 620 px.
- [x] Detail view still scrolls for smaller resized windows.

**Dependencies:** None
