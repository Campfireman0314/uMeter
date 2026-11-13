## ADDED Requirements

### Requirement: Rate-correct K-weighted loudness

The PCM analyzer SHALL design K-weighting for its configured input sample rate, preserve the established 48 kHz calibration, and retain the positive-sample-rate API. Calibration acceptance SHALL cover 44.1, 48, and 96 kHz without claiming certification or calibrated behavior for unusually low clamped rates.

#### Scenario: Non-48-kHz reference level
- **WHEN** a 44.1 kHz stereo analyzer processes a steady 2 kHz sine with peak amplitude 0.5 duplicated to both channels
- **THEN** its available momentary reading is within 0.1 LU of the independent reference literal -3.635606 LUFS

#### Scenario: Common-rate calibration and frequency response
- **WHEN** synthetic reference tones at 100 Hz, 1 kHz, 2 kHz, and 10 kHz are processed at 44.1, 48, and 96 kHz
- **THEN** the available momentary readings agree with independently precomputed reference literals within the documented 0.1 LU tolerance
- **AND** a stereo 1 kHz reference tone at -23 dBFS reads within 0.1 LU of -23 LUFS

### Requirement: Streaming measurement continuity

The analyzer SHALL preserve numeric measurement semantics across PCM block partitioning and reset, including startup windows, silence, and independent Overall/Mid/Side content. Sample-rate-specific filter state SHALL reset with the rest of the analyzer.

#### Scenario: Partitioned PCM input
- **WHEN** the same non-48-kHz signal is processed in one block or in smaller aligned blocks
- **THEN** the final available loudness readings agree within the documented numeric tolerance

#### Scenario: Reset and restart
- **WHEN** the analyzer is reset after a valid loudness reading
- **THEN** momentary and integrated readings become unavailable
- **AND** reprocessing the same signal restores the rate-correct reading

### Requirement: Independent true-peak trust fixture and fallback

True-peak tests SHALL include a closed-form known-signal expectation in addition to backend agreement. Optional native acceleration SHALL retain the managed fallback without changing snapshot field semantics.

#### Scenario: Known intersample peak
- **WHEN** a phase-pi/4 sine at one quarter of the sample rate has continuous peak amplitude 0.9
- **THEN** the expected continuous peak is 20 log10(0.9), approximately -0.9151 dBFS, rather than its lower discrete sample peak
- **AND** the 8x estimator agrees within the documented 0.04 dB fixture tolerance at 44.1, 48, and 96 kHz

#### Scenario: Native library unavailable
- **WHEN** the native DSP library cannot be loaded
- **THEN** PCM analysis continues through the managed implementation and the numerical analyzer regression fixtures remain applicable
