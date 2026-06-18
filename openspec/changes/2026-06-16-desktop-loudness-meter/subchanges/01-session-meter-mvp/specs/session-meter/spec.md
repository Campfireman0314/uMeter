# Session Meter MVP Spec

## Requirement: Floating Meter Window
**Trace**: R1

The application shall start directly into a compact floating desktop meter window.

### Scenario: Window starts above normal desktop apps
Given the application is launched
When the main window is shown
Then the window is borderless, compact, draggable, and topmost
And the first screen is the meter, not a landing or setup page

## Requirement: Default Render Session Discovery
**Trace**: R2

The application shall discover audio sessions from the current default render endpoint.

### Scenario: Active app sessions are listed
Given one or more desktop applications are producing render audio sessions
When the meter refreshes
Then each discoverable session is represented as a row with a stable session key
And each row exposes the process id when Windows provides one

## Requirement: Friendly Session Labels
**Trace**: R3

The application shall show a readable app/session label for each discovered row.

### Scenario: Process-backed session has a friendly name
Given a session has a valid process id
When the session row is rendered
Then the row label uses the process main module description or process name when available
And the row falls back to WASAPI display/session identifiers when process metadata is unavailable

## Requirement: Live Peak-Derived Metering
**Trace**: R4

The application shall continuously update a live level meter for each discovered session using WASAPI session peak information.

### Scenario: Session peak changes update the row
Given a session exposes audio meter information
When the session peak changes
Then the row level updates within the configured refresh cadence
And the row exposes both a normalized level and dBFS-derived text

## Requirement: Session Lifecycle Robustness
**Trace**: R5

The application shall tolerate session churn and Core Audio access failures without crashing the UI.

### Scenario: Session disappears during refresh
Given a session ends or becomes inaccessible
When the next refresh occurs
Then the app removes or marks the row stale without terminating
And the next successful refresh can add new sessions

## Requirement: Metric Honesty
**Trace**: R6

The MVP shall clearly distinguish its peak-derived session level from true loudness analysis.

### Scenario: User sees the MVP limitation
Given the main window is visible
When the user reads the meter header or status text
Then the app labels the metric as session peak/level
And the repository design notes explain that LUFS/process-loopback analysis belongs to a later slice

