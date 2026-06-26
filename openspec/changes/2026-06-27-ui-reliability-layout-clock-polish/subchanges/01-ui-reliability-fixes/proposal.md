# 01-ui-reliability-fixes

## Goal
Fix small interaction reliability defects that make existing UI controls feel stale or unresponsive.

## In Scope
- Ensure slider visuals update immediately when the app theme changes, without requiring mouse hover.
- Audit tray icon context-menu creation/show/exit paths and fix likely causes of intermittent right-click non-response.
- Add focused tests where behavior can be validated without fragile UI automation.

## Out of Scope
- Redesigning app themes.
- Adding new tray menu commands.
- Reworking single-instance startup beyond tray-menu reliability needs.

## Depends On
- Existing theme resources and tray lifecycle code.

## Done Means
- Theme changes repaint slider colors immediately.
- Tray right-click menu remains available across hide/show, activation, and exit paths.
- Relevant tests/build pass.
