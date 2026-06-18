# Process Loopback Integration Spec

## Requirement: Session Snapshots Include Process Metrics
**Trace**: R1

The session meter service shall merge process PCM analyzer metrics into matching application/session snapshots.

### Scenario: Analyzer metrics are available for a process
Given a session has a process ID
And process-loopback metrics are available for that process ID
When the session snapshot is returned
Then the snapshot includes momentary loudness, short-term loudness, integrated loudness, true peak, RMS, stereo image, and correlation values

### Scenario: Analyzer metrics are unavailable
Given a session has no process ID or process-loopback metrics are unavailable
When the session snapshot is returned
Then peak-derived values continue to display
And sample-derived metrics remain unavailable

## Requirement: Process Loopback Provider Lifecycle
**Trace**: R2

The application shall manage process-loopback capture workers for currently visible process-backed audio sessions.

### Scenario: Process-backed sessions are visible
Given one or more sessions have process IDs
When the session service refreshes
Then the provider is asked to track those process IDs
And stale process capture workers are stopped when their process IDs are no longer visible

## Requirement: Windows Process Loopback Capture
**Trace**: R3

The process metric provider shall attempt Windows process-loopback capture on supported systems and fail gracefully otherwise.

### Scenario: Process loopback is supported
Given Windows process-loopback activation succeeds
When PCM packets are captured for the process
Then packets are converted to floating-point samples and fed into the PCM metric analyzer

### Scenario: Process loopback is unsupported or fails
Given the OS or activation path does not support process-loopback capture
When the provider is asked for metrics
Then it returns unavailable metrics without crashing the UI refresh loop
And retries are backed off to avoid repeatedly starting failed workers

## Requirement: Detail View Room
**Trace**: R4

The default floating window size shall provide enough vertical space for the detail view.

### Scenario: App launches
Given the main window opens at its default size
When the user switches to detail view
Then the detail content has a taller default viewport than the previous compact height
