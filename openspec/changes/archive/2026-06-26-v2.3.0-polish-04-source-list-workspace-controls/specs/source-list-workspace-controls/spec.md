# Source List Workspace Controls Spec

## Requirements

### R1: Inline Title Alias Editing
The global source row SHALL allow editing a source alias directly from the row title area. Editing SHALL persist to the existing per-source workspace alias and update the row title.

### R2: Hide Sources
Each source row SHALL expose a hide action. Hidden sources SHALL be omitted from the global source list by default without forgetting their saved preferences.

### R3: Show Hidden Toggle
The global source list SHALL expose a show-hidden toggle. When enabled, hidden sources SHALL appear again and be visually/actionably restorable.

### R4: User Source Ordering
The global source list SHALL allow users to move/reorder rows. The order SHALL persist by source key and apply across refreshes, alive sources, and retained hibernating rows.

### R5: New Sources Remain Predictable
Sources without explicit user order SHALL keep the existing kind/name/key sort after ordered sources.
