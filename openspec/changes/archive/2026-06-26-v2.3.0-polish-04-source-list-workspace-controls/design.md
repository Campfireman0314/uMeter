# Design

## U1: Source Row Preferences Extend Workspace

`SourceWorkspaceSettings` gains:

- `IsHidden`
- `OrderIndex`

`SourceRowPreferences` mirrors those fields. Existing alias and analysis preferences continue to use the same source-key storage.

## U2: View Model Filtering And Ordering

`MainViewModel.Sessions` remains the displayed collection. Hidden rows are excluded from the collection unless `ShowHiddenSources` is true. Hidden live sources keep their preferences and can reappear when the toggle is enabled.

Ordering uses nullable `OrderIndex`. Ordered rows sort first by explicit order; unordered rows fall back to the existing kind/display/key sort. Move commands assign contiguous order values to current visible rows and persist affected source preferences.

## U3: Row Commands And UI

Rows get commands for:

- start/cancel/commit alias editing;
- hide/restore;
- move up/down.

The global toolbar gains a `Show hidden` checkbox. The row title area swaps between text and a focused alias editor. The separate row alias editor is removed from the card so alias editing is merged into the title.

Drag-and-drop can call the same move/reorder helpers; command buttons remain the accessible baseline.
