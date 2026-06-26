# Design

## Overview

Reuse the existing row-level alias-editing model in the detail surfaces instead of adding another naming form. The global source row already exposes the desired interaction: double-click the title, edit `AliasEditText`, Enter/focus-lost commits, and Escape cancels. Inline and pop-up detail titles will bind to the same `AudioSessionMeterViewModel` state and commands, so alias persistence and duplicate-name disambiguation stay in one place.

## Implementation Units

### U1: Inline detail title editor
Module-boundary:
- `src/DesktopLoudnessMeter.App/MainWindow.xaml`
- `src/DesktopLoudnessMeter.App/MainWindow.xaml.cs`

Replace the inline detail display-only title with the same title/TextBox visibility toggle used by source rows. Set the title editor area's data context to `SelectedDetailSession` so existing title-edit handlers can execute the selected row's alias commands. Remove the separate alias row and relocate the existing forget-source command into the inline detail toolbar.

Traces: R1, R2, R3, R7, R8, R9, R10, R11

### U2: Pop-up detail title editor
Module-boundary:
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml`
- `src/DesktopLoudnessMeter.App/DetailWindow.xaml.cs`

Replace the pop-up content title with a title/TextBox visibility toggle bound to `Session`. Add the same double-click, key, focus, and focus-select handlers used by source rows. Keep the pop-up chrome header display-only so it can continue to serve as the drag handle without conflicting with title editing.

Traces: R4, R5, R6, R7, R9, R10, R11

## Verification

- Build validates WPF XAML bindings and event-handler wiring.
- Existing view-model tests validate alias commit/cancel, persistence, raw source identity stability, and collision display-title behavior.
- Manual UI validation remains pending for double-click focus and absence of overlapping controls in the inline and pop-up title areas.
