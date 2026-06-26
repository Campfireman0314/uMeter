# Title Editors Specification

## Requirements

### Requirement 1: Inline detail title starts editing from the title area
**Trace**: R1

Given an inline detail view is open for a selected source, when the user double-clicks the displayed source title, then the title area enters alias-edit mode for the same selected source item and focuses the title editor.

### Requirement 2: Inline detail title commits through row alias command
**Trace**: R2

Given the inline detail title editor is active, when the user presses Enter or moves focus away, then the editor commits `AliasEditText` through the selected source item's existing alias commit command.

### Requirement 3: Inline detail title cancels through row alias command
**Trace**: R3

Given the inline detail title editor is active, when the user presses Escape, then the editor cancels through the selected source item's existing alias cancel command.

### Requirement 4: Popup detail title starts editing from the title area
**Trace**: R4

Given a detail pop-up is open for a source, when the user double-clicks the displayed source title in the pop-up content header, then the title area enters alias-edit mode for the pop-up session and focuses the title editor.

### Requirement 5: Popup detail title commits through session alias command
**Trace**: R5

Given the pop-up title editor is active, when the user presses Enter or moves focus away, then the editor commits through the same session alias commit command used by source items.

### Requirement 6: Popup detail title cancels through session alias command
**Trace**: R6

Given the pop-up title editor is active, when the user presses Escape, then the editor cancels through the same session alias cancel command used by source items.

### Requirement 7: Detail surfaces no longer show duplicate alias/naming bars
**Trace**: R7

Given either the inline detail panel or a detail pop-up is visible, then no separate "Alias" label plus naming text box is displayed on that detail surface.

### Requirement 8: Inline detail forget command remains available
**Trace**: R8

Given the inline detail panel removes the alias bar, then the existing forget-source command remains available from the inline detail controls.

### Requirement 9: Alias commits keep existing persistence and identity behavior
**Trace**: R9

Given an alias is committed from a source item, inline detail title, or pop-up detail title, then the source display title updates from the same row-level alias state and persistence path.

### Requirement 10: Alias cancellation preserves prior alias and raw identity
**Trace**: R10

Given an alias edit is canceled from any title editor, then the prior alias and raw source identity remain unchanged.

### Requirement 11: Display-name collision disambiguation remains unchanged
**Trace**: R11

Given a display-name collision exists, then the existing disambiguated display title behavior remains unchanged after the editor redesign.
