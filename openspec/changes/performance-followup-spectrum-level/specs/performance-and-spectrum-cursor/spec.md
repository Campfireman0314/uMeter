## ADDED Requirements

### Requirement: Lower allocation cost without fidelity loss

The analyzer SHALL reduce unnecessary window/display allocation relative to baseline 35f713f without reducing numeric precision, graph refresh cadence, resolution, or immutable published-view lifetime. Scalar sliding windows SHALL not require capacity doubling solely to evict one old sample at full capacity.

#### Scenario: Appending to a full scalar window
- **WHEN** a new power sample enters a full preallocated window
- **THEN** the oldest value is evicted without growing the queue
- **AND** measurement results preserve the existing chronological arithmetic behavior

#### Scenario: Retaining published graph data
- **WHEN** more PCM arrives, mode changes, or reset occurs
- **THEN** previously published waveform, spectrum, channel, and stereo views remain unchanged

### Requirement: Exact integrated-loudness scaling

Integrated-loudness evaluation SHALL avoid complete historical scans on each new completed block through an exact-value aggregate index or equivalent exact method. Absolute/relative gates, strict threshold membership, duplicates, reset and unavailable results SHALL match a separate two-pass reference within 1e-10 LU. No level quantization or history truncation is permitted.

#### Scenario: Long varied block history
- **WHEN** many completed blocks include changing loudness, repeated powers, quiet sections and threshold-equality cases
- **THEN** each requested result agrees with the separate reference within the stated numeric budget
- **AND** repeated queries without a completed block reuse the cached result

#### Scenario: Reset after accumulated history
- **WHEN** integrated measurement resets
- **THEN** prior values do not contribute to subsequent queries

### Requirement: Representative matched performance evidence

The benchmark SHALL support optional channel graphs and multiple analyzer sources without new packages. Evidence SHALL report matched baseline/final parameters, backend, repeats, time, cumulative allocations, numerical output and memory tradeoffs. An isolated long-history gate benchmark MAY supplement full-analyzer measurements. No wall-clock threshold is a unit-test oracle.

#### Scenario: Channel-enabled multi-source run
- **WHEN** three analyzers process the same specified workload with channel waveform and spectrum enabled
- **THEN** the harness identifies those settings and reports aggregate throughput and allocations with representative per-source numeric results

### Requirement: Historical spectrum cursor level

The shared spectrograph cursor label SHALL include frequency and the FFT-derived level in dBFS from the nearest retained source frequency bucket at the hovered historical time column. It SHALL NOT derive the number from colors, a rendered-row maximum, or an unrelated newest spectrum.

#### Scenario: Partial right-aligned history
- **WHEN** the cursor is above the filled right-hand portion of a partially filled history
- **THEN** its level is taken from the corresponding retained column
- **AND** hovering unfilled history does not show a fabricated level

#### Scenario: Wrapped history and zoomed frequency
- **WHEN** circular history has wrapped and the frequency viewport is panned or zoomed
- **THEN** the cursor maps to the historical column and source bucket actually represented by its position, including the snapped drawing seam

#### Scenario: Different buckets share a rendered row
- **WHEN** multiple source buckets are combined into one rendering row
- **THEN** the cursor reads the nearest source bucket rather than automatically reporting that row's strongest bucket

### Requirement: Honest spectrum readout limits

The readout SHALL use source numeric levels before display color transforms and format dBFS to one decimal. It SHALL distinguish unavailable history, at/below -200 dBFS floor, and at/above 0 dBFS ceiling from unconstrained finite measurements. The readout is a bucket-level estimate, not an arbitrary-frequency precision or compliance claim.

#### Scenario: Color settings change
- **WHEN** theme, display floor, or display ceiling changes without changing retained samples
- **THEN** the numeric frequency/level selection remains unchanged

#### Scenario: Clamped or invalid level
- **WHEN** a retained normalized value is zero, one, or invalid
- **THEN** the label indicates the corresponding floor, ceiling, or unavailable result rather than inventing an exact unclamped measurement

### Requirement: Shared lightweight cursor behavior

The cursor change SHALL apply through the shared spectrum control to inline, popup and per-channel views without audio processing on pointer movement. Existing pan/zoom, hidden-control gates, rollover rendering and reset behavior SHALL be preserved.

#### Scenario: History reset or source-size change
- **WHEN** spectrum history is reset or rebuilt for a changed source-bucket count
- **THEN** old levels are not presented as current history
