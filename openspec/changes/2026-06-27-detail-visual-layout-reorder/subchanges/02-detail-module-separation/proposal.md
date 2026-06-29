# Detail Module Separation

## Goal

Separate loudness clock, waveform, spectrograph, and stereo/correlation into independent detail modules with individual visibility/settings affordances and size persistence.

## In Scope

- Give clock, waveform, and spectrograph individual toggles and settings buttons/popups in inline and pop-up detail views.
- Treat stereo image plus correlation as one shrinkable detail module.
- Persist and restore module visibility and user-adjusted heights through per-source workspace settings and layout preset JSON.
- Keep existing analysis settings and defaults compatible with older workspace files.

## Out of Scope

- Drag-and-drop reordering between modules; that belongs to the next subchange.
- New DSP metrics or new graph types.

## Depends On

- Existing detail preference persistence and JSON layout preset service.

## Done Means

- Each detail visualization can be independently shown and configured.
- Stereo/correlation can be vertically resized.
- Layout preset import/export includes module sizes and separated settings.
- Existing workspace files without module layout data still load with current defaults.
