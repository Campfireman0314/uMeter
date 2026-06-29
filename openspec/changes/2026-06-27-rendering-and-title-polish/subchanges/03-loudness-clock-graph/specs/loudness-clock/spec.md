# Loudness Clock Specification

## Requirements

### Requirement 1: Clock samples advance from momentary loudness frames
**Trace**: R1

Given a source has a finite momentary loudness value and receives a fresh plot frame, then the source exposes a clock plot tick that advances for the loudness clock visualization.

### Requirement 2: Below-minimum loudness is omitted
**Trace**: R2

Given a loudness sample is below the configured clock minimum, when the clock graph maps the sample, then the sample is not plotted.

### Requirement 3: Minimum loudness maps to the inner radius
**Trace**: R3

Given a loudness sample equals the configured clock minimum, when the clock graph maps the sample, then the sample maps to the graph's inner radius.

### Requirement 4: Maximum loudness maps to the outer radius
**Trace**: R4

Given a loudness sample equals or exceeds the configured clock maximum, when the clock graph maps the sample, then the sample maps to the graph's outer radius.

### Requirement 5: Intermediate loudness maps proportionally
**Trace**: R5

Given a loudness sample falls between the configured minimum and maximum, when the clock graph maps the sample, then its radius is proportional within the inner-to-outer radius span.

### Requirement 6: Inline detail exposes clock visibility and range controls
**Trace**: R6

Given inline detail is open for a source, when the user opens detail visualization controls, then the user can show or hide the loudness clock graph and edit its minimum and maximum loudness.

### Requirement 7: Pop-up detail exposes clock visibility and range controls
**Trace**: R7

Given a detail pop-up is open for a source, when the user opens detail visualization controls, then the user can show or hide the loudness clock graph and edit its minimum and maximum loudness.

### Requirement 8: Clock settings persist per source
**Trace**: R8

Given the user changes clock visibility or range settings for a source, when the source detail view is reopened or the app reloads workspace settings, then those clock settings are restored for the same source.

### Requirement 9: Invalid clock ranges are clamped to a usable span
**Trace**: R9

Given the user changes the clock minimum or maximum, when the value would collapse or invert the range, then the view model clamps the range so maximum remains above minimum by at least the documented minimum span.
