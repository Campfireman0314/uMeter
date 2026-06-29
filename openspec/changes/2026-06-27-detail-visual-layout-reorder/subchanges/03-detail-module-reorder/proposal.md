# Detail Module Reorder

## Goal

Let users drag and reorder the stereo/correlation, collision analyzer, loudness clock, waveform, and spectrograph detail modules like global source rows.

## In Scope

- Add drag handles and drop hints for detail modules in inline and pop-up detail views.
- Bring collision into the same draggable module layout and preserve its source picker/settings.
- Keep each module header visible with an expand/shrink toggle and header-local settings gear.
- Polish existing global source-row drag feedback so top insertion and the drag cursor behave predictably.
- Persist module order per source and include order in layout preset JSON.
- Keep drag/drop robust when modules are hidden, a selected source disappears, or a preset contains duplicate/unknown module IDs.
- Add focused view-model tests for order normalization, persistence, and preset round-trip behavior.

## Out of Scope

- Cross-source drag/drop.
- Replacing the existing source-row persistence model.

## Depends On

- Detail module separation and module layout model from subchange 02.

## Done Means

- Visible detail modules, including collision, can be reordered in both inline and pop-up detail surfaces.
- Hidden modules retain order for later display.
- Stereo/correlation can be collapsed without losing its saved height or settings.
- Presets and workspace settings preserve module order.
- Managed tests/build pass.
