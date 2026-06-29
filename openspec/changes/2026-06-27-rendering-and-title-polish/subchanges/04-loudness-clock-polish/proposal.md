# 04-loudness-clock-polish

## Goal
Polish the loudness clock so it behaves like a stable clock face: plotted samples advance around a fixed table, wrap without rotating the old data, support a user-defined cycle duration, show denser loudness scale markers, highlight samples above an adjustable warning level, and render loudness as a filled fan rather than a thin line.

## In Scope
- Stable clock-phase mapping based on elapsed cycle position rather than list index.
- User-configurable cycle duration with default 30 seconds, minimum 10 seconds, and maximum 600 seconds.
- Adjustable warning loudness between configured minimum and maximum, persisted per source.
- At least five loudness scale markers in the clock control.
- Filled fan rendering with a distinct warning color above the warning loudness.
- Inline and pop-up detail controls and persistence for new clock settings.

## Out of Scope
- Direction switching UI.
- Multi-source clock comparison.
- Audio analyzer cadence changes.
- Theme editor changes beyond using existing signal/warm plot colors.

## Depends On
- `03-loudness-clock-graph`

## Done Means
- Mapper/planner tests prove stable phase wrapping and at least five scale markers.
- View-model tests prove cycle/warning persistence and clamping in inline and pop-up detail.
- XAML builds with new controls bound in both detail surfaces.
- Focused and full test suites pass.
