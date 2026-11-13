## ADDED Requirements

### Requirement: Exact integrated-loudness result reuse

The integrated gate SHALL reuse its exact result, including an unavailable result, while its completed block set is unchanged. A newly completed block or reset SHALL invalidate the cached result. This optimization SHALL NOT substitute a quantized or rolling approximation for the existing two-stage historical gate.

#### Scenario: Repeated reads between completed gate blocks
- **WHEN** integrated loudness is queried repeatedly without another completed gate block
- **THEN** the previous exact result is reused without rerunning the historical gating pass

#### Scenario: New block and reset
- **WHEN** another gate block completes or the gate is reset
- **THEN** the next query evaluates the changed block set or returns the reset unavailable state

### Requirement: Cadenced stereo display data and current numeric metrics

Stereo point-cloud construction SHALL avoid scanning the complete recent window on each PCM block and SHALL honor frame-based performance cadence. Numeric measurements SHALL remain current after PCM processing. Overall spectrum availability for existing collision and Signal Shuffle consumers SHALL be preserved regardless of graph visibility.

#### Scenario: Input within the display interval
- **WHEN** new PCM arrives before the point-cloud refresh interval elapses
- **THEN** the published point-cloud view can be reused while numeric stereo metrics reflect the current window

#### Scenario: Mode change and reset
- **WHEN** performance mode changes or the analyzer resets
- **THEN** point-cloud refresh eligibility is invalidated and reset does not republish stale points

### Requirement: Stable published display snapshots

Published stereo-point, waveform, and spectrum collections, including channel collections, SHALL remain unchanged when later PCM, display refreshes, mode changes, or reset occur. Current cache references SHALL be released on reset.

#### Scenario: Consumer retains an earlier view
- **WHEN** a consumer retains a published view through multiple subsequent eligible refreshes
- **THEN** its retained samples and count remain unchanged

#### Scenario: Consumer retains a view across reset
- **WHEN** reset clears the current analyzer display caches
- **THEN** the current display-data properties return unavailable values
- **AND** previously published views retain the samples originally delivered

### Requirement: Reproducible performance evidence

The existing benchmark executable SHALL support full-analyzer measurements without additional packages, report native availability and run parameters, and retain the true-peak benchmark. Final evidence SHALL distinguish elapsed time, throughput, cumulative allocations, and correctness tradeoffs. Timing SHALL NOT be a unit-test pass threshold.

#### Scenario: Matched baseline and final runs
- **WHEN** the same harness and native backend process the same signal with matched rate, channels, block size, mode, duration, warmup, and repeats
- **THEN** the report records actual baseline and final time and allocation results
- **AND** any allocation regression introduced by stable publication is explicit rather than hidden by superseded results

#### Scenario: Analyzer-only selection
- **WHEN** the analyzer-only benchmark is selected
- **THEN** native backend availability is printed even though the separate true-peak section is not run
