# Single Instance Launch Spec

## Requirements

### R1: Only One Primary Instance Per User Session
The desktop app SHALL allow only one primary uMeter process per interactive user session.

### R2: Secondary Launches Must Exit Before App Resources Start
A secondary launch SHALL exit before constructing UDP receivers, timers, tray icons, main windows, or settings writers.

### R3: Secondary Launches Must Request Activation
When a secondary launch detects an existing primary instance, it SHALL attempt to request activation of the primary instance before exiting.

### R4: Primary Must Restore Or Activate On Request
The primary instance SHALL restore/show/focus the main window when an activation request is received, including tray-hidden or minimized states.

### R5: Normal Secondary Launches Are Informational
Rejected secondary launches SHALL be logged as normal informational startup exits, not crashes.
