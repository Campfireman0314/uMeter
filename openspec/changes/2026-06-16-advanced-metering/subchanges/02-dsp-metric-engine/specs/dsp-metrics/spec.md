# DSP Metric Engine Spec

## Requirement: PCM Analyzer Input Contract
**Trace**: R1

The application shall provide a reusable PCM metric analyzer that accepts interleaved floating-point sample blocks for one or two channels.

### Scenario: Analyzer processes valid PCM blocks
Given an analyzer configured with a positive sample rate and one or two channels
When interleaved samples are processed
Then the analyzer updates a metrics snapshot without requiring any WPF or WASAPI dependency

### Scenario: Analyzer rejects invalid input
Given an analyzer configuration or sample block with invalid channel count, sample rate, oversampling factor, or frame alignment
When the analyzer is constructed or processes the block
Then it throws a clear argument exception

## Requirement: Loudness Metrics
**Trace**: R2

The analyzer shall compute K-weighted momentary, short-term, and integrated loudness values from PCM samples.

### Scenario: Time-window loudness is available
Given enough non-silent PCM has been processed
When the snapshot is read
Then momentary loudness uses a 400 ms window
And short-term loudness uses a 3 s window
And integrated loudness is accumulated from 400 ms windows with 100 ms hops

### Scenario: Integrated loudness uses gating
Given a stream includes loud sections and sections below the absolute or relative gate
When integrated loudness is computed
Then blocks below the absolute gate do not contribute
And blocks more than 10 LU below the ungated gated-average threshold do not contribute

## Requirement: True Peak
**Trace**: R3

The analyzer shall estimate true-peak level with selectable 2x, 4x, and 8x oversampling factors.

### Scenario: True peak follows oversampling option
Given the same PCM stream is analyzed with different oversampling factors
When snapshots are read
Then each analyzer returns a true-peak dBFS value
And higher oversampling factors evaluate more intermediate sample positions

## Requirement: RMS And Stereo Metrics
**Trace**: R4

The analyzer shall compute 100 ms RMS level, stereo image coordinates, and correlation from recent PCM samples.

### Scenario: Recent stereo metrics are available
Given enough stereo PCM has been processed
When the snapshot is read
Then RMS reflects the most recent 100 ms window
And correlation is reported in the range -1 to +1
And stereo image coordinates are clamped to the range -1 to +1

## Requirement: Resettable State
**Trace**: R5

The analyzer shall support resetting all accumulated history.

### Scenario: Analyzer is reset
Given an analyzer has processed non-silent PCM
When reset is requested
Then subsequent snapshots return unavailable metrics until enough new PCM is processed
And integrated, RMS, loudness, true-peak, stereo, and correlation histories are cleared
