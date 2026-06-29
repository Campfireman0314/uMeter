# Loudness Clock Polish Specification

## Requirements

### Requirement 1: Clock angle uses stable cycle phase
**Trace**: R1

Given the loudness clock reaches the configured cycle duration, when new samples continue to arrive, then their angles wrap to the start of the fixed clock face without rotating previously plotted table positions.

### Requirement 2: Cycle duration is configurable and clamped
**Trace**: R2

Given the user edits loudness clock cycle duration, when the value is saved or restored, then the duration defaults to 30 seconds and is clamped between 10 seconds and 600 seconds.

### Requirement 3: Loudness scale has at least five markers
**Trace**: R3

Given the clock renders its loudness scale, then it provides at least five marker values between the configured minimum and maximum loudness.

### Requirement 4: Warning loudness is configurable and clamped
**Trace**: R4

Given the user edits the warning loudness, when the value would fall outside the configured clock loudness range, then the view model clamps it between the current minimum and maximum loudness.

### Requirement 5: Above-warning samples use a warning plot color
**Trace**: R5

Given a plotted loudness sample is greater than or equal to the configured warning loudness, when the clock draws the sample fan, then that sample is rendered in a distinct warning color.

### Requirement 6: Clock renders loudness as a filled fan
**Trace**: R6

Given plotted samples are at or above the configured minimum loudness, when the clock renders them, then it fills the radial area between the inner radius and the sample radius instead of only drawing a line.

### Requirement 7: New clock settings persist per source
**Trace**: R7

Given the user changes cycle duration or warning loudness for a source, when the inline or pop-up detail view is reopened, then the settings are restored for the same source.
