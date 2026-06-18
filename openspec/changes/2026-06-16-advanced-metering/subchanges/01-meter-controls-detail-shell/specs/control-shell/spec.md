# Meter Controls And Detail Shell Spec

## Requirement: Default Metric Mode Control
**Trace**: R1

The application shall let the user choose whether the primary row value displays Peak or LUFS.

### Scenario: User selects Peak or LUFS
Given the main window is visible
When the user changes the metric mode control
Then each session row updates its primary value text according to the selected mode
And LUFS mode shows a clear unavailable state until sample-derived loudness data exists

## Requirement: True-Peak Display Options
**Trace**: R2

The application shall expose a true-peak visibility toggle and an oversampling selector with 2x, 4x, and 8x options.

### Scenario: User changes true-peak options
Given the main window is visible
When the user enables true peak and selects an oversampling rate
Then the selected rate is reflected in the UI state
And future detail views can read that preference from the main view model

## Requirement: Inline Per-Session Detail Surface
**Trace**: R3

Each application/session row shall provide a detail action that switches the main window into an inline detail surface for that session.

### Scenario: User opens and returns from session detail
Given a session row is visible
When the user activates its detail action
Then the main window displays details for that session in the same window
And a return button is available in the top-left corner
When the user activates the return button
Then the main window returns to the global session list

## Requirement: Detail Meter Layout
**Trace**: R7

The inline detail surface shall display level metrics as dB-scaled horizontal meters with scale markings and numeric values to their right, followed by stereo image and correlation displays.

### Scenario: User reads detail metrics
Given the detail surface is visible
When sample-derived metrics are unavailable
Then Momentary loudness, Short-term loudness, Integrated loudness, True-peak level, and 100 ms RMS level show marked horizontal meters with unavailable numeric values
And the stereo image graph appears as a semi-circle fan with glowing sample dots below the level meters
And the correlation meter appears at the bottom of the stereo image area as a floating arrow over a -1 to +1 bar

## Requirement: Always-On-Top Toggle
**Trace**: R4

The application shall let the user enable or disable the main window's topmost behavior.

### Scenario: User changes topmost behavior
Given the main window is visible
When the user toggles always-on-top
Then the main window Topmost property follows the selected value
And the control clearly reflects the current value

## Requirement: Reset Commands
**Trace**: R5

The application shall provide global and per-session reset commands.

### Scenario: User resets all or one session
Given the app has active session rows
When the user activates global reset
Then all session display/analyzer reset counters advance
When the user activates a detail reset
Then only that session's reset counter advances

## Requirement: Analyzer-Ready Metric State
**Trace**: R6

The view model shall carry optional analyzer metrics without requiring the analyzer implementation in this slice.

### Scenario: Metrics are unavailable
Given no PCM analyzer is connected
When details are displayed
Then sample-derived metrics show an unavailable placeholder
And peak-derived values continue to display normally

## Requirement: Smoother Meter Motion
**Trace**: R8

The peak-derived display shall use a finer UI refresh cadence and smoothing so level meters move more fluently than the original 100 ms update.

### Scenario: Peak values change quickly
Given session peak values update over time
When the UI refresh loop runs
Then the default refresh label is below 100 ms
And displayed levels move toward new values without abrupt full jumps
