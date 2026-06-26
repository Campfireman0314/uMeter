# 02-unified-source-title-editors

## Goal
Make inline detail and pop-up detail source-title editing match the global source item experience, replacing separate naming bars with the same title-area edit interaction.

## In Scope
- Rework inline detail title presentation so alias editing happens from the title area, consistent with source rows.
- Rework pop-up detail title presentation the same way.
- Remove or hide the separate naming bars where they duplicate title editing.
- Preserve existing alias persistence, cancel/commit behavior, duplicate-name disambiguation, and detail-window title behavior.
- Add focused view-model tests where behavior can be tested without visual automation.

## Out of Scope
- Changing source identity or persistence keys.
- Changing global source row hide/show/reorder controls.
- Full visual redesign outside the naming/title area.
- Installer, version, or release metadata changes.

## Depends On
- None. Can be implemented after or in parallel with the rendering slice.

## Done Means
- Global rows, inline detail, and pop-up detail use consistent source-title editing behavior.
- Separate naming bars are removed from inline and pop-up detail surfaces.
- Alias commit/cancel behavior still persists through the existing workspace settings.
- Focused tests and the managed test suite pass.
